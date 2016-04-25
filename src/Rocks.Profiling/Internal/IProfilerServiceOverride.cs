using JetBrains.Annotations;
using SimpleInjector;

namespace Rocks.Profiling.Internal
{
    internal interface IProfilerServiceOverride
    {
        /// <summary>
        ///     Applies service registration override.
        /// </summary>
        void Apply([NotNull] Container container);
    }
}