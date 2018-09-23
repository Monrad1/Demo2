using System;
using System.Collections.Generic;
using System.Text;

namespace Demo.Carter.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PostAttribute : Attribute, IHttpMethodAttribute
    {
        public string Metohd => HttpMethods.Post;
        public string Path { get; }

        public PostAttribute(string path = null)
        {
            Path = path;
        }
    }
}
