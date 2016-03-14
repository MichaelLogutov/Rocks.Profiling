using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Rocks.Profiling.Data
{
    /// <summary>
    ///     Represents information about some arbitrary operation during profiling (for example, method execution).
    /// </summary>
    public class ProfileOperation : IEnumerable<ProfileOperation>, IDisposable
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
        [NotNull]
        public string Name { get; }

        /// <summary>
        ///     Operation category.
        /// </summary>
        [CanBeNull]
        public string Category { get; }

        /// <summary>
        ///     Additional data about operation.
        ///     The key is case sensitive.
        /// </summary>
        [CanBeNull]
        public IDictionary<string, object> Data { get; set; }

        /// <summary>
        ///     Gets or sets additional data for this operation by key.
        ///     The key is case sensitive.
        /// </summary>
        public object this[[CanBeNull] string dataKey]
        {
            get { return this.Data?[dataKey]; }
            // ReSharper disable once AssignNullToNotNullAttribute
            set { this.Add(dataKey, value); }
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
        ///     Parent node.
        /// </summary>
        [CanBeNull]
        public ProfileOperation Parent { get; }

        /// <summary>
        ///     A list of child nodes.
        /// </summary>
        [CanBeNull]
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
        ///     Adds new data.
        ///     Overwrite previous value with the same <paramref name="dataKey" />.
        ///     The key is case sensitive.
        /// </summary>
        public void Add([NotNull] string dataKey, [CanBeNull] object dataValue)
        {
            if (string.IsNullOrEmpty(dataKey))
                throw new ArgumentException("Argument is null or empty", nameof(dataKey));

            if (this.Data == null)
                this.Data = new Dictionary<string, object>(StringComparer.Ordinal);

            this.Data[dataKey] = dataValue;
        }


        /// <summary>
        ///     Gets total duration of child nodes.
        /// </summary>
        public TimeSpan GetTotalDuration()
        {
            if (this.childNodes == null)
                return TimeSpan.Zero;

            var total_ticks = 0L;

            foreach (var operation in this.childNodes)
            {
                if (operation.EndTime == null)
                    continue;

                total_ticks += (operation.EndTime.Value - operation.StartTime).Ticks;
            }

            return new TimeSpan(total_ticks);
        }


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

            result += this.Name;

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

        #region IEnumerable<ProfileOperation> Members

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<ProfileOperation> GetEnumerator()
        {
            if (this.childNodes == null)
                return ((IEnumerable<ProfileOperation>) new ProfileOperation[0]).GetEnumerator();

            return this.childNodes.GetEnumerator();
        }


        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.childNodes == null)
                return new ProfileOperation[0].GetEnumerator();

            return ((IEnumerable) this.childNodes).GetEnumerator();
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
}