using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class Profiler : IProfiler
    {
        private readonly ICurrentSessionProvider currentSession;
        private readonly IProfilerLogger logger;
        private readonly ICompletedSessionsProcessorQueue completedSessionsProcessorQueue;
        private readonly IEnumerable<IProfilerEventsHandler> eventsHandlers;


        /// <exception cref="ArgumentNullException"><paramref name="configuration"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="currentSession"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="completedSessionsProcessorQueue"/> is <see langword="null" />.</exception>
        public Profiler([NotNull] IProfilerConfiguration configuration,
                        [NotNull] ICurrentSessionProvider currentSession,
                        [NotNull] IProfilerLogger logger,
                        [NotNull] ICompletedSessionsProcessorQueue completedSessionsProcessorQueue,
                        [NotNull] IEnumerable<IProfilerEventsHandler> eventsHandlers)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (currentSession == null)
                throw new ArgumentNullException(nameof(currentSession));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (completedSessionsProcessorQueue == null)
                throw new ArgumentNullException(nameof(completedSessionsProcessorQueue));

            if (eventsHandlers == null)
                throw new ArgumentNullException(nameof(eventsHandlers));

            this.Configuration = configuration;
            this.currentSession = currentSession;
            this.logger = logger;
            this.completedSessionsProcessorQueue = completedSessionsProcessorQueue;
            this.eventsHandlers = eventsHandlers;
        }


        /// <summary>
        ///     Current profiler configuration.
        /// </summary>
        public IProfilerConfiguration Configuration { get; }


        /// <summary>
        ///     Creates new profile session.
        ///     If there was already session started - throws an exception.<br />
        ///     Any exceptions this method throws are swallowed and logged to <see cref="IProfilerLogger"/>.
        /// </summary>
        public void Start(IDictionary<string, object> additionalSessionData = null)
        {
            try
            {
                if (!this.Configuration.Enabled)
                    return;

                if (this.currentSession.Get() != null)
                    throw new SessionAlreadyStartedProfilingException();

                var session = new ProfileSession(this, this.logger, this.eventsHandlers);

                if (additionalSessionData != null)
                    session.AddData(additionalSessionData);

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
                if (!this.Configuration.Enabled)
                    return null;

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
                if (!this.Configuration.Enabled)
                    return;

                var session = this.currentSession.Get();
                if (session == null)
                    throw new NoCurrentSessionProfilingException();

                if (additionalSessionData != null)
                    session.AddData(additionalSessionData);

                this.completedSessionsProcessorQueue.Add(session);

                foreach (var events_handler in this.eventsHandlers)
                {
                    try
                    {
                        events_handler.OnSessionEnded(session);
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex);
                    }
                }

                this.currentSession.Delete();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }
    }
}