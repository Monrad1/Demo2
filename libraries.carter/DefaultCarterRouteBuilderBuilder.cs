using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Carter;
using Carter.ModelBinding;
using Carter.Request;
using Carter.Response;
using Demo.Carter.Attributes;
using FluentValidation;
using FluentValidation.Results;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.CSharp.RuntimeBinder;

namespace Demo.Carter
{
    internal class DefaultCarterRouteBuilderBuilder : ICarterRouteBuilderBuilder
    {
        public ICarterRouteBuilder Build(Type type)
        {
            var routes = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .SelectMany(ExtractRouteInfo)
                             .Where(m => m.Method != null)
                             .Select(WrapAsCarterHandler);

            return new CarterRouteBuilder(routes);
        }

        private static CarterRouteInfo WrapAsCarterHandler(RouteInfo routeInfo)
        {
            var handler = CreateHandler(routeInfo.Handler);
            return new CarterRouteInfo(routeInfo.Method,
                                       routeInfo.Path,
                                       async (nmb, ctx) =>
                                       {
                                           var response = await handler(nmb, ctx);
                                           return response.Negotiate(ctx.Response, default);
                                       });
        }

        private static Func<CarterModuleBase, HttpContext, Task<IResponse>> CreateHandler(MethodInfo handler)
        {
            var parameterMappers = handler.GetParameters()
                                                   .Select(CreateParameterMapper);
            var returnValueMapper = GetAsyncReturnValueMapper(handler.ReturnType);

            return (self, r) => CallHandlerAsync(handler, parameterMappers, returnValueMapper, self, r);
        }

        private static async Task<IResponse> CallHandlerAsync(MethodInfo handler, 
                                                             IEnumerable<ParameterMapper> parameterMappers,
                                                             ReturnValueMapper returnValueMapper, 
                                                             CarterModuleBase self, 
                                                             HttpContext ctx)
        {
            try
            {
                var mappedParameters = parameterMappers.Select(m => m(self, ctx)).ToArray();
                
                var actualReturnValue = handler.Invoke(self, mappedParameters);
                var mappedReturnValue = await returnValueMapper(actualReturnValue);
                return mappedReturnValue;
            }
            catch (ValidationException e)
            {
                if(!e.Errors?.Any() ?? true)
                    return ResponseBuilder.BadRequest.WithErrorBody(e.Message).ToResponse();
                return ResponseBuilder.BadRequest.WithErrorBody(e.Errors).ToResponse();
            }
        }

        private static ReturnValueMapper GetAsyncReturnValueMapper(Type returnType)
        {
            if (returnType == typeof(Task<IResponse>))
                return o => (Task<IResponse>) o;

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResultType = returnType.GenericTypeArguments[0];
                var untypedTask = GetUntypedTask(taskResultType);
                var innerReturnValueMapper = GetReturnValueMapper(taskResultType);

                return async o =>
                {
                    var taskResult = await untypedTask.GetReultAsync((Task) o).ConfigureAwait(false);
                    var mappedTaskResult = innerReturnValueMapper(taskResult);
                    return mappedTaskResult;
                };
            }

            var actualReturnValueMapper = GetReturnValueMapper(returnType);
            return o => Task.FromResult(actualReturnValueMapper(o));
        }

        private static Func<object, IResponse> GetReturnValueMapper(Type resultType)
        {
            if(typeof(Task).IsAssignableFrom(resultType))
                throw new ArgumentException($"Cannot map {nameof(Task)} type. Use {nameof(GetAsyncReturnValueMapper)} instead.", nameof(resultType));

            if (typeof(IResponse).IsAssignableFrom(resultType))
                return o => (IResponse) o;

            if (typeof(IResponseBuilder).IsAssignableFrom(resultType))
                return o => ((IResponseBuilder) o).ToResponse();

            if (typeof(ValidationResult).IsAssignableFrom(resultType))
                return o =>
                        {
                            var r = (ValidationResult)o;
                            if (r.IsValid)
                                return ResponseBuilder.NoContent.ToResponse();

                            return ResponseBuilder.BadRequest.WithErrorBody(r).ToResponse();
                        };

            if (resultType == typeof(HttpStatusCode))
                return o => ResponseBuilder.WithStatusCode((HttpStatusCode) o).ToResponse();

            return o =>
            {
                if (o == null)
                    return null;

                if (o is IResponse response)
                    return response;

                if (o is IResponseBuilder responseBuilder)
                    return responseBuilder.ToResponse();

                return ResponseBuilder.Ok.ToResponse();
            };
        }

        private static IUntypedTask GetUntypedTask([NotNull] Type resultType)
        {
            if (resultType == null)
                throw new ArgumentNullException(nameof(resultType));

            var taskResultType = resultType;
            var untypedTaskType = typeof(UntypedTask<>).MakeGenericType(taskResultType);
            return (IUntypedTask) Activator.CreateInstance(untypedTaskType);
        }

        private static ParameterMapper CreateParameterMapper(ParameterInfo parameterInfo)
        {
            if (IsSingleValueParameter(parameterInfo.ParameterType))
                return CreateDynamicParameterMapper(parameterInfo);

            return CreateComplexParameterMapper(parameterInfo);
        }

        private static ParameterMapper CreateComplexParameterMapper(ParameterInfo parameterInfo)
        {
            var nonGenericBinderType = parameterInfo.GetCustomAttribute<DoNotValidateAttribute>() != null
                                            ? typeof(TypedModuleBinder<>)
                                            : typeof(ValidatingTypedBinder<>);

            var binderType = nonGenericBinderType.MakeGenericType(parameterInfo.ParameterType);
            var binder = (IBinder) Activator.CreateInstance(binderType);

            return (self, ctx) => binder.Bind(ctx);
        }

        private static ParameterMapper CreateDynamicParameterMapper(ParameterInfo parameterInfo)
        {
            Func<CarterModule, HttpContext, object> getPropertyInclQueryString = (self, r) =>
                                                                                    {
                                                                                        var v = r.GetRouteValue(parameterInfo.Name);

                                                                                        if(v == null && r.Request.Query.ContainsKey(parameterInfo.Name))
                                                                                            v = r.Request.Query.As<string>(parameterInfo.Name);

                                                                                        return v;
                                                                                    };

            if (parameterInfo.HasDefaultValue)
            {
                var preDefaultValueGetPropertyInclQueryString = getPropertyInclQueryString;
                getPropertyInclQueryString = (self, r) =>
                                                    {
                                                        var v = preDefaultValueGetPropertyInclQueryString(self, r);

                                                        return v;
                                                    };
            }

            Func<object, object> typeConverter;
            if (parameterInfo.ParameterType.IsEnum)
                typeConverter = o => Enum.Parse(parameterInfo.ParameterType, Convert.ToString(o));
            else
                typeConverter = o => Convert.ChangeType(o, parameterInfo.ParameterType);

            return (self, r) =>
                            {
                                var propertyValue = getPropertyInclQueryString(self, r);
                                var convertedPropertyValue = typeConverter(propertyValue);
                                return convertedPropertyValue;
                            };
        }

        private static bool IsSingleValueParameter(Type parameterType)
        {
            return parameterType == typeof(string) || parameterType.IsValueType;
        }

        private delegate Task<IResponse> ReturnValueMapper(object actualReturnValue);

        private static IEnumerable<RouteInfo> ExtractRouteInfo(MethodInfo methodInfo)
        {
            return methodInfo.GetCustomAttributes(true)
                             .OfType<IHttpMethodAttribute>()
                             .Select(methodAttribute => new RouteInfo(methodAttribute.Metohd,
                                                                      methodAttribute.Path,
                                                                      methodInfo));
        }

        private delegate object ParameterMapper(CarterModuleBase self, HttpContext ctx);

        private class RouteInfo
        {
            public MethodInfo Handler { get; }
            public string Method { get; }
            public string Path { get; }

            public RouteInfo(string method, string path, MethodInfo handler)
            {
                Method = method;
                Path = path;
                Handler = handler;
            }
        }

        private class CarterRouteBuilder : ICarterRouteBuilder
        {
            private readonly IEnumerable<CarterRouteInfo> _routes;

            public CarterRouteBuilder([NotNull] IEnumerable<CarterRouteInfo> routes)
            {
                _routes = routes ?? throw new ArgumentNullException(nameof(routes));
            }

            public void Build(CarterModuleBase parentModule)
            {
                foreach (var route in _routes)
                    parentModule.Routes.Add((route.Method, route.Path), r => route.Handler(parentModule, r));
            }
        }

        private delegate Task<Task> RouteHandler(CarterModuleBase self, HttpContext ctx);

        private class CarterRouteInfo
        {
            public RouteHandler Handler { get; }
            public string Method { get; }
            public string  Path { get; }

            public CarterRouteInfo(string method, string path, RouteHandler handler)
            {
                Method = method;
                Path = path;
                Handler = handler;
            }
        }

        private interface IBinder
        {
            object Bind(HttpContext ctx);
        }

        private class TypedModuleBinder<T> : IBinder
        {
            public object Bind(HttpContext ctx)
            {
                return ctx.Request.Bind<T>();
            }
        }

        private class ValidatingTypedBinder<T> : IBinder
        {
            public object Bind(HttpContext ctx)
            {
                var result = ctx.Request.BindAndValidate<T>();

                if (result.ValidationResult.Errors.Any())
                    throw new ValidationException(result.ValidationResult.Errors);

                return result.Data;
            }
        }
    }
}
