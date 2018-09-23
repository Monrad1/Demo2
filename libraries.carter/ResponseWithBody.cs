using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;
using Carter.Response;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Demo.Carter
{
    public class ResponseWithBody<T> : Response<T>
        where T : class 
    {
        private readonly string _contentType;
        private readonly T _body;

        internal ResponseWithBody(HttpStatusCode statusCode, [NotNull] T body, string contentType, IImmutableDictionary<string, string> headers)
            : base(statusCode, headers)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _contentType = contentType;
        }

        internal override Task Negotiate(HttpResponse response, object model)
        {
            if (_body is HttpResponse bodyAsHttpResponse)
                return bodyAsHttpResponse.Negotiate(bodyAsHttpResponse);

            if (!string.IsNullOrWhiteSpace(_contentType))
                response.ContentType = _contentType;

            var task = base.Negotiate(response, _body);

            return task;
        }
    }
}
