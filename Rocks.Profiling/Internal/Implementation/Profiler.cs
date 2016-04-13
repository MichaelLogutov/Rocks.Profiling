using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class Profiler : IProfiler
    {
        #region Private readonly fields

        private readonly ICurrentSessionProvider currentSession;
        private readonly IProfilerLogger logger;
        private readonly ICompletedSessionsProcessorQueue completedSessionsProcessorQueue;

        #endregion

        #region Construct

        public Profiler([NotNull] ProfilerConfiguration configuration,
                        [NotNull] ICurrentSessionProvider currentSession,
                        [NotNull] IProfilerLogger logger,
                        [NotNull] ICompletedSessionsProcessorQueue completedSessionsProcessorQueue)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (currentSession == null)
                throw new ArgumentNullException(nameof(currentSession));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (completedSessionsProcessorQueue == null)
                throw new ArgumentNullException(nameof(completedSessionsProcessorQueue));

            this.Configuration = configuration;
            this.currentSession = currentSession;
            this.logger = logger;
            this.completedSessionsProcessorQueue = completedSessionsProcessorQueue;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Current profiler configuration.
        /// </summary>
        public ProfilerConfiguration Configuration { get; }

        #endregion

        #region IProfiler Members

        /// <summary>
        ///     Creates new profile session.
        ///     If there was already session started - throws an exception.<br />
        ///     Any exceptions this method throws are swallowed and logged to <see cref="IProfilerLogger"/>.
        /// </summary>
        public void Start(IDictionary<string, object> additionalSessionData = null)
        {
            try
            {
                if (this.currentSession.Get() != null)
                    throw new SessionAlreadyStartedProfilingException();

                var session = new ProfileSession(this, this.logger);

                if (additionalSessionData != null)
                    session.AddAdditionalData(additionalSessionData);

                this.currentSession.Set(session);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="specification"/>.<br />
        ///     Uppon disposing will store the results of measurement in the current session.<br />
        ///     If there is no session started - returns dummy operation that will do nothing.
        /// </summary>
        public ProfileOperation Profile(ProfileOperationSpecification specification)
        {
            try
            {
                var session = this.currentSession.Get();
                var operation = session?.StartMeasure(specification);

                return operation;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
                return null;
            }
        }


        /// <summary>
        ///     Stops current profile session and stores the results.
        ///     If there is no session started - throws an exception.<br />
        ///     Any exceptions this method throws are swallowed and logged to <see cref="IProfilerLogger"/>.
        /// </summary>
        public void Stop(IDictionary<string, object> additionalSessionData = null)
        {
            try
            {
                var session = this.currentSession.Get();
                if (session == null)
                    throw new NoCurrentSessionProfilingException();

                if (additionalSessionData != null)
                    session.AddAdditionalData(additionalSessionData);

                this.completedSessionsProcessorQueue.Add(session);

                this.currentSession.Delete();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }

        #endregion
    }
}