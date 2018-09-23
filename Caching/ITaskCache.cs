using System.Threading.Tasks;

namespace Sekoia.Caching
{
    public interface ITaskCache<T>
    {
        /// <summary>
        /// Return from the cache the value for the given key. If value is already present in cache,
        /// that value will be returned. Otherwise value is first generated.
        ///
        /// Return value can be a completed or running task-object. If the task-object is completed,
        /// it has run succesfully to completion. 
        /// If the cache contains a task that will end up throwing an exception in the future, the same
        /// task instance is returned to all the callers of this method. This means that any given
        /// caller of this method should anticipate the type of exceptions that could be thrown from
        /// the updateFunc used by any of the caller of this method.
        /// </summary>
        /// <param name="key">Key that matches the wanted return value.</param>
        /// <returns>Returned task-object can be completed or running. Note that the task might result in exception.</returns>
        Task<T> AddOrGetExistingAsync(string key);

        /// <summary>
        /// Empties the cache from all entries.
        /// </summary>
        void Clear();

        /// <summary>
        /// Does the cache alrealy contain a value for the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool Contains(string key);

        /// <summary>
        /// Invalidate the value for the given key, if value exists.
        /// </summary>
        /// <param name="key"></param>
        void Invalidate(string key);
    }
}
