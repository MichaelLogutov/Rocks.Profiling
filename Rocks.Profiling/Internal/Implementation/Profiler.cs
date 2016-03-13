using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Rocks.Profiling.Data;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class Profiler : IProfiler
    {
        #region Static fields

        private static readonly AsyncLocal<ProfileSession> CurrentSession = new AsyncLocal<ProfileSession>();

        #endregion

        #region Private readonly fields

        private readonly IProfilerLogger logger;
        private readonly ICompletedSessionsProcessorQueue completedSessionsProcessorQueue;

        #endregion

        #region Construct

        public Profiler([NotNull] ProfilerConfiguration configuration,
                        [NotNull] IProfilerLogger logger,
                        [NotNull] ICompletedSessionsProcessorQueue completedSessionsProcessorQueue)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (completedSessionsProcessorQueue == null)
                throw new ArgumentNullException(nameof(completedSessionsProcessorQueue));

            this.Configuration = configuration;
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
        public void Start()
        {
            try
            {
                if (CurrentSession.Value != null)
                    throw new SessionAlreadyStartedProfilingException();

                CurrentSession.Value = new ProfileSession(this.logger);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="name"/>, <paramref name="category"/> and additional <paramref name="data"/>.<br />
        ///     Uppon disposing will store the results of measurement in the current session.<br />
        ///     If there is no session started - will do nothing on disposing.<br />
        ///     Any exceptions this method throws are swallowed and logged to <see cref="IProfilerLogger"/>.
        /// </summary>
        public IProfileOperation Profile(string name, string category = null, IDictionary<string, object> data = null)
        {
            var operation = new ProfileOperation(name, category, data);
            operation.Profiler = this;

            CurrentSession.Value?.StartMeasure(operation);

            return operation;
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
                var session = CurrentSession.Value;
                if (session == null)
                    throw new NoCurrentSessionProfilingException();

                this.completedSessionsProcessorQueue.Add(new CompletedSessionInfo(session, additionalSessionData));

                CurrentSession.Value = null;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }

        #endregion
    }
}