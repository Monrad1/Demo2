using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Sekoia.Caching.Implementation;

namespace Sekoia.Caching.Builders
{
    public class WithElementRetrieverCacheBuilder<TElement, TId>
        where TElement : class
    {
        private readonly TimeSpan? _arrayMaxAge;
        private readonly ElementCache<TElement, TId> _elementCache;
        private readonly TimeSpan? _elementCacheMaxAge;
        private readonly Func<TId, Task<TElement>> _elementRetriever;
        private readonly Func<TElement, TId> _idExtractor;

        public IElementCache<TElement, TId> ElementCache => _elementCache;

        public WithElementRetrieverCacheBuilder([NotNull] Func<TElement, TId> idExtractor,
                                                [NotNull] Func<TId, Task<TElement>> elementRetriever,
                                                TimeSpan? elementCacheMaxAge = null,
                                                TimeSpan? arrayMaxAge = null)
            : this(idExtractor, elementRetriever, elementCacheMaxAge, arrayMaxAge, null) { }

        private WithElementRetrieverCacheBuilder(Func<TElement, TId> idExtractor,
                                                 Func<TId, Task<TElement>> elementRetriever,
                                                 TimeSpan? elementCacheMaxAge,
                                                 TimeSpan? arrayMaxAge,
                                                 ElementCache<TElement, TId> elementCache)
        {
            _idExtractor = idExtractor ?? throw new ArgumentNullException(nameof(idExtractor));
            _elementRetriever = elementRetriever ?? throw new ArgumentNullException(nameof(elementRetriever));
            _arrayMaxAge = arrayMaxAge;
            _elementCacheMaxAge = elementCacheMaxAge;
            _elementCache = elementCache ?? new ElementCache<TElement, TId>(elementRetriever);

            _elementCache.MaxAge = () => _elementCacheMaxAge ?? TimeSpan.FromMinutes(1);
        }

        public IArrayCache<TElement, TArrayKey> GetArrayCache<TArrayKey>(Func<TArrayKey, IObservable<TElement>> arrayRetriever)
        {
            return new ArrayCache<TElement, TId, TArrayKey>(arrayRetriever,
                                                            _idExtractor,
                                                            e => _elementCache.Set(_idExtractor(e), e),
                                                            e => ElementCache.Get(e))
                   {
                       MaxAge = () => _arrayMaxAge ?? TimeSpan.FromMinutes(1)
                   };
        }

        public ISingleArrayCache<TElement> GetArrayCacheWithoutKey(Func<IObservable<TElement>> arrayRetriever)
        {
            return new SingleArrayCache<TElement, TId>(arrayRetriever,
                                                       _idExtractor,
                                                       e => _elementCache.Set(_idExtractor(e), e),
                                                       e => _elementCache.Get(e))
                   {
                       MaxAge = () => _arrayMaxAge ?? TimeSpan.FromMinutes(1)
                   };
        }

        public WithElementRetrieverCacheBuilder<TElement, TId> WithArrayMaxAge(TimeSpan maxAge)
        {
            return new WithElementRetrieverCacheBuilder<TElement, TId>(_idExtractor,
                                                                       _elementRetriever,
                                                                       _elementCacheMaxAge,
                                                                       maxAge,
                                                                       _elementCache);
        }

        public WithElementRetrieverCacheBuilder<TElement, TId> WithElementMaxAge(TimeSpan maxAge)
        {
            return new WithElementRetrieverCacheBuilder<TElement, TId>(_idExtractor,
                                                                       _elementRetriever,
                                                                       maxAge,
                                                                       _arrayMaxAge,
                                                                       _elementCache);
        }
    }
}
