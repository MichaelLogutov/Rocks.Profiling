using System;

namespace Rocks.Profiling.Exceptions
{
    /// <summary>
    ///     Operations are out of order.
    /// </summary>
    public class OperationsOutOfOrderProfillingException : ProfilingException
    {
        public OperationsOutOfOrderProfillingException(Exception innerException = null)
            : base("Operations are out of order.", innerException)
        {
        }


        public OperationsOutOfOrderProfillingException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}