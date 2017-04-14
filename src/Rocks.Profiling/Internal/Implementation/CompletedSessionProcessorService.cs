﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CompletedSessionProcessorService : ICompletedSessionProcessorService
    {
        private readonly IProfilerConfiguration configuration;
        private readonly IProfilerResultsStorage resultsStorage;
        private readonly ICompletedSessionProcessingFilter completedSessionFilter;


        public CompletedSessionProcessorService([NotNull] IProfilerConfiguration configuration,
                                                [NotNull] IProfilerResultsStorage resultsStorage,
                                                [NotNull] ICompletedSessionProcessingFilter completedSessionFilter)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (resultsStorage == null)
                throw new ArgumentNullException(nameof(resultsStorage));

            if (completedSessionFilter == null)
                throw new ArgumentNullException(nameof(completedSessionFilter));

            this.configuration = configuration;
            this.resultsStorage = resultsStorage;
            this.completedSessionFilter = completedSessionFilter;
        }


        /// <summary>
        ///     Determines if completed session is needs to be processed.
        /// </summary>
        public bool ShouldProcess(ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var should_process = this.completedSessionFilter.ShouldProcess(session);
            if (should_process != null)
                return should_process.Value;

            if (session.Operations.Count == 0)
                return false;

            if (session.HasOperationLongerThanNormal)
                return true;

            if (session.Duration < this.configuration.SessionMinimalDuration)
                return false;

            return true;
        }


        /// <summary>
        ///     Perform processing of completed sessions (like, storing the result).
        /// </summary>
        public Task ProcessAsync(IReadOnlyList<ProfileSession> sessions,
                                 CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sessions == null)
                throw new ArgumentNullException(nameof(sessions));

            if (sessions.Count == 0)
                return Task.CompletedTask;

            return this.resultsStorage.AddAsync(sessions, cancellationToken);
        }
    }
}