using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caching.Test
{
    public static class Extensions
    {
        public static string Join<T>(this IEnumerable<T> sequece, string separator)
        {
            if (sequece == null)
                throw new ArgumentNullException(nameof(sequece));

            return string.Join(separator, sequece);
        }

        // ReSharper disable once ConsiderUsingAsyncSuffix
        /// <summary>
        /// Wraps the specified value in a Task (litarally returns Task.FromResult(value)).
        /// </summary>
        public static Task<T> InTask<T>(this T value)
        {
            return Task.FromResult(value);
        }
    }
}
