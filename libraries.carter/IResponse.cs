using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Demo.Carter
{
    internal interface IResponse
    {
        Task Negotiate(HttpResponse response, object model);
    }
}