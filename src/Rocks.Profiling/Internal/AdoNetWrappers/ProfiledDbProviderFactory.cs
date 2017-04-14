using System;
using System.Data.Common;
using System.Reflection;
using Rocks.Profiling.Internal.Implementation;

namespace Rocks.Profiling.Internal.AdoNetWrappers
{
    internal abstract class ProfiledDbProviderFactory : DbProviderFactory
    {
    }

    internal class ProfiledDbProviderFactory<TProviderFactory> : ProfiledDbProviderFactory, IServiceProvider
        where TProviderFactory : DbProviderFactory
    {
        public static readonly ProfiledDbProviderFactory<TProviderFactory> Instance = new ProfiledDbProviderFactory<TProviderFactory>();

        private readonly TProviderFactory innerFactory;


        /// <exception cref="NotSupportedException">Provider doesn't have Instance property.</exception>
        public ProfiledDbProviderFactory()
        {
            var field = typeof (TProviderFactory).GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            if (field == null)
                throw new NotSupportedException("Provider doesn't have Instance property.");

            // ReSharper disable ExceptionNotDocumented
            this.innerFactory = (TProviderFactory) field.GetValue(null);
            // ReSharper restore ExceptionNotDocumented
        }


        public override bool CanCreateDataSourceEnumerator => this.innerFactory.CanCreateDataSourceEnumerator;


        public override DbConnection CreateConnection()
        {
            var connection = this.innerFactory.CreateConnection();

            if (!ProfilerFactory.GetCurrentProfiler().Configuration.Enabled)
                return connection;

            return new ProfiledDbConnection(connection, this);
        }


        public override DbCommand CreateCommand()
        {
            var command = this.innerFactory.CreateCommand();

            if (!ProfilerFactory.GetCurrentProfiler().Configuration.Enabled)
                return command;

            return new ProfiledDbCommand(command);
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


        public object GetService(Type serviceType)
        {
            if (serviceType == this.GetType())
                return this.innerFactory;

            var service = ((IServiceProvider) this.innerFactory).GetService(serviceType);

            return service;
        }
    }
}