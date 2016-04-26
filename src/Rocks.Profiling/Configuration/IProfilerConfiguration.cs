using System;
using SimpleInjector;

namespace Rocks.Profiling.Configuration
{
    public interface IProfilerConfiguration
    {
        /// <summary>
        ///     If true then enables profiling the most expensive stream of operations.<br />
        ///     Value can be specified in application config key "Profiling.ProfilingEnabled".<br />
        ///     Default value is true.
        /// </summary>
        bool ProfilingEnabled { get; set; }

        /// <summary>
        ///     Any sessions with total operation duration less than specified will be ignored.<br />
        ///     Value can be specified in application config key "Profiling.SessionMinimalDuration".<br />
        ///     Default value is 500 ms.
        /// </summary>
        TimeSpan SessionMinimalDuration { get; set; }

        /// <summary>
        ///     Size of the buffer which will hold completed profile session results before it can be processed.<br />
        ///     If there is no room in buffer for new session results - they will be discarded.<br />
        ///     Value can be specified in application config key "Profiling.ResultsBufferSize".<br />
        ///     Default value is 10000.
        /// </summary>
        int ResultsBufferSize { get; set; }

        /// <summary>
        ///     Time to hold off processing resulting sessions to accumulate the batch of them.<br />
        ///     Value can be specified in application config key "ProfilingConfiguration.ResultsProcessBatchDelay".<br />
        ///     Default is 1 second.
        /// </summary>
        TimeSpan ResultsProcessBatchDelay { get; set; }

        /// <summary>
        ///     Maximum sessions batch size that can be processed at one time.<br />
        ///     Value can be specified in application config key "ProfilingConfiguration.ResultsProcessMaxBatchSize".<br />
        ///     Default is 10.
        /// </summary>
        int ResultsProcessMaxBatchSize { get; set; }

        /// <summary>
        ///     Specifies if operations needs to capture current call stack when started.<br />
        ///     Value can be specified in application config key "Profiling.CaptureCallStacks".<br />
        ///     Default is false.
        /// </summary>
        bool CaptureCallStacks { get; set; }


        /// <summary>
        ///     Overrides specified <typeparamref name="TService" /> implementation.
        /// </summary>
        /// <param name="lifestyle">Lifestyle of the overriden sevice. If null - singleton will be used.</param>
        IProfilerConfiguration OverrideService<TService, TImplementation>(Lifestyle lifestyle = null)
            where TImplementation : TService;
    }
}