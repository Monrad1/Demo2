using System;

namespace Demo.Carter
{
    public interface ICarterRouteBuilderBuilder
    {
        ICarterRouteBuilder Build(Type type);
    }
}