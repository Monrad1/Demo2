using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;

namespace Demo.Carter
{
    internal class DefaultCarterRouteBuilderCache : ICarterRouteBuilderCache
    {
        private readonly ICarterRouteBuilderBuilder _carterRouteBuilderBuilder;
        private readonly ConcurrentDictionary<Type, ICarterRouteBuilder> _cache = new ConcurrentDictionary<Type, ICarterRouteBuilder>();

        public DefaultCarterRouteBuilderCache([NotNull] ICarterRouteBuilderBuilder carterRouteBuilderBuilder)
        {
            _carterRouteBuilderBuilder = carterRouteBuilderBuilder ?? throw new ArgumentNullException(nameof(carterRouteBuilderBuilder));
        }

        public ICarterRouteBuilder BuildOrGet(Type type)
        {
            return _cache.GetOrAdd(type, t => _carterRouteBuilderBuilder.Build(t));
        }
    }
}
