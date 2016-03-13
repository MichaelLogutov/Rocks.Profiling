using System;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class ProfilingResultsProcessor : IProfilingResultsProcessor
    {
        /// <summary>
        ///     Add results from completed profiling session.
        /// </summary>
        public void Add(ProfileSession session, object additionalSessionData)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            //throw new NotImplementedException();
        }
    }
}