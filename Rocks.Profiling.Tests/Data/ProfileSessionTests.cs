using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Ploeh.AutoFixture;
using Rocks.Profiling.Data;
using Xunit;

namespace Rocks.Profiling.Tests.Data
{
    public class ProfileSessionTests
    {
        [Fact]
        public async Task StartAndStop_CorrectlySetsTheTimeOfTheRootOperation ()
        {
            // arrange
            var fixture = new FixtureBuilder ().Build ();

            var sut = fixture.Create<ProfileSession>();


            // act
            using (sut.StartMeasure("test"))
                await Task.Delay(100).ConfigureAwait(false);


            // assert
            sut.OperationsTreeRoot.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        }
    }
}
