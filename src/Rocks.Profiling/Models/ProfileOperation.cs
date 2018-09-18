using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Helpers;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Represents information about some arbitrary operation during profiling (for example, method execution).
    /// </summary>
    [DataContract]
    public class ProfileOperation : IDisposable
    {
        private readonly Stopwatch time;


        /// <summary>
        ///     Initializes a new instance of the <see cref="ProfileOperation" /> class.<br />
        ///     This method indended to be called from <see cref="ProfileSession"/>
        ///     and should not be called manually.
        /// </summary>
        public ProfileOperation(int id,
                                [NotNull] ProfileSession session,
                                [NotNull] ProfileOperationSpecification specification)
            : this(id: id,
                   profiler: session.Profiler,
                   session: session,
                   specification: specification,
                   parent: null)
        {
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="ProfileOperation" /> class.<br />
        ///     This method indended to be called from <see cref="ProfileSession"/>
        ///     and should not be called manually.
        /// </summary>
        public ProfileOperation(int id,
                                [NotNull] IProfiler profiler,
                                [CanBeNull] ProfileSession session,
                                [NotNull] ProfileOperationSpecification specification,
                                [CanBeNull] ProfileOperation parent)
        {
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            this.Id = id;
            this.Profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));

            if (session != null)
            {
                if (session.Profiler != profiler)
                    throw new InvalidOperationException("Session.Profiler does not match specified profiler.");

                this.Session = session;
                this.StartTime = session.Time;
            }

            if (this.Profiler.Configuration.CaptureCallStacks)
            {
                var current_assembly = this.GetType().Assembly;
                this.CallStack = new StackTrace(true).ToAsyncString(x => x.DeclaringType?.Assembly != current_assembly);
            }

            this.Name = specification.Name;
            this.Category = specification.Category;
            this.Resource = specification.Resource;
            this.NormalDuration = specification.NormalDuration;
            this.StartDate = DateTime.UtcNow;

            this.Parent = parent;

            if (specification.Data != null)
                this.Data = new Dictionary<string, object>(specification.Data, StringComparer.Ordinal);

            // start timer as close to the profiled code as possible
            this.time = Stopwatch.StartNew();
        }


        /// <summary>
        ///     Id of the operation inside <see cref="Session" />.
        /// </summary>
        [DataMember]
        public int Id { get; }

        /// <summary>
        ///     Operation name.
        /// </summary>
        [NotNull, DataMember]
        public string Name { get; }

        /// <summary>
        ///     Operation category.
        /// </summary>
        [CanBeNull, DataMember]
        public string Category { get; }

        /// <summary>
        ///     The resource which current operation is workgin with.<br />
        ///     For example, "MyDbServer - MyDatabase".
        /// </summary>
        [CanBeNull, DataMember]
        public string Resource { get; set; }

        /// <summary>
        ///     Represents a concatination of <see cref="Category"/>, <see cref="Name"/>
        ///     and <see cref="Resource"/> properties.
        /// </summary>
        [NotNull]
        public string FullName
        {
            get
            {
                string result = null;

                if (this.Category != null)
                    result = this.Category + "::";

                result += this.Name;

                if (this.Resource != null)
                    result += "::" + this.Resource;

                return result;
            }
        }

        /// <summary>
        ///     Additional data about operation.
        ///     The key is case sensitive.
        /// </summary>
        [CanBeNull, DataMember]
        public IDictionary<string, object> Data { get; set; }

        /// <summary>
        ///     Gets or sets additional data for this operation by key.
        ///     The key is case sensitive.
        /// </summary>
        /// <exception cref="ArgumentException" accessor="set">Argument <paramref name="dataKey" /> is null or empty</exception>
        public object this[[CanBeNull] string dataKey]
        {
            get
            {
                if (string.IsNullOrEmpty(dataKey))
                    throw new ArgumentException("Argument is null or empty", nameof(dataKey));

                if (this.Data == null)
                    return null;

                if (!this.Data.TryGetValue(dataKey, out var result))
                    return null;

                return result;
            }

            set
            {
                if (string.IsNullOrEmpty(dataKey))
                    throw new ArgumentException("Argument is null or empty", nameof(dataKey));

                if (this.Data == null)
                    this.Data = new Dictionary<string, object>(StringComparer.Ordinal);

                this.Data[dataKey] = value;
            }
        }

        /// <summary>
        ///     Profiler for this operation.
        /// </summary>
        [NotNull]
        public IProfiler Profiler { get; }

        /// <summary>
        ///     Profile session for this operation.
        /// </summary>
        [CanBeNull]
        public ProfileSession Session { get; }

        /// <summary>
        ///     Start time of the operation relative to the session.<br />
        ///     In case of sessionless operation the start time will be null.
        /// </summary>
        public TimeSpan? StartTime { get; }

        /// <summary>
        ///     End time of the operation relative to the session. 
        /// </summary>
        public TimeSpan? EndTime { get; internal set; }

        /// <summary>
        ///     Absolute start datetime of the operation, in UTC timezone.
        /// </summary>
        [DataMember]
        public DateTime StartDate { get; }

        /// <summary>
        ///     Gets the total duration of the operation.
        /// </summary>
        [DataMember]
        public TimeSpan Duration => this.time.Elapsed;

        /// <summary>
        ///     Gets the duration which considered "normal" for this operation.
        /// </summary>
        public TimeSpan? NormalDuration { get; }

        /// <summary>
        ///     Parent node. Will be null for root nodes.
        /// </summary>
        [CanBeNull]
        public ProfileOperation Parent { get; }

        /// <summary>
        ///     Returns true if current profile scope has been completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        ///     Returns call stack of the operation start.<br />
        ///     This property filled only if <see cref="IProfilerConfiguration.CaptureCallStacks" /> is <see langword="true" />.
        /// </summary>
        [CanBeNull, DataMember(Name = "CallStack", EmitDefaultValue = false)]
        public string CallStack { get; }


        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString() => this.FullName;


        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (this.IsCompleted)
                return;

            this.time?.Stop();
            this.Session?.StopMeasure(this);

            (this.Profiler as IInternalProfiler)?.OnOperationEnded(this);

            this.IsCompleted = true;
        }
    }
}