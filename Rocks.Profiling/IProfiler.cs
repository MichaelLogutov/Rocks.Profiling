using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Data;

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
        void Start();


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="name"/>, <paramref name="category"/> and additional <paramref name="data"/>.<br />
        ///     Uppon disposing will store the results of measurement in the current session.<br />
        ///     If there is no session started - returns dummy operation that will do nothing.
        /// </summary>
        [NotNull]
        ProfileOperation Profile([NotNull] string name,
                                 [CanBeNull] string category = null,
                                 [CanBeNull] IDictionary<string, object> data = null);


        /// <summary>
        ///     Stops current profile session and stores the results.
        /// </summary>
        void Stop([CanBeNull] IDictionary<string, object> additionalSessionData = null);

        #endregion
    }
}