using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private readonly ProfilerConfiguration configuration;
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

        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="processorService"/> is <see langword="null" />.</exception>
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

            this.configuration = configuration;
            this.processorService = processorService;
            this.logger = logger;

            this.dataToProcess = new BlockingCollection<ProfileSession>(this.configuration.ResultsBufferSize);
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
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null" />.</exception>
        public void Add(ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (this.dataToProcess.IsAddingCompleted)
                return;

            try
            {
                this.EnsureProcessingTaskStarted();

                if (!this.dataToProcess.TryAdd(session))
                    throw new ResultsProcessorOverflowProfilingException();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
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
            var sessions = new List<ProfileSession>(this.configuration.ResultsProcessMaxBatchSize);

            while (!this.cancellationTokenSource.IsCancellationRequested && !this.dataToProcess.IsCompleted)
            {
                try
                {
                    if (this.configuration.ResultsProcessMaxBatchSize == 1)
                    {
                        // shortcut for particular case
                        var session = this.dataToProcess.Take(this.cancellationTokenSource.Token);
                        if (session == null)
                            break;

                        sessions.Add(session);
                    }
                    else
                    {
                        while (sessions.Count < this.configuration.ResultsProcessMaxBatchSize)
                        {
                            ProfileSession session;
                            if (!this.dataToProcess.TryTake(out session,
                                                            (int) this.configuration.ResultsProcessBatchDelay.TotalMilliseconds,
                                                            this.cancellationTokenSource.Token))
                                break;

                            if (this.processorService.ShouldProcess(session))
                                sessions.Add(session);
                        }
                    }

                    if (sessions.Count > 0)
                    {
                        await this.processorService.ProcessAsync(sessions, this.cancellationTokenSource.Token).ConfigureAwait(false);
                        sessions.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
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