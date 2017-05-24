using JetBrains.Annotations;
using Ploeh.AutoFixture;
using Rocks.Profiling.Models;
using Xunit;

namespace Rocks.Profiling.Tests
{
    public class CommonTests
    {
        private readonly IFixture fixture;


        public CommonTests()
        {
            this.fixture = new FixtureBuilder().Build();
        }


        [Fact]
        public void FixtureCreate_Always_CreatesOperation()
        {
            // arrange


            // act
            this.fixture.Create<ProfileOperation>();


            // assert
        }


        [UsedImplicitly]
        private class MyClass
        {
            private readonly IProfiler profiler;


            public MyClass(IProfiler profiler)
            {
                this.profiler = profiler;
            }


            public void Run()
            {
                using (this.profiler.Profile(new ProfileOperationSpecification("test")))
                {
                }
            }
        }


        [Fact]
        public void ProfileInTests_DoesNotGeneratesException()
        {
            // arrange


            // act
            this.fixture.Create<MyClass>().Run();


            // assert
        }
    }
}