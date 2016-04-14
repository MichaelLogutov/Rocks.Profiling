using System.Linq;
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


        [Fact]
        public void GetDescendantsAndSelf_WithSubChildren_CorrectlyReturnsAllDescendants()
        {
            // arrange
            var session = this.fixture.Create<ProfileSession>();

            var sut = new ProfileOperation(1, session, new ProfileOperationSpecification("a"));
            var b = new ProfileOperation(2, session, new ProfileOperationSpecification("b"));
            var c = new ProfileOperation(3, session, new ProfileOperationSpecification("c"));
            var d = new ProfileOperation(4, session, new ProfileOperationSpecification("d"));

            sut.Add(b);
            sut.Add(c);
            b.Add(d);


            // act
            var result = sut.GetDescendantsAndSelf();


            // assert
            result.Select(x => x.Name).Should().Equal("a", "b", "d", "c");
        }
    }
}