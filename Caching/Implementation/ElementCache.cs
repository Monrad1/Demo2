using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Sekoia.Caching.Implementation
{
    internal class ElementCache<TElement, TId> : IElementCache<TElement, TId>
        where TElement : class
    {
        private readonly Dictionary<TId, CacheEntry<TElement>> _cache = new Dictionary<TId, CacheEntry<TElement>>();
        private readonly Func<TId, Task<TElement>> _elementRetriever;
        private readonly object _mutex = new object();

        public Func<TimeSpan> MaxAge { get; set; } = () => TimeSpan.FromMinutes(1);

        public ElementCache([NotNull] Func<TId, Task<TElement>> elementRetriever)
        {
            _elementRetriever = elementRetriever ?? throw new ArgumentNullException(nameof(elementRetriever));
        }

        public IObservable<TElement> Get(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var cacheEntry = GetCacheEntry(id);
            return cacheEntry.Observable;
        }

        public void Refresh(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var cacheEntry = GetCacheEntry(id, false);
            cacheEntry?.Refresh();
        }

        public void RefreshAll()
        {
            lock (_mutex)
            {
                foreach (var cacheEntry in _cache.Values)
                    cacheEntry.Refresh();
            }
        }

        public void Remove(TId id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            lock (_mutex)
            {
                var cacheEntry = GetCacheEntry(id, false);

                cacheEntry?.Dispose();
                _cache.Remove(id);
            }
        }

        public IObservable<TElement> Set([NotNull] TId id, [NotNull] TElement element)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            var cacheEntry = GetCacheEntry(id);

            cacheEntry.SetCachedValue(element);

            return cacheEntry.Observable;
        }

        private CacheEntry<TElement> CreateCacheEntry(TId id)
        {
            return new CacheEntry<TElement>(() => _elementRetriever(id),
                                            MaxAge,
                                            () => Remove(id));
        }

        private CacheEntry<TElement> GetCacheEntry(TId id, bool createIfMissing = true)
        {
            lock (_mutex)
            {
                CacheEntry<TElement> cacheEntry;
                if (!_cache.TryGetValue(id, out cacheEntry) && createIfMissing)
                {
                    cacheEntry = CreateCacheEntry(id);
                    _cache[id] = cacheEntry;
                }

                return cacheEntry;
            }
        }
    }
}
