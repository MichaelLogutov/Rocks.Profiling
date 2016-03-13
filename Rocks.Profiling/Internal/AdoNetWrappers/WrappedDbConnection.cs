using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    [DesignerCategory("")]
    public class WrappedDbConnection : DbConnection
    {
        #region Construct

        public WrappedDbConnection(DbConnection connection)
            : this(connection, connection.TryGetProviderFactory())
        {
        }


        public WrappedDbConnection(DbConnection connection, DbProviderFactory innerProviderFactory)
        {
            this.InnerConnection = connection;
            this.InnerProviderFactory = innerProviderFactory;
        }

        #endregion

        #region Public properties

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

        public override int ConnectionTimeout
        {
            get { return this.InnerConnection.ConnectionTimeout; }
        }

        public override string Database
        {
            get { return this.InnerConnection.Database; }
        }

        public override string DataSource
        {
            get { return this.InnerConnection.DataSource; }
        }

        public override ConnectionState State
        {
            get { return this.InnerConnection.State; }
        }

        public override string ServerVersion
        {
            get { return this.InnerConnection.ServerVersion; }
        }

        public override ISite Site
        {
            get { return this.InnerConnection.Site; }
            set { this.InnerConnection.Site = value; }
        }

        #endregion

        #region Public methods

        public override void ChangeDatabase(string databaseName)
        {
            this.InnerConnection.ChangeDatabase(databaseName);
        }


        public override void Close()
        {
            this.InnerConnection.Close();
        }


        public override void Open()
        {
            this.InnerConnection.Open();
        }


        public override void EnlistTransaction(Transaction transaction)
        {
            this.InnerConnection.EnlistTransaction(transaction);
        }


        public override DataTable GetSchema()
        {
            return this.InnerConnection.GetSchema();
        }


        public override DataTable GetSchema(string collectionName)
        {
            return this.InnerConnection.GetSchema(collectionName);
        }


        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return this.InnerConnection.GetSchema(collectionName, restrictionValues);
        }

        #endregion

        #region Protected properties

        protected override DbProviderFactory DbProviderFactory
        {
            get { return this.InnerProviderFactory; }
        }

        #endregion

        #region Protected methods

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return this.InnerConnection.BeginTransaction(isolationLevel);
        }


        protected override DbCommand CreateDbCommand()
        {
            return new WrappedDbCommand(this.InnerConnection.CreateCommand(), this);
        }


        protected override object GetService(Type service)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            return ((IServiceProvider) this.InnerConnection).GetService(service);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing && this.InnerConnection != null)
                this.InnerConnection.Dispose();

            this.InnerConnection = null;
            this.InnerProviderFactory = null;

            base.Dispose(disposing);
        }

        #endregion
    }
}