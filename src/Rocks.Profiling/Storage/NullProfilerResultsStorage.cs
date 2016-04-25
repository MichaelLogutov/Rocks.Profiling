using System.Collections.Generic;
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
        ///     Adds new profile <paramref name="sessions"/> to the storage.
        /// </summary>
        public Task AddAsync(IReadOnlyList<ProfileSession> sessions, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }
    }
}