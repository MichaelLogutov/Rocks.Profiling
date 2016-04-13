using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling
{
    /// <summary>
    ///     Provides methods to work with profiler.
    /// </summary>
    public interface IProfiler
    {
        #region Public properties

        /// <summary>
        ///     Current profiler configuration.
        /// </summary>
        ProfilerConfiguration Configuration { get; }

        #endregion

        #region Public methods

        /// <summary>
        ///     Creates new profile session.
        /// </summary>
        void Start([CanBeNull] IDictionary<string, object> additionalSessionData = null);


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="specification"/>.<br />
        ///     Uppon disposing will store the results of measurement in the current session.<br />
        ///     If there is no session started - returns dummy operation that will do nothing.
        /// </summary>
        [CanBeNull, MustUseReturnValue]
        ProfileOperation Profile([NotNull] ProfileOperationSpecification specification);


        /// <summary>
        ///     Stops current profile session and stores the results.
        /// </summary>
        void Stop([CanBeNull] IDictionary<string, object> additionalSessionData = null);

        #endregion
    }
}