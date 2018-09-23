using System;

namespace Demo.Carter.Attributes
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class DoNotValidateAttribute : Attribute
    {
    }
}
