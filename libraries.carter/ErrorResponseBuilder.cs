using System.Collections.Immutable;
using System.Net;
using JetBrains.Annotations;

namespace Demo.Carter
{
    public class ErrorResponseBuilder : IResponseBuilder
    {
        private readonly object _errorBody;
        private readonly IImmutableDictionary<string, string> _headers;
        private readonly HttpStatusCode _statusCode;

        public ErrorResponseBuilder(HttpStatusCode statusCode, [NotNull] IImmutableDictionary<string, string> headers, object errorBody = null)
        {
            _statusCode = statusCode;
            _errorBody = errorBody;
            _headers = headers ?? ImmutableDictionary<string, string>.Empty;
        }
        public ErrorResponse ToResponse()
        {
            return new ErrorResponse(_statusCode, _errorBody, _headers);
        }

        public ErrorResponseBuilder WithErrorBody(object errorBody)
        {
            return new ErrorResponseBuilder(_statusCode, _headers, errorBody);
        }

        public ErrorResponseBuilder WithHeader(string key, string value)
        {
            return new ErrorResponseBuilder(_statusCode, _headers.SetItem(key, value));
        }

        Response<T> IResponseBuilder.ToResponse<T>()
        {
            return new ErrorResponse<T>(ToResponse());
        }

        Response IResponseBuilder.ToResponse()
        {
            return ToResponse();
        }
    }
}
