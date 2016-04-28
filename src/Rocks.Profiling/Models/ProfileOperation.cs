using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Rocks.Profiling.Configuration;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Represents information about some arbitrary operation during profiling (for example, method execution).
    /// </summary>
    [DataContract]
    public class ProfileOperation : IDisposable
    {
        #region Construct

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProfileOperation" /> class.<br />
        ///     This method indended to be called from <see cref="ProfileSession"/>
        ///     and should not be called manually.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="session" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="specification" /> is <see langword="null" />.</exception>
        public ProfileOperation(int id,
                                [NotNull] ProfileSession session,
                                [NotNull] ProfileOperationSpecification specification,
                                [CanBeNull] ProfileOperation parent = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            this.Id = id;
            this.Session = session;

            this.Profiler = this.Session.Profiler;
            this.StartTime = this.Session.Time;

            this.Name = specification.Name;
            this.Category = specification.Category;
            this.Resource = specification.Resource;
            this.NormalDuration = specification.NormalDuration;

            this.Parent = parent;

            if (specification.Data != null)
                this.Data = new Dictionary<string, object>(specification.Data, StringComparer.Ordinal);
        }

        #endregion

        #region Public properties

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
                    result = $"{this.Category}::";

                result += this.Name;

                if (this.Resource != null)
                    result += $"::{this.Resource}";

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

                object result;
                if (!this.Data.TryGetValue(dataKey, out result))
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
        ///     Start time of the operation.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        ///     End time of the operation.
        /// </summary>
        public TimeSpan? EndTime { get; internal set; }

        /// <summary>
        ///     Gets the total duration of the operation.
        ///     This property returns time passed between <see cref="StartTime" /> and <see cref="EndTime" />.
        /// </summary>
        [DataMember]
        public TimeSpan Duration => this.EndTime - this.StartTime ?? TimeSpan.Zero;

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
        public string CallStack { get; internal set; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return this.FullName;
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (this.IsCompleted)
                return;

            this.Session?.StopMeasure(this);

            this.IsCompleted = true;
        }

        #endregion
    }
}