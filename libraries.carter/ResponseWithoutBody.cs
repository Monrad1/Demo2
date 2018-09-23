using System.Collections.Immutable;
using System.Net;

namespace Demo.Carter
{
    internal class ResponseWithoutBody<T> : Response<T>
        where T :class
    {
        public ResponseWithoutBody(HttpStatusCode statusCode, IImmutableDictionary<string, string> headers) : base(statusCode, headers)
        {
        }
    }
}
