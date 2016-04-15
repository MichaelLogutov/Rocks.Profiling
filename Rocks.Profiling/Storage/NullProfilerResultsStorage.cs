using System.Threading;
using System.Threading.Tasks;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Storage
{
    /// <summary>
    ///     Represents dummy storage that discards all results.
    /// </summary>
    public class NullProfilerResultsStorage : IProfilerResultsStorage
    {
        /// <summary>
        ///     Adds new profile <paramref name="session"/> to the storage.
        /// </summary>
        public Task AddAsync(ProfileSession session, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}