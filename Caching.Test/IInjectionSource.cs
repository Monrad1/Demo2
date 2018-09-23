using System.Collections.Generic;

namespace Caching.Test
{
    public interface IInjectionSource<out TItem>
    {
        IEnumerable<TItem> Items { get; }
    }
}
