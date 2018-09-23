using System.Threading.Tasks;

namespace Demo.Carter
{
    internal class UntypedTask<T> : IUntypedTask
    {
        public async Task<object> GetReultAsync(Task task)
        {
            return await ((Task<T>) task).ConfigureAwait(false);
        }
    }
}
