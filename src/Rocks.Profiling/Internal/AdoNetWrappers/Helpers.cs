using System.Data.Common;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal static class Helpers
    {
        public static DbProviderFactory TryGetProviderFactory(this DbConnection connection)
        {
            var wrapped_db_connection = connection as ProfiledDbConnection;
            if (wrapped_db_connection != null)
                return wrapped_db_connection.InnerProviderFactory;

            return DbProviderFactories.GetFactory(connection);
        }
    }
}