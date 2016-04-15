using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Ploeh.AutoFixture;
using Rocks.Helpers;
using Rocks.Profiling.Exceptions;
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
        private readonly ProfilerConfiguration configuration;
        private readonly ICurrentSessionProvider currentSessionProvider;

        #endregion

        #region Construct

        public ProfilerTests()
        {
            this.fixture = new FixtureBuilder().Build();

            this.configuration = this.fixture.Freeze<ProfilerConfiguration>();
            this.currentSessionProvider = this.fixture.Freeze<ICurrentSessionProvider>();

            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Get().Returns(session);
        }

        #endregion

        #region Public methods

        [Fact]
        public void NoSession_Profile_DoesNotThrow()
        {
            // arrange
            this.currentSessionProvider.Get().ReturnsNull();


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
            this.currentSessionProvider.Get().ReturnsNull();


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
        public void Profile_NestedOperations_CorrectlyBuildsHierarchy()
        {
            // arrange
            var additional_session_data = new Dictionary<string, object>();

            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("a")))
            {
                using (sut.Profile(new ProfileOperationSpecification("b")))
                using (sut.Profile(new ProfileOperationSpecification("c")))
                {
                }

                using (sut.Profile(new ProfileOperationSpecification("d")))
                {
                }
            }

            sut.Stop(additional_session_data);


            // assert
            results.Should().HaveCount(1);
            results[0].OperationsTreeRoot.Id.Should().Be(1);
            results[0].OperationsTreeRoot.Name.Should().Be(ProfileOperationNames.ProfileSessionRoot);

            results[0].OperationsTreeRoot.ChildNodes.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).Name.Should().Be("a");
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).Id.Should().Be(2);

            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.Should().HaveCount(2);
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).Name.Should().Be("b");
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).Id.Should().Be(3);

            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).ChildNodes.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).Name.Should().Be("c");
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).ChildNodes.ElementAt(0).Id.Should().Be(4);

            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(1).Name.Should().Be("d");
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0).ChildNodes.ElementAt(1).Id.Should().Be(5);
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


        [Fact, MethodImpl(MethodImplOptions.NoInlining)]
        public void Profile_WithCaptureCallStacks_FillsOperationCallStackProperty()
        {
            // arrange
            this.configuration.CaptureCallStacks = true;

            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("a")))
            {
            }

            sut.Stop();


            // assert
            results.Should().HaveCount(1);
            results[0].OperationsTreeRoot.ChildNodes.ElementAt(0)
                      .CallStack.SplitNullSafe('\n')
                      .First()
                      .Should().Contain(nameof(this.Profile_WithCaptureCallStacks_FillsOperationCallStackProperty));
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