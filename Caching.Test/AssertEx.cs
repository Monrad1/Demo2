using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sekoia.Caching.Test
{
    public static class AssertEx
    {
        public static async Task EqualAsync<T>(T expected, Func<T> actualFactory, TimeSpan? timeout = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(5);

            var sw = Stopwatch.StartNew();

            while (true)
            {
                var actual = actualFactory();

                if (Equals(expected, actual))
                    return;

                if (sw.Elapsed > timeout)
                    throw new TimeoutException($"Timeout occured waiting for actual value (latest: {actual}) to be equal to expected '{expected}'. Timeout was {timeout}.");

                await Task.Delay(TimeSpan.FromMilliseconds(100))
                          .ConfigureAwait(false);
            }
        }
    }
}
