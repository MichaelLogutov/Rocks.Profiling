using System;
using System.Collections.Generic;
using System.Web;
using Moq;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Kernel;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Loggers;
using SimpleInjector;

namespace Rocks.Profiling.Tests
{
    public class FixtureBuilder
    {
        private readonly IList<Action<IFixture>> additionalInitializers;


        public FixtureBuilder()
        {
            this.additionalInitializers = new List<Action<IFixture>>();
        }


        public FixtureBuilder With(Action<IFixture> initializer)
        {
            this.additionalInitializers.Add(initializer);
            return this;
        }


        public IFixture Build()
        {
            var fixture = new Fixture();
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            fixture.Customize(new AutoConfiguredMoqCustomization());

            {
                var logger_mock = new Mock<RethrowProfilerLogger>();
                logger_mock.CallBase = true;
                fixture.Inject<IProfilerLogger>(logger_mock.Object);
            }

            fixture.Inject<Func<HttpContextBase>>(() => null);

            {
                var container = new Container { Options = { AllowOverridingRegistrations = true } };
                container.ResolveUnregisteredType += (sender, args) =>
                                                     {
                                                         args.Register(() => new SpecimenContext(fixture).Resolve
                                                                                 (new SeededRequest(args.UnregisteredServiceType, null)));
                                                     };

                fixture.Inject(container);
            }

            fixture.Customize<IProfiler>
                (c => c.FromFactory(() =>
                                    {
                                        var profiler = new Mock<IProfiler>();
                                        var configuration = (IProfilerConfiguration) new SpecimenContext(fixture).Resolve
                                                                                         (new SeededRequest(typeof(IProfilerConfiguration), null));

                                        profiler.Setup(x => x.Configuration).Returns(configuration);

                                        return profiler.Object;
                                    })
                       .OmitAutoProperties());


            fixture.Customize<IProfilerConfiguration>
                (c => c.FromFactory(() =>
                                    {
                                        var mock = new Mock<ProfilerConfiguration>();
                                        mock.CallBase = true;

                                        return mock.Object;
                                    })
                       .OmitAutoProperties());

            foreach (var initializer in this.additionalInitializers)
                initializer(fixture);

            return fixture;
        }
    }
}