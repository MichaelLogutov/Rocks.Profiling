using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Rocks.Profiling.Data;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Loggers;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     Represents a stream of profile events.
    ///     This class is not thread safe.
    /// </summary>
    internal sealed class ProfileSession
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

        public ProfileSession([NotNull] IProfilerLogger logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.logger = logger;

            this.stopwatch = Stopwatch.StartNew();
            this.OperationsTreeRoot = new ProfileOperation(ProfileOperationNames.ProfileSession);

            this.currentOperation = this.OperationsTreeRoot;
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Current time in session.
        /// </summary>
        public TimeSpan Time => this.stopwatch.Elapsed;

        /// <summary>
        ///     The root of the session operations tree.
        /// </summary>
        public ProfileOperation OperationsTreeRoot { get; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Starts new operation measure.
        /// </summary>
        public void StartMeasure([NotNull] ProfileOperation operation)
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
        public void StopMeasure([NotNull] ProfileOperation operation)
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


        /// <summary>
        ///     Gets total duration of operations.
        /// </summary>
        public TimeSpan GetTotalDuration()
        {
            if (this.OperationsTreeRoot.ChildNodes == null)
                return TimeSpan.Zero;

            var total_ticks = 0L;

            foreach (var operation in this.OperationsTreeRoot.ChildNodes)
            {
                if (operation.EndTime == null)
                    continue;

                total_ticks += (operation.EndTime.Value - operation.StartTime).Ticks;
            }

            return new TimeSpan(total_ticks);
        }

        #endregion
    }
}