using System.Data.Common;
using Rocks.Helpers;


namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal static class Helpers
    {
        public static DbProviderFactory TryGetProviderFactory(this DbConnection connection)
        {
            var wrapped_db_connection = connection as ProfiledDbConnection;
            if (wrapped_db_connection != null)
                return wrapped_db_connection.InnerProviderFactory;

            return DbFactory.Get(connection);
        }
    }
}