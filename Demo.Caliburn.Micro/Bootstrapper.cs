using System.Diagnostics;
using Autofac;
using Caliburn.Micro;
using Caliburn.Micro.Autofac;
using Demo.Caliburn.Micro.ViewModels;

namespace Demo.Caliburn.Micro
{
    public class Bootstrapper : AutofacBootstrapper<ShellViewModel>
    {
        private readonly IEventAggregator _eventAggregator;

        public Bootstrapper()
        {

            _eventAggregator = new EventAggregator();
            Initialize();
        }

        protected override void ConfigureBootstrapper()
        {
            base.ConfigureBootstrapper();

            EnforceNamespaceConvention = true;
            AutoSubscribeEventAggegatorHandlers = true;
            ViewModelBaseType = typeof(IScreen);
            base.CreateEventAggregator = () => _eventAggregator;

            var p = Process.GetCurrentProcess();
            p.PriorityBoostEnabled = true;
            p.PriorityClass = ProcessPriorityClass.High;
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            base.ConfigureContainer(builder);

            builder.RegisterType<ShellViewModel>();

            builder.RegisterType<CarService>()
                   .As<ICarService>()
                   .SingleInstance();

            builder.RegisterType<SearchService>()
                   .As<ISearchService>()
                   .SingleInstance();
        }

        protected override void StartRuntime()
        {
            base.StartRuntime();
            DisplayRootViewFor<ShellViewModel>();
        }
    }
}
