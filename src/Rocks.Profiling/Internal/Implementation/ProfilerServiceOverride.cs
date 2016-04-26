using System;
using JetBrains.Annotations;
using SimpleInjector;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class ProfilerServiceOverride<TService, TImplementation> : IProfilerServiceOverride
        where TImplementation : TService
    {
        public ProfilerServiceOverride([NotNull] Lifestyle lifestyle)
        {
            if (lifestyle == null)
                throw new ArgumentNullException(nameof(lifestyle));

            this.Lifestyle = lifestyle;
        }


        /// <summary>
        ///     Lifestyle of the overriden service.
        /// </summary>
        [NotNull]
        public Lifestyle Lifestyle { get; }


        /// <summary>
        ///     Applies service registration override.
        /// </summary>
        public void Apply(Container container)
        {
            container.Register(typeof (TService), typeof (TImplementation), this.Lifestyle);
        }
    }
}