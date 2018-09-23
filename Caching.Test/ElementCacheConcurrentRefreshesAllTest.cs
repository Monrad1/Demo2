using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caching.Test;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Sekoia.Caching.Test
{
    public class ElementCacheConcurrentRefreshesAllTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ElementCacheConcurrentRefreshesAllTest([NotNull] ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        }

        [Theory]
        [AutoMoqData]
        public void TestConcurrentRefreshesArePerformedSequentially(string key)
        {
            int elementRetrieverCalls = 0;
            var mutex = new object();
            var waitHandleContinueInRetriever = new ManualResetEvent(false);
            var waitHandleInsideRetrieverMutexZone = new ManualResetEvent(false);
            var mutexZoneWasViolated = false;

            var elementCache = CacheBuilder.For<string>()
                                           .WithIdExtractor(v => v)
                                           .WithElementRetriever(v =>
                                                                 {
                                                                     return Task.Run(() =>
                                                                                     {
                                                                                         var tryEnter = Monitor.TryEnter(mutex);
                                                                                         if (!tryEnter)
                                                                                             mutexZoneWasViolated = true;

                                                                                         Assert.True(tryEnter, "Unable to get elementer retriever mutex.");
                                                                                         try
                                                                                         {
                                                                                             waitHandleInsideRetrieverMutexZone.Set();
                                                                                             waitHandleContinueInRetriever.WaitOne();

                                                                                             return Task.FromResult($"{v}-{Interlocked.Increment(ref elementRetrieverCalls)}");
                                                                                         }
                                                                                         finally
                                                                                         {
                                                                                             Monitor.Exit(mutex);
                                                                                         }
                                                                                     });
                                                                 })
                                           .ElementCache;

            var values = new List<string>();
            Exception exception = null;

            using (elementCache.Get(key)
                               .SubscribeOn(TaskPoolScheduler.Default)
                               .ObserveOn(TaskPoolScheduler.Default)
                               .DistinctUntilChanged()
                               .Subscribe(v => { values.Add(v); },
                                          e => exception = e))
            {
                waitHandleContinueInRetriever.Set();
                while (values.Count == 0)
                {
                    Thread.Sleep(100);
                }
                Assert.Single(values);
                //_testOutputHelper.WriteLine($"values: {values.Join(", ")}");

                waitHandleContinueInRetriever.Reset();
                waitHandleInsideRetrieverMutexZone.Reset();

                elementCache.RefreshAll();

                Assert.Null(exception);
                Assert.False(mutexZoneWasViolated);

                //_testOutputHelper.WriteLine($"values: {values.Join(", ")}");

                waitHandleInsideRetrieverMutexZone.WaitOne();
                waitHandleContinueInRetriever.Set();

                //_testOutputHelper.WriteLine($"values: {values.Join(", ")}");
                while (values.Count < 2 && exception == null)
                {
                    Thread.Sleep(100);
                }
                //_testOutputHelper.WriteLine($"values: {values.Join(", ")}");

                Assert.Null(exception);
                Assert.True(values.Count > 1);
                Assert.False(mutexZoneWasViolated);

                Assert.Equal($"{key}-{elementRetrieverCalls}",
                             values.Last());
            }

            //_testOutputHelper.WriteLine($"values: {values.Join(", ")}");
        }

        [Theory]
        [AutoMoqData]
        public void TestConcurrentRefreshesArePerformedSequentially_Continously(string key)
        {
            for (int i = 0; i < 100; i++)
            {
                TestConcurrentRefreshesArePerformedSequentially(key);
            }
        }
    }
}
