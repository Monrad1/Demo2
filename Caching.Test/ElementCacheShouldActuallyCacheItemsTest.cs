using Caching.Test;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class ElementCacheShouldActuallyCacheItemsTest
    {
        [Fact]
        public async Task TestArrayCacheReturnsCachedItemsOnSubsequentCallsAndOnlyOneCallToRetrieverPerRefresh()
        {
            var throwException = false;

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     // ReSharper disable once AccessToModifiedClosure
                                                                     Assert.False(throwException, "Retriever should not have been called!");
                                                                     throwException = true;

                                                                     return v.InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            throwException = false;
            elementCache.Refresh(id);

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            throwException = false;
            elementCache.Refresh(id);

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());
        }

        [Fact]
        public async Task TestElementCacheCanCacheMultipleValues()
        {
            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v => v.InTask())
                                           .ElementCache;

            var id1 = Guid.NewGuid()
                          .ToString();
            var id2 = Guid.NewGuid()
                          .ToString();

            Assert.Equal(id1,
                         await elementCache.Get(id1)
                                           .FirstAsync());

            Assert.Equal(id2,
                         await elementCache.Get(id2)
                                           .FirstAsync());

            Assert.Equal(id1,
                         await elementCache.Get(id1)
                                           .FirstAsync());

            Assert.Equal(id2,
                         await elementCache.Get(id2)
                                           .FirstAsync());
        }

        [Fact]
        public async Task TestElementCacheReturnsCachedItemsOnSubsequentCalls()
        {
            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v => v.InTask())
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());
        }

        [Fact]
        public async Task TestElementCacheReturnsCachedItemsOnSubsequentCallsAndHandlesRefresh()
        {
            var position = 0;

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     // ReSharper disable once AccessToModifiedClosure
                                                                     // ReSharper disable once ConvertToLambdaExpression
                                                                     return $"{v}:{position}".InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            var manualWaitHandle = new ManualResetEvent(false);
            var actualItemsTcs = new TaskCompletionSource<string[]>();
            elementCache.Get(id)
                        .Do(v => manualWaitHandle.Set())
                        .Take(3)
                        .ToArray()
                        .Subscribe(v => { actualItemsTcs.SetResult(v); });

            manualWaitHandle.WaitOne();
            manualWaitHandle.Reset();

            position++;
            elementCache.Refresh(id);

            manualWaitHandle.WaitOne();
            manualWaitHandle.Reset();

            position++;
            elementCache.Refresh(id);

            manualWaitHandle.WaitOne();
            manualWaitHandle.Reset();

            var expectedItems = new[] { $"{id}:0", $"{id}:1", $"{id}:2" };
            Assert.Equal(expectedItems,
                         await actualItemsTcs.Task.ConfigureAwait(false));
        }

        [Fact]
        public void TestElementCacheReturnsCachedItemsOnSubsequentCallsAndHandlesRefresh_MultipleSubscriber()
        {
            var position = 0;

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     // ReSharper disable once AccessToModifiedClosure
                                                                     // ReSharper disable once ConvertToLambdaExpression
                                                                     return $"{v}:{position}".InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            var waitHandleContinue1 = new ManualResetEvent(false);
            var waitHandleContinue2 = new ManualResetEvent(false);
            var waitHandleComplete1 = new ManualResetEvent(false);
            var waitHandleComplete2 = new ManualResetEvent(false);
            var actualItems1 = new ConcurrentDictionary<string, string>();
            var actualItems2 = new ConcurrentDictionary<string, string>();

            using (elementCache.Get(id)
                               .Do(v => waitHandleContinue1.Set())
                               .Do(v => actualItems1[v] = v)
                               .TakeWhile(v => !actualItems1.ContainsKey($"{id}:2"))
                               .Subscribe(v => { }, () => waitHandleComplete1.Set()))
            using (elementCache.Get(id)
                               .Do(v => waitHandleContinue2.Set())
                               .Do(v => actualItems2[v] = v)
                               .TakeWhile(v => !actualItems2.ContainsKey($"{id}:2"))
                               .Subscribe(v => { }, () => waitHandleComplete2.Set()))
            {
                waitHandleContinue1.WaitOne();
                waitHandleContinue2.WaitOne();
                waitHandleContinue1.Reset();
                waitHandleContinue2.Reset();

                position++;
                elementCache.Refresh(id);

                waitHandleContinue1.WaitOne();
                waitHandleContinue2.WaitOne();
                waitHandleContinue1.Reset();
                waitHandleContinue2.Reset();

                position++;
                elementCache.Refresh(id);

                waitHandleContinue1.WaitOne();
                waitHandleContinue2.WaitOne();
                waitHandleContinue1.Reset();
                waitHandleContinue2.Reset();

                waitHandleComplete1.WaitOne();
                waitHandleComplete2.WaitOne();

                var expectedItems = new[] { $"{id}:0", $"{id}:2" };
                Assert.Equal(expectedItems,
                             new[]
                             {
                                 actualItems1.Values.OrderBy(v => v)
                                             .First(),
                                 actualItems1.Values.OrderBy(v => v)
                                             .Last()
                             });
                Assert.Equal(expectedItems,
                             new[]
                             {
                                 actualItems2.Values.OrderBy(v => v)
                                             .First(),
                                 actualItems2.Values.OrderBy(v => v)
                                             .Last()
                             });
            }
        }

        [Fact]
        public void TestElementCacheReturnsCachedItemsOnSubsequentCallsAndHandlesRefresh_MultipleSubscriber_Continously()
        {
            for (int i = 0; i < 100; i++)
            {
                TestElementCacheReturnsCachedItemsOnSubsequentCallsAndHandlesRefresh_MultipleSubscriber();
            }
        }

        [Fact]
        public async Task TestElementCacheReturnsNewValueAfterRefreshEvenIfUnsubscribed()
        {
            int callsToServer = 0;
            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => Guid.NewGuid()
                                                                     .ToString())
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     Interlocked.Increment(ref callsToServer);
                                                                     return Guid.NewGuid()
                                                                                .ToString()
                                                                                .InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            var first = await elementCache.Get(id)
                                          .FirstAsync();
            Assert.NotEmpty(first);
            Assert.Equal(1, callsToServer);

            elementCache.RefreshAll();
            var second = await elementCache.Get(id)
                                           .FirstAsync();
            Assert.NotEqual(first, second);
            Assert.Equal(2, callsToServer);

            elementCache.RefreshAll();
            var third = await elementCache.Get(id)
                                          .FirstAsync();
            Assert.NotEqual(first, third);
            Assert.NotEqual(second, third);
            Assert.Equal(3, callsToServer);
        }

        [Fact]
        public async Task TestElementCacheReturnsNewValueAfterTimeout()
        {
            int callsToServer = 0;
            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => Guid.NewGuid()
                                                                     .ToString())
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     Interlocked.Increment(ref callsToServer);
                                                                     return Guid.NewGuid()
                                                                                .ToString()
                                                                                .InTask();
                                                                 })
                                           .WithElementMaxAge(TimeSpan.FromSeconds(1))
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            var first = await elementCache.Get(id)
                                          .FirstAsync();
            Assert.NotEmpty(first);
            Assert.Equal(1, callsToServer);

            await Task.Delay(TimeSpan.FromSeconds(1))
                      .ConfigureAwait(false);
            var second = await elementCache.Get(id)
                                           .FirstAsync();
            Assert.NotEqual(first, second);
            Assert.Equal(2, callsToServer);

            await Task.Delay(TimeSpan.FromSeconds(1))
                      .ConfigureAwait(false);
            var third = await elementCache.Get(id)
                                          .FirstAsync();
            Assert.NotEqual(first, third);
            Assert.NotEqual(second, third);
            Assert.Equal(3, callsToServer);
        }
    }
}
