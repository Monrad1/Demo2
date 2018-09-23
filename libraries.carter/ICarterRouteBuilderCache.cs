using System;

namespace Demo.Carter
{
    internal interface ICarterRouteBuilderCache
    {
        ICarterRouteBuilder BuildOrGet(Type type);
    }
}