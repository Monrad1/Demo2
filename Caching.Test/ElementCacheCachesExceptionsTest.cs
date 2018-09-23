using Caching.Test;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class ElementCacheCachesExceptionsTest
    {
        [Fact]
        public async Task TestElementCacheDoesNotCacheExceptions()
        {
            bool shouldThrowException = true;

            const string exceptionMessage = "Element retriever throws exception.";

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     // ReSharper disable once AccessToModifiedClosure
                                                                     if (shouldThrowException)
                                                                         throw new Exception(exceptionMessage);

                                                                     return v.InTask();
                                                                 })
                                           .ElementCache;

            var id = Guid.NewGuid()
                         .ToString();

            var actualException = await Assert.ThrowsAsync<Exception>(async () => await elementCache.Get(id)
                                                                                                    .FirstAsync())
                                              .ConfigureAwait(false);
            Assert.Equal(exceptionMessage,
                         actualException?.Message);

            shouldThrowException = false;

            Assert.Equal(id,
                         await elementCache.Get(id)
                                           .FirstAsync());
        }
    }
}
