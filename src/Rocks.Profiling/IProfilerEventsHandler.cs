using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling
{
    /// <summary>
    ///     Represents a service that can globaly handle different profiler events.
    ///     There can be multiple implementation of this interface registered in DI container and all will be called.
    /// </summary>
    public interface IProfilerEventsHandler
    {
        /// <summary>
        ///     Signals the end of the profiled session.
        /// </summary>
        /// <param name="session"></param>
        void OnSessionEnded([NotNull] ProfileSession session);


        /// <summary>
        ///     Signals the end of the profiled operation.
        /// </summary>
        void OnOperationEnded([NotNull] ProfileOperation operation);
    }
}