using System;

namespace Rocks.Profiling.Loggers
{
    /// <summary>
    ///     A profiler loger that rethrows passed exception.
    /// </summary>
    public class RethrowProfilerLogger : IProfilerLogger
    {
        /// <summary>
        ///     Will be called on unhandled exceptions during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        /// <exception cref="Exception">Always thrown.</exception>
        public void LogError(Exception ex)
        {
            // ReSharper disable once ThrowingSystemException
            throw ex;
        }
    }
}