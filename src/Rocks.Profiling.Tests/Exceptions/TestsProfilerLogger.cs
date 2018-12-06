using System;
using Rocks.Profiling.Loggers;

namespace Rocks.Profiling.Tests.Exceptions
{
    /// <summary>
    ///     A profiler loger that rethrows passed error exception.
    /// </summary>
    public class TestsProfilerLogger : IProfilerLogger
    {
        /// <summary>
        ///     Will be called on warnings during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        public virtual void LogWarning(string message, Exception ex = null)
        {
            Console.WriteLine("WARNING: {0}\n{1}", message, ex);
        }


        /// <summary>
        ///     Will be called on unhandled exceptions during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        /// <exception cref="Exception">Always thrown.</exception>
        public virtual void LogError(Exception ex)
        {
            if (ex is ValidTestException)
                return;

            throw ex;
        }
    }
}