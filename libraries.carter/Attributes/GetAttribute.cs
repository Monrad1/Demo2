using System;

namespace Demo.Carter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class GetAttribute : Attribute, IHttpMethodAttribute
    {
        public string Metohd => HttpMethods.Get;
        public string Path { get; }

        public GetAttribute(string path = null)
        {
            Path = path;
        }
    }
}
