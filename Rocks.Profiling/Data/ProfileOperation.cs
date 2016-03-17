using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Rocks.Profiling.Data
{
    /// <summary>
    ///     Represents information about some arbitrary operation during profiling (for example, method execution).
    /// </summary>
    [DataContract]
    public class ProfileOperation : IDisposable
    {
        #region Private fields

        [CanBeNull]
        private IList<ProfileOperation> childNodes;

        #endregion

        #region Construct

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProfileOperation" /> class.
        /// </summary>
        internal ProfileOperation([NotNull] ProfileSession session,
                                  [NotNull] string name,
                                  string category = null,
                                  [CanBeNull] ProfileOperation parent = null,
                                  IDictionary<string, object> data = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Argument is null or empty", nameof(name));

            this.Session = session;

            this.Profiler = this.Session.Profiler;
            this.StartTime = this.Session.Time;

            this.Name = name;
            this.Category = category;

            this.Parent = parent;

            if (data != null)
                this.Data = new Dictionary<string, object>(data, StringComparer.Ordinal);
        }

        #endregion

        #region Public properties

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
        ///     Additional data about operation.
        ///     The key is case sensitive.
        /// </summary>
        [CanBeNull, DataMember]
        public IDictionary<string, object> Data { get; set; }

        /// <summary>
        ///     Gets or sets additional data for this operation by key.
        ///     The key is case sensitive.
        /// </summary>
        public object this[[CanBeNull] string dataKey]
        {
            get { return this.Data?[dataKey]; }

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
        ///     This property returns time passed between <see cref="StartTime"/> and <see cref="EndTime"/>.
        /// </summary>
        [DataMember]
        public TimeSpan Duration => this.EndTime - this.StartTime ?? TimeSpan.Zero;

        /// <summary>
        ///     Parent node.
        /// </summary>
        [CanBeNull]
        public ProfileOperation Parent { get; }

        /// <summary>
        ///     A list of child nodes.
        /// </summary>
        [CanBeNull, DataMember (Name = "Operations")]
        public IEnumerable<ProfileOperation> ChildNodes => this.childNodes;

        /// <summary>
        ///     Returns true if <see cref="ChildNodes" /> is null or empty.
        /// </summary>
        public bool IsEmpty => this.childNodes == null || this.childNodes.Count == 0;

        /// <summary>
        ///     Returns count of the child nodes.
        /// </summary>
        public int Count => this.childNodes?.Count ?? 0;

        /// <summary>
        ///     Returns true if current profile scope has been completed.
        /// </summary>
        public bool IsCompleted { get; private set; }

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
            string result = null;

            if (this.Category != null)
                result = $"[{this.Category}] ";

            result += $"{this.Name} ({this.Count} operations)";

            return result;
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

        #region Protected methods

        /// <summary>
        ///     Adds new child node.
        /// </summary>
        internal void Add([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (this.childNodes == null)
                this.childNodes = new List<ProfileOperation>();

            this.childNodes.Add(operation);
        }

        #endregion
    }


    public static class ProfileOperationExtensions
    {
        #region Static methods

        /// <summary>
        ///     Sets new data value if <paramref name="operation"/> is not null.
        ///     Overwrite previous value with the same <paramref name="dataKey" />.
        ///     The key is case sensitive.
        /// </summary>
        [CanBeNull]
        public static ProfileOperation WithOperationData([CanBeNull] this ProfileOperation operation,
                                                         [NotNull] string dataKey,
                                                         [CanBeNull] object dataValue)
        {
            if (operation != null)
                operation[dataKey] = dataValue;

            return operation;
        }

        #endregion
    }
}