using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sekoia.Caching
{
    /// <summary>
    /// http://blogs.msdn.com/b/pfxteam/archive/2011/01/15/asynclazy-lt-t-gt.aspx
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class AsyncLazy<T> : Lazy<Task<T>>
    {
        public AsyncLazy(Func<T> valueFactory)
            : base(() => Task.Factory.StartNew(valueFactory)) { }

        public AsyncLazy(Func<Task<T>> taskFactory)
            : base(() => Task.Factory
                             .StartNew(taskFactory)
                             .Unwrap()) { }

        public TaskAwaiter<T> GetAwaiter()
        {
            return Value.GetAwaiter();
        }
    }
}
