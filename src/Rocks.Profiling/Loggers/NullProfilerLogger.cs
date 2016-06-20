using System;

namespace Rocks.Profiling.Loggers
{
    /// <summary>
    ///     A profiler loger that does no logging.
    /// </summary>
    public class NullProfilerLogger : IProfilerLogger
    {
        /// <summary>
        ///     Will be called on warnings during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        public void LogWarning(string message, Exception ex = null)
        {
        }


        /// <summary>
        ///     Will be called on unhandled exceptions during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        public void LogError(Exception ex)
        {
        }
    }
}