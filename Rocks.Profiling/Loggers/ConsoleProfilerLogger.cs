using System;

namespace Rocks.Profiling.Loggers
{
    /// <summary>
    ///     A profiler loger that logs to console.
    /// </summary>
    public class ConsoleProfilerLogger : IProfilerLogger
    {
        /// <summary>
        ///     Will be called on unhandled exceptions during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        public void LogError(Exception ex)
        {
            Console.WriteLine("\n" + ex + "\n\n");
        }
    }
}