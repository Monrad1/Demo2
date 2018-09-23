using System;
using System.Linq;
using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Sekoia.Caching
{
    public static class TaskCache
    {
        public static ITaskCache<T> Create<T>(Func<string, Task<T>> valueFactory, TimeSpan? cacheTimeout = null)
        {
            return new TaskCache<T>(valueFactory, cacheTimeout);
        }
    }

    /// <summary>
    /// Implementation of the ICache that uses MemoryCache internally.
    /// </summary>
    internal class TaskCache<T> : ITaskCache<T>
    {
        private readonly MemoryCache _cache = new MemoryCache(nameof(TaskCache<T>));
        private readonly TimeSpan _cacheTimeout;
        private readonly Func<string, Task<T>> _valueFactory;

        public TaskCache(Func<string, Task<T>> valueFactory, TimeSpan? cacheTimeout = null)
        {
            _valueFactory = valueFactory;
            _cacheTimeout = cacheTimeout ?? TimeSpan.FromHours(1);
        }

        public async Task<T> AddOrGetExistingAsync(string key)
        {
            var asyncLazyValue = CreateAsyncLazy(key);
            var existingValue = AddOrGetExisting(key,
                                                 asyncLazyValue,
                                                 DateTimeOffset.Now.Add(_cacheTimeout));

            if (existingValue != null)
                asyncLazyValue = existingValue;

            try
            {
                var result = await asyncLazyValue;

                // The awaited Task has completed. Check that the task still is the same version
                // that the cache returns (i.e. the awaited task has not been invalidated during the await).
                if (asyncLazyValue != AddOrGetExisting(key,
                                                       CreateAsyncLazy(key),
                                                       DateTimeOffset.Now.Add(_cacheTimeout)))
                {
                    // The awaited value is no more the most recent one.
                    // Get the most recent value with a recursive call.
                    return await AddOrGetExistingAsync(key)
                                     .ConfigureAwait(false);
                }
                return result;
            }
            catch (Exception)
            {
                // Task object for the given key failed with exception. Remove the task from the cache.
                _cache.Remove(key);
                // Re throw the exception to be handled by the caller.
                throw;
            }
        }

        public void Clear()
        {
            // A snapshot of keys is taken to avoid enumerating collection during changes.
            var keys = _cache.Select(i => i.Key)
                             .ToList();
            keys.ForEach(k => _cache.Remove(k));
        }

        public bool Contains(string key)
        {
            return _cache.Contains(key);
        }

        public void Invalidate(string key)
        {
            _cache.Remove(key);
        }

        private AsyncLazy<T> AddOrGetExisting(string key, AsyncLazy<T> value, DateTimeOffset absoluteExpiration)
        {
            return (AsyncLazy<T>)_cache.AddOrGetExisting(key,
                                                         value,
                                                         absoluteExpiration);
        }

        private AsyncLazy<T> CreateAsyncLazy(string key)
        {
            return new AsyncLazy<T>(() => _valueFactory(key));
        }
    }
}
