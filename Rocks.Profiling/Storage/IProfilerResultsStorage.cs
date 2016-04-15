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
        ///     Adds new profile <paramref name="session"/> to the storage.
        /// </summary>
        Task AddAsync([NotNull] ProfileSession session, CancellationToken cancellationToken = default(CancellationToken));
    }
}