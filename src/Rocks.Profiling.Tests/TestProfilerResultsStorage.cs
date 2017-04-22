using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;

namespace Rocks.Profiling.Tests
{
    internal class TestProfilerResultsStorage : IProfilerResultsStorage
    {
        public ConcurrentQueue<ProfileSession> ProfileSessions { get; } = new ConcurrentQueue<ProfileSession>();


        public Task AddAsync(IReadOnlyList<ProfileSession> sessions, CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var session in sessions)
                this.ProfileSessions.Enqueue(session);

            return Task.CompletedTask;
        }
    }
}