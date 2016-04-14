using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Ploeh.AutoFixture;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class CompletedSessionsProcessorQueueTests
    {
        [Fact]
        public async Task Add_ProcessCompletedSession()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var session = fixture.Create<ProfileSession>();

            var processor_service = fixture.Freeze<ICompletedSessionProcessorService>();
            processor_service.ShouldProcess(null).ReturnsForAnyArgs(true);


            // act
            fixture.Create<CompletedSessionsProcessorQueue>().Add(session);
            await Task.Delay(100).ConfigureAwait(false); // wait background processing task


            // assert
            await processor_service.Received(1)
                                   .ProcessAsync(session, Arg.Any<CancellationToken>())
                                   .ConfigureAwait(false);
        }
    }
}