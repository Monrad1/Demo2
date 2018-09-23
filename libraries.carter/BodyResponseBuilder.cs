using System;
using System.Collections.Immutable;
using System.Net;
using JetBrains.Annotations;

namespace Demo.Carter
{
    public class BodyResponseBuilder<T> : IResponseBuilder<T>
        where T : class 
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _contentType;
        private readonly IImmutableDictionary<string, string> _headers;
        private readonly T _body;

        public BodyResponseBuilder(HttpStatusCode statusCode, [NotNull] T body, string contentType, IImmutableDictionary<string, string> headers)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _statusCode = statusCode;
            _contentType = contentType;
            _headers = headers ?? ImmutableDictionary<string, string>.Empty;
        }

        public Response<T> ToResponse()
        {
            return new ResponseWithBody<T>(_statusCode, _body, _contentType, _headers);
        }

        public BodyResponseBuilder<T> WithContentType(string contentType) 
        {
            return new BodyResponseBuilder<T>(_statusCode, _body, contentType, _headers);
        }

        public BodyResponseBuilder<T> WithHeader(string key, string value)
        {
            return new BodyResponseBuilder<T>(_statusCode, _body, _contentType, _headers.SetItem(key, value));
        }

        Response<T1> IResponseBuilder.ToResponse<T1>()
        {
            return new ResponseWithBody<T1>(_statusCode, (T1)(object)(_body), _contentType, _headers);
        }

        Response IResponseBuilder.ToResponse()
        {
            return ((IResponseBuilder<T>) this).ToResponse();
        }
    }
}
