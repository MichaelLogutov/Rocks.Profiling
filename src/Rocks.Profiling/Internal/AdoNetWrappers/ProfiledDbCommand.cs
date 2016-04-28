using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    [DesignerCategory("")]
    internal class ProfiledDbCommand : DbCommand
    {
        #region Private readonly fields

        private readonly IProfiler profiler;

        #endregion

        #region Construct

        public ProfiledDbCommand(DbCommand innerCommand)
        {
            this.InnerCommand = innerCommand;
            this.profiler = ProfilerFactory.GetCurrentProfiler();
        }


        public ProfiledDbCommand(DbCommand innerCommand, ProfiledDbConnection innerConnection)
            : this(innerCommand)
        {
            this.InnerConnection = innerConnection;
        }

        #endregion

        #region Public properties

        public DbCommand InnerCommand { get; private set; }
        public ProfiledDbConnection InnerConnection { get; private set; }
        public ProfiledDbTransaction InnerTransaction { get; private set; }

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

        public override void Cancel() => this.InnerCommand.Cancel();
        public override void Prepare() => this.InnerCommand.Prepare();


        public override int ExecuteNonQuery()
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteNonQuery))
                return this.InnerCommand.ExecuteNonQuery();
        }


        /// <exception cref="DbException">An error occurred while executing the command text.</exception>
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


        /// <exception cref="DbException">An error occurred while executing the command text.</exception>
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
                this.InnerConnection = value as ProfiledDbConnection;
                if (this.InnerConnection != null)
                    this.InnerCommand.Connection = this.InnerConnection.InnerConnection;
                else
                {
                    this.InnerConnection = new ProfiledDbConnection(value);
                    this.InnerCommand.Connection = this.InnerConnection.InnerConnection;
                }
            }
        }

        protected override DbTransaction DbTransaction
        {
            get { return this.InnerTransaction; }
            set
            {
                this.InnerTransaction = value as ProfiledDbTransaction;
                if (value != null && this.InnerTransaction == null)
                    this.InnerTransaction = new ProfiledDbTransaction(value, this.InnerConnection);

                this.InnerCommand.Transaction = this.InnerTransaction?.InnerTransaction;
            }
        }

        #endregion

        #region Protected methods

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteReader))
                return this.InnerCommand.ExecuteReader(behavior);
        }


        /// <exception cref="DbException">An error occurred while executing the command text.</exception>
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            using (this.Profile(ProfileOperationNames.DbCommandExecuteReaderAsync))
            {
                return await this.InnerCommand
                                 .ExecuteReaderAsync(behavior, cancellationToken)
                                 .ConfigureAwait(false);
            }
        }


        protected override DbParameter CreateDbParameter() => this.InnerCommand.CreateParameter();


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

        [CanBeNull]
        private IDisposable Profile(string name)
        {
            var specification = new ProfileOperationSpecification(name);
            specification.Category = ProfileOperationCategories.Sql;

            var operation = this.profiler.Profile(specification);

            if (operation != null)
            {
                var server = this.InnerCommand.Connection.DataSource;
                var database = this.InnerCommand.Connection.Database;

                operation.Resource = server + " - " + database;
                operation["Server"] = server;
                operation["Database"] = database;
                operation["Sql"] = this.InnerCommand.CommandText;
            }

            return operation;
        }

        #endregion
    }
}