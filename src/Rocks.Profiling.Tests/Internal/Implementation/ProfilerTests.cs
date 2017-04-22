﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Moq;
using Ploeh.AutoFixture;
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
            act.ShouldNotThrow();
        }


        [Fact]
        public void Stop_NoSessionActive_Throws()
        {
            // arrange
            this.currentSessionProvider.Setup(x => x.Get()).Returns((ProfileSession) null);


            // act
            Action act = () => this.fixture.Create<Profiler>().Stop();


            // assert
            act.ShouldThrow<NoCurrentSessionProfilingException>();
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
            results[0].Data.ShouldBeEquivalentTo(new Dictionary<string, object> { ["a"] = 1, ["b"] = 2, ["c"] = 3 });
        }


        [Fact]
        public void Stop_Always_CallsEventsHandlers()
        {
            // arrange
            var events_handler1 = new Mock<IProfilerEventsHandler>();
            var events_handler2 = new Mock<IProfilerEventsHandler>();

            this.fixture.Inject<IEnumerable<IProfilerEventsHandler>>(new[] { events_handler1.Object, events_handler2.Object });

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop();


            // assert
            events_handler1.Verify(m => m.OnSessionEnded(It.Is<ProfileSession>(x => x != null)), Times.Once);
            events_handler2.Verify(m => m.OnSessionEnded(It.Is<ProfileSession>(x => x != null)), Times.Once);
        }


        [Fact]
        public void Stop_OneEventHandlerThroes_StillCallsRemainingEventsHandlers()
        {
            // arrange
            var events_handler1 = new Mock<IProfilerEventsHandler>();
            var events_handler2 = new Mock<IProfilerEventsHandler>();

            events_handler1.Setup(x => x.OnSessionEnded(It.IsAny<ProfileSession>())).Throws<ValidTestException>();

            this.fixture.Inject<IEnumerable<IProfilerEventsHandler>>(new[] { events_handler1.Object, events_handler2.Object });

            var sut = this.fixture.Create<Profiler>();


            // act
            using (sut.Profile(new ProfileOperationSpecification("test")))
            {
            }
            sut.Stop();


            // assert
            events_handler2.Verify(x => x.OnSessionEnded(It.IsAny<ProfileSession>()), Times.Once);
        }


        [Fact]
        public void OperationEnded_Always_CallsEventsHandlers()
        {
            // arrange
            var events_handler1 = new Mock<IProfilerEventsHandler>();
            var events_handler2 = new Mock<IProfilerEventsHandler>();

            this.fixture.Inject<IEnumerable<IProfilerEventsHandler>>(new[] { events_handler1.Object, events_handler2.Object });

            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Setup(x => x.Get()).Returns(session);

            var operation = this.fixture.Create<Profiler>().Profile(new ProfileOperationSpecification("test"));


            // act
            ((IDisposable) operation)?.Dispose();


            // assert
            events_handler1.Verify(m => m.OnOperationEnded(operation), Times.Once);
            events_handler2.Verify(m => m.OnOperationEnded(operation), Times.Once);
        }


        [Fact]
        public void OperationEnded_OneEventHandlerThroes_StillCallsRemainingEventsHandlers()
        {
            // arrange
            var events_handler1 = new Mock<IProfilerEventsHandler>();
            var events_handler2 = new Mock<IProfilerEventsHandler>();

            events_handler1.Setup(x => x.OnOperationEnded(It.IsAny<ProfileOperation>())).Throws<ValidTestException>();

            this.fixture.Inject<IEnumerable<IProfilerEventsHandler>>(new[] { events_handler1.Object, events_handler2.Object });

            var session = this.fixture.Create<ProfileSession>();
            this.currentSessionProvider.Setup(x => x.Get()).Returns(session);

            var operation = this.fixture.Create<Profiler>().Profile(new ProfileOperationSpecification("test"));


            // act
            ((IDisposable) operation)?.Dispose();


            // assert
            events_handler2.Verify(m => m.OnOperationEnded(operation), Times.Once);
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
            results[0].Operations.ShouldAllBeEquivalentTo
                      (new object[]
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
            act.ShouldThrow<OperationsOutOfOrderProfillingException>();
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
            act.ShouldThrow<OperationsOutOfOrderProfillingException>();
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
            results[0].Operations[0]
                      .CallStack.SplitNullSafe('\n')
                      .First()
                      .Should().Contain(nameof(this.Profile_WithCaptureCallStacks_FillsOperationCallStackProperty));
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