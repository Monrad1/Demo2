using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Caching.Test
{
    public class InjectionSourceAttribute : Attribute
    {
        public Type SourceType { get; }

        public InjectionSourceAttribute([NotNull] Type sourceType)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType));

            SourceType = sourceType;
            CreateReplacer();
        }

        public IEnumerable<IEnumerable<object>> GetData(IEnumerable<IEnumerable<object>> parmss)
        {
            return CreateReplacer().GetData(parmss);
        }

        private IReplacer CreateReplacer()
        {
            if (SourceType.GetConstructor(Type.EmptyTypes) == null)
                throw new ArgumentException("InjectionSourceAttribute.SourceType '" + SourceType + "' does not have a default constructor.");

            var replacerType = typeof(Replacer<,>);

            var injectionSourceInterface = SourceType.GetInterfaces()
                                                     .Where(i => i.IsGenericType)
                                                     .Where(i => i.GetGenericTypeDefinition() == typeof(IInjectionSource<>))
                                                     .FirstOrDefault();
            if (injectionSourceInterface == null)
                throw new ArgumentException("InjectionSourceAttribute.SourceType '" + SourceType + "' does not implement '" + typeof(IInjectionSource<>) + "'.");

            var itemType = injectionSourceInterface.GetGenericArguments()[0];
            replacerType = replacerType.MakeGenericType(SourceType, itemType);

            return (IReplacer)Activator.CreateInstance(replacerType);
        }

        private class Replacer<TInjectionSource, TItem> : IReplacer
            where TInjectionSource : IInjectionSource<TItem>, new()
        {
            public IEnumerable<IEnumerable<object>> GetData(IEnumerable<IEnumerable<object>> parmss)
            {
                return from parms in parmss
                       from factory in GetItems()
                       select ReplaceValue(parms, factory).ToArray();
            }

            private static IEnumerable<TItem> GetItems()
            {
                return new TInjectionSource().Items;
            }

            private static IEnumerable<object> ReplaceValue(IEnumerable<object> parms, TItem value)
            {
                foreach (var parm in parms)
                {
                    if (parm is TItem)
                        yield return value;
                    else
                        yield return parm;
                }
            }
        }

        private interface IReplacer
        {
            IEnumerable<IEnumerable<object>> GetData(IEnumerable<IEnumerable<object>> parmss);
        }
    }
}
