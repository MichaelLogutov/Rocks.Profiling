using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.SimpleInjector.Attributes;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CompletedSessionsProcessorQueue : ICompletedSessionsProcessorQueue, IDisposable
    {
        #region Private readonly fields

        [ThreadSafe]
        private readonly object processingTaskInitializationLock = new object();

        private readonly ProfilerConfiguration configuration;
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

        public CompletedSessionsProcessorQueue([NotNull] ProfilerConfiguration configuration, ICompletedSessionProcessorService processorService)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            this.configuration = configuration;
            this.processorService = processorService;
            this.dataToProcess = new BlockingCollection<CompletedSessionInfo>(this.configuration.ResultsBufferSize);
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
                this.configuration.LogError(ex);
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
                             var completed_session_info = this.dataToProcess.Take();

                             if (this.processorService.ShouldProcess(completed_session_info))
                                 this.processorService.Process(completed_session_info);
                         }
                     },
                     this.cancellationTokenSource.Token);
            }
        }

        #endregion
    }
}