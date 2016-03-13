using JetBrains.Annotations;
using Rocks.Profiling.Internal.Implementation;

namespace Rocks.Profiling.Internal
{
    /// <summary>
    ///     Consumes the results of the profiling and sends them to storage if needed.
    /// </summary>
    internal interface IProfilingResultsProcessor
    {
        /// <summary>
        ///     Add results from completed profiling session.
        /// </summary>
        void Add([NotNull] ProfileSession session, [CanBeNull] object additionalSessionData);
    }
}