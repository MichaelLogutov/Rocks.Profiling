using System;
using System.Data.Common;
using System.Reflection;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal abstract class WrappedDbProviderFactory : DbProviderFactory
    {
    }

    internal class WrappedDbProviderFactory<TProviderFactory> : WrappedDbProviderFactory, IServiceProvider
        where TProviderFactory : DbProviderFactory
    {
        #region Constants

        public static readonly WrappedDbProviderFactory<TProviderFactory> Instance = new WrappedDbProviderFactory<TProviderFactory>();

        #endregion

        #region Private readonly fields

        private readonly TProviderFactory innerFactory;

        #endregion

        #region Construct

        public WrappedDbProviderFactory()
        {
            var field = typeof (TProviderFactory).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (field == null)
                throw new NotSupportedException("Provider doesn't have Instance property.");

            this.innerFactory = (TProviderFactory) field.GetValue(null);
        }

        #endregion

        #region Public properties

        public override bool CanCreateDataSourceEnumerator
        {
            get { return this.innerFactory.CanCreateDataSourceEnumerator; }
        }

        #endregion

        #region Public methods

        public override DbConnection CreateConnection()
        {
            var connection = this.innerFactory.CreateConnection();

            if (!ProfilerFactory.GetCurrentProfiler().Configuration.ShouldInterceptAdoNet)
                return connection;

            return new WrappedDbConnection(connection, this);
        }


        public override DbCommand CreateCommand()
        {
            var command = this.innerFactory.CreateCommand();

            if (!ProfilerFactory.GetCurrentProfiler().Configuration.ShouldInterceptAdoNet)
                return command;

            return new WrappedDbCommand(command);
        }


        public override DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            return this.innerFactory.CreateConnectionStringBuilder();
        }


        public override DbCommandBuilder CreateCommandBuilder()
        {
            return this.innerFactory.CreateCommandBuilder();
        }


        public override DbDataAdapter CreateDataAdapter()
        {
            return this.innerFactory.CreateDataAdapter();
        }


        public override DbDataSourceEnumerator CreateDataSourceEnumerator()
        {
            return this.innerFactory.CreateDataSourceEnumerator();
        }


        public override DbParameter CreateParameter()
        {
            return this.innerFactory.CreateParameter();
        }

        #endregion

        #region IServiceProvider Members

        public object GetService(Type serviceType)
        {
            if (serviceType == this.GetType())
                return this.innerFactory;

            var service = ((IServiceProvider) this.innerFactory).GetService(serviceType);

            return service;
        }

        #endregion
    }
}