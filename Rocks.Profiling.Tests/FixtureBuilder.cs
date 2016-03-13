using System;
using System.Collections.Generic;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Kernel;
using Rocks.Profiling.Loggers;
using SimpleInjector;

namespace Rocks.Profiling.Tests
{
    public class FixtureBuilder
    {
        #region Private readonly fields

        private readonly IList<Action<IFixture>> additionalInitializers;

        #endregion

        #region Construct

        public FixtureBuilder()
        {
            this.additionalInitializers = new List<Action<IFixture>>();
        }

        #endregion

        #region Public methods

        public FixtureBuilder With(Action<IFixture> initializer)
        {
            this.additionalInitializers.Add(initializer);
            return this;
        }


        public IFixture Build()
        {
            var fixture = new Fixture();
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            fixture.Customize(new AutoConfiguredNSubstituteCustomization());

            fixture.Inject<IProfilerLogger>(new RethrowProfilerLogger());

            {
                var container = new Container { Options = { AllowOverridingRegistrations = true } };
                container.ResolveUnregisteredType += (sender, args) =>
                                                     {
                                                         args.Register(() => new SpecimenContext(fixture).Resolve
                                                                                 (new SeededRequest(args.UnregisteredServiceType, null)));
                                                     };

                fixture.Inject(container);
            }

            foreach (var initializer in this.additionalInitializers)
                initializer(fixture);

            return fixture;
        }

        #endregion
    }
}