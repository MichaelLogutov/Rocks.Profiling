using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Rocks.Helpers;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Xunit;

// ReSharper disable ThrowingSystemException

namespace Rocks.Profiling.Tests.IntegrationTests
{
    [Trait("Category", "IntegrationTests")]
    public class ProfilerMultithreadStressTest
    {
        [Fact]
        public void MultithreadStressTest_DoesNotThrow()
        {
            // arrange
            ProfilingLibrary.Setup(() => null);
            ProfilingLibrary.Container.RegisterSingleton<IProfilerLogger, RethrowProfilerLogger>();


            var tasks = Enumerable.Range(0, 10)
                                  .Select(x => Task.Run(() => ProfileAsync(x)))
                                  .ToArray();


            // act
            var result = Task.WaitAll(tasks, 5000);


            // assert
            result.Should().BeTrue();
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
    }
}