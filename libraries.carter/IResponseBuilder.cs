namespace Demo.Carter
{
    internal interface IResponseBuilder
    {
        Response ToResponse();

        Response<T> ToResponse<T>()
            where T : class;
    }

    internal interface IResponseBuilder<T> : IResponseBuilder
        where T : class
    {
        new Response<T> ToResponse();
    }
}