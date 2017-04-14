using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    [DesignerCategory("")]
    public class ProfiledDbConnection : DbConnection
    {
        public ProfiledDbConnection(DbConnection connection)
            : this(connection, connection.TryGetProviderFactory())
        {
        }


        public ProfiledDbConnection(DbConnection connection, DbProviderFactory innerProviderFactory)
        {
            this.InnerConnection = connection;
            this.InnerProviderFactory = innerProviderFactory;
        }


        public DbConnection InnerConnection { get; private set; }
        public DbProviderFactory InnerProviderFactory { get; private set; }

        public override event StateChangeEventHandler StateChange
        {
            add
            {
                if (this.InnerConnection != null)
                    this.InnerConnection.StateChange += value;
            }
            remove
            {
                if (this.InnerConnection != null)
                    this.InnerConnection.StateChange -= value;
            }
        }

        public override string ConnectionString
        {
            get { return this.InnerConnection.ConnectionString; }
            set { this.InnerConnection.ConnectionString = value; }
        }

        public override int ConnectionTimeout => this.InnerConnection.ConnectionTimeout;

        public override string Database => this.InnerConnection.Database;

        public override string DataSource => this.InnerConnection.DataSource;

        public override ConnectionState State => this.InnerConnection.State;

        public override string ServerVersion => this.InnerConnection.ServerVersion;

        public override ISite Site
        {
            get { return this.InnerConnection.Site; }
            set { this.InnerConnection.Site = value; }
        }

        public override void ChangeDatabase(string databaseName) => this.InnerConnection.ChangeDatabase(databaseName);
        public override void Close() => this.InnerConnection.Close();
        public override void Open() => this.InnerConnection.Open();
        public override void EnlistTransaction(Transaction transaction) => this.InnerConnection.EnlistTransaction(transaction);
        public override DataTable GetSchema() => this.InnerConnection.GetSchema();
        public override DataTable GetSchema(string collectionName) => this.InnerConnection.GetSchema(collectionName);
        public override DataTable GetSchema(string collectionName, string[] restrictionValues) => this.InnerConnection.GetSchema(collectionName, restrictionValues);

        protected override DbProviderFactory DbProviderFactory => this.InnerProviderFactory;


        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            var transaction = new ProfiledDbTransaction(this.InnerConnection.BeginTransaction(isolationLevel), this);

            return transaction;
        }


        protected override DbCommand CreateDbCommand() => new ProfiledDbCommand(this.InnerConnection.CreateCommand(), this);


        // ReSharper disable once SuspiciousTypeConversion.Global
        protected override object GetService(Type service) => ((IServiceProvider) this.InnerConnection).GetService(service);


        protected override void Dispose(bool disposing)
        {
            if (disposing)
                this.InnerConnection?.Dispose();

            this.InnerConnection = null;
            this.InnerProviderFactory = null;

            base.Dispose(disposing);
        }
    }
}