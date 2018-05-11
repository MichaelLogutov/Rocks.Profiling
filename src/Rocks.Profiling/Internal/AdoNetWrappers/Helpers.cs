using System.Data.Common;

#if NETSTANDARD2_0
    using Rocks.Helpers;
#endif


namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal static class Helpers
    {
        public static DbProviderFactory TryGetProviderFactory(this DbConnection connection)
        {
            var wrapped_db_connection = connection as ProfiledDbConnection;
            if (wrapped_db_connection != null)
                return wrapped_db_connection.InnerProviderFactory;

#if NET471
            return DbProviderFactories.GetFactory(connection);
#endif
#if NETSTANDARD2_0
            return DbFactory.Get(connection);
#endif
        }
    }
}