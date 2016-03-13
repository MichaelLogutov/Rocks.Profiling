using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Rocks.Profiling.Data
{
    /// <summary>
    ///     Represents information about some arbitrary operation during profiling (for example, method execution).
    /// </summary>
    public interface IProfileOperation : IDisposable
    {
        /// <summary>
        ///     Operation name.
        /// </summary>
        [NotNull]
        string Name { get; }

        /// <summary>
        ///     Operation category.
        /// </summary>
        [CanBeNull]
        string Category { get; }

        /// <summary>
        ///     Additional data about operation.
        ///     The key is case sensitive.
        /// </summary>
        [CanBeNull]
        IDictionary<string, object> Data { get; }

        /// <summary>
        ///     Gets or sets additional data for this operation by key.
        ///     The key is case sensitive.
        /// </summary>
        [CanBeNull]
        object this[[NotNull] string dataKey] { get; set; }


        /// <summary>
        ///     Adds new data.
        ///     Overwrite previous value with the same <paramref name="dataKey" />.
        ///     The key is case sensitive.
        /// </summary>
        void Add([NotNull] string dataKey, [CanBeNull] object dataValue);
    }
}