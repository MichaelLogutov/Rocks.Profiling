using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Rocks.SimpleInjector.Attributes;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CompletedSessionsProcessorQueue : ICompletedSessionsProcessorQueue, IDisposable
    {
        #region Private readonly fields

        [ThreadSafe]
        private readonly object processingTaskInitializationLock = new object();

        private readonly IProfilerLogger logger;
        private readonly ICompletedSessionProcessorService processorService;

        [ThreadSafe]
        private readonly BlockingCollection<ProfileSession> dataToProcess;

        [ThreadSafe]
        private readonly CancellationTokenSource cancellationTokenSource;

        #endregion

        #region Private fields

        [ThreadSafe]
        private Task processingTask;

        #endregion

        #region Construct

        public CompletedSessionsProcessorQueue([NotNull] ProfilerConfiguration configuration,
                                               [NotNull] IProfilerLogger logger,
                                               [NotNull] ICompletedSessionProcessorService processorService)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (processorService == null)
                throw new ArgumentNullException(nameof(processorService));

            this.processorService = processorService;
            this.logger = logger;

            this.dataToProcess = new BlockingCollection<ProfileSession>(configuration.ResultsBufferSize);
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                this.cancellationTokenSource.Cancel();
                this.dataToProcess.CompleteAdding();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }

        #endregion

        #region IProfilingResultsProcessor Members

        /// <summary>
        ///     Add results from completed profiling session.
        /// </summary>
        /// <exception cref="ResultsProcessorOverflowProfilingException">Results buffer size limit reached.</exception>
        public void Add(ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (this.dataToProcess.IsAddingCompleted)
                return;

            this.EnsureProcessingTaskStarted();

            if (!this.dataToProcess.TryAdd(session))
                throw new ResultsProcessorOverflowProfilingException();
        }

        #endregion

        #region Private methods

        private void EnsureProcessingTaskStarted()
        {
            if (this.processingTask != null)
                return;

            lock (this.processingTaskInitializationLock)
            {
                if (this.processingTask != null)
                    return;

                this.processingTask = Task.Run(this.ProcessAsync, this.cancellationTokenSource.Token);
            }
        }


        private async Task ProcessAsync()
        {
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var session = this.dataToProcess.Take();

                    if (this.processorService.ShouldProcess(session))
                        await this.processorService.ProcessAsync(session, this.cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex);
                }
            }
        }

        #endregion
    }
}