using Caching.Test;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class ArrayCacheShouldActuallyCacheItemsTest
    {
        [Fact]
        public async Task TestArrayCacheReturnsCachedItemsOnSubsequentCalls()
        {
            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v => v.InTask())
                                         .GetArrayCache<string>(v => new[] { $"{v}1", $"{v}2", $"{v}3" }.ToObservable());

            var id = Guid.NewGuid()
                         .ToString();

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());
        }

        [Fact]
        public async Task TestArrayCacheReturnsCachedItemsOnSubsequentCallsAndHandlesRefresh()
        {
            var position = 0;

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v => v.InTask())
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    // ReSharper disable AccessToModifiedClosure
                                                                    // ReSharper disable once ConvertToLambdaExpression
                                                                    return new[] { $"{v}1:{position}", $"{v}2:{position}", $"{v}3:{position}" }.ToObservable();
                                                                    // ReSharper restore AccessToModifiedClosure
                                                                });


            var id = Guid.NewGuid()
                         .ToString();

            var manualWaitHandle = new ManualResetEvent(false);
            var actualItemsTcs = new TaskCompletionSource<string[]>();
            arrayCache.Get(id)
                      .Do(v => manualWaitHandle.Set())
                      .Take(3)
                      .ToArray()
                      .Subscribe(v =>
                                 {
                                     actualItemsTcs.SetResult(v.SelectMany(i => i)
                                                               .ToArray());
                                 });

            manualWaitHandle.WaitOne();
            manualWaitHandle.Reset();

            position++;
            arrayCache.Refresh(id);

            manualWaitHandle.WaitOne();
            manualWaitHandle.Reset();

            position++;
            arrayCache.Refresh(id);

            manualWaitHandle.WaitOne();
            manualWaitHandle.Reset();

            var expectedItems = new[] { $"{id}1:0", $"{id}2:0", $"{id}3:0", $"{id}1:1", $"{id}2:1", $"{id}3:1", $"{id}1:2", $"{id}2:2", $"{id}3:2" };
            var actualItems = await actualItemsTcs.Task.ConfigureAwait(false);

            Assert.Equal(expectedItems,
                         actualItems);
        }

        [Fact]
        public async Task TestArrayCacheReturnsCachedItemsOnSubsequentCallsAndOnlyOneCallToRetrieverPerRefresh()
        {
            var throwException = false;

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v => v.InTask())
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    Assert.False(throwException, "Retriever should not have been called!");
                                                                    throwException = true;

                                                                    return new[] { $"{v}1", $"{v}2", $"{v}3" }.ToObservable();
                                                                });

            var id = Guid.NewGuid()
                         .ToString();

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            throwException = false;
            arrayCache.Refresh(id);

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            throwException = false;
            arrayCache.Refresh(id);

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());
        }

        [Fact]
        public async Task TestArrayCacheReturnsCanCacheMultipleValues()
        {
            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v => v.InTask())
                                         .GetArrayCache<string>(v => new[] { $"{v}1", $"{v}2", $"{v}3" }.ToObservable());

            var id1 = Guid.NewGuid()
                          .ToString();
            var id2 = Guid.NewGuid()
                          .ToString();

            Assert.Equal(new[] { $"{id1}1", $"{id1}2", $"{id1}3" },
                         await arrayCache.Get(id1)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id2}1", $"{id2}2", $"{id2}3" },
                         await arrayCache.Get(id2)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id1}1", $"{id1}2", $"{id1}3" },
                         await arrayCache.Get(id1)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());

            Assert.Equal(new[] { $"{id2}1", $"{id2}2", $"{id2}3" },
                         await arrayCache.Get(id2)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());
        }
    }
}
