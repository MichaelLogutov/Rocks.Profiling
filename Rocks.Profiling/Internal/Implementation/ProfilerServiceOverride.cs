using SimpleInjector;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class ProfilerServiceOverride<TService, TImplementation> : IProfilerServiceOverride
        where TImplementation : TService
    {
        /// <summary>
        ///     Applies service registration override.
        /// </summary>
        public void Apply(Container container)
        {
            container.RegisterSingleton(typeof (TService), typeof (TImplementation));
        }
    }
}