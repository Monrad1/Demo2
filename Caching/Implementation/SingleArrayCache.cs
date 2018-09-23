using System;
using System.Collections.Generic;
using System.Reactive;
using JetBrains.Annotations;

namespace Sekoia.Caching.Implementation
{
    internal class SingleArrayCache<TElement, TId> : ArrayCache<TElement, TId, Unit>, ISingleArrayCache<TElement>
    {
        public SingleArrayCache([NotNull] Func<IObservable<TElement>> arrayRetriever,
                                [NotNull] Func<TElement, TId> idExtractor,
                                [NotNull] Action<TElement> elementCacher,
                                [NotNull] Func<TId, IObservable<TElement>> elementRetriever)
            : base(_ => arrayRetriever(), idExtractor, elementCacher, elementRetriever) { }

        public IObservable<IEnumerable<TElement>> Get()
        {
            return Get(Unit.Default);
        }

        public void Refresh()
        {
            Refresh(Unit.Default);
        }
    }
}
