using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.Results;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Demo.Carter
{
    public class ErrorResponse : Response
    {
        private readonly object _errorBody;

        public ErrorResponse(HttpStatusCode statusCode, IImmutableDictionary<string, string> headers) : base(statusCode, headers) { }

        public ErrorResponse(HttpStatusCode statusCode, object errorBody,
            IImmutableDictionary<string, string> headers) : base(statusCode, headers)
        {
            _errorBody = errorBody;
        }

        internal override Task Negotiate(HttpResponse response, object model)
        {
            if (_errorBody == null)
                return base.Negotiate(response, model);

            if (_errorBody is IEnumerable<ValidationFailure> validationFailures)
                return base.Negotiate(response , new ValidationResult(validationFailures));

            return base.Negotiate(response, _errorBody);
        }
    }

    internal class ErrorResponse<T> : Response<T> where T : class
    {
        private readonly ErrorResponse _errorResponse;

        public ErrorResponse([NotNull] ErrorResponse errorResponse) 
            : base(HttpStatusCode.IMUsed, ImmutableDictionary<string, string>.Empty)
        {
            _errorResponse = errorResponse ?? throw new ArgumentNullException(nameof(errorResponse));
        }

        internal override Task Negotiate(HttpResponse response, object model)
        {
            return _errorResponse.Negotiate(response, model);
        }
    }
}
