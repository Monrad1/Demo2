using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Carter;
using Carter.Request;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo
{
    public class Startup
    {
        //private readonly IConfigurationRoot _config;

        //public Startup(IHostingEnvironment env)
        //{
        //    var builder = new ConfigurationBuilder()
        //        .AddJsonFile("appsettings.json")
        //        .SetBasePath(env.ContentRootPath);

        //    _config = builder.Build();
        //}

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCarter();

            //// Create the container builder.
            //var builder = new ContainerBuilder();

            //builder.Populate(services);
            ////builder.RegisterType<MyType>().As<IMyType>();
            //var applicationContainer = builder.Build();

            //// Create the IServiceProvider based on the container.
            //return new AutofacServiceProvider(applicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IConfiguration config)
        {
            var appConfig = new AppConfiguration();
            config.Bind(appConfig);

            app.UseCarter(GetOptions());

            //app.UseOwin(x => x.UseBuilder().UseCarter(GetOptions()));
        }

        private CarterOptions GetOptions()
        {
            return new CarterOptions(ctx => this.GetBeforeHook(ctx), ctx => this.GetAfterHook(ctx));
        }

        private Task<bool> GetBeforeHook(HttpContext ctx)
        {
            ctx.Request.Headers["HOWDY"] = "FOLKS";
            return Task.FromResult(true);
        }

        private async Task GetAfterHook(HttpContext ctx)
        {
            Debug.WriteLine($"We hit a route!");
        }
    }
}
