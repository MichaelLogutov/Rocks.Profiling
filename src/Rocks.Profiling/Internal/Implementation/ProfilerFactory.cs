using JetBrains.Annotations;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     Profiler factory service locator.
    /// </summary>
    internal static class ProfilerFactory
    {
        [NotNull]
        public static IProfiler GetCurrentProfiler() => ProfilingLibrary.Container.GetInstance<IProfiler>();
    }
}