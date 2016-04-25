using System;
using System.Collections.Generic;
using System.Configuration;
using Rocks.Helpers;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Implementation;
using SimpleInjector;

namespace Rocks.Profiling
{
    /// <summary>
    ///     Profiler configuration.
    /// </summary>
    public class ProfilerConfiguration
    {
        #region Private readonly fields

        private readonly IList<IProfilerServiceOverride> serviceOverrides;

        #endregion

        #region Construct

        public ProfilerConfiguration()
        {
            this.serviceOverrides = new List<IProfilerServiceOverride>();
        }

        #endregion

        #region Public properties

        /// <summary>
        ///     If true then enables profiling the most expensive stream of operations.<br />
        ///     Value can be specified in application config key "Profiling.ProfilingEnabled".<br />
        ///     Default value is true.
        /// </summary>
        public bool ProfilingEnabled { get; set; }


        /// <summary>
        ///     Any sessions with total operation duration less than specified will be ignored.<br />
        ///     Value can be specified in application config key "Profiling.SessionMinimalDuration".<br />
        ///     Default value is 500 ms.
        /// </summary>
        public TimeSpan SessionMinimalDuration { get; set; }


        /// <summary>
        ///     Size of the buffer which will hold completed profile session results before it can be processed.<br />
        ///     If there is no room in buffer for new session results - they will be discarded.<br />
        ///     Value can be specified in application config key "Profiling.ResultsBufferSize".<br />
        ///     Default value is 10000.
        /// </summary>
        public int ResultsBufferSize { get; set; }


        /// <summary>
        ///     Time to hold off processing resulting sessions to accumulate the batch of them.<br />
        ///     Value can be specified in application config key "ProfilingConfiguration.ResultsProcessBatchDelay".<br />
        ///     Default is 1 second.
        /// </summary>
        public TimeSpan ResultsProcessBatchDelay { get; set; }


        /// <summary>
        ///     Maximum sessions batch size that can be processed at one time.<br />
        ///     Value can be specified in application config key "ProfilingConfiguration.ResultsProcessMaxBatchSize".<br />
        ///     Default is 10.
        /// </summary>
        public int ResultsProcessMaxBatchSize { get; set; }


        /// <summary>
        ///     Specifies if operations needs to capture current call stack when started.<br />
        ///     Value can be specified in application config key "Profiling.CaptureCallStacks".<br />
        ///     Default is false.
        /// </summary>
        public bool CaptureCallStacks { get; set; }

        #endregion

        #region Static methods

        public static ProfilerConfiguration FromAppConfig()
        {
            var result = new ProfilerConfiguration();

            result.Load();

            return result;
        }

        #endregion

        #region Public methods

        /// <summary>
        ///     Overrides specified <typeparamref name="TService" /> implementation.
        /// </summary>
        public ProfilerConfiguration OverrideService<TService, TImplementation>()
            where TImplementation : TService
        {
            this.serviceOverrides.Add(new ProfilerServiceOverride<TService, TImplementation>());
            return this;
        }

        #endregion

        #region Protected properties

        /// <summary>
        ///     Returns true, if ADO.NET calls should be intercepted.
        /// </summary>
        internal bool ShouldInterceptAdoNet => this.ProfilingEnabled;

        #endregion

        #region Protected methods

        /// <summary>
        ///     Loads configuration values.<br />
        ///     Default implementation loads values from application settings.
        /// </summary>
        protected virtual void Load()
        {
            this.SessionMinimalDuration =
                ConfigurationManager.AppSettings["Profiling.SessionMinimalDuration"].ToTime() ??
                TimeSpan.FromMilliseconds(500);

            this.ProfilingEnabled =
                ConfigurationManager.AppSettings["Profiling.ProfilingEnabled"].ToBool() ??
                true;

            this.ResultsBufferSize =
                (ConfigurationManager.AppSettings["Profiling.ResultsBufferSize"].ToInt() ??
                 10000).RequiredGreaterThan(0, nameof(this.ResultsBufferSize));

            this.ResultsProcessBatchDelay =
                ConfigurationManager.AppSettings["Profiling.ResultsProcessBatchDelay"].ToTime()
                ?? TimeSpan.FromSeconds(1);

            this.ResultsProcessMaxBatchSize =
                (ConfigurationManager.AppSettings["Profiling.ResultsProcessMaxBatchSize"].ToInt() ??
                 10).RequiredGreaterThan(0, nameof(this.ResultsProcessMaxBatchSize));

            this.CaptureCallStacks =
                ConfigurationManager.AppSettings["Profiling.CaptureCallStacks"].ToBool() ??
                false;
        }


        /// <summary>
        ///     Performs configuration of the DI container based on configuration values.
        /// </summary>
        internal void ConfigureServices(Container container)
        {
            foreach (var service_override in this.serviceOverrides)
                service_override.Apply(container);
        }

        #endregion
    }
}