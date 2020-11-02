using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Rocks.Dataflow.Fluent;
using Rocks.Helpers;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;
using Rocks.Profiling.Tests.Exceptions;
using Xunit;

// ReSharper disable ThrowingSystemException

namespace Rocks.Profiling.Tests.IntegrationTests
{
    [Collection("IntegrationTests")]
    public class ProfilerMultithreadStressTest
    {
        [Fact]
        public void MultithreadStressTest_DoesNotThrow()
        {
            // arrange
            ProfilingLibrary.Setup(() => null);
            ProfilingLibrary.Container.RegisterSingleton<IProfilerLogger, TestsProfilerLogger>();


            var tasks = Enumerable.Range(0, 10)
                                  .Select(x => Task.Run(() => ProfileAsync(x)))
                                  .ToArray();


            // act
            var result = Task.WaitAll(tasks, 10000);


            // assert
            result.Should().BeTrue();
        }


        [Fact]
        public async Task Dataflow_CorrectlyRecordsTheSessionAsync()
        {
            // arrange
            var profiler_results_storage = new TestProfilerResultsStorage();

            ProfilingLibrary.Setup(() => null);
            ProfilingLibrary.Container.RegisterSingleton<IProfilerLogger, TestsProfilerLogger>();
            ProfilingLibrary.Container.RegisterInstance<IProfilerResultsStorage>(profiler_results_storage);
            ProfilingLibrary.Container.RegisterSingleton<IProfilerConfiguration, TestProfilerConfiguration>();


            var exceptions = new List<Exception>();

            var dataflow = DataflowFluent
                           .ReceiveDataOfType<int>()
                           .TransformAsync(async id =>
                                           {
                                               var item = new DataflowItemContext
                                                          {
                                                              Id = id,
                                                              ProfileSession = ProfilingLibrary.StartProfiling()
                                                          };

                                               using (ProfilingLibrary.Profile(item.ProfileSession, new ProfileOperationSpecification("a")))
                                               {
                                                   await Task.Delay(50).ConfigureAwait(true);
                                               }

                                               return item;
                                           })
                           .ProcessAsync(async item =>
                                         {
                                             using (ProfilingLibrary.Profile(item.ProfileSession, new ProfileOperationSpecification("b")))
                                             {
                                                 await Task.Delay(50).ConfigureAwait(true);
                                             }
                                         })
                           .ActionAsync(item =>
                                        {
                                            ProfilingLibrary.StopProfiling(item.ProfileSession);
                                            return Task.CompletedTask;
                                        })
                           .WithDefaultExceptionLogger((ex, obj) => exceptions.Add(ex))
                           .CreateDataflow();


            // act
            await dataflow.ProcessAsync(Enumerable.Range(0, 10)).ConfigureAwait(false);
            await Task.Delay(100);


            // assert
            exceptions.Should().BeEmpty();
            profiler_results_storage.ProfileSessions.ToArray()
                                    .SelectMany(x => x.Operations)
                                    .Select(x => x.Name)
                                    .GroupBy(x => x)
                                    .Select(x => new { Name = x.Key, Count = x.Count() })
                                    .Should().BeEquivalentTo(new { Name = "a", Count = 10 },
                                                             new { Name = "b", Count = 10 });
        }


        private static async Task ProfileAsync(int id)
        {
            var profiler = ProfilerFactory.GetCurrentProfiler();
            profiler.Start();

            await OperationAsync($"session_{id}", 0).ConfigureAwait(false);

            profiler.Stop();
        }


        private static async Task OperationAsync(string namePrefix, int level)
        {
            var profiler = ProfilerFactory.GetCurrentProfiler();

            for (var k = 0; k < 10; k++)
            {
                var name = $"{namePrefix}_{k}";

                using (profiler.Profile(new ProfileOperationSpecification(name)))
                {
                    if (level < 5 && RandomizationExtensions.Random.NextBool())
                        await OperationAsync(name, level + 1).ConfigureAwait(false);
                }
            }
        }


        private class DataflowItemContext
        {
            public int Id { get; set; }
            public ProfileSession ProfileSession { get; set; }
        }

        public class TestProfilerConfiguration : ProfilerConfiguration
        {
            public override bool Enabled { get; } = true;
            public override TimeSpan SessionMinimalDuration { get; } = TimeSpan.FromMilliseconds(1);
            public override TimeSpan ResultsProcessBatchDelay { get; } = TimeSpan.FromMilliseconds(100);
        }
    }
}