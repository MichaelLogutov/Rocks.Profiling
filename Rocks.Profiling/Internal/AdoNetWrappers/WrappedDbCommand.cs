using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Data;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    [DesignerCategory("")]
    public class WrappedDbCommand : DbCommand
    {
        #region Private readonly fields

        private readonly IProfiler profiler;

        #endregion

        #region Construct

        public WrappedDbCommand(DbCommand innerCommand)
        {
            this.InnerCommand = innerCommand;
            this.profiler = ProfilerFactory.GetCurrentProfiler();
        }


        public WrappedDbCommand(DbCommand innerCommand, WrappedDbConnection innerConnection)
            : this(innerCommand)
        {
            this.InnerConnection = innerConnection;
        }

        #endregion

        #region Public properties

        public DbCommand InnerCommand { get; set; }

        public WrappedDbConnection InnerConnection { get; set; }

        public override string CommandText
        {
            get { return this.InnerCommand.CommandText; }
            set { this.InnerCommand.CommandText = value; }
        }

        public override int CommandTimeout
        {
            get { return this.InnerCommand.CommandTimeout; }
            set { this.InnerCommand.CommandTimeout = value; }
        }

        public override CommandType CommandType
        {
            get { return this.InnerCommand.CommandType; }
            set { this.InnerCommand.CommandType = value; }
        }

        public override bool DesignTimeVisible
        {
            get { return this.InnerCommand.DesignTimeVisible; }
            set { this.InnerCommand.DesignTimeVisible = value; }
        }

        public override ISite Site
        {
            get { return this.InnerCommand.Site; }
            set { this.InnerCommand.Site = value; }
        }

        public override UpdateRowSource UpdatedRowSource
        {
            get { return this.InnerCommand.UpdatedRowSource; }
            set { this.InnerCommand.UpdatedRowSource = value; }
        }

        #endregion

        #region Public methods

        public override void Cancel()
        {
            this.InnerCommand.Cancel();
        }


        public override void Prepare()
        {
            this.InnerCommand.Prepare();
        }


        public override int ExecuteNonQuery()
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteNonQuery))
                return this.InnerCommand.ExecuteNonQuery();
        }


        public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteNonQueryAsync))
            {
                return await this.InnerCommand
                                 .ExecuteNonQueryAsync(cancellationToken)
                                 .ConfigureAwait(false);
            }
        }


        public override object ExecuteScalar()
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteScalar))
                return this.InnerCommand.ExecuteScalar();
        }


        public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteScalarAsync))
            {
                return await this.InnerCommand
                                 .ExecuteScalarAsync(cancellationToken)
                                 .ConfigureAwait(false);
            }
        }

        #endregion

        #region Protected properties

        protected override DbParameterCollection DbParameterCollection => this.InnerCommand.Parameters;

        protected override DbConnection DbConnection
        {
            get { return this.InnerConnection; }

            set
            {
                this.InnerConnection = value as WrappedDbConnection;
                if (this.InnerConnection != null)
                    this.InnerCommand.Connection = this.InnerConnection.InnerConnection;
                else
                {
                    this.InnerConnection = new WrappedDbConnection(value);
                    this.InnerCommand.Connection = this.InnerConnection.InnerConnection;
                }
            }
        }

        protected override DbTransaction DbTransaction
        {
            get { return this.InnerCommand.Transaction; }
            set { this.InnerCommand.Transaction = value; }
        }

        #endregion

        #region Protected methods

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteReader))
                return this.InnerCommand.ExecuteReader(behavior);
        }


        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteReaderAsync))
            {
                return await this.InnerCommand
                                 .ExecuteReaderAsync(behavior, cancellationToken)
                                 .ConfigureAwait(false);
            }
        }


        protected override DbParameter CreateDbParameter()
        {
            return this.InnerCommand.CreateParameter();
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.InnerCommand?.Dispose();

            this.InnerCommand = null;
            this.InnerConnection = null;

            base.Dispose(disposing);
        }

        #endregion

        #region Private methods

        [NotNull]
        private IDisposable Profile(string name)
        {
            var operation = this.profiler.Profile(name, ProfileOperationCategories.Sql);
            operation["Sql"] = this.InnerCommand.CommandText;

            return operation;
        }

        #endregion
    }
}