using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;

namespace WebSandbox.NetFramework
{
    public class ProfilerJsonResultsStorage : IProfilerResultsStorage
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Profiler");


        public Task AddAsync(IReadOnlyList<ProfileSession> sessions, CancellationToken cancellationToken = default(CancellationToken))
        {
            var json = JsonConvert.SerializeObject(sessions,
                                                   new JsonSerializerSettings
                                                   {
                                                       Formatting = Formatting.Indented,
                                                       ContractResolver = new CamelCasePropertyNamesContractResolver()
                                                   });
            
            Logger.Info(json);

            return Task.CompletedTask;
        }
    }
}