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

            this.OperationsTreeRoot = new ProfileOperation(this, ProfileOperationNames.ProfileSessionRoot);

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
        ///     Get the total duration of the operations in the session.
        /// </summary>
        public TimeSpan Duration => this.OperationsTreeRoot.Duration;

        /// <summary>
        ///     The root of the session operations tree.
        /// </summary>
        public ProfileOperation OperationsTreeRoot { get; }

        #endregion

        #region Protected methods

        /// <summary>
        ///     Starts new operation measure.
        /// </summary>
        [NotNull, MustUseReturnValue]
        internal ProfileOperation StartMeasure([NotNull] string name,
                                               [CanBeNull] string category = null,
                                               [CanBeNull] IDictionary<string, object> data = null)
        {
            var operation = new ProfileOperation(session: this,
                                                 name: name,
                                                 category: category,
                                                 parent: this.currentOperation,
                                                 data: data);

            this.currentOperation.Add(operation);
            this.currentOperation = operation;

            return operation;
        }


        /// <summary>
        ///     Stops operation measure.
        ///     This method should not be called directly - it will be called automatically
        ///     uppon disposing of <see cref="ProfileOperation"/> returned from <see cref="StartMeasure"/>.
        /// </summary>
        internal void StopMeasure([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                if (operation.Session != this)
                    throw new OperationFromAnotherSessionProfilingException();

                if (this.currentOperation != operation)
                    throw new OperationsOutOfOrderProfillingException();

                if (this.currentOperation.Parent == null)
                    throw new OperationsOutOfOrderProfillingException();

                this.OperationsTreeRoot.EndTime = this.currentOperation.EndTime = this.Time;
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