using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     A default null-object implementation of IProfilerEventsHandler.
    /// </summary>
    internal class NullProfilerEventsHandler : IProfilerEventsHandler
    {
        public void OnSessionEnded(ProfileSession session)
        {
        }


        public void OnOperationEnded(ProfileOperation operation)
        {
        }
    }
}