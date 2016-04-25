using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class NullCompletedSessionProcessingFilter : ICompletedSessionProcessingFilter
    {
        /// <summary>
        ///     Determines if completed session is needs to be processed.
        /// </summary>
        public bool ShouldProcess(ProfileSession session)
        {
            return true;
        }
    }
}