using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Dapper;
using Rocks.Helpers;
using Rocks.Profiling;
using Rocks.Profiling.Internal.AdoNetWrappers;
using Rocks.Profiling.Models;

namespace WebSandbox.NetFramework.Controllers
{
    public class SandboxWebController : ApiController
    {
        private readonly IProfiler profiler;


        public SandboxWebController(IProfiler profiler)
        {
            this.profiler = profiler;
        }


        [HttpGet, Route("test")]
        public async Task<IList<string>> Test(int delay, CancellationToken cancellationToken)
        {
            var result = new List<string>();

            using (this.profiler.Profile(new ProfileOperationSpecification("test")))
            {
                using (var connection = new ProfiledDbConnection(new SqlConnection(ConfigurationManager.ConnectionStrings["Test"].ConnectionString)))
                {
                    {
                        var id = (await connection.QueryAsync<int>(
                                  "select top 1 Id from TestRocksProfilingTable where 1 = 1 order by Id desc; " +
                                  $"waitfor delay '{TimeSpan.FromMilliseconds(delay)}';")).FirstOrNull();

                        result.Add($"Selected via ADO+Dapper: {id}");
                    }
                    
                    using (var command = connection.CreateCommand())
                    {
                        if (connection.State != ConnectionState.Open)
                            await connection.OpenAsync(cancellationToken);
                        
                        command.CommandText = "select top 1 Id from TestRocksProfilingTable where 2 = 2 order by Id desc; " +
                                              $"waitfor delay '{TimeSpan.FromMilliseconds(delay)}';";

                        var id = await command.ExecuteScalarAsync(cancellationToken) as int?;

                        result.Add($"Selected via ADO: {id}");
                    }

                    using (var command = connection.CreateCommand())
                    {
                        if (connection.State != ConnectionState.Open)
                            await connection.OpenAsync(cancellationToken);
                        
                        command.CommandText = "select top 1 Id from TestRocksProfilingTable where 3 = 3 order by Id desc; " +
                                              $"waitfor delay '{TimeSpan.FromMilliseconds(delay)}';";

                        var id = await command.ExecuteScalarAsync(cancellationToken) as int?;

                        result.Add($"Selected via ADO (2): {id}");
                    }
                }

                return result;
            }
        }
    }
}