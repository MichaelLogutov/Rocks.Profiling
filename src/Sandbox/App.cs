using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Rocks.Helpers;
using Rocks.Profiling;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;
using SimpleInjector;


namespace Sandbox
{
    public static class App
    {
        private sealed class ConsoleProfileResultsStorage : IProfilerResultsStorage
        {
            public Task AddAsync(IReadOnlyList<ProfileSession> sessions,
                                 CancellationToken cancellationToken = default(CancellationToken))
            {
                var json = JsonConvert.SerializeObject(sessions,
                                                       new JsonSerializerSettings
                                                       {
                                                           Formatting = Formatting.Indented,
                                                           ContractResolver =
                                                               new CamelCasePropertyNamesContractResolver()
                                                       });

                Console.WriteLine("\n" + json);

                return Task.CompletedTask;
            }
        }


        public static async Task Main()
        {
            try
            {
                ConfigurationManager.AppSettings["Profiling.Enabled"] = "true";
                ConfigurationManager.AppSettings["Profiling.ResultsProcessBatchDelay"] = "00:00:00";
                ConfigurationManager.AppSettings["Profiling.ResultsProcessMaxBatchSize"] = "1";

                var container = new Container { Options = { AllowOverridingRegistrations = true } };
                ProfilingLibrary.Setup(() => null, container);

                container.RegisterSingleton<IProfilerLogger, ConsoleProfilerLogger>();
                container.RegisterSingleton<IProfilerResultsStorage, ConsoleProfileResultsStorage>();

                container.Verify();

                ProfilingLibrary.StartProfiling();

                using (var connection = ConfigurationManager.ConnectionStrings["Test"].CreateDbConnection())
                {
                    connection.Execute(@"truncate table TestRocksProfilingTable");

                    var count = connection.Execute(@"insert TestRocksProfilingTable(Data) values (@data)",
                                                   new[]
                                                   {
                                                       new { data = "123" },
                                                       new { data = "456" },
                                                       new { data = "789" }
                                                   }
                    );

                    Console.WriteLine("Inserted rows: {0}", count);
                }

                using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    using (var connection = ConfigurationManager.ConnectionStrings["Test"].CreateDbConnection())
                    {
                        var count = connection.Execute(@"insert TestRocksProfilingTable(Data) values (@data)",
                                                       new[]
                                                       {
                                                           new { data = "2 123" },
                                                           new { data = "2 456" },
                                                           new { data = "2 789" }
                                                       }
                        );

                        Console.WriteLine("Inserted rows: {0}", count);
                    }
                }

                using (var connection = ConfigurationManager.ConnectionStrings["Test"]
                                                            .CreateDbConnection())
                {
                    var data = (await connection.QueryAsync<string>("select top 1 Data from TestRocksProfilingTable order by Id;" +
                                                                    "waitfor delay '00:00:01'")).FirstOrDefault();

                    Console.WriteLine("Selected via ADO: {0}", data);
                }

                ProfilingLibrary.StopProfiling(new Dictionary<string, object> { { "name", "test session" } });

                Task.Delay(500).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n{0}\n\n", ex);
            }
        }
    }
}