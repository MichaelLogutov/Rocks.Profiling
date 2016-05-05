using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class NullCompletedSessionProcessingFilter : ICompletedSessionProcessingFilter
    {
        /// <summary>
        ///     Determines if completed session is needs to be processed.<br />
        ///     The implementation of this method should be thread safe.<br />
        ///     If <see langword="null" /> returned - the default filtering sequence will be performed.
        /// </summary>
        public bool? ShouldProcess(ProfileSession session) => null;
    }
}