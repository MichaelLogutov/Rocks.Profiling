using System;

namespace Rocks.Profiling.Loggers
{
    /// <summary>
    ///     A profiler loger that logs to console.
    /// </summary>
    public class ConsoleProfilerLogger : IProfilerLogger
    {
        /// <summary>
        ///     Will be called on warnings during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        public void LogWarning(string message, Exception ex = null)
        {
            if (ex != null)
                Console.WriteLine($"[WARN] {message}: {ex}\n\n");
            else
                Console.WriteLine($"[WARN] {message}\n\n");
        }


        /// <summary>
        ///     Will be called on unhandled exceptions during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        public void LogError(Exception ex)
        {
            Console.WriteLine($"[ERROR] \n{ex}\n\n");
        }
    }
}