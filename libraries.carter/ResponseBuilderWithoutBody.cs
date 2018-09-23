using System;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using JetBrains.Annotations;

namespace Demo.Carter
{
    public class ResponseBuilderWithoutBody : IResponseBuilder
    {
        private readonly IImmutableDictionary<string, string> _headers;
        private readonly HttpStatusCode _statusCode;

        public ResponseBuilderWithoutBody(HttpStatusCode statusCode, IImmutableDictionary<string, string> headers)
        {
            _statusCode = statusCode;
            _headers = headers;
        }

        public ErrorResponseBuilder AsError()
        {
            return new ErrorResponseBuilder(_statusCode, _headers);
        }

        public Response ToResponse()
        {
            return new Response(_statusCode, _headers);
        }

        public BodyResponseBuilder<T> WithBody<T>(T body)
            where T : class 
        {
            return new BodyResponseBuilder<T>(_statusCode, body, null, null);
        }

        public ErrorResponseBuilder WithErrorBody(object errorBody)
        {
            return new ErrorResponseBuilder(_statusCode, _headers, errorBody);
        }

        public ResponseBuilderWithoutBody WithHeader(string key, string value)
        {
            return new ResponseBuilderWithoutBody(_statusCode, _headers.SetItem(key, value));
        }

        public BodyResponseBuilder<Func<Stream>> WithStreamBody<T>(Func<T> body) where T : Stream
        {
            return new BodyResponseBuilder<Func<Stream>>(_statusCode, body, null, _headers);
        }

        Response<T> IResponseBuilder.ToResponse<T>()
        {
            return new ResponseWithoutBody<T>(_statusCode, _headers);
        }

        public static implicit operator Response([NotNull] ResponseBuilderWithoutBody responseBuilder)
        {
            if (responseBuilder == null) throw new ArgumentNullException(nameof(responseBuilder));

            return responseBuilder.ToResponse();
        }

        public static implicit operator ErrorResponseBuilder([NotNull] ResponseBuilderWithoutBody responseBuilder)
        {
            if (responseBuilder == null) throw new ArgumentNullException(nameof(responseBuilder));

            return responseBuilder.AsError();
        }
    }
}
