using System;
using JetBrains.Annotations;

namespace Sekoia.Caching
{
    public interface IElementCache<out TElement, in TId>
        where TElement : class
    {
        IObservable<TElement> Get([NotNull] TId id);
        void Refresh([NotNull] TId id);
        void RefreshAll();
        void Remove([NotNull] TId id);
    }
}
