using System.Data.Common;
#if NETSTANDARD
using Rocks.Helpers;

#endif


namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal static class Helpers
    {
        public static DbProviderFactory TryGetProviderFactory(this DbConnection connection)
        {
            if (connection is ProfiledDbConnection wrapped_db_connection)
                return wrapped_db_connection.InnerProviderFactory;

#if !NETSTANDARD
            return DbProviderFactories.GetFactory(connection);
#else
            return GlobalDbFactoriesProvider.Get(connection);
#endif
        }
    }
}