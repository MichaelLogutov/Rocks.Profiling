using JetBrains.Annotations;

namespace Rocks.Profiling.Models
{
    public static class ProfileOperationExtensions
    {
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
    }
}