using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Ploeh.AutoFixture;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Implementation;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class ProfilerTests
    {
        #region Public methods

        [Fact]
        public void NoSession_Profile_DoesNotThrow()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();


            // act
            Action act = () => fixture.Create<Profiler>().Profile("test");


            // assert
            act.ShouldNotThrow();
        }


        [Fact]
        public void Stop_NoSessionActive_Throws()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();


            // act
            Action act = () => fixture.Create<Profiler>().Stop();


            // assert
            act.ShouldThrow<NoCurrentSessionProfilingException>();
        }


        [Fact]
        public void Profile_ThenStop_HasActiveSession_SendsTheSessionWithEventToResultsProcessor()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var additional_session_data = new Dictionary<string, object>();

            var results = CaptureProfileSessionAddedToTheResultsProcessor(fixture);

            var sut = fixture.Create<Profiler>();


            // act
            sut.Start();
            sut.Profile("test").Dispose();
            sut.Stop(additional_session_data);


            // assert
            results.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.Select(x => x.Name).Should().Equal("test");
        }


        [Fact]
        public void Stop_HasActiveSession_SendsAdditionalSessionDataToResultsProcessor()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var additional_session_data = new Dictionary<string, object>();
            var results_processor = fixture.Freeze<ICompletedSessionsProcessorQueue>();

            var sut = fixture.Create<Profiler>();


            // act
            sut.Start();
            sut.Stop(additional_session_data);


            // assert
            results_processor
                .Received(1)
                .Add(Arg.Is<CompletedSessionInfo>(x => x != null && x.AdditionalData == additional_session_data));
        }


        [Fact]
        public async Task Start_Profile_Stop_InParallelTasks_SendsDifferentResults()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var results = CaptureProfileSessionAddedToTheResultsProcessor(fixture);

            var sut = fixture.Create<Profiler>();


            // act
            await Task.WhenAll
                (
                    Task.Run(() =>
                             {
                                 sut.Start();
                                 sut.Profile("a").Dispose();
                                 sut.Stop();
                             }),
                    Task.Run(() =>
                             {
                                 sut.Start();
                                 sut.Profile("b").Dispose();
                                 sut.Stop();
                             })
                ).ConfigureAwait(false);


            // assert
            results.Should().HaveCount(2);

            results
                .SelectMany(x => x.OperationsTreeRoot.ChildNodes.Select(n => n.Name))
                .Should().BeEquivalentTo("a", "b");
        }


        [Fact]
        public void Start_Profile_Stop_InParallelThreads_SendsDifferentResults()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var results = CaptureProfileSessionAddedToTheResultsProcessor(fixture);

            var sut = fixture.Create<Profiler>();


            // act
            var t1 = new Thread(() =>
                                {
                                    sut.Start();
                                    sut.Profile("a").Dispose();
                                    sut.Stop();
                                });

            var t2 = new Thread(() =>
                                {
                                    sut.Start();
                                    sut.Profile("b").Dispose();
                                    sut.Stop();
                                });

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();


            // assert
            results.Should().HaveCount(2);

            results
                .SelectMany(x => x.OperationsTreeRoot.ChildNodes.Select(n => n.Name))
                .Should().BeEquivalentTo("a", "b");
        }


        [Fact]
        public void Measure_NestedOperations_CorrectlyBuildsHierarchy()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();


            var additional_session_data = new Dictionary<string, object>();

            var results = CaptureProfileSessionAddedToTheResultsProcessor(fixture);

            var sut = fixture.Create<Profiler>();


            // act
            sut.Start();

            using (sut.Profile("a"))
                sut.Profile("b").Dispose();

            sut.Stop(additional_session_data);


            // assert
            results.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.First().Name.Should().Be("a");
            results[0].OperationsTreeRoot.ChildNodes.First().ChildNodes.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.First().ChildNodes.First().Name.Should().Be("b");
        }


        [Fact]
        public void Profile_OperationsDisposedOutOfOrder_Throws()
        {
            // arrange
            var fixture = new FixtureBuilder().Build();

            var sut = fixture.Create<Profiler>();


            // act
            sut.Start();

            Action act = () =>
                         {
                             var operation1 = sut.Profile("a");
                             sut.Profile("b");
                             operation1.Dispose();
                         };


            // assert
            act.ShouldThrow<OperationsOutOfOrderProfillingException>();
        }

        #endregion

        #region Private methods

        private static IList<ProfileSession> CaptureProfileSessionAddedToTheResultsProcessor(IFixture fixture)
        {
            var results = new List<ProfileSession>();

            fixture.Freeze<ICompletedSessionsProcessorQueue>()
                   .WhenForAnyArgs(x => x.Add(null))
                   .Do(x => results.Add(x.Arg<CompletedSessionInfo>().Session));

            return results;
        }

        #endregion
    }
}