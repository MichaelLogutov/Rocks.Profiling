using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Data;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     Represents information about some arbitrary operation during profiling (for example, method execution).
    /// </summary>
    internal class ProfileOperation : IProfileOperation
    {
        #region Construct

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProfileOperation" /> class.
        /// </summary>
        public ProfileOperation([NotNull] string name,
                                string category = null,
                                IDictionary<string, object> data = null)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Argument is null or empty", nameof(name));

            this.Name = name;
            this.Category = category;

            if (data != null)
                this.Data = new Dictionary<string, object>(data, StringComparer.Ordinal);
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     Operation name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Operation category.
        /// </summary>
        public string Category { get; }

        /// <summary>
        ///     Additional data about operation.
        ///     The key is case sensitive.
        /// </summary>
        public IDictionary<string, object> Data { get; private set; }

        /// <summary>
        ///     Gets or sets additional data for this operation by key.
        ///     The key is case sensitive.
        /// </summary>
        public object this[string dataKey]
        {
            get { return this.Data?[dataKey]; }
            set { this.Add(dataKey, value); }
        }

        /// <summary>
        ///     Profiler for this operation.
        /// </summary>
        [NotNull]
        public IProfiler Profiler { get; set; }

        /// <summary>
        ///     Profile session for this operation.
        /// </summary>
        [NotNull]
        public ProfileSession Session { get; set; }

        /// <summary>
        ///     Start time of the operation.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        ///     End time of the operation.
        /// </summary>
        public TimeSpan? EndTime { get; set; }

        /// <summary>
        ///     Parent node.
        /// </summary>
        [CanBeNull]
        public ProfileOperation Parent { get; set; }

        /// <summary>
        ///     A list of child nodes.
        /// </summary>
        [CanBeNull]
        public IList<ProfileOperation> ChildNodes { get; set; }

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

            result += this.Name;

            return result;
        }


        /// <summary>
        ///     Adds new child node.
        /// </summary>
        public void Add([NotNull] ProfileOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (this.ChildNodes == null)
                this.ChildNodes = new List<ProfileOperation>();

            this.ChildNodes.Add(operation);
        }

        #endregion

        #region IProfileOperation Members

        /// <summary>
        ///     Adds new data.
        ///     Overwrite previous value with the same <paramref name="dataKey" />.
        ///     The key is case sensitive.
        /// </summary>
        public void Add(string dataKey, object dataValue)
        {
            if (string.IsNullOrEmpty(dataKey))
                throw new ArgumentException("Argument is null or empty", nameof(dataKey));

            if (this.Data == null)
                this.Data = new Dictionary<string, object>(StringComparer.Ordinal);

            this.Data[dataKey] = dataValue;
        }


        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.IsCompleted)
                return;

            try
            {
                this.Session.StopMeasure(this);
                this.IsCompleted = true;
            }
            catch (Exception ex)
            {
                this.Profiler.Configuration.LogError(ex);
            }
        }

        #endregion
    }
}