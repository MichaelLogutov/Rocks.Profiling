using System;

namespace Rocks.Profiling.Exceptions
{
    /// <summary>
    ///     No profiling session has been started.
    /// </summary>
    public class NoCurrentSessionProfilingException : ProfilingException
    {
        public NoCurrentSessionProfilingException(Exception innerException = null)
            : base("No profiling session has been started.", innerException)
        {
        }


        public NoCurrentSessionProfilingException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}