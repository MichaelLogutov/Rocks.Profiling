using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Models;
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
        public bool ShouldProcess(ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (session.HasOperationLongerThanNormal)
                return true;

            if (session.OperationsTreeRoot.IsEmpty)
                return false;

            if (session.Duration < this.configuration.SessionMinimalDuration)
                return false;

            return true;
        }


        /// <summary>
        ///     Perform processing of completed session (like, storing the result).
        /// </summary>
        public Task ProcessAsync(ProfileSession session, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var result = new ProfileResult(session);

            return this.resultsStorage.AddAsync(result, cancellationToken);
        }

        #endregion
    }
}