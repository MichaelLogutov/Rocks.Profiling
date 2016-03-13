using System;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CompletedSessionProcessorService : ICompletedSessionProcessorService
    {
        #region Private readonly fields

        private readonly ProfilerConfiguration configuration;

        #endregion

        #region Construct

        public CompletedSessionProcessorService(ProfilerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        #endregion

        #region ICompletedSessionProcessorService Members

        /// <summary>
        ///     Determines if completed session is needs to be processed.
        /// </summary>
        public bool ShouldProcess(CompletedSessionInfo completedSessionInfo)
        {
            if (completedSessionInfo == null)
                throw new ArgumentNullException(nameof(completedSessionInfo));

            var session = completedSessionInfo.Session;
            if (session.OperationsTreeRoot.IsEmpty)
                return false;

            var total_duration = GetSessionTotalDuration(session);

            if (total_duration < this.configuration.SessionMinimalDuration)
                return false;

            return true;
        }


        /// <summary>
        ///     Perform processing of completed session (like, storing the result).
        /// </summary>
        public void Process(CompletedSessionInfo completedSessionInfo)
        {
            if (completedSessionInfo == null)
                throw new ArgumentNullException(nameof(completedSessionInfo));

            throw new NotImplementedException();
        }

        #endregion

        #region Private methods

        private static TimeSpan GetSessionTotalDuration(ProfileSession session)
        {
            if (session.OperationsTreeRoot.ChildNodes == null)
                return TimeSpan.Zero;

            var total_ticks = 0L;

            foreach (var operation in session.OperationsTreeRoot.ChildNodes)
            {
                if (operation.EndTime == null)
                    continue;

                total_ticks += (operation.EndTime.Value - operation.StartTime).Ticks;
            }

            return new TimeSpan(total_ticks);
        }

        #endregion
    }
}