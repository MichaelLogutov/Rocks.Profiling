using System;

namespace Rocks.Profiling.Exceptions
{
    /// <summary>
    ///     The operation is from another session.
    /// </summary>
    public class OperationFromAnotherSessionProfilingException : ProfilingException
    {
        public OperationFromAnotherSessionProfilingException(Exception innerException = null)
            : base("The operation is from another session.", innerException)
        {
        }


        public OperationFromAnotherSessionProfilingException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}