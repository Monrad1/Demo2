using Sekoia.Caching.Builders;

namespace Sekoia.Caching
{
    public class CacheBuilder
    {
        public static ForTypeCacheBuilder<TElement> For<TElement>() 
            where TElement : class
        {
            return new ForTypeCacheBuilder<TElement>();
        }
    }
}