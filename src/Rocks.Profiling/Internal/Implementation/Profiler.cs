using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class Profiler : IProfiler, IInternalProfiler
    {
        private readonly ICurrentSessionProvider currentSession;
        private readonly IProfilerLogger logger;
        private readonly ICompletedSessionsProcessorQueue completedSessionsProcessorQueue;
        private readonly IProfilerEventsHandler eventsHandler;


        public Profiler([NotNull] IProfilerConfiguration configuration,
                        [NotNull] ICurrentSessionProvider currentSession,
                        [NotNull] IProfilerLogger logger,
                        [NotNull] ICompletedSessionsProcessorQueue completedSessionsProcessorQueue,
                        [NotNull] IProfilerEventsHandler eventsHandler)
        {
            this.Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.currentSession = currentSession ?? throw new ArgumentNullException(nameof(currentSession));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.completedSessionsProcessorQueue = completedSessionsProcessorQueue ?? throw new ArgumentNullException(nameof(completedSessionsProcessorQueue));
            this.eventsHandler = eventsHandler ?? throw new ArgumentNullException(nameof(eventsHandler));
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
        public ProfileSession Start(IDictionary<string, object> additionalSessionData = null)
        {
            try
            {
                if (!this.Configuration.Enabled)
                    return null;

                if (this.currentSession.Get() != null)
                    throw new SessionAlreadyStartedProfilingException();

                var session = new ProfileSession(this, this.logger);

                if (additionalSessionData != null)
                    session.AddData(additionalSessionData);

                this.currentSession.Set(session);

                return session;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
                return null;
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

                ProfileOperation operation;
                if (session != null)
                {
                    operation = session.StartMeasure(specification);
                }
                else
                {
                    operation = new ProfileOperation(id: 0,
                                                     profiler: this,
                                                     session: null,
                                                     specification: specification,
                                                     parent: null);
                }

                return operation;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
                return null;
            }
        }


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="specification"/>.<br />
        ///     Uppon disposing will store the results of measurement in the specified <paramref name="session"/>.<br />
        /// </summary>
        public ProfileOperation Profile(ProfileSession session, ProfileOperationSpecification specification)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            try
            {
                if (!this.Configuration.Enabled)
                    return null;

                var operation = session.StartMeasure(specification);

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

                this.StopSession(additionalSessionData, session);

                this.currentSession.Delete();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        /// <summary>
        ///     Stops specified profile <paramref name="session"/> and stores the results.
        /// </summary>
        public void Stop(ProfileSession session, IDictionary<string, object> additionalSessionData = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            try
            {
                if (!this.Configuration.Enabled)
                    return;

                this.StopSession(additionalSessionData, session);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        /// <summary>
        ///     Profiled operation has been ended.
        /// </summary>
        public void OnOperationEnded(ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                this.eventsHandler.OnOperationEnded(operation);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        private void StopSession(IDictionary<string, object> additionalSessionData, ProfileSession session)
        {
            if (additionalSessionData != null)
                session.AddData(additionalSessionData);

            this.completedSessionsProcessorQueue.Add(session);

            try
            {
                this.eventsHandler.OnSessionEnded(session);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }
    }
}