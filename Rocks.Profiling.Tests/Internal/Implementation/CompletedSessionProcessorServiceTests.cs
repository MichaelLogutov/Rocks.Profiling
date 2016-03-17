using System;
using System.Threading.Tasks;
using FluentAssertions;
using Ploeh.AutoFixture;
using Rocks.Profiling.Data;
using Rocks.Profiling.Internal.Implementation;
using Xunit;

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class CompletedSessionProcessorServiceTests
    {
        #region Public methods

        [Fact]
        public void ShouldProcess_SessionHasNoOperations_ReturnsFalse()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();


            var session = fixture.Create<ProfileSession>();


            // act
            var result = fixture.Create<CompletedSessionProcessorService>().ShouldProcess(new CompletedSessionInfo(session));


            // assert
            result.Should().BeFalse();
        }


        [Fact]
        public void ShouldProcess_SessionHasTotalTimeLessThanMinimum_ReturnsFalse()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            ConfigureSessionMinimalDuration(fixture, TimeSpan.FromSeconds(1));

            var session = fixture.Create<ProfileSession>();
            using (session.StartMeasure("test"))
            {
            }

            // act
            var result = fixture.Create<CompletedSessionProcessorService>().ShouldProcess(new CompletedSessionInfo(session));


            // assert
            result.Should().BeFalse();
        }


        [Fact]
        public async Task ShouldProcess_SessionHasTotalTimeMoreThanMinimum_ReturnsTrue()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            ConfigureSessionMinimalDuration(fixture, TimeSpan.FromMilliseconds(100));

            var session = fixture.Create<ProfileSession>();

            using (session.StartMeasure("test"))
                await Task.Delay(101).ConfigureAwait(false);


            // act
            var result = fixture.Create<CompletedSessionProcessorService>().ShouldProcess(new CompletedSessionInfo(session));


            // assert
            result.Should().BeTrue();
        }

        #endregion

        #region Private methods

        private static void ConfigureSessionMinimalDuration(IFixture fixture, TimeSpan duration)
        {
            var configuration = fixture.Freeze<ProfilerConfiguration>();
            configuration.SessionMinimalDuration = duration;
        }

        #endregion
    }
}