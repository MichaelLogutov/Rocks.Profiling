using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ploeh.AutoFixture;
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
        public async Task StartAndStop_CorrectlySetsTheTimeOfTheRootOperation()
        {
            // arrange
            var sut = this.fixture.Create<ProfileSession>();


            // act
            using (sut.StartMeasure(new ProfileOperationSpecification("test")))
                await Task.Delay(100).ConfigureAwait(false);


            // assert
            sut.OperationsTreeRoot.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }
}