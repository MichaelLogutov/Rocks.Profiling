using System;
using System.Configuration;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Rocks.Helpers;

namespace Rocks.Profiling
{
    /// <summary>
    ///     Profiler configuration.
    /// </summary>
    public class ProfilerConfiguration
    {
        #region Public properties

        /// <summary>
        ///     If true then enables autoprofiling the most expensive stream of operations.<br />
        ///     Value can be specified in application config key "Profiling.AutoProfileEnabled".<br />
        ///     Default value is true.
        /// </summary>
        public bool AutoProfileEnabled { get; set; }

        /// <summary>
        ///     True if logging profiler internal events is enabled.<br />
        ///     Value can be specified in application config key "Profiling.LoggingEnabled".<br />
        ///     Default value is false.
        /// </summary>
        public bool LoggingEnabled { get; set; }

        /// <summary>
        ///     Error logging action. Will be called on unhandled exceptions if <see cref="LoggingEnabled" /> is true.<br />
        ///     The implementation of this action should be thread safe. <br />
        ///     Default value is null.
        /// </summary>
        [CanBeNull]
        public Action<Exception> ErrorLogger { get; set; }

        ///// <summary>
        /////     Any <see cref="DbCommand" /> events with duration less than specified will be ignored.<br />
        /////     Value can be specified in application config key "Profiling.DbCommandEventMinimalDuration".<br />
        /////     Default value is 1 ms.
        ///// </summary>
        //public TimeSpan DbCommandEventMinimalDuration { get; set; }

        #endregion

        #region Static methods

        public static ProfilerConfiguration FromAppConfig()
        {
            var result = new ProfilerConfiguration();

            result.AutoProfileEnabled = ConfigurationManager.AppSettings["Profiling.AutoProfileEnabled"].ToBool() ?? true;
            result.LoggingEnabled = ConfigurationManager.AppSettings["Profiling.LoggingEnabled"].ToBool() ?? false;

            //result.DbCommandEventMinimalDuration = ConfigurationManager.AppSettings["Profiling.DbCommandEventMinimalDuration"].ToTime() ??
            //                                       TimeSpan.FromMilliseconds(1);

            return result;
        }

        #endregion

        #region Public methods

        /// <summary>
        ///     Logs an exception to <see cref="ErrorLogger" /> if <see cref="LoggingEnabled" /> is true.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(Exception ex)
        {
            if (!this.LoggingEnabled)
                return;

            try
            {
                this.ErrorLogger?.Invoke(ex);
            }
                // ReSharper disable once RedundantCatchClause
            catch
            {
#if DEBUG
                // ReSharper disable once ThrowingSystemException
                throw;
#endif
            }
        }

        #endregion

        #region Protected properties

        /// <summary>
        ///     Returns true, if ADO.NET calls should be intercepted.
        /// </summary>
        internal bool ShouldInterceptAdoNet => this.AutoProfileEnabled;

        #endregion
    }
}