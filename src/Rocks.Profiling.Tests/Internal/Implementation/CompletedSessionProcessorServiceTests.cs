using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Ploeh.AutoFixture;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Xunit;
// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class CompletedSessionProcessorServiceTests
    {
        #region Private readonly fields

        private readonly IFixture fixture;

        #endregion

        #region Construct

        public CompletedSessionProcessorServiceTests()
        {
            this.fixture = new FixtureBuilder().Build();

            this.fixture.Freeze<ICompletedSessionProcessingFilter>()
                .ShouldProcess(null)
                .ReturnsForAnyArgs(true);
        }

        #endregion

        #region Public methods

        [Fact]
        public void ShouldProcess_SessionHasNoOperations_ReturnsFalse()
        {
            // arrange
            var session = this.fixture.Create<ProfileSession>();


            // act
            var result = this.fixture.Create<CompletedSessionProcessorService>().ShouldProcess(session);


            // assert
            result.Should().BeFalse();
        }


        [Fact]
        public void ShouldProcess_SessionHasTotalTimeLessThanMinimum_ReturnsFalse()
        {
            // arrange
            this.ConfigureSessionMinimalDuration(TimeSpan.FromSeconds(1));

            var session = this.fixture.Create<ProfileSession>();
            using (session.StartMeasure(new ProfileOperationSpecification("test")))
            {
            }

            // act
            var result = this.fixture.Create<CompletedSessionProcessorService>().ShouldProcess(session);


            // assert
            result.Should().BeFalse();
        }


        [Fact]
        public async Task ShouldProcess_SessionHasTotalTimeMoreThanMinimum_ReturnsTrue()
        {
            // arrange
            this.ConfigureSessionMinimalDuration(TimeSpan.FromMilliseconds(100));

            var session = this.fixture.Create<ProfileSession>();

            using (session.StartMeasure(new ProfileOperationSpecification("test")))
                await Task.Delay(101).ConfigureAwait(false);


            // act
            var result = this.fixture.Create<CompletedSessionProcessorService>().ShouldProcess(session);


            // assert
            result.Should().BeTrue();
        }


        [Fact]
        public async Task ShouldProcess_SessionHasTotalTimeLessThanMinimum_ButHasOperationLongerThanNormalDuration_ReturnsTrue()
        {
            // arrange
            this.ConfigureSessionMinimalDuration(TimeSpan.FromSeconds(10));

            var session = this.fixture.Create<ProfileSession>();
            using (session.StartMeasure(new ProfileOperationSpecification("test") { NormalDuration = TimeSpan.FromMilliseconds(1) }))
                await Task.Delay(10).ConfigureAwait(false);


            // act
            var result = this.fixture.Create<CompletedSessionProcessorService>().ShouldProcess(session);


            // assert
            result.Should().BeTrue();
        }

        #endregion

        #region Private methods

        private void ConfigureSessionMinimalDuration(TimeSpan duration)
        {
            var configuration = this.fixture.Freeze<ProfilerConfiguration>();
            configuration.SessionMinimalDuration = duration;
        }

        #endregion
    }
}