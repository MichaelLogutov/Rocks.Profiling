using FluentAssertions;
using Ploeh.AutoFixture;
using Rocks.Profiling.Models;
using Xunit;

namespace Rocks.Profiling.Tests.Models
{
    public class ProfileOperationTests
    {
        private readonly IFixture fixture;


        public ProfileOperationTests()
        {
            this.fixture = new FixtureBuilder().Build();
        }


        [Theory]
        [InlineData(null, "name", null, "name")]
        [InlineData("category", "name", null, "category::name")]
        [InlineData(null, "name", "resource", "name::resource")]
        [InlineData("category", "name", "resource", "category::name::resource")]
        public void Always_ReturnsCorrectFullName(string category, string name, string resource, string expected)
        {
            // arrange
            var sut = new ProfileOperation(1,
                                           this.fixture.Create<ProfileSession>(),
                                           new ProfileOperationSpecification(name) { Category = category });

            sut.Resource = resource;


            // act
            var result = sut.FullName;


            // assert
            result.Should().Be(expected);
        }
    }
}