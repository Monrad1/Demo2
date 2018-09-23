using System;
using System.Threading.Tasks;
using Carter;
using Demo.Carter.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Demo.Carter
{
    public class CarterModuleBase : CarterModule
    {
        private static readonly ICarterRouteBuilderCache _carterRouteBuilderCache =
            new DefaultCarterRouteBuilderCache(new DefaultCarterRouteBuilderBuilder());

        protected new void Delete(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Delete(path, handler);
        [Obsolete("Annotate a method with the " + nameof(GetAttribute) + " attribute to declare a GET handler")]
        protected new void Get(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Get(path, handler);
        protected new void Head(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Head(path, handler);
        protected new void Options(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Options(path, handler);
        protected new void Patch(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Patch(path, handler);
        protected new void Post(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Post(path, handler);
        protected new void Put(string path, Func<HttpRequest, HttpResponse, RouteData, Task> handler) => base.Put(path, handler);

        public CarterModuleBase() : this(string.Empty){ }

        public CarterModuleBase(string modulePath) : base(modulePath)
        {
            InitializeRoutes();
        }

        private void InitializeRoutes()
        {
            GetCarterRouterBuilder()
                .Build(this);
        }

        private ICarterRouteBuilder GetCarterRouterBuilder()
        {
            return _carterRouteBuilderCache.BuildOrGet(GetType());
        }
    }
}
