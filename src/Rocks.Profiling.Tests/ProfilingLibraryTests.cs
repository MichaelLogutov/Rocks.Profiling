using FluentAssertions;
using Rocks.Helpers;
using Rocks.Profiling.Internal.AdoNetWrappers;
using Xunit;


namespace Rocks.Profiling.Tests
{
    public sealed class ProfilingLibraryTests
    {
        [Fact]
        public void Always_DbConnectionTypeShouldBeEquivalentTo()
        {
            // arrange
            ProfilingLibrary.Setup(() => null);

            // act
            var connection = "server=localhost; database=TestDb; Integrated Security=True; Max Pool Size=5".CreateDbConnection();
            var connectionType = connection.GetType();

            // assert
            connectionType.Should().Be(typeof(ProfiledDbConnection));
        }
    }
}