using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;

namespace Rocks.Profiling.Data
{
    /// <summary>
    ///     Represents a stream of profile events.
    ///     This class is not thread safe.
    /// </summary>
    public sealed class ProfileSession
    {
        #region Private readonly fields

        private readonly Stopwatch stopwatch;
        private readonly IProfilerLogger logger;

        #endregion

        #region Private fields

        [NotNull]
        private ProfileOperation currentOperation;

        #endregion

        #region Construct

        public ProfileSession([NotNull] IProfiler profiler, [NotNull] IProfilerLogger logger)
        {
            if (profiler == null)
                throw new ArgumentNullException(nameof(profiler));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.stopwatch = Stopwatch.StartNew();

            this.Profiler = profiler;
            this.logger = logger;

            this.OperationsTreeRoot = new ProfileOperation(this, ProfileOperationNames.ProfileSession);

            this.currentOperation = this.OperationsTreeRoot;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Profiler of the session.
        /// </summary>
        public IProfiler Profiler { get; }

        /// <summary>
        ///     Additional data associated with the session.
        /// </summary>
        public IDictionary<string, object> AdditionalData { get; set; }

        /// <summary>
        ///     Current time in session.
        /// </summary>
        public TimeSpan Time => this.stopwatch.Elapsed;

        /// <summary>
        ///     The root of the session operations tree.
        /// </summary>
        public ProfileOperation OperationsTreeRoot { get; }

        #endregion

        #region Protected methods

        /// <summary>
        ///     Starts new operation measure.
        /// </summary>
        internal void StartMeasure([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                operation.Session = this;
                operation.StartTime = this.Time;
                operation.Parent = this.currentOperation;

                this.currentOperation.Add(operation);
                this.currentOperation = operation;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }


        /// <summary>
        ///     Stops operation measure.
        /// </summary>
        internal void StopMeasure([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                if (this.currentOperation != operation)
                    throw new OperationsOutOfOrderProfillingException();

                if (this.currentOperation.Parent == null)
                    throw new OperationsOutOfOrderProfillingException();

                this.currentOperation.EndTime = this.Time;
                this.currentOperation = this.currentOperation.Parent;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }

        #endregion
    }
}