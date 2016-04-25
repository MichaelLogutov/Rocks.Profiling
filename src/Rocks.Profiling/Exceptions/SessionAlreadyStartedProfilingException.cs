using System;

namespace Rocks.Profiling.Exceptions
{
    /// <summary>
    ///     Profiling session has been already started.
    /// </summary>
    [Serializable]
    public class SessionAlreadyStartedProfilingException : ProfilingException
    {
        public SessionAlreadyStartedProfilingException(Exception innerException = null)
            : base("Profiling session has been already started.", innerException)
        {
        }


        public SessionAlreadyStartedProfilingException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}