using System;
using JetBrains.Annotations;

namespace Sekoia.Caching.Builders
{
    public class ForTypeCacheBuilder<TElement>
        where TElement : class
    {
        public WithIdExtractorCacheBuilder<TElement, TId> WithIdExtractor<TId>([NotNull] Func<TElement, TId> idExtractor)
        {
            if (idExtractor == null)
                throw new ArgumentNullException(nameof(idExtractor));

            return new WithIdExtractorCacheBuilder<TElement, TId>(idExtractor);
        }
    }
}
