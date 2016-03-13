using System;
using FluentAssertions;
using Ploeh.AutoFixture;
using Rocks.Profiling.Internal.Implementation;
using Xunit;

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class ProfilingResultsProcessorTests
    {
        [Fact]
        public void Add_ByDefault_ShouldNotThrow()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var session = fixture.Create<ProfileSession>();


            // act
            Action act = () => fixture.Create<ProfilingResultsProcessor>().Add(session, null);


            // assert
            act.ShouldNotThrow();
        }
    }
}