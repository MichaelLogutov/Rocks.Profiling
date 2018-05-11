using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using AutoFixture;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Xunit;

namespace Rocks.Profiling.Tests.Models
{
    public class ProfileSessionTests
    {
        private readonly IFixture fixture;


        public ProfileSessionTests()
        {
            this.fixture = new FixtureBuilder().Build();
        }


        [Fact]
        public async Task StartAndStop_CorrectlySetsSessionDuration()
        {
            // arrange
            var sut = this.fixture.Create<ProfileSession>();


            // act
            using (sut.StartMeasure(new ProfileOperationSpecification("test")))
                await Task.Delay(100).ConfigureAwait(false);


            // assert
            sut.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }


        [Fact]
        public void StartAndStop_InSeparatedTask_ShouldNotThrow()
        {
            // arrange
            var profiler = this.fixture.Create<Profiler>();

            var task1 = this.CreateTask(profiler, "test1");
            var task2 = this.CreateTask(profiler, "test2");

            // act
            Func<Task> act = async () =>
                             {
                                 await task1.ConfigureAwait(false);
                                 await task2.ConfigureAwait(false);
                             };

            // assert
            act.Should().NotThrow();
        }


        private async Task<int> CreateTask(IProfiler profiler, string name)
        {
            using (profiler.Profile(new ProfileOperationSpecification(name)
                                    {
                                        Category = "Test"
                                    }))
            {
                await Task.Delay(100);
            }

            return 42;
        }
    }
}