using System;
using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Carter.Response;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Demo.Carter
{
    public class Response : IResponse
    {
        private readonly IImmutableDictionary<string, string> _headers;
        private readonly HttpStatusCode _statusCode;

        internal Response(HttpStatusCode statusCode, [NotNull] IImmutableDictionary<string, string> headers)
        {
            _headers = headers ?? throw new ArgumentNullException(nameof(headers));
            _statusCode = statusCode;
        }

        internal virtual Task Negotiate(HttpResponse response, object model)
        {
            foreach (var header in _headers)
                response.Headers.Add(header.Key, header.Value);

            response.StatusCode = (int)_statusCode;

            if (model is Stream)
                throw new AvoidReturningStreamsDirectlyException();

            if (model is Func<Stream> stream)
                return response.FromStream(stream(), "application/octet-stream");

            return response.Negotiate(model, response.HttpContext.RequestAborted);
        }

        Task IResponse.Negotiate(HttpResponse response, object model)
        {
            return Negotiate(response, model);
        }

        public static implicit operator Response(HttpStatusCode statusCode)
        {
            return ResponseBuilder.WithStatusCode(statusCode);
        }
    }

    public abstract class Response<T> : Response
        where T : class
    {
        internal Response(HttpStatusCode statusCode, IImmutableDictionary<string, string> headers)
            : base(statusCode, headers) { }

        public static implicit operator Response<T>(T body)
        {
            if (body is IResponseBuilder responseBuilder)
                return responseBuilder.ToResponse<T>();

            return ResponseBuilder.WithStatusCode(HttpStatusCode.OK)
                .WithBody(body)
                .ToResponse();
        }

        public static implicit operator Response<T>([NotNull] ErrorResponse errorResponse)
        {
            if (errorResponse == null) throw new ArgumentNullException(nameof(errorResponse));

            return new ErrorResponse<T>(errorResponse);
        }

        public static implicit operator Response<T>([NotNull] ErrorResponseBuilder errorResponseBuilder)
        {
            if (errorResponseBuilder == null) throw new ArgumentNullException(nameof(errorResponseBuilder));

            return errorResponseBuilder.ToResponse();
        }

        public static implicit operator Response<T>([NotNull] BodyResponseBuilder<T> errorResponseBuilder)
        {
            if (errorResponseBuilder == null) throw new ArgumentNullException(nameof(errorResponseBuilder));

            return errorResponseBuilder.ToResponse();
        }
    }
}
