using System;
using System.Collections.Generic;

namespace Sekoia.Caching
{
    public interface ISingleArrayCache<out TElement>
    {
        IObservable<IEnumerable<TElement>> Get();
        void Refresh();
    }
}