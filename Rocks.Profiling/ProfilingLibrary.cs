using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Web;
using JetBrains.Annotations;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.AdoNetWrappers;
using Rocks.Profiling.Internal.Implementation;
using SimpleInjector;

namespace Rocks.Profiling
{
    public static class ProfilingLibrary
    {
        #region Static fields

        private static Container container;

        #endregion

        #region Public properties

        /// <summary>
        ///     Get current library DI container.
        /// </summary>
        /// <exception cref="InvalidOperationException" accessor="get">ProfilingLibrary не инициализирована. Необходимо вызвать ProfilingLibrary.Setup ()</exception>
        public static Container Container
        {
            get
            {
                if (container == null)
                    throw new InvalidOperationException("ProfilingLibrary не инициализирована. Необходимо вызвать ProfilingLibrary.Setup ()");

                return container;
            }
            internal set { container = value; }
        }

        internal static Func<HttpContextBase> HttpContextFactory { get; private set; }

        #endregion

        #region Static methods

        public static void Setup(Func<HttpContextBase> httpContextFactory,
                                 Container externalContainer = null,
                                 Lifestyle defaultLifestyle = null,
                                 Action<ProfilerConfiguration> configure = null)
        {
            if (externalContainer == null)
                externalContainer = new Container { Options = { AllowOverridingRegistrations = true } };

            if (defaultLifestyle == null)
                // ReSharper disable once RedundantAssignment
                defaultLifestyle = Lifestyle.Transient;

            RegisterAll(externalContainer, configure);

            container = externalContainer;
            HttpContextFactory = httpContextFactory;
        }

        #endregion

        #region Private methods

        private static void RegisterAll(Container c, [CanBeNull] Action<ProfilerConfiguration> configure)
        {
            c.RegisterSingleton(() =>
                                {
                                    var config = ProfilerConfiguration.FromAppConfig();
                                    configure?.Invoke(config);

                                    return config;
                                });

            c.Register<IProfiler, Profiler>();
            c.RegisterSingleton<ICompletedSessionsProcessorQueue, CompletedSessionsProcessorQueue>();
            c.RegisterSingleton<ICompletedSessionProcessorService, CompletedSessionProcessorService>();

            ReplaceProviderFactories();
        }


        private static void ReplaceProviderFactories()
        {
            var table = GetDbProviderFactoryConfigTable();
            if (table == null)
                return;

            foreach (var row in table.Rows.Cast<DataRow>().ToList())
            {
                DbProviderFactory factory;
                try
                {
                    factory = DbProviderFactories.GetFactory(row);
                }
                    // ReSharper disable once CatchAllClause
                catch (Exception)
                {
                    continue;
                }

                if (factory is WrappedDbProviderFactory)
                    continue;

                var proxy_type = typeof (WrappedDbProviderFactory<>).MakeGenericType(factory.GetType());

                var wrapped_provider_row = table.NewRow();
                wrapped_provider_row["Name"] = row["Name"];
                wrapped_provider_row["Description"] = row["Description"];
                wrapped_provider_row["InvariantName"] = row["InvariantName"];
                wrapped_provider_row["AssemblyQualifiedName"] = proxy_type.AssemblyQualifiedName;

                table.Rows.Remove(row);
                table.Rows.Add(wrapped_provider_row);
            }
        }


        [CanBeNull]
        private static DataTable GetDbProviderFactoryConfigTable()
        {
            try
            {
                // force initialization
                DbProviderFactories.GetFactory("Unknown");
            }
            catch (ArgumentException)
            {
            }

            var type = typeof (DbProviderFactories);

            var config_table_field = type.GetField("_configTable", BindingFlags.NonPublic | BindingFlags.Static)
                                     ?? type.GetField("_providerTable", BindingFlags.NonPublic | BindingFlags.Static);

            if (config_table_field == null)
                return null;

            var config_table = config_table_field.GetValue(null);

            var config_table_dataset = config_table as DataSet;
            if (config_table_dataset != null)
                return config_table_dataset.Tables["DbProviderFactories"];

            return (DataTable) config_table;
        }

        #endregion
    }
}