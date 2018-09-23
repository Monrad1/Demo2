using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Carter.ModelBinding;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Demo.Carter
{
    public static class CarterBaseExtensions
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();
        
        /// <summary>
        /// Bind the incoming request body to a model
        /// </summary>
        /// <param name="request">Current <see cref="HttpRequest"/></param>
        /// <typeparam name="T">Model type</typeparam>
        /// <returns>Bound model</returns>
        public static T Bind<T>(this HttpRequest request)
        {
            if (request.HasFormContentType)
            {
                var res = JObject.FromObject(request.Form.ToDictionary(key => key.Key, val =>
                {
                    var type = typeof(T);
                    var propertyType = type.GetProperty(val.Key).PropertyType;

                    if (propertyType.IsArray() || propertyType.IsCollection() || propertyType.IsEnumerable())
                    {
                        var colType = propertyType.GetElementType();
                        if (colType == null)
                        {
                            colType = propertyType.GetGenericArguments().FirstOrDefault();
                        }
                        return val.Value.Select(y => Convert.ChangeType(y, colType));
                    }
                    //int, double etc
                    return Convert.ChangeType(val.Value[0], propertyType);
                }));

                var instance = res.ToObject<T>();
                return instance;
            }

            using (TextReader streamReader = new StreamReader(request.Body))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var body = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonTextReader) ?? new Dictionary<string, object>();
                var query = request.Query.Keys.ToDictionary(k => k, v => request.Query[v][0] as object);
                var routeData = request.HttpContext.GetRouteData().Values;
                var merged = body.Concat(routeData.Where(x => !body.Keys.Contains(x.Key))).Concat(query.Where(x => !body.Keys.Contains(x.Key))).ToDictionary(key => key.Key, val => val.Value);
                return JObject.FromObject(merged).ToObject<T>();
            }
        }

        public static (ValidationResult ValidationResult, T Data) BindAndValidate<T>(this HttpRequest request)
        {
            var model = request.Bind<T>();
            if (model == null)
            {
                model = Activator.CreateInstance<T>();
            }

            var validationResult = request.Validate(model);
            return (validationResult, model);
        }

        public static bool IsArray(this Type source)
        {
            return source.BaseType == typeof(Array);
        }

        public static bool IsCollection(this Type source)
        {
            var collectionType = typeof(ICollection<>);

            return source.IsGenericType && source
                       .GetInterfaces()
                       .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == collectionType);
        }

        public static bool IsEnumerable(this Type source)
        {
            var enumerableType = typeof(IEnumerable<>);

            return source.IsGenericType && source.GetGenericTypeDefinition() == enumerableType;
        }
    }
}
