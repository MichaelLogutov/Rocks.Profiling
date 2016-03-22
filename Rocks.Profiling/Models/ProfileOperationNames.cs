using System.Data.Common;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Predefined names of profile operations.
    /// </summary>
    public static class ProfileOperationNames
    {
        /// <summary>
        ///     The root of the session operations tree.
        /// </summary>
        public static string ProfileSessionRoot = "Root";

        /// <summary>
        ///     Execution of <see cref="DbCommand.ExecuteNonQuery"/> method.
        /// </summary>
        public static string DbCommandExecuteNonQuery = "DbCommand_ExecuteNonQuery";

        /// <summary>
        ///     Execution of <see cref="DbCommand.ExecuteNonQueryAsync()"/> method.
        /// </summary>
        public static string DbCommandExecuteNonQueryAsync = "DbCommand_ExecuteNonQueryAsync";

        /// <summary>
        ///     Execution of <see cref="DbCommand.ExecuteScalar"/> method.
        /// </summary>
        public static string DbCommandExecuteScalar = "DbCommand_ExecuteScalar";

        /// <summary>
        ///     Execution of <see cref="DbCommand.ExecuteScalarAsync()"/> method.
        /// </summary>
        public static string DbCommandExecuteScalarAsync = "DbCommand_ExecuteScalarAsync";

        /// <summary>
        ///     Execution of <see cref="DbCommand.ExecuteReader()"/> method.
        /// </summary>
        public static string DbCommandExecuteReader = "DbCommand_ExecuteReader";

        /// <summary>
        ///     Execution of <see cref="DbCommand.ExecuteReaderAsync()"/> method.
        /// </summary>
        public static string DbCommandExecuteReaderAsync = "DbCommand_ExecuteReaderAsync";
    }
}