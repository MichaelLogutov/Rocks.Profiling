using System;
using FluentAssertions;
using Ploeh.AutoFixture;
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
            AddOperation(session);


            // act
            var result = fixture.Create<CompletedSessionProcessorService>().ShouldProcess(new CompletedSessionInfo(session));


            // assert
            result.Should().BeFalse();
        }


        [Fact]
        public void ShouldProcess_SessionHasTotalTimeMoreThanMinimum_ReturnsTrue()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            ConfigureSessionMinimalDuration(fixture, TimeSpan.FromSeconds(1));

            var session = fixture.Create<ProfileSession>();
            AddOperation(session, 0, 100);
            AddOperation(session, 100, 1000);


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


        private static void AddOperation(ProfileSession session)
        {
            var operation = new ProfileOperation("test");

            session.StartMeasure(operation);
            session.StopMeasure(operation);
        }


        private static void AddOperation(ProfileSession session, int startTimeMs, int endTimeMs)
        {
            var operation = new ProfileOperation("test");

            session.StartMeasure(operation);
            session.StopMeasure(operation);

            operation.StartTime = TimeSpan.FromMilliseconds(startTimeMs);
            operation.EndTime = TimeSpan.FromMilliseconds(endTimeMs);
        }

        #endregion
    }
}