using JetBrains.Annotations;
using Rocks.Profiling.Data;

namespace Rocks.Profiling.Storage
{
    /// <summary>
    ///     Methods for storing the results of the profiling.
    /// </summary>
    public interface IProfilerResultsStorage
    {
        /// <summary>
        ///     Adds new profile <paramref name="result"/> to the storage.
        /// </summary>
        void Add([NotNull] ProfileResult result);
    }
}