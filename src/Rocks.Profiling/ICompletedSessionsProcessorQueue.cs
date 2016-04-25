using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling
{
    /// <summary>
    ///     A queue for the completed profile session, which will handles the processing.
    /// </summary>
    public interface ICompletedSessionsProcessorQueue
    {
        /// <summary>
        ///     Add results from completed profiling session.
        /// </summary>
        void Add([NotNull] ProfileSession session);
    }
}