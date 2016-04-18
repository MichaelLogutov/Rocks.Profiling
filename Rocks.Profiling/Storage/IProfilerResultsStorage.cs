using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Storage
{
    /// <summary>
    ///     Methods for storing the results of the profiling.
    /// </summary>
    public interface IProfilerResultsStorage
    {
        /// <summary>
        ///     Adds new profile <paramref name="sessions"/> to the storage.
        /// </summary>
        Task AddAsync([NotNull] IReadOnlyList<ProfileSession> sessions,
                      CancellationToken cancellationToken = default(CancellationToken));
    }
}