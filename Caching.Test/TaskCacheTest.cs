using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Caching.Test;
using JetBrains.Annotations;
using Xunit;

namespace Sekoia.Caching.Test
{
    public class TaskCacheTests
    {
        /// <summary>
        /// Modifying a key-value-pair in the cache should not affect other key-value-pairs
        /// (when eviction policys are not causing changes).
        /// </summary>
        [Fact]
        public async Task AddOrGetExisting_DifferentKeysInCacheFunctionIndependently()
        {
            const string testkey1 = "key1";
            const string testValue1 = "value1";
            int value1GeneratedTimes = 0;

            const string testkey2 = "key2";
            const string testValue2 = "value2";
            int value2GeneratedTimes = 0;


            var cache = TaskCache.Create(k =>
                                         {
                                             switch (k)
                                             {
                                                 case testkey1:
                                                     value1GeneratedTimes++;
                                                     return Task.FromResult(new TestValue(testValue1));
                                                 case testkey2:
                                                     value2GeneratedTimes++;
                                                     return Task.FromResult(new TestValue(testValue2));
                                                 default:
                                                     throw new ArgumentOutOfRangeException();
                                             }
                                         });

            var value1Get1 = await cache.AddOrGetExistingAsync(testkey1)
                                        .ConfigureAwait(false);
            var value2Get1 = await cache.AddOrGetExistingAsync(testkey2)
                                        .ConfigureAwait(false);
            var value1Get2 = await cache.AddOrGetExistingAsync(testkey1)
                                        .ConfigureAwait(false);
            var value2Get2 = await cache.AddOrGetExistingAsync(testkey2)
                                        .ConfigureAwait(false);

            Assert.True(1 == value1GeneratedTimes, "Value 1 should be built only once.");
            Assert.True(1 == value1GeneratedTimes, "Value 2 should be built only once.");

            Assert.Equal(testValue1, value1Get1.Value);
            Assert.Equal(testValue1, value1Get2.Value);
            Assert.Equal(testValue2, value2Get1.Value);
            Assert.Equal(testValue2, value2Get2.Value);

            // Invalidation should affect only the right key-value-pair.
            cache.Invalidate(testkey1);

            var value1Get3 = await cache.AddOrGetExistingAsync(testkey1)
                                        .ConfigureAwait(false);
            var value2Get3 = await cache.AddOrGetExistingAsync(testkey2)
                                        .ConfigureAwait(false);

            Assert.True(2 == value1GeneratedTimes, "Value 1 should be rebuilt.");
            Assert.True(1 == value2GeneratedTimes, "Value 2 should (still) be built only once.");

            Assert.Equal(testValue1, value1Get3.Value);
            Assert.Equal(testValue2, value2Get3.Value);
        }

        /// <summary>
        /// Test the following scenario:
        /// - Cache user A gets a Task from the cache and starts to await it.
        /// - While A is awaiting, the value is invalidated.
        /// - After the A's await is done, A should have a result that was generated after the invalidation,
        /// and not the original (now invalidated) result that A started to await in the first step. This
        /// "result swich" should be invicible to A.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AddOrGetExisting_DoesNotReturnResultsThatWereInvalidatedDuringAwait()
        {
            string key = "key";
            string earlierValue = "first";
            string laterValue = "second";

            var laterTaskStart = new ManualResetEvent(false);
            var firstValueFactoryStarted = new ManualResetEvent(false);
            var laterValueFactoryStarted = new ManualResetEvent(false);
            var firstValueFactoryContinue = new ManualResetEvent(false);
            var laterValueFactoryContinue = new ManualResetEvent(false);

            int valueFactory1Executed = 0;
            int valueFactory2Executed = 0;

            Func<Task<TestValue>> valueFactory1 = () =>
                                                  {
                                                      return Task.Factory.StartNew(() =>
                                                                                   {
                                                                                       firstValueFactoryStarted.Set();
                                                                                       firstValueFactoryContinue.WaitOne();
                                                                                       valueFactory1Executed++;
                                                                                       return new TestValue(earlierValue);
                                                                                   });
                                                  };

            Func<Task<TestValue>> valueFactory2 = () =>
                                                  {
                                                      return Task.Factory.StartNew(() =>
                                                                                   {
                                                                                       laterValueFactoryStarted.Set();
                                                                                       laterValueFactoryContinue.WaitOne();
                                                                                       valueFactory2Executed++;
                                                                                       return new TestValue(laterValue);
                                                                                   });
                                                  };


            var firstCall = true;
            var cache = TaskCache.Create(k =>
                                         {
                                             if (firstCall)
                                             {
                                                 firstCall = false;
                                                 return valueFactory1();
                                             }

                                             return valueFactory2();
                                         });

            var cacheUserTask1 =
                Task.Factory.StartNew(async () => await cache.AddOrGetExistingAsync(key)
                                                             .ConfigureAwait(false));

            var cacheUserTask2 = Task.Factory.StartNew(async () =>
                                                       {
                                                           laterTaskStart.WaitOne();
                                                           return await cache.AddOrGetExistingAsync(key)
                                                                             .ConfigureAwait(false);
                                                       });

            // Wait until the first value get from cache is in the middle of the value generation.
            // At this point, a Task that is running but not completed has been added to the cache.
            // CacheUserTask1 is awaiting for the Task to complete.
            firstValueFactoryStarted.WaitOne();

            // While the first value get is still running, invalidate the value.
            cache.Invalidate(key);

            // Second get from the cache can now begin.
            // Because the first (still uncompleted) value was invalidated, cacheUserTask2's fetch should start a new value generation.
            laterTaskStart.Set();

            // New value generation has started but not yet completed.
            laterValueFactoryStarted.WaitOne();

            // Let first value generation run to completion.
            firstValueFactoryContinue.Set();

            // Let second value generation run to completion.
            laterValueFactoryContinue.Set();

            await Task.WhenAll(new List<Task>
                               {
                                   cacheUserTask1,
                                   cacheUserTask2
                               })
                      .ConfigureAwait(false);


            Assert.True(laterValue == cacheUserTask1.Result.Result.Value,
                        "The first fetch from the cache should have returned the value generated by the second fetch, because the first value was invalidated while still running.");
            Assert.True(laterValue == cacheUserTask2.Result.Result.Value,
                        "The second fetch should have returned the later value.");

            Assert.True(1 == valueFactory1Executed, "The first valueFactory should have been called once.");
            Assert.True(1 == valueFactory2Executed, "The second valueFactory should have been called once.");
        }

        [Fact]
        public async Task AddOrGetExisting_ExceptionsFromValueGenerationCanBeHandled()
        {
            string testkey = "key";
            string testValue = "value";
            string testExceptionMessage = "this is exception";

            int valueFactoryCalledTimes = 0;
            Func<Task<TestValue>> valueFactory = () =>
                                                 {
                                                     valueFactoryCalledTimes++;
                                                     return Task.Factory.StartNew(() =>
                                                                                  {
                                                                                      Thread.Sleep(10);
                                                                                      if (valueFactoryCalledTimes != -9999)
                                                                                      {
                                                                                          // Throw always
                                                                                          throw new Exception(
                                                                                                              testExceptionMessage);
                                                                                      }
                                                                                      return new TestValue(testValue);
                                                                                  });
                                                 };

            Exception exception;


            var cache = TaskCache.Create(async k =>
                                         {
                                             var res = await valueFactory()
                                                           .ConfigureAwait(false);
                                             return res;
                                         });

            // Use the cache.
            try
            {
                await cache.AddOrGetExistingAsync(testkey)
                           .ConfigureAwait(false);
                throw new AssertException("This point should never be reached, because the valueFactory should always throw.");
            }
            catch (Exception ex) when (!(ex is AssertException))
            {
                exception = ex;
            }

            Assert.Equal(testExceptionMessage, exception.Message);

            // Use the cache again.
            try
            {
                await cache.AddOrGetExistingAsync(testkey)
                           .ConfigureAwait(false);
                throw new AssertException("This point should never be reached, because the valueFactory should always throw.");
            }
            catch (Exception ex) when (!(ex is AssertException)) { }

            Assert.Equal(2, valueFactoryCalledTimes);
        }

        [Fact]
        public async Task AddOrGetExisting_FailedTasksAreNotPersisted()
        {
            string testValue = "value1";
            string testkey = "key1";
            string exceptionMessage = "First two calls will fail.";

            int valueGeneratedTimes = 0;
            Func<Task<TestValue>> valueFactory = () =>
                                                 {
                                                     valueGeneratedTimes++;

                                                     return Task.Factory.StartNew(() =>
                                                                                  {
                                                                                      if (valueGeneratedTimes <= 2)
                                                                                      {
                                                                                          throw new Exception(
                                                                                                              exceptionMessage);
                                                                                      }
                                                                                      return new TestValue(testValue);
                                                                                  });
                                                 };

            var cache = TaskCache.Create(k => valueFactory());


            var cacheTask = cache.AddOrGetExistingAsync(testkey);
            await SilentlyHandleFaultingTaskAsync(cacheTask, exceptionMessage)
                .ConfigureAwait(false);
            Assert.True(cacheTask.IsFaulted, "First value generation should fail.");
            Assert.True(1 == valueGeneratedTimes, "Value should be build 1 times.");

            cacheTask = cache.AddOrGetExistingAsync(testkey);
            await SilentlyHandleFaultingTaskAsync(cacheTask, exceptionMessage)
                .ConfigureAwait(false);
            Assert.True(cacheTask.IsFaulted, "Second value generation should fail.");
            Assert.True(2 == valueGeneratedTimes, "Value should be build 2 times, because first failed.");

            cacheTask = cache.AddOrGetExistingAsync(testkey);
            var cacheValue = await cacheTask.ConfigureAwait(false);
            Assert.True(cacheTask.IsCompleted, "Value generation should succeed the third time.");
            Assert.True(3 == valueGeneratedTimes, "Value should be build 3 times, because first two times failed.");
            Assert.True(testValue == cacheValue.Value, "Cache should return correct value.");

            cacheTask = cache.AddOrGetExistingAsync(testkey);
            cacheValue = await cacheTask.ConfigureAwait(false);
            Assert.True(cacheTask.IsCompleted, "Value generation should succeed the fourth time.");
            Assert.True(3 == valueGeneratedTimes,
                        "Value should be build 3 times, because first two times failed, but third succeeded.");
            Assert.True(testValue == cacheValue.Value, "Cache should return correct value.");
        }

        /// <summary>
        /// Test that for subsequent calls the value is only generated once, and after that
        /// the same generated value is returned.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AddOrGetExisting_GeneratesTheValueOnlyOnce()
        {
            string testValue = "value1";
            string testkey = "key1";
            int valueGeneratedTimes = 0;
            Func<Task<TestValue>> valueFactory = () =>
                                                 {
                                                     valueGeneratedTimes++;
                                                     return Task.FromResult(new TestValue(testValue));
                                                 };

            var cache = TaskCache.Create(k => valueFactory());

            var value1 = await cache.AddOrGetExistingAsync(testkey)
                                    .ConfigureAwait(false);
            Assert.True(testValue == value1.Value);

            var value2 = await cache.AddOrGetExistingAsync(testkey)
                                    .ConfigureAwait(false);
            Assert.True(testValue == value2.Value);

            var value3 = await cache.AddOrGetExistingAsync(testkey)
                                    .ConfigureAwait(false);
            Assert.True(testValue == value3.Value);

            Assert.True(1 == valueGeneratedTimes, "Value should be generated only once.");
        }

        /// <summary>
        /// Test that the cache returns the value that the valueFactory function generates.
        /// </summary>
        [Fact]
        public async Task AddOrGetExisting_ReturnsValueFromValueFactory()
        {
            string testValue = "value1";
            Func<Task<TestValue>> valueFactory = () => Task.FromResult(new TestValue(testValue));

            var cache = TaskCache.Create(k => valueFactory());

            var value = await cache.AddOrGetExistingAsync("key1")
                                   .ConfigureAwait(false);

            Assert.True(testValue == value.Value);
        }

        [Fact]
        public async Task AddOrGetExisting_ValueIsRebuildAfterInvalidation()
        {
            string testValue = "value1";
            string testkey = "key1";
            int valueGeneratedTimes = 0;
            Func<Task<TestValue>> valueFactory = () =>
                                                 {
                                                     valueGeneratedTimes++;
                                                     return Task.FromResult(new TestValue(testValue));
                                                 };

            var cache = TaskCache.Create(k => valueFactory());

            var value1 = await cache.AddOrGetExistingAsync(testkey)
                                    .ConfigureAwait(false);
            Assert.True(testValue == value1.Value);

            var value2 = await cache.AddOrGetExistingAsync(testkey)
                                    .ConfigureAwait(false);
            Assert.True(testValue == value2.Value);

            Assert.True(1 == valueGeneratedTimes, "Value should be generated only once.");

            cache.Invalidate(testkey);

            var value3 = await cache.AddOrGetExistingAsync(testkey)
                                    .ConfigureAwait(false);
            Assert.True(testValue == value3.Value);

            Assert.True(2 == valueGeneratedTimes, "Value should be regenerated after invalidation.");
        }

        [Fact]
        public async Task Contains_ReturnsTrueWhenKeyExists()
        {
            string testkey1 = "key1";
            string testkey2 = "key2";

            Func<Task<TestValue>> valueFactory = () => Task.FromResult(new TestValue("test"));

            var cache = TaskCache.Create(k => valueFactory());

            Assert.False(cache.Contains(testkey1));
            Assert.False(cache.Contains(testkey2));

            await cache.AddOrGetExistingAsync(testkey1)
                       .ConfigureAwait(false);

            Assert.True(cache.Contains(testkey1));
            Assert.False(cache.Contains(testkey2));
        }

        [AssertionMethod]
        private static async Task SilentlyHandleFaultingTaskAsync(Task task, string expectedExceptionMessage)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.Equal(expectedExceptionMessage, ex.Message);
            }
        }

        private class TestValue
        {
            public string Value { get; }

            public TestValue(string value)
            {
                Value = value;
            }
        }
    }
}
