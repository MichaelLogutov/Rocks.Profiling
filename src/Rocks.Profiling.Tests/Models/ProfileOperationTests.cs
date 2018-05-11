using System;
using FluentAssertions;
using AutoFixture;
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
            var session = this.fixture.Create<ProfileSession>();

            var sut = new ProfileOperation(1,
                                           session.Profiler,
                                           session,
                                           new ProfileOperationSpecification(name) { Category = category },
                                           null);

            sut.Resource = resource;


            // act
            var result = sut.FullName;


            // assert
            result.Should().Be(expected);
        }


        [Fact]
        public void Always_ReturnsCorrectDateTimeInUtc()
        {
            // arrange
            var session = this.fixture.Create<ProfileSession>();
            var expected_date_time = DateTime.UtcNow;


            // act
            var sut = new ProfileOperation(1,
                                           session.Profiler,
                                           session,
                                           this.fixture.Create<ProfileOperationSpecification>(),
                                           null);


            // assert
            sut.StartDate.Should().BeAfter(expected_date_time).And.BeCloseTo(expected_date_time, 1000);
        }
    }
}