using System.Net;

namespace Demo.Carter
{
    public class ResponseBuilder
    {
        public static ErrorResponseBuilder BadRequest => WithStatusCode(HttpStatusCode.BadRequest);
        public static ResponseBuilderWithoutBody Ok => WithStatusCode(HttpStatusCode.OK);
        public static ResponseBuilderWithoutBody NoContent => WithStatusCode(HttpStatusCode.NoContent);
        public static ErrorResponseBuilder NotFound => WithStatusCode(HttpStatusCode.NotFound);

        public static ResponseBuilderWithoutBody WithStatusCode(HttpStatusCode statusCode)
        {
            return new ResponseBuilderWithoutBody(statusCode, null);
        }
    }
}