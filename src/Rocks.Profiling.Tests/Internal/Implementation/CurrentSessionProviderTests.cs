using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using AutoFixture;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Xunit;

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class CurrentSessionProviderTests
    {
        private readonly IFixture fixture;


        public CurrentSessionProviderTests()
        {
            this.fixture = new FixtureBuilder().Build();
        }


        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(32)]
        [InlineData(128)]
        public async Task Set_ThenGet_InParallelTasks_ReturnsDifferentResults(int dop)
        {
            // arrange
            var sut = this.fixture.Create<CurrentSessionProvider>();

            var sessions = Enumerable.Range(0, dop)
                                     .Select(_ => this.fixture.Create<ProfileSession>())
                                     .ToList();


            // act
            await Task.WhenAll
                (
                    sessions.Select(session => Task.Run(() =>
                                                        {
                                                            sut.Set(session);
                                                            sut.Get().Should().BeSameAs(session);
                                                        }))
                ).ConfigureAwait(false);


            // assert
        }
    }
}