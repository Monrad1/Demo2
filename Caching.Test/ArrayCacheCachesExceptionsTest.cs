using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class ArrayCacheCachesExceptionsTest
    {
        [Fact]
        public async Task TestArrayCacheDoesNotCacheExceptions()
        {
            bool shouldThrowException = true;

            const string exceptionMessage = "Element retriever throws exception.";

            var arrayCache = CacheBuilder.For<string>()
                                         .WithIdExtractor(v => v)
                                         .WithElementRetriever(v => Task.FromResult(v))
                                         .GetArrayCache<string>(v =>
                                                                {
                                                                    // ReSharper disable once AccessToModifiedClosure
                                                                    if (shouldThrowException)
                                                                        throw new Exception(exceptionMessage);

                                                                    return new[] { $"{v}1", $"{v}2", $"{v}3" }.ToObservable();
                                                                });

            var id = Guid.NewGuid()
                         .ToString();

            var actualException = await Assert.ThrowsAsync<Exception>(async () => await arrayCache.Get(id)
                                                                                                  .FirstAsync())
                                              .ConfigureAwait(false);
            Assert.Equal(exceptionMessage,
                         actualException?.Message);

            shouldThrowException = false;

            Assert.Equal(new[] { $"{id}1", $"{id}2", $"{id}3" },
                         await arrayCache.Get(id)
                                         .Select(v => v.ToArray())
                                         .FirstAsync());
        }
    }
}
