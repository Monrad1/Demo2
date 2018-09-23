using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Sekoia.Caching.Implementation
{
    internal class CacheEntry<T> : IDisposable
        where T : class
    {
        private static readonly Notification<CacheValue> _emptyNotification = Notification.CreateOnNext(new CacheValue(null, DateTimeOffset.MinValue));
        private static readonly Notification<CacheValue> _refreshNotification = Notification.CreateOnNext(new CacheValue(null, DateTimeOffset.MinValue));

        private readonly SerialDisposable _baseSubscription = new SerialDisposable();
        private readonly BehaviorSubject<Notification<CacheValue>> _latestValue = new BehaviorSubject<Notification<CacheValue>>(_emptyNotification);
        private readonly Func<TimeSpan> _maxAge;
        private readonly object _mutex = new object();
        private readonly Action _removeCacheEntry;

        private volatile Task<T> _currentElementRetrieverTask;

        private bool _refreshRequested;
        private Subject<Unit> _triggerFetchFromServer;

        public IObservable<T> Observable { get; }

        public CacheEntry([NotNull] Func<Task<T>> elementRetriever,
                          Func<TimeSpan> maxAge,
                          [NotNull] Action removeCacheEntry)
        {
            if (elementRetriever == null)
                throw new ArgumentNullException(nameof(elementRetriever));

            _maxAge = maxAge ?? throw new ArgumentNullException(nameof(maxAge));
            _removeCacheEntry = removeCacheEntry ?? throw new ArgumentNullException(nameof(removeCacheEntry));

            Observable = CreateObservable(elementRetriever);
        }

        public void Dispose()
        {
            _triggerFetchFromServer.Dispose();
            _baseSubscription.Dispose();
            _latestValue.Dispose();
        }

        public void Refresh()
        {
            if (_latestValue.HasObservers)
            {
                _latestValue.OnNext(_refreshNotification);
                _triggerFetchFromServer.OnNext(Unit.Default);
            }
            else
                _latestValue.OnNext(null);
        }

        public void SetCachedValue(T value)
        {
            _latestValue.OnNext(Notification.CreateOnNext(new CacheValue(value)));
        }

        /// <summary>
        /// Creates an Rx observable for the this instance. 
        /// </summary>
        /// <param name="elementRetriever">The retriever to use when fetching an element.</param>
        /// <returns>An observable that remains up to date when timing out and responds to external updates.</returns>
        private IObservable<T> CreateObservable(Func<Task<T>> elementRetriever)
        {
            _triggerFetchFromServer = new Subject<Unit>();

            // Creates an observable that retrieves an element and emits it, each time the
            // triggerFetchFromServer emits an event, regardless of value.
            var fetchFromServerObservable = _triggerFetchFromServer.Throttle(TimeSpan.FromMilliseconds(100))
                                                                   .Select(_ => SafelyCallElementRetrieverAsync(elementRetriever))
                                                                   .DistinctUntilChanged()
                                                                   .Select(t => t.ToObservable())
                                                                   .Switch()
                                                                   .Select(v => new CacheValue(v))
                                                                   .Materialize();

            _baseSubscription.Disposable = fetchFromServerObservable.Subscribe(v => _latestValue.OnNext(v));

            // Creates an observable only returns non-timed out valid values and errors.
            // Refreshes are triggered as necessary.
            var timeoutHandlingObservable = _latestValue.Where(v =>
                                                               {
                                                                   if (_refreshNotification.Equals(v))
                                                                       return false;
                                                                   if (v?.Kind == NotificationKind.OnCompleted)
                                                                       return false;
                                                                   if (v?.Kind == NotificationKind.OnError)
                                                                   {
                                                                       _removeCacheEntry();
                                                                       return true;
                                                                   }

                                                                   // If value hasn't timed out, use it
                                                                   if (!HasTimedOut(v?.Value.CacheTime ?? DateTimeOffset.MinValue))
                                                                       return true;

                                                                   // If value has timed out, ignore it and refresh if not currently refreshing
                                                                   if (_latestValue.HasObservers)
                                                                       _triggerFetchFromServer.OnNext(Unit.Default);

                                                                   return false;
                                                               })
                                                        .Dematerialize()
                                                        .Select(v => v.Value);

            return timeoutHandlingObservable;
        }

        private bool HasTimedOut(DateTimeOffset cacheTime)
        {
            return Age(cacheTime) > _maxAge();
        }

        private Task<T> SafelyCallElementRetrieverAsync(Func<Task<T>> elementRetriever)
        {
            lock (_mutex)
            {
                var currentElementRetrieverTask = _currentElementRetrieverTask;
                if (currentElementRetrieverTask != null)
                {
                    _refreshRequested = true;
                    return currentElementRetrieverTask;
                }


                var currentElementRetriverTask = elementRetriever();
                _currentElementRetrieverTask = currentElementRetriverTask;

                currentElementRetriverTask
                    .ContinueWith(t =>
                                  {
                                      lock (_mutex)
                                      {
                                          if (ReferenceEquals(currentElementRetriverTask, _currentElementRetrieverTask))
                                              _currentElementRetrieverTask = null;

                                          if (_refreshRequested)
                                          {
                                              _refreshRequested = false;
                                              Refresh();
                                          }
                                      }
                                  });

                return currentElementRetriverTask;
            }
        }

        private static TimeSpan Age(DateTimeOffset cacheTime)
        {
            return DateTimeOffset.Now - cacheTime;
        }

        private class CacheValue
        {
            public DateTimeOffset CacheTime { get; }
            public T Value { get; }

            public CacheValue(T value, DateTimeOffset? cacheTime = null)
            {
                Value = value;
                CacheTime = cacheTime ?? DateTimeOffset.Now;
            }
        }
    }
}
