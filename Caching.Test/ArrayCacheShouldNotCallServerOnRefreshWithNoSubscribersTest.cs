using Caching.Test;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class ArrayCacheShouldNotCallServerOnRefreshWithNoSubscribersTest
    {
        [Fact]
        public async Task TestRefreshAllCallsServerWhenSubscribed_MultipleValuesInArrayCache()
        {
            int callsToServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                                               {
                                                                   throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                                                   return Task.FromResult(v);
#pragma warning restore 162
                                                               })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    Interlocked.Increment(ref callsToServer);
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    return GenerateValues(3, valueFromServer + "|" + v)
                                                                        .ToObservable();
                                                                });

            var id1 = Guid.NewGuid()
                          .ToString();
            var id2 = Guid.NewGuid()
                          .ToString();
            var id3 = Guid.NewGuid()
                          .ToString();

            string value1 = null;
            string value2 = null;
            string value3 = null;

            using (arrayCache.Get(id1)
                             .Subscribe(v => value1 = v.Join("#"), e => { }, () => { }))
            using (arrayCache.Get(id2)
                             .Subscribe(v => value2 = v.Join("#"), e => { }, () => { }))
            using (arrayCache.Get(id3)
                             .Subscribe(v => value3 = v.Join("#"), e => { }, () => { }))
            {
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                Assert.Equal(3, callsToServer);


                // Update value being returned by server and refresh
                valueFromServer = Guid.NewGuid()
                                      .ToString();
                arrayCache.RefreshAll();


                // Server should be called and return new value and thus have been called twice
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                Assert.Equal(6, callsToServer);
            }
        }

        [Fact]
        public async Task TestRefreshAllDoesNotCallServerWhenNotSubscribed_MultipleValuesInArrayCache()
        {
            var callsToServer = new ConcurrentDictionary<string, int>();
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                                               {
                                                                   throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                                                   return Task.FromResult(v);
#pragma warning restore 162
                                                               })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    callsToServer.AddOrUpdate(v, _ => 1, (_, c) => c + 1);
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    return GenerateValues(3, valueFromServer + "|" + v)
                                                                        .ToObservable();
                                                                });

            var id1 = Guid.NewGuid()
                          .ToString();
            var id2 = Guid.NewGuid()
                          .ToString();
            var id3 = Guid.NewGuid()
                          .ToString();

            string value1 = null;
            string value2 = null;
            string value3 = null;


            string oldValueFromServer2;
            string oldValueFromServer3;
            using (arrayCache.Get(id1)
                             .Subscribe(v => value1 = v.Join("#"), e => { }, () => { }))
            {
                using (arrayCache.Get(id2)
                                 .Subscribe(v => value2 = v.Join("#"), e => { }, () => { }))
                {
                    using (arrayCache.Get(id3)
                                     .Subscribe(v => value3 = v.Join("#"), e => { }, () => { }))
                    {
                        await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                                      .ConfigureAwait(false);
                        await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                                      .ConfigureAwait(false);
                        await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                                      .ConfigureAwait(false);

                        Assert.Equal(1, callsToServer[id1]);
                        Assert.Equal(1, callsToServer[id2]);
                        Assert.Equal(1, callsToServer[id3]);
                    }


                    // Update value being returned by server and refresh
                    oldValueFromServer3 = valueFromServer;
                    valueFromServer = Guid.NewGuid()
                                          .ToString();
                    arrayCache.RefreshAll();

                    // Server should be called and return new value and thus have been called twice
                    await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                                  .ConfigureAwait(false);
                    await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                                  .ConfigureAwait(false);
                    await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer3 + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                                  .ConfigureAwait(false);

                    Assert.Equal(2, callsToServer[id1]);
                    Assert.Equal(2, callsToServer[id2]);
                    Assert.Equal(1, callsToServer[id3]);
                }


                // Update value being returned by server and refresh
                oldValueFromServer2 = valueFromServer;
                valueFromServer = Guid.NewGuid()
                                      .ToString();
                arrayCache.RefreshAll();

                // Server should be called and return new value and thus have been called twice
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer2 + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer3 + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);

                Assert.Equal(3, callsToServer[id1]);
                Assert.Equal(2, callsToServer[id2]);
                Assert.Equal(1, callsToServer[id3]);
            }


            // Update value being returned by server and refresh
            var oldValueFromServer1 = valueFromServer;
            valueFromServer = Guid.NewGuid()
                                  .ToString();
            arrayCache.RefreshAll();

            // Server should be called and return new value and thus have been called twice
            await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer1 + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);
            await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer2 + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);
            await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer3 + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);

            Assert.Equal(3, callsToServer[id1]);
            Assert.Equal(2, callsToServer[id2]);
            Assert.Equal(1, callsToServer[id3]);
        }

        [Fact]
        public async Task TestRefreshAllDoesNotCallServerWhenNotSubscribed_MultipleValuesInArrayCacheSameKey()
        {
            var callsToServer = new ConcurrentDictionary<string, int>();
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                         {
                                             throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                             return Task.FromResult(v);
#pragma warning restore 162
                                         })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    Thread.Sleep(500);
                                             callsToServer.AddOrUpdate(v, _ => 1, (_, c) => c + 1);
                                             // ReSharper disable once AccessToModifiedClosure
                                             return GenerateValues(3, valueFromServer + "|" + v)
                                                 .ToObservable();
                                         });

            var id1 = Guid.NewGuid()
                          .ToString();
            var id2 = id1;
            var id3 = id1;

            string value1 = null;
            string value2 = null;
            string value3 = null;


            string oldValueFromServer2;
            string oldValueFromServer3;
            using (arrayCache.Get(id1)
                               .SubscribeOn(TaskPoolScheduler.Default)
                             .Subscribe(v => value1 = v.Join("#"), e => { }, () => { }))
            {
                using (arrayCache.Get(id2)
                               .SubscribeOn(TaskPoolScheduler.Default)
                                 .Subscribe(v => value2 = v.Join("#"), e => { }, () => { }))
                {
                    using (arrayCache.Get(id3)
                               .SubscribeOn(TaskPoolScheduler.Default)
                                     .Subscribe(v => value3 = v.Join("#"), e => { }, () => { }))
                    {
                        await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                                      .ConfigureAwait(false);
                        await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                                      .ConfigureAwait(false);
                        await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                                      .ConfigureAwait(false);

                        await Task.Delay(5000);

                        Assert.Equal(1, callsToServer[id1]);
                        Assert.Equal(1, callsToServer[id2]);
                        Assert.Equal(1, callsToServer[id3]);
                    }

                    // Update value being returned by server and refresh
                    oldValueFromServer3 = valueFromServer;
                    valueFromServer = Guid.NewGuid()
                                          .ToString();
                    arrayCache.RefreshAll();

                    // Server should be called and return new value and thus have been called twice
                    await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                                  .ConfigureAwait(false);
                    await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                                  .ConfigureAwait(false);
                    await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer3 + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                                  .ConfigureAwait(false);

                    Assert.Equal(2, callsToServer[id1]);
                    Assert.Equal(2, callsToServer[id2]);
                    Assert.Equal(2, callsToServer[id3]);
                }


                // Update value being returned by server and refresh
                oldValueFromServer2 = valueFromServer;
                valueFromServer = Guid.NewGuid()
                                      .ToString();
                arrayCache.RefreshAll();

                // Server should be called and return new value and thus have been called twice
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer2 + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer3 + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);

                Assert.Equal(3, callsToServer[id1]);
                Assert.Equal(3, callsToServer[id2]);
                Assert.Equal(3, callsToServer[id3]);
            }

            // Update value being returned by server and refresh
            var oldValueFromServer1 = valueFromServer;
            valueFromServer = Guid.NewGuid()
                                  .ToString();
            arrayCache.RefreshAll();

            // Server should be called and return new value and thus have been called twice
            await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer1 + "|" + id1), () => value1, TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);
            await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer2 + "|" + id2), () => value2, TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);
            await AssertEx.EqualAsync(GenerateValue(3, oldValueFromServer3 + "|" + id3), () => value3, TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);

            Assert.Equal(3, callsToServer[id1]);
            Assert.Equal(3, callsToServer[id2]);
            Assert.Equal(3, callsToServer[id3]);
        }

        [Fact]
        public async Task TestRefreshAllDoesNotCallServerWhenNotSubscribed_SingleValueInArrayCache()
        {
            int callsToServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                                               {
                                                                   throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                                                   return Task.FromResult(v);
#pragma warning restore 162
                                                               })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    Interlocked.Increment(ref callsToServer);
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    return GenerateValues(3, valueFromServer + "|" + v)
                                                                        .ToObservable();
                                                                });

            var id = Guid.NewGuid()
                         .ToString();
            string value = null;
            using (arrayCache.Get(id)
                             .Subscribe(v => value = v.Join("#")))
            {
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id), () => value, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);

                // Server should have been called once at this point
                Assert.Equal(1, callsToServer);
            } // Unsubscribe from by existing using-statement


            // Server should not be called so we want to assert against the current value returned by the server.
            // But should the server be called we still want it to return a new value.
            var oldValueFromServer = valueFromServer;
            valueFromServer = Guid.NewGuid()
                                  .ToString();

            // Refresh 
            arrayCache.RefreshAll();

            // Server should still only be called the once
            await Task.Delay(TimeSpan.FromSeconds(5))
                      .ConfigureAwait(false);
            Assert.Equal(1, callsToServer);
            Assert.Equal(GenerateValue(3, oldValueFromServer + "|" + id), value);
        }

        [Fact]
        public async Task TestRefreshCallsServerWhenSubscribed_SingleValueInArrayCache()
        {
            int callsToServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                                               {
                                                                   throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                                                   return Task.FromResult(v);
#pragma warning restore 162
                                                               })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    Interlocked.Increment(ref callsToServer);
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    return GenerateValues(3, valueFromServer + "|" + v)
                                                                        .ToObservable();
                                                                });

            var id = Guid.NewGuid()
                         .ToString();
            string value = null;
            using (arrayCache.Get(id)
                             .Subscribe(v => value = v.Join("#"), e => { }, () => { }))
            {
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id), () => value, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                Assert.Equal(1, callsToServer);


                // Update value being returned by server and refresh
                valueFromServer = Guid.NewGuid()
                                      .ToString();
                arrayCache.Refresh(id);


                // Server should be called and return new value and thus have been called twice
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id), () => value, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);
                Assert.Equal(2, callsToServer);
            }
        }

        [Fact]
        public async Task TestRefreshDoesNotCallServerWhenNotSubscribed_MultipleValuesInArrayCache()
        {
            int callsToServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                                               {
                                                                   throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                                                   return Task.FromResult(v);
#pragma warning restore 162
                                                               })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    Interlocked.Increment(ref callsToServer);
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    return GenerateValues(3, valueFromServer + "|" + v)
                                                                        .ToObservable();
                                                                });

            var id = Guid.NewGuid()
                         .ToString();
            string value = null;

            arrayCache.Get(Guid.NewGuid()
                               .ToString())
                      .Subscribe(_ => { })
                      .Dispose();
            arrayCache.Get(Guid.NewGuid()
                               .ToString())
                      .Subscribe(_ => { })
                      .Dispose();

            using (arrayCache.Get(id)
                             .Subscribe(v => value = v.Join("#")))
            {
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id), () => value, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);

                // Server should have been called once at this point
                Assert.Equal(3, callsToServer);
            } // Unsubscribe from by existing using-statement


            // Server should not be called so we want to assert against the current value returned by the server.
            // But should the server be called we still want it to return a new value.
            var oldValueFromServer = valueFromServer;
            valueFromServer = Guid.NewGuid()
                                  .ToString();

            // Refresh 
            arrayCache.Refresh(id);

            // Server should still only be called the once
            await Task.Delay(TimeSpan.FromSeconds(5))
                      .ConfigureAwait(false);
            Assert.Equal(3, callsToServer);
            Assert.Equal(GenerateValue(3, oldValueFromServer + "|" + id), value);
        }

        [Fact]
        public async Task TestRefreshDoesNotCallServerWhenNotSubscribed_SingleValueInArrayCache()
        {
            int callsToServer = 0;
            string valueFromServer = Guid.NewGuid()
                                         .ToString();

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v =>
                                                               {
                                                                   throw new NotSupportedException("Element retriever not supported.");
#pragma warning disable 162
                                                                   return Task.FromResult(v);
#pragma warning restore 162
                                                               })
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    Interlocked.Increment(ref callsToServer);
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    return GenerateValues(3, valueFromServer + "|" + v)
                                                                        .ToObservable();
                                                                });

            var id = Guid.NewGuid()
                         .ToString();
            string value = null;
            using (arrayCache.Get(id)
                             .Subscribe(v => value = v.Join("#")))
            {
                await AssertEx.EqualAsync(GenerateValue(3, valueFromServer + "|" + id), () => value, TimeSpan.FromSeconds(5))
                              .ConfigureAwait(false);

                // Server should have been called once at this point
                Assert.Equal(1, callsToServer);
            } // Unsubscribe from by existing using-statement


            // Server should not be called so we want to assert against the current value returned by the server.
            // But should the server be called we still want it to return a new value.
            var oldValueFromServer = valueFromServer;
            valueFromServer = Guid.NewGuid()
                                  .ToString();

            // Refresh 
            arrayCache.Refresh(id);

            // Server should still only be called the once
            await Task.Delay(TimeSpan.FromSeconds(5))
                      .ConfigureAwait(false);
            Assert.Equal(1, callsToServer);
            Assert.Equal(GenerateValue(3, oldValueFromServer + "|" + id), value);
        }

        private static string GenerateValue(int count, string baseValue) => GenerateValues(count, baseValue)
            .Join("#");

        private static string[] GenerateValues(int count, string baseValue) => Enumerable.Range(0, count)
                                                                                         .Select(i => $"{baseValue}{i}")
                                                                                         .ToArray();
    }
}
