using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Internal.Helpers;
using Rocks.Profiling.Loggers;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Represents a stream of profile events.
    ///     This class is not thread safe.
    /// </summary>
    [DataContract]
    public sealed class ProfileSession
    {
        #region Private readonly fields

        private readonly Stopwatch stopwatch;
        private readonly IProfilerLogger logger;
        private readonly List<ProfileOperation> operations;
        private readonly Stack<ProfileOperation> operationsStack;

        private int newId;

        #endregion

        #region Construct

        /// <exception cref="ArgumentNullException"><paramref name="profiler"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null" />.</exception>
        public ProfileSession([NotNull] IProfiler profiler, [NotNull] IProfilerLogger logger)
        {
            if (profiler == null)
                throw new ArgumentNullException(nameof(profiler));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            this.stopwatch = Stopwatch.StartNew();

            this.Profiler = profiler;
            this.logger = logger;

            this.operations = new List<ProfileOperation>();
            this.operationsStack = new Stack<ProfileOperation>();
            this.Data = new Dictionary<string, object>(StringComparer.Ordinal);
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
        [NotNull, DataMember(Name = "Data", EmitDefaultValue = false)]
        public IDictionary<string, object> Data { get; }

        /// <summary>
        ///     Gets or sets additional data for this session by key.
        ///     The key is case sensitive.
        /// </summary>
        /// <exception cref="ArgumentException" accessor="set">Argument <paramref name="dataKey" /> is null or empty</exception>
        public object this[[NotNull] string dataKey]
        {
            get
            {
                if (string.IsNullOrEmpty(dataKey))
                    throw new ArgumentException("Argument is null or empty", nameof(dataKey));

                object result;
                if (!this.Data.TryGetValue(dataKey, out result))
                    return null;

                return result;
            }

            set
            {
                if (string.IsNullOrEmpty(dataKey))
                    throw new ArgumentException("Argument is null or empty", nameof(dataKey));

                this.Data[dataKey] = value;
            }
        }

        /// <summary>
        ///     Current time in session.
        /// </summary>
        public TimeSpan Time => this.stopwatch.Elapsed;

        /// <summary>
        ///     Get the total duration of all operations in the session.
        /// </summary>
        [DataMember]
        public TimeSpan Duration { get; private set; }

        /// <summary>
        ///     The list of all operations in the session.
        /// </summary>
        [DataMember(Name = "Operations", EmitDefaultValue = false)]
        public IReadOnlyList<ProfileOperation> Operations => this.operations;

        /// <summary>
        ///     Returns true if there is an operation which <see cref="ProfileOperation.Duration" />
        ///     greater or equal to it's <see cref="ProfileOperation.NormalDuration" />.
        /// </summary>
        [DataMember]
        public bool HasOperationLongerThanNormal { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Adds new additional data to the <see cref="Data" /> dictionary.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="additionalData"/> is <see langword="null" />.</exception>
        public void AddData([NotNull] IDictionary<string, object> additionalData)
        {
            if (additionalData == null)
                throw new ArgumentNullException(nameof(additionalData));

            foreach (var kv in additionalData)
                this.Data[kv.Key] = kv.Value;
        }

        #endregion

        #region Protected methods

        /// <summary>
        ///     Starts new operation measure.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="specification"/> is <see langword="null" />.</exception>
        [NotNull, MustUseReturnValue]
        internal ProfileOperation StartMeasure([NotNull] ProfileOperationSpecification specification)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            var last_operation = this.operationsStack.Count > 0 ? this.operationsStack.Peek() : null;

            this.newId++;

            var operation = new ProfileOperation(id: this.newId,
                                                 session: this,
                                                 specification: specification,
                                                 parent: last_operation);

            if (this.Profiler.Configuration.CaptureCallStacks)
            {
                var current_assembly = this.GetType().Assembly;
                operation.CallStack = new StackTrace(true).ToAsyncString(x => x.DeclaringType?.Assembly != current_assembly);
            }

            this.operations.Add(operation);
            this.operationsStack.Push(operation);

            return operation;
        }


        /// <summary>
        ///     Stops operation measure.
        ///     This method should not be called directly - it will be called automatically
        ///     uppon disposing of <see cref="ProfileOperation" /> returned from <see cref="StartMeasure" />.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null" />.</exception>
        internal void StopMeasure([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            try
            {
                if (operation.Session != this)
                    throw new OperationFromAnotherSessionProfilingException();

                var current_operation = this.operationsStack.Pop();
                if (current_operation != operation)
                    throw new OperationsOutOfOrderProfillingException();

                var parent_operation = this.operationsStack.Count > 0 ? this.operationsStack.Peek() : null;
                if (parent_operation != operation.Parent)
                    throw new OperationsOutOfOrderProfillingException();

                operation.EndTime = this.Time;

                this.Duration += operation.Duration;

                if (operation.Duration >= operation.NormalDuration)
                    this.HasOperationLongerThanNormal = true;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex);
            }
        }

        #endregion
    }
}