using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using JetBrains.Annotations;

namespace Sekoia.Caching.Implementation
{
    internal class ArrayCache<TElement, TId, TArrayKey> : IArrayCache<TElement, TArrayKey>
    {
        private readonly Func<TArrayKey, IObservable<TElement>> _arrayRetriever;
        private readonly Dictionary<TArrayKey, ArrayCacheEntry> _cache = new Dictionary<TArrayKey, ArrayCacheEntry>();
        private readonly Action<TElement> _elementCacher;
        private readonly Func<TId, IObservable<TElement>> _elementRetriever;
        private readonly Func<TElement, TId> _idExtractor;
        private readonly object _mutex = new object();

        public Func<TimeSpan> MaxAge { get; set; } = () => TimeSpan.FromMinutes(1);

        public ArrayCache([NotNull] Func<TArrayKey, IObservable<TElement>> arrayRetriever,
                          [NotNull] Func<TElement, TId> idExtractor,
                          [NotNull] Action<TElement> elementCacher,
                          [NotNull] Func<TId, IObservable<TElement>> elementRetriever)
        {
            if (arrayRetriever == null)
                throw new ArgumentNullException(nameof(arrayRetriever));
            if (idExtractor == null)
                throw new ArgumentNullException(nameof(idExtractor));
            if (elementCacher == null)
                throw new ArgumentNullException(nameof(elementCacher));
            if (elementRetriever == null)
                throw new ArgumentNullException(nameof(elementRetriever));

            _arrayRetriever = arrayRetriever;
            _idExtractor = idExtractor;
            _elementCacher = elementCacher;
            _elementRetriever = elementRetriever;
        }

        public IObservable<IReadOnlyList<TElement>> Get([NotNull] TArrayKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var arrayCacheEntry = GetArrayCacheEntry(key);
            return arrayCacheEntry.Observable;
        }

        public void Refresh([NotNull] TArrayKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var arrayCacheEntry = GetArrayCacheEntry(key, false);

            arrayCacheEntry?.CacheEntry.Refresh();
        }

        public void RefreshAll()
        {
            lock (_mutex)
            {
                foreach (var cacheEntry in _cache.Values)
                    cacheEntry.CacheEntry.Refresh();
            }
        }

        private ArrayCacheEntry CreateCacheEntry(TArrayKey key)
        {
            var cacheEntry = new CacheEntry<IReadOnlyList<TId>>(() =>
                                                                {
                                                                    var arrayObservable = _arrayRetriever(key);
                                                                    return arrayObservable.Do(v => _elementCacher(v))
                                                                                          .Select(v => _idExtractor(v))
                                                                                          .ToList()
                                                                                          .Cast<IReadOnlyList<TId>>()
                                                                                          .ToTask();
                                                                },
                                                                MaxAge,
                                                                () => RemoveCacheEntry(key));

            var arrayCacheEntry = new ArrayCacheEntry(cacheEntry, _elementRetriever);

            return arrayCacheEntry;
        }

        private ArrayCacheEntry GetArrayCacheEntry(TArrayKey key, bool createIfMissing = true)
        {
            lock (_mutex)
            {
                ArrayCacheEntry arrayCacheEntry;
                if (!_cache.TryGetValue(key, out arrayCacheEntry) && createIfMissing)
                {
                    arrayCacheEntry = CreateCacheEntry(key);
                    _cache[key] = arrayCacheEntry;
                }

                return arrayCacheEntry;
            }
        }

        private void RemoveCacheEntry(TArrayKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            lock (_mutex)
            {
                var cacheEntry = GetArrayCacheEntry(key, false);

                cacheEntry?.CacheEntry.Dispose();
                _cache.Remove(key);
            }
        }

        private class ArrayCacheEntry
        {
            public CacheEntry<IReadOnlyList<TId>> CacheEntry { get; }
            public IObservable<IReadOnlyList<TElement>> Observable { get; }

            public ArrayCacheEntry(CacheEntry<IReadOnlyList<TId>> cacheEntry, Func<TId, IObservable<TElement>> elementCacheRetriever)
            {
                CacheEntry = cacheEntry;
                Observable = cacheEntry.Observable
                                       .Select(ids => ids.Select(elementCacheRetriever)
                                                         .ToImmutableList())
                                       .Select(valuesObservables =>
                                               {
                                                   if (!valuesObservables.Any())
                                                       return System.Reactive.Linq.Observable.Return(ImmutableList<TElement>.Empty);

                                                   return valuesObservables.CombineLatest(l => l.ToImmutableList());
                                               })
                                       .Switch()
                                       .Throttle(TimeSpan.FromMilliseconds(100));
            }
        }
    }
}
