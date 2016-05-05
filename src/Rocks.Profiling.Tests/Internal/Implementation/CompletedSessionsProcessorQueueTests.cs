using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Ploeh.AutoFixture;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class CompletedSessionsProcessorQueueTests
    {
        private readonly IFixture fixture;
        private readonly ICompletedSessionProcessorService processorService;
        private readonly IProfilerConfiguration configuration;


        public CompletedSessionsProcessorQueueTests()
        {
            this.fixture = new FixtureBuilder().Build();

            this.configuration = this.fixture.Freeze<IProfilerConfiguration>();

            this.processorService = this.fixture.Freeze<ICompletedSessionProcessorService>();
            this.processorService.ShouldProcess(null).ReturnsForAnyArgs(true);
        }


        [Fact]
        public async Task Add_One_ThenDelay_ThenAnother_WaitsForBatchTimeForTheSecond()
        {
            // arrange
            this.configuration.ResultsProcessBatchDelay.Returns(TimeSpan.FromMilliseconds(1000));

            var session1 = this.CreateSession(1);
            var session2 = this.CreateSession(2);

            var stored_sessions = this.CaptureStoredSessions();

            var sut = this.fixture.Create<CompletedSessionsProcessorQueue>();


            // act
            sut.Add(session1);
            await Task.Delay(300).ConfigureAwait(false);
            var result1 = stored_sessions.ToArray();

            sut.Add(session2);

            await Task.Delay(1200).ConfigureAwait(false);
            var result2 = stored_sessions.ToArray();


            // assert
            result1.Should().BeEmpty();
            result2.Should().Equal("1, 2");
        }


        [Fact]
        public async Task Add_ThreeTimesInARow_WithMaxBatchSizeTwo_ProcessTwoFirstSessions()
        {
            // arrange
            this.configuration.ResultsProcessBatchDelay.Returns(TimeSpan.FromMilliseconds(500));
            this.configuration.ResultsProcessMaxBatchSize.Returns(2);

            var session1 = this.CreateSession(1);
            var session2 = this.CreateSession(2);
            var session3 = this.CreateSession(3);

            var stored_sessions = this.CaptureStoredSessions();

            var sut = this.fixture.Create<CompletedSessionsProcessorQueue>();


            // act
            sut.Add(session1);
            var result1 = stored_sessions.ToList();

            sut.Add(session2);
            await Task.Delay(200).ConfigureAwait(false);
            var result2 = stored_sessions.ToList();

            sut.Add(session3);
            await Task.Delay(100).ConfigureAwait(false);
            var result3 = stored_sessions.ToList();


            // assert
            result1.Should().BeEmpty();
            result2.Should().Equal("1, 2");
            result3.Should().Equal("1, 2");
        }


        [Fact]
        public async Task Add_Ones_WaitsForBatchTimeAndProcessOne()
        {
            // arrange
            this.configuration.ResultsProcessBatchDelay.Returns(TimeSpan.FromMilliseconds(300));

            var session = this.CreateSession(1);

            var stored_sessions = this.CaptureStoredSessions();


            // act
            this.fixture.Create<CompletedSessionsProcessorQueue>().Add(session);
            var result1 = stored_sessions.ToList();

            await Task.Delay(400).ConfigureAwait(false);

            var result2 = stored_sessions.ToList();


            // assert
            result1.Should().BeEmpty();
            result2.Should().Equal("1");
        }


        [Fact]
        public async Task Add_Ones_BatchSizeOne_ProcessOne()
        {
            // arrange
            this.configuration.ResultsProcessBatchDelay.Returns(TimeSpan.FromMilliseconds(400));
            this.configuration.ResultsProcessMaxBatchSize.Returns(1);

            var session = this.CreateSession(1);

            var stored_sessions = this.CaptureStoredSessions();


            // act
            this.fixture.Create<CompletedSessionsProcessorQueue>().Add(session);
            var result1 = stored_sessions.ToList();

            await Task.Delay(200).ConfigureAwait(false);

            var result2 = stored_sessions.ToList();


            // assert
            result1.Should().BeEmpty();
            result2.Should().Equal("1");
        }


        private ProfileSession CreateSession(int id)
        {
            var session = this.fixture.Create<ProfileSession>();
            session["id"] = id;

            return session;
        }


        private ConcurrentQueue<string> CaptureStoredSessions()
        {
            var result = new ConcurrentQueue<string>();

            this.processorService
                .WhenForAnyArgs(x => x.ProcessAsync(null))
                .Do(x =>
                    {
                        var sessions = x.Arg<IReadOnlyList<ProfileSession>>();
                        result.Enqueue(string.Join(", ", sessions.Select(s => s["id"])));
                    });

            return result;
        }
    }
}