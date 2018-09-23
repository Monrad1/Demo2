using System.Threading.Tasks;

namespace Demo.Carter
{
    internal interface IUntypedTask
    {
        Task<object> GetReultAsync(Task task);
    }
}