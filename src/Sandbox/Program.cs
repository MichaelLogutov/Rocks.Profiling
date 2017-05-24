using System;
using System.Collections.Generic;
using System.Configuration;
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
    public class Program
    {
        public class ConsoleProfileResultsStorage : IProfilerResultsStorage
        {
            /// <summary>
            ///     Adds new profile <paramref name="sessions"/> to the storage.
            /// </summary>
            public Task AddAsync(IReadOnlyList<ProfileSession> sessions, CancellationToken cancellationToken = default(CancellationToken))
            {
                var json = JsonConvert.SerializeObject(sessions,
                                                       new JsonSerializerSettings
                                                       {
                                                           Formatting = Formatting.Indented,
                                                           ContractResolver = new CamelCasePropertyNamesContractResolver()
                                                       });

                Console.WriteLine("\n" + json);

                return Task.CompletedTask;
            }
        }


        public class ConsoleProfileEventHandlers : IProfilerEventsHandler
        {
            public void OnSessionEnded(ProfileSession session)
            {
                Console.WriteLine("IProfilerEventsHandler.OnSessionEnded: {0}", session);
            }


            public void OnOperationEnded(ProfileOperation operation)
            {
                Console.WriteLine("IProfilerEventsHandler.OnOperationEnded: {0} (duration {1})", operation, operation.Duration);
            }
        }


        public static void Main()
        {
            try
            {
                ConfigurationManager.AppSettings["Profiling.Enabled"] = "true";
                ConfigurationManager.AppSettings["Profiling.ResultsProcessBatchDelay"] = "00:00:00";
                ConfigurationManager.AppSettings["Profiling.ResultsBufferSize"] = "1";

                var container = new Container { Options = { AllowOverridingRegistrations = true } };
                ProfilingLibrary.Setup(() => null, container);

                container.RegisterSingleton<IProfilerLogger, ConsoleProfilerLogger>();
                container.RegisterSingleton<IProfilerResultsStorage, ConsoleProfileResultsStorage>();
                container.RegisterSingleton<IProfilerEventsHandler, ConsoleProfileEventHandlers>();

                container.Verify();


                //ProfilingLibrary.StartProfiling();

                using (var data_context = new LingToSqlDataContext(ConfigurationManager.ConnectionStrings["Test"].CreateDbConnection()))
                {
                    var dto = new TestRocksProfilingTable { Data = "abc" };
                    data_context.TestRocksProfilingTables.InsertOnSubmit(dto);
                    data_context.SubmitChanges();

                    Console.WriteLine("Inserted via Linq-to-sql: {0}", dto.Id);
                }

                using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                using (var data_context = new LingToSqlDataContext(ConfigurationManager.ConnectionStrings["Test"].CreateDbConnection()))
                {
                    var dto = new TestRocksProfilingTable { Data = "abc" };
                    data_context.TestRocksProfilingTables.InsertOnSubmit(dto);
                    data_context.SubmitChanges();

                    Console.WriteLine("Rollback via Linq-to-sql: {0}", dto.Id);
                }

                using (var connection = ConfigurationManager.ConnectionStrings["Test"].CreateDbConnection())
                {
                    var id = connection.Query<int>("select top 1 Id from TestRocksProfilingTable order by Id desc").FirstOrNull();

                    Console.WriteLine("Selected via ADO: {0}", id);
                }

                //ProfilingLibrary.StopProfiling(new Dictionary<string, object>
                //                               {
                //                                   { "name", "test session" }
                //                               });

                Task.Delay(500).Wait();
            }
            // ReSharper disable once CatchAllClause
            catch (Exception ex)
            {
                Console.WriteLine("\n\n{0}\n\n", ex);
            }
        }
    }
}