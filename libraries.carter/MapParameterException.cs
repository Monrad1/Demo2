using System;
using System.Reflection;

namespace Demo.Carter
{
    internal class MapParameterException : Exception
    {
        public ParameterInfo ParameterInfo { get; }

        public MapParameterException(ParameterInfo parameterInfo)
        {
            ParameterInfo = parameterInfo;
        }
    }
}