using JetBrains.Annotations;

namespace Rocks.Profiling
{
    /// <summary>
    ///     Profiler factory service locator.
    /// </summary>
    public static class ProfilerFactory
    {
        #region Static fields

        private const string ProfilerInstanceKey = "__PROFILER_INSTANCE";

        [NotNull]
        private static readonly IProfiler SingletonInstance = CreateInstance();

        #endregion

        #region Static methods

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

        #endregion

        #region Private methods

        private static IProfiler CreateInstance()
        {
            return ProfilingLibrary.Container.GetInstance<IProfiler>();
        }

        #endregion
    }
}