using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;
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
        private readonly BlockingCollection<CompletedSessionInfo> dataToProcess;

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

            this.dataToProcess = new BlockingCollection<CompletedSessionInfo>(configuration.ResultsBufferSize);
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
        public void Add(CompletedSessionInfo completedSessionInfo)
        {
            if (completedSessionInfo == null)
                throw new ArgumentNullException(nameof(completedSessionInfo));

            if (this.dataToProcess.IsAddingCompleted)
                return;

            this.EnsureProcessingTaskStarted();

            if (!this.dataToProcess.TryAdd(completedSessionInfo))
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

                this.processingTask = Task.Run
                    (() =>
                     {
                         while (!this.cancellationTokenSource.IsCancellationRequested)
                         {
                             try
                             {
                                 var completed_session_info = this.dataToProcess.Take();

                                 if (this.processorService.ShouldProcess(completed_session_info))
                                     this.processorService.Process(completed_session_info);
                             }
                             catch (Exception ex)
                             {
                                 this.logger.LogError(ex);
                             }
                         }
                     },
                     this.cancellationTokenSource.Token);
            }
        }

        #endregion
    }
}