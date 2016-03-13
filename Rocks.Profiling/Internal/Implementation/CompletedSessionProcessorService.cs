using System;
using JetBrains.Annotations;
using Rocks.Profiling.Data;
using Rocks.Profiling.Storage;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CompletedSessionProcessorService : ICompletedSessionProcessorService
    {
        #region Private readonly fields

        private readonly ProfilerConfiguration configuration;
        private readonly IProfilerResultsStorage resultsStorage;

        #endregion

        #region Construct

        public CompletedSessionProcessorService([NotNull] ProfilerConfiguration configuration,
                                                [NotNull] IProfilerResultsStorage resultsStorage)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (resultsStorage == null)
                throw new ArgumentNullException(nameof(resultsStorage));

            this.configuration = configuration;
            this.resultsStorage = resultsStorage;
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

            var total_duration = session.GetTotalDuration();

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

            var result = new ProfileResult(completedSessionInfo);

            this.resultsStorage.Add(result);
        }

        #endregion
    }
}