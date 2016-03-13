using System;

namespace Rocks.Profiling.Exceptions
{
    /// <summary>
    ///     An error occurred during profiling.
    /// </summary>
    [Serializable]
    public class ProfilingException : Exception
    {
        public ProfilingException(Exception innerException = null)
            : base("An error occurred during profiling.", innerException)
        {
        }


        public ProfilingException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}