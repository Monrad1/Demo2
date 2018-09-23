using System;
using System.Collections.Generic;

namespace Sekoia.Caching
{
    public interface IArrayCache<out TElement, in TArrayKey>
    {
        IObservable<IReadOnlyList<TElement>> Get(TArrayKey key);
        void Refresh(TArrayKey key);
        void RefreshAll();
    }
}
