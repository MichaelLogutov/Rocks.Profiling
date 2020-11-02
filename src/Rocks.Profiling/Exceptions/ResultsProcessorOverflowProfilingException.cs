using System;

namespace Rocks.Profiling.Exceptions
{
    /// <summary>
    ///     Results processor has reached the limit of incoming buffer. Session result will be discarded.
    /// </summary>
    public class ResultsProcessorOverflowProfilingException : ProfilingException
    {
        public ResultsProcessorOverflowProfilingException(Exception innerException = null)
            : base("Results processor has reached the limit of incoming buffer. Session result will be discarded.", innerException)
        {
        }


        public ResultsProcessorOverflowProfilingException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}