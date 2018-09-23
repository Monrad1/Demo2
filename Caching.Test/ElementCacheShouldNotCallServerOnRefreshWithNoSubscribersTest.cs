using Caching.Test;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class ElementCacheShouldNotCallServerOnRefreshWithNoSubscribersTest
    {
        [Fact]
        public async Task TestRefreshCallsServerWhenSubscribed()
        {
            int callsToServer = 0;
            int callsFromServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     Interlocked.Increment(ref callsToServer);
                                                                     // ReSharper disable once AccessToModifiedClosure
                                                                     return (valueFromServer + "|" + v).InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();
            string value = null;
            using (elementCache.Get(id)
                               .Subscribe(v =>
                                          {
                                              value = v;
                                              Interlocked.Increment(ref callsFromServer);
                                          }))
            {
                await AssertEx.EqualAsync(valueFromServer + "|" + id, () => value, TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                Assert.Equal(1, callsToServer);


                // Update value being returned by server and refresh
                valueFromServer = Guid.NewGuid()
                                      .ToString();
                elementCache.Refresh(id);


                // Server should be called and return new value and thus have been called twice
                await AssertEx.EqualAsync(2, () => callsToServer).ConfigureAwait(false);
                await AssertEx.EqualAsync(2, () => callsFromServer).ConfigureAwait(false);
                Assert.Equal(valueFromServer + "|" + id, value);
            }
        }

        [Fact]
        public async Task TestRefreshDoesNotCallServerWhenNotSubscribed()
        {
            int callsToServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     Interlocked.Increment(ref callsToServer);
                                                                     // ReSharper disable once AccessToModifiedClosure
                                                                     return (valueFromServer + "|" + v).InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();
            string value = null;
            using (var disposable = elementCache.Get(id)
                                                .Subscribe(v => value = v))
            {
                await AssertEx.EqualAsync(valueFromServer + "|" + id, () => value, TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                // Server should have been called once at this point
                Assert.Equal(1, callsToServer);
                disposable.Dispose();
            } // Unsubscribe from by existing using-statement


            // Server should not be called so we want to assert against the current value returned by the server.
            // But should the server be called we still want it to return a new value.
            var oldValueFromServer = valueFromServer;
            valueFromServer = Guid.NewGuid()
                                  .ToString();

            // Refresh 
            elementCache.Refresh(id);

            // Server should still only be called the once
            await Task.Delay(TimeSpan.FromSeconds(5))
                      .ConfigureAwait(false);
            Assert.Equal(1, callsToServer);
            Assert.Equal(oldValueFromServer + "|" + id, value);
        }
    }
}
