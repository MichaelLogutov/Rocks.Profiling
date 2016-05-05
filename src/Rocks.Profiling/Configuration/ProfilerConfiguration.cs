using System;
using System.Configuration;
using Rocks.Helpers;

namespace Rocks.Profiling.Configuration
{
    /// <summary>
    ///     Profiler configuration.
    /// </summary>
    public class ProfilerConfiguration : IProfilerConfiguration
    {
        public ProfilerConfiguration()
        {
            this.Enabled =
                ConfigurationManager.AppSettings["Profiling.Enabled"].ToBool() ??
                true;

            this.SessionMinimalDuration =
                ConfigurationManager.AppSettings["Profiling.SessionMinimalDuration"].ToTime() ??
                TimeSpan.FromMilliseconds(500);

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
        ///     If true then enables profiling the most expensive stream of operations.<br />
        ///     Value can be specified in application config key "Profiling.Enabled".<br />
        ///     Default value is true.
        /// </summary>
        public virtual bool Enabled { get; }


        /// <summary>
        ///     Any sessions with total operation duration less than specified will be ignored.<br />
        ///     Value can be specified in application config key "Profiling.SessionMinimalDuration".<br />
        ///     Default value is 500 ms.
        /// </summary>
        public virtual TimeSpan SessionMinimalDuration { get; }


        /// <summary>
        ///     Size of the buffer which will hold completed profile session results before it can be processed.<br />
        ///     If there is no room in buffer for new session results - they will be discarded.<br />
        ///     Value can be specified in application config key "Profiling.ResultsBufferSize".<br />
        ///     Default value is 10000.
        /// </summary>
        public virtual int ResultsBufferSize { get; }


        /// <summary>
        ///     Time to hold off processing resulting sessions to accumulate the batch of them.<br />
        ///     Value can be specified in application config key "ProfilingConfiguration.ResultsProcessBatchDelay".<br />
        ///     Default is 1 second.
        /// </summary>
        public virtual TimeSpan ResultsProcessBatchDelay { get; }


        /// <summary>
        ///     Maximum sessions batch size that can be processed at one time.<br />
        ///     Value can be specified in application config key "ProfilingConfiguration.ResultsProcessMaxBatchSize".<br />
        ///     Default is 10.
        /// </summary>
        public virtual int ResultsProcessMaxBatchSize { get; }


        /// <summary>
        ///     Specifies if operations needs to capture current call stack when started.<br />
        ///     Value can be specified in application config key "Profiling.CaptureCallStacks".<br />
        ///     Default is false.
        /// </summary>
        public virtual bool CaptureCallStacks { get; }
    }
}