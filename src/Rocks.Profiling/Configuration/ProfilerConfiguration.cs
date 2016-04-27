using System;
using System.Collections.Generic;
using System.Configuration;
using JetBrains.Annotations;
using Rocks.Helpers;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Implementation;
using SimpleInjector;

namespace Rocks.Profiling.Configuration
{
    /// <summary>
    ///     Profiler configuration.
    /// </summary>
    public class ProfilerConfiguration : IProfilerConfiguration
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
        ///     Value can be specified in application config key "Profiling.Enabled".<br />
        ///     Default value is true.
        /// </summary>
        public bool Enabled { get; set; }


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

        [NotNull]
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
        /// <param name="lifestyle">Lifestyle of the overriden sevice. If null - singleton will be used.</param>
        public IProfilerConfiguration OverrideService<TService, TImplementation>(Lifestyle lifestyle = null)
            where TImplementation : TService
        {
            var service_override = new ProfilerServiceOverride<TService, TImplementation>(lifestyle ?? Lifestyle.Singleton);

            this.serviceOverrides.Add(service_override);

            return this;
        }

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

            this.Enabled =
                ConfigurationManager.AppSettings["Profiling.Enabled"].ToBool() ??
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