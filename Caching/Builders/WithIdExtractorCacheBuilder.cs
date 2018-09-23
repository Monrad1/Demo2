using System;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Sekoia.Caching.Builders
{
    public class WithIdExtractorCacheBuilder<TElement, TId>
        where TElement : class
    {
        private readonly Func<TElement, TId> _idExtractor;

        public WithIdExtractorCacheBuilder([NotNull] Func<TElement, TId> idExtractor)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
        }

        public WithElementRetrieverCacheBuilder<TElement, TId> WithElementRetriever([NotNull] Func<TId, Task<TElement>> elementRetriever)
        {
            if (elementRetriever == null)
                throw new ArgumentNullException(nameof(elementRetriever));

            return new WithElementRetrieverCacheBuilder<TElement, TId>(_idExtractor, elementRetriever);
        }

        public WithElementRetrieverCacheBuilder<TElement, TId> WithElementRetriever([NotNull] Func<TId, IObservable<TElement>> elementRetriever)
        {
            if (elementRetriever == null)
                throw new ArgumentNullException(nameof(elementRetriever));


            return WithElementRetriever(id => elementRetriever(id)
                                            .ToTask());
        }
    }
}
