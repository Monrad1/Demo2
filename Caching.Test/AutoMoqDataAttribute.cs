using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using AutoFixture.Xunit2;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Caching.Test
{
    public class AutoMoqDataAttribute : AutoDataAttribute
    {
        public AutoMoqDataAttribute(bool useStrictMocks = false)
            : base(() => CreateFixture(useStrictMocks)) { }


        public override IEnumerable<object[]> GetData(MethodInfo methodUnderTest)
        {
            IEnumerable<IEnumerable<object>> parmss = base.GetData(methodUnderTest)
                                                          .ToArray();

            var injectedParmss = methodUnderTest.GetCustomAttributes<InjectionSourceAttribute>(false)
                                                .Aggregate(parmss, (pss, a) => a.GetData(parmss));

            return injectedParmss.Select(l => l.ToArray())
                                 .ToArray();
        }

        private static IFixture CreateFixture(bool useStrictMocks)
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization())
                                       .Customize(new StictMockCreator(useStrictMocks));
            return fixture;
        }

        private class StictMockCreator : ICustomization, ISpecimenBuilder
        {
            private readonly bool _allMocksAreStrict;

            public StictMockCreator(bool allMocksAreStrict)
            {
                _allMocksAreStrict = allMocksAreStrict;
            }

            public object Create(object request, ISpecimenContext context)
            {
                var orgRequest = request;

                bool createStrictMock = _allMocksAreStrict;

                var parameterInfo = request as ParameterInfo;
                if (parameterInfo != null)
                {
                    request = parameterInfo.ParameterType;
                    createStrictMock = parameterInfo.GetCustomAttributes(typeof(StrictAttribute))
                                                    .Any();
                }

                if (createStrictMock)
                {
                    var type = request as Type;
                    if (type != null && typeof(Mock).IsAssignableFrom(type))
                        return Activator.CreateInstance(type, MockBehavior.Strict);
                }

                return new NoSpecimen();
            }

            public void Customize(IFixture fixture)
            {
                fixture.Customizations.Insert(0, this);
            }
        }
    }
}
