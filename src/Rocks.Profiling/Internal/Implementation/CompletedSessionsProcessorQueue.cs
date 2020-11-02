using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Internal.Helpers;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Rocks.SimpleInjector.Attributes;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CompletedSessionsProcessorQueue : ICompletedSessionsProcessorQueue, IDisposable
    {
        [ThreadSafe]
        private readonly object processingTaskInitializationLock = new object();

        private readonly IProfilerConfiguration configuration;
        private readonly IProfilerLogger logger;
        private readonly ICompletedSessionProcessorService processorService;

        [ThreadSafe]
        private readonly ConcurrentQueue<ProfileSession> dataToProcess;

        [ThreadSafe]
        private readonly CancellationTokenSource cancellationTokenSource;

        [ThreadSafe]
        private bool disposed;

        [ThreadSafe]
        private Task processingTask;


        public CompletedSessionsProcessorQueue([NotNull] IProfilerConfiguration configuration,
            [NotNull] IProfilerLogger logger,
            [NotNull] ICompletedSessionProcessorService processorService)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.processorService = processorService ?? throw new ArgumentNullException(nameof(processorService));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.dataToProcess = new ConcurrentQueue<ProfileSession>();
            this.cancellationTokenSource = new CancellationTokenSource();
        }


        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.disposed)
                return;

            try
            {
                if (!this.dataToProcess.IsEmpty)
                {
                    try
                    {
                        this.processingTask.Wait(this.configuration.ResultsProcessBatchDelay);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                
                this.cancellationTokenSource.Cancel();
            }
            catch
            {
                // ignored
            }

            this.disposed = true;
        }


        /// <summary>
        ///     Add results from completed profiling session.
        /// </summary>
        /// <exception cref="ResultsProcessorOverflowProfilingException">Results buffer size limit reached.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null" />.</exception>
        public void Add(ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (this.disposed)
                return;

            try
            {
                if (!this.processorService.ShouldProcess(session))
                    return;

                this.EnsureProcessingTaskStarted();

                var retries = this.configuration.ResultsBufferAddRetriesCount;
                while (true)
                {
                    if (this.dataToProcess.Count < this.configuration.ResultsBufferSize)
                    {
                        this.dataToProcess.Enqueue(session);
                        return;
                    }

                    var overflow_exception = new ResultsProcessorOverflowProfilingException();
                    this.logger.LogWarning(overflow_exception.Message, overflow_exception);

                    retries--;
                    if (retries < 0)
                        throw overflow_exception;

                    this.dataToProcess.TryDequeue(out _);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        private void EnsureProcessingTaskStarted()
        {
            if (this.processingTask != null)
                return;

            lock (this.processingTaskInitializationLock)
            {
                this.processingTask ??= Task.Run(this.ProcessAsync, this.cancellationTokenSource.Token);
            }
        }


        private async Task ProcessAsync()
        {
            var sessions = new List<ProfileSession>(this.configuration.ResultsProcessMaxBatchSize);

            while (!this.disposed && !this.cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    if (this.configuration.ResultsProcessMaxBatchSize == 1)
                    {
                        // shortcut for particular case
                        if (this.dataToProcess.TryDequeue(out var session))
                            sessions.Add(session);
                    }
                    else
                    {
                        while (sessions.Count < this.configuration.ResultsProcessMaxBatchSize)
                        {
                            if (this.disposed || this.cancellationTokenSource.IsCancellationRequested)
                                break;

                            if (!this.dataToProcess.TryDequeue(out var session))
                                break;

                            sessions.Add(session);
                        }
                    }

                    if (sessions.Count > 0)
                    {
                        if (this.configuration.Enabled)
                            await this.processorService.ProcessAsync(sessions, this.cancellationTokenSource.Token).ConfigureAwait(false);

                        sessions.Clear();
                    }

                    if (this.configuration.ResultsProcessBatchDelay > TimeSpan.Zero)
                    {
                        await Task
                            .Delay(this.configuration.ResultsProcessBatchDelay, this.cancellationTokenSource.Token)
                            .Silent()
                            .ConfigureAwait(false);
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
    }
}