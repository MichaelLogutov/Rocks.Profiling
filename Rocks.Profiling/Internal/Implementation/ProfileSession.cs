using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Rocks.Profiling.Data;
using Rocks.Profiling.Exceptions;

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

        #endregion

        #region Private fields

        [NotNull]
        private ProfileOperation currentOperation;

        #endregion

        #region Construct

        public ProfileSession()
        {
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

            operation.Session = this;
            operation.StartTime = this.Time;
            operation.Parent = this.currentOperation;

            this.currentOperation.Add(operation);
            this.currentOperation = operation;
        }


        /// <summary>
        ///     Stops operation measure.
        /// </summary>
        public void StopMeasure([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (this.currentOperation != operation)
                throw new OperationsOutOfOrderProfillingException();

            if (this.currentOperation.Parent == null)
                throw new OperationsOutOfOrderProfillingException();

            this.currentOperation.EndTime = this.Time;
            this.currentOperation = this.currentOperation.Parent;
        }

        #endregion
    }
}