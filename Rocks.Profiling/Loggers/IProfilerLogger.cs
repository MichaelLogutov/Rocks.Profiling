using System;
using JetBrains.Annotations;

namespace Rocks.Profiling.Loggers
{
    /// <summary>
    ///     Methods for logging events during profiling.
    /// </summary>
    public interface IProfilerLogger
    {
        /// <summary>
        ///     Will be called on unhandled exceptions during profiling.<br />
        ///     The implementation must be thread safe.
        /// </summary>
        void LogError([NotNull] Exception ex);
    }
}