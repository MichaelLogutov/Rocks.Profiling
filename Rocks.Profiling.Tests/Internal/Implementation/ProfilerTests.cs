using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Ploeh.AutoFixture;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class ProfilerTests
    {
        #region Private readonly fields

        private readonly IFixture fixture;
        private readonly ICurrentSessionProvider currentSessionProvider;

        #endregion

        #region Construct

        public ProfilerTests()
        {
            this.fixture = new FixtureBuilder().Build();

            this.currentSessionProvider = this.fixture.Freeze<ICurrentSessionProvider>();
            this.currentSessionProvider.Get().Returns(this.fixture.Create<ProfileSession>());
        }

        #endregion

        #region Public methods

        [Fact]
        public void NoSession_Profile_DoesNotThrow()
        {
            // arrange
            this.currentSessionProvider.Get().Returns((ProfileSession) null);


            // act
            Action act = () =>
                         {
                             using (this.fixture.Create<Profiler>().Profile(new ProfileOperationSpecification("test")))
                             {
                             }
                         };


            // assert
            act.ShouldNotThrow();
        }


        [Fact]
        public void Stop_NoSessionActive_Throws()
        {
            // arrange
            this.currentSessionProvider.Get().Returns((ProfileSession) null);


            // act
            Action act = () => this.fixture.Create<Profiler>().Stop();


            // assert
            act.ShouldThrow<NoCurrentSessionProfilingException>();
        }


        [Fact]
        public void Start_SetsCurrentSession()
        {
            // arrange
            this.currentSessionProvider.Get().Returns((ProfileSession) null);


            // act
            this.fixture.Create<Profiler>().Start();


            // assert
            this.currentSessionProvider.Received(1).Set(Arg.Is<ProfileSession>(x => x != null));
        }


        [Fact]
        public void Stop_HasActiveSession_SendsTheSessionWithEventToResultsProcessor()
        {
            // arrange
            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop();


            // assert
            results.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.Select(x => x.Name).Should().Equal("test");
        }


        [Fact]
        public void Stop_HasActiveSession_SendsCombinedAdditionalDataToResultsProcessor()
        {
            // arrange
            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Get().Returns(session);

            session.AddAdditionalData(new Dictionary<string, object> { ["a"] = 1, ["c"] = 3 });

            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 });


            // assert
            results.Should().HaveCount(1);
            results[0].AdditionalData.ShouldBeEquivalentTo(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2, ["c"] = 3 });
        }


        [Fact]
        public void Measure_NestedOperations_CorrectlyBuildsHierarchy()
        {
            // arrange
            var additional_session_data = new Dictionary<string, object>();

            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("a")))
            using (sut.Profile(new ProfileOperationSpecification("b")))
            {
            }

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
            var sut = this.fixture.Create<Profiler>();


            // act
            Action act = () =>
                         {
                             using (sut.Profile(new ProfileOperationSpecification("a")))
                                 // ReSharper disable once MustUseReturnValue
                                 sut.Profile(new ProfileOperationSpecification("b"));
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
                   .Do(x => results.Add(x.Arg<ProfileSession>()));

            return results;
        }

        #endregion
    }
}