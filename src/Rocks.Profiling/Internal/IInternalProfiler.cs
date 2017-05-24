using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal
{
    /// <summary>
    ///     Internal methods of the profiler.
    /// </summary>
    internal interface IInternalProfiler
    {
        /// <summary>
        ///     Profiled operation has been ended.
        /// </summary>
        void OnOperationEnded([NotNull] ProfileOperation operation);
    }
}