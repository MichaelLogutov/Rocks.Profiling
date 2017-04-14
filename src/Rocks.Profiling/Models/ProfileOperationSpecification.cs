using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Info about the operation to be profiled.
    /// </summary>
    public class ProfileOperationSpecification
    {
        /// <exception cref="ArgumentException">Argument <paramref name="name"/> is null or whitespace</exception>
        public ProfileOperationSpecification([NotNull] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument is null or whitespace", nameof(name));

            this.Name = name;
        }


        /// <exception cref="ArgumentException">Argument <paramref name="name"/> is null or whitespace</exception>
        public ProfileOperationSpecification([CanBeNull] string category, [NotNull] string name)
            : this(name)
        {
            this.Category = category;
        }


        /// <exception cref="ArgumentException">Argument <paramref name="name"/> is null or whitespace</exception>
        public ProfileOperationSpecification([CanBeNull] string category, [NotNull] string name, [CanBeNull] string resource)
            : this(category, name)
        {
            this.Resource = resource;
        }


        /// <summary>
        ///     Name of the operation.<br />
        ///     For a list of predefined names see <see cref="ProfileOperationNames" />.
        /// </summary>
        [NotNull]
        public string Name { get; }

        /// <summary>
        ///     A category of the operation.<br />
        ///     For a list of predefined categories see <see cref="ProfileOperationCategories" />.<br />
        ///     Default is null.
        /// </summary>
        [CanBeNull]
        public string Category { get; set; }

        /// <summary>
        ///     The resource which current operation is workgin with.<br />
        ///     For example, "MyDbServer - MyDatabase".
        /// </summary>
        [CanBeNull]
        public string Resource { get; set; }

        /// <summary>
        ///     Additional data that will be stored with the operation (for example, SQL text).<br />
        ///     Default is null.
        /// </summary>
        [CanBeNull]
        public IDictionary<string, object> Data { get; set; }

        /// <summary>
        ///     A maximum duration of the operation that considered "normal".<br />
        ///     If operation has duration longer that specified - it will always trigger session processing.<br />
        ///     Default is null (not specified).
        /// </summary>
        public TimeSpan? NormalDuration { get; set; }
    }
}