using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal
{
    /// <summary>
    ///     Handles the routines used for processing completed profile sessions.
    /// </summary>
    internal interface ICompletedSessionProcessorService
    {
        /// <summary>
        ///     Determines if completed session is needs to be processed.
        /// </summary>
        bool ShouldProcess([NotNull] ProfileSession session);


        /// <summary>
        ///     Perform processing of completed session (like, storing the result).
        /// </summary>
        void Process([NotNull] ProfileSession session);
    }
}