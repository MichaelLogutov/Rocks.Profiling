using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     A default null-object implementation of IProfilerEventsHandler.
    /// </summary>
    internal class NullProfilerEventsHandler : IProfilerEventsHandler
    {
        public void OnSessionStarted(ProfileSession session)
        {
        }


        public void OnOperationStarted(ProfileOperation operation)
        {
        }


        public void OnSessionEnded(ProfileSession session)
        {
        }


        public void OnOperationEnded(ProfileOperation operation)
        {
        }
    }
}