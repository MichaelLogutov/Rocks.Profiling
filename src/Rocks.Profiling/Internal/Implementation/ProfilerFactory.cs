using JetBrains.Annotations;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     Profiler factory service locator.
    /// </summary>
    internal static class ProfilerFactory
    {
        private const string ProfilerInstanceKey = "__PROFILER_INSTANCE";

        [NotNull]
        private static readonly IProfiler SingletonInstance = CreateInstance();


        /// <summary>
        ///     Gets the current profiler instance.
        /// </summary>
        [NotNull]
        public static IProfiler GetCurrentProfiler()
        {
            IProfiler result;

            var http_context = ProfilingLibrary.HttpContextFactory();
            if (http_context != null)
            {
                result = http_context.Items[ProfilerInstanceKey] as IProfiler;

                if (result == null)
                {
                    result = CreateInstance();
                    http_context.Items[ProfilerInstanceKey] = result;
                }
            }
            else
                result = SingletonInstance;

            return result;
        }


        private static IProfiler CreateInstance()
        {
            return ProfilingLibrary.Container.GetInstance<IProfiler>();
        }
    }
}