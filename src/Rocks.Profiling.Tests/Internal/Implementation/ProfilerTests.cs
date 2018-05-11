using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Moq;
using AutoFixture;
using Rocks.Helpers;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Exceptions;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;
using Rocks.Profiling.Tests.Exceptions;
using Xunit;

// ReSharper disable AssignNullToNotNullAttribute

namespace Rocks.Profiling.Tests.Internal.Implementation
{
    public class ProfilerTests
    {
        private readonly IFixture fixture;
        private readonly Mock<IProfilerConfiguration> configuration;
        private readonly Mock<ICurrentSessionProvider> currentSessionProvider;


        public ProfilerTests()
        {
            this.fixture = new FixtureBuilder().Build();

            this.configuration = this.fixture.FreezeMock<IProfilerConfiguration>();
            this.currentSessionProvider = this.fixture.FreezeMock<ICurrentSessionProvider>();

            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Setup(x => x.Get()).Returns(session);
        }


        [Fact]
        public void NoSession_Profile_DoesNotThrow()
        {
            // arrange
            this.currentSessionProvider.Setup(x => x.Get()).Returns((ProfileSession) null);


            // act
            Action act = () =>
                         {
                             using (this.fixture.Create<Profiler>().Profile(new ProfileOperationSpecification("test")))
                             {
                             }
                         };


            // assert
            act.Should().NotThrow();
        }


        [Fact]
        public void NoSession_Profile_ReturnsOperation()
        {
            // arrange
            this.currentSessionProvider.Setup(x => x.Get()).Returns((ProfileSession) null);


            // act
            var result = this.fixture.Create<Profiler>().Profile(new ProfileOperationSpecification("test"));


            // assert
            result.Should().NotBeNull();
        }


        [Fact]
        public void Stop_NoSessionActive_Throws()
        {
            // arrange
            this.currentSessionProvider.Setup(x => x.Get()).Returns((ProfileSession) null);


            // act
            Action act = () => this.fixture.Create<Profiler>().Stop();


            // assert
            act.Should().Throw<NoCurrentSessionProfilingException>();
        }


        [Fact]
        public void Start_SetsCurrentSession()
        {
            // arrange
            this.currentSessionProvider.Setup(x => x.Get()).Returns((ProfileSession) null);


            // act
            var result = this.fixture.Create<Profiler>().Start();


            // assert
            this.currentSessionProvider.Verify(m => m.Set(result));
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
            results[0].Operations.Select(x => x.Name).Should().Equal("test");
        }


        [Fact]
        public void Stop_HasActiveSession_SendsCombinedAdditionalDataToResultsProcessor()
        {
            // arrange
            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Setup(x => x.Get()).Returns(session);

            session.AddData(new Dictionary<string, object> { ["a"] = 1, ["c"] = 3 });

            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2 });


            // assert
            results.Should().HaveCount(1);
            results[0].Data.Should().BeEquivalentTo(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2, ["c"] = 3 });
        }


        [Fact]
        public void Stop_Always_CallsEventsHandlers()
        {
            // arrange
            var events_handler = this.fixture.FreezeMock<IProfilerEventsHandler>();

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop();


            // assert
            events_handler.Verify(m => m.OnSessionEnded(It.Is<ProfileSession>(x => x != null)), Times.Once);
        }


        [Fact]
        public void Stop_OneEventHandlerThrows_DoesNotThrow()
        {
            // arrange
            var events_handler = this.fixture.FreezeMock<IProfilerEventsHandler>();
            events_handler.Setup(x => x.OnSessionEnded(It.IsAny<ProfileSession>())).Throws<ValidTestException>();

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop();


            // assert
        }


        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void OperationEnded_Always_CallsEventsHandlers(bool noSession)
        {
            // arrange
            var events_handler = this.fixture.FreezeMock<IProfilerEventsHandler>();
            var profiler = this.fixture.Create<Profiler>();

            if (noSession)
                this.currentSessionProvider.Setup(x => x.Get()).Returns((ProfileSession) null);
            else
            {
                this.fixture.Inject<IProfiler>(profiler);
                this.currentSessionProvider.Setup(x => x.Get()).Returns(this.fixture.Create<ProfileSession>);
            }

            var sut = profiler.Profile(new ProfileOperationSpecification("test"));


            // act
            ((IDisposable) sut)?.Dispose();


            // assert
            events_handler.Verify(x => x.OnOperationEnded(sut), Times.Once);
        }


        [Fact]
        public void OperationEnded_EventHandlerThrows_DoesNotThrowsFromDispose()
        {
            // arrange
            var events_handler = this.fixture.FreezeMock<IProfilerEventsHandler>();

            events_handler.Setup(x => x.OnOperationEnded(It.IsAny<ProfileOperation>())).Throws<ValidTestException>();

            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Setup(x => x.Get()).Returns(session);

            var operation = this.fixture.Create<Profiler>().Profile(new ProfileOperationSpecification("test"));


            // act
            Action act = () => ((IDisposable) operation)?.Dispose();


            // assert
            act.Should().NotThrow();
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

            using (sut.Profile(new ProfileOperationSpecification("e")))
            using (sut.Profile(new ProfileOperationSpecification("f")))
            {
            }

            sut.Stop(additional_session_data);


            // assert
            results.Should().HaveCount(1);
            results[0]
                .Operations.Should().BeEquivalentTo(new object[]
                                                    {
                                                        new { Id = 1, Name = "a", ParentId = (int?) null },
                                                        new { Id = 2, Name = "b", ParentId = 1 },
                                                        new { Id = 3, Name = "c", ParentId = 2 },
                                                        new { Id = 4, Name = "d", ParentId = 1 },
                                                        new { Id = 5, Name = "e", ParentId = (int?) null },
                                                        new { Id = 6, Name = "f", ParentId = 5 }
                                                    },
                                                    o => o.ExcludingMissingMembers());
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
            act.Should().Throw<OperationsOutOfOrderProfillingException>();
        }


        [Fact]
        public void Profile_OperationHasWrongParent_Throws()
        {
            // arrange
            var sut = this.fixture.Create<Profiler>();


            // act
            Action act = () =>
                         {
                             using (sut.Profile(new ProfileOperationSpecification("a")))
                             using (var operation = sut.Profile(new ProfileOperationSpecification("b")))
                                 operation.SetPropertyValue(x => x.Parent, null);
                         };


            // assert
            act.Should().Throw<OperationsOutOfOrderProfillingException>();
        }


        [Fact, MethodImpl(MethodImplOptions.NoInlining)]
        public void Profile_WithCaptureCallStacks_FillsOperationCallStackProperty()
        {
            // arrange
            this.configuration.Setup(x => x.CaptureCallStacks).Returns(true);

            var results = CaptureProfileSessionAddedToTheResultsProcessor(this.fixture);

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("a")))
            {
            }

            sut.Stop();


            // assert
            results.Should().HaveCount(1);
            results[0]
                .Operations[0]
                .CallStack.SplitNullSafe('\n')
                .First()
                .Should()
                .Contain(nameof(this.Profile_WithCaptureCallStacks_FillsOperationCallStackProperty));
        }


        private static IList<ProfileSession> CaptureProfileSessionAddedToTheResultsProcessor(IFixture fixture)
        {
            var results = new List<ProfileSession>();

            fixture.FreezeMock<ICompletedSessionsProcessorQueue>()
                   .Setup(x => x.Add(It.IsAny<ProfileSession>()))
                   .Callback<ProfileSession>(x => results.Add(x));

            return results;
        }
    }
}