using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Web;
using JetBrains.Annotations;
using Rocks.Profiling.Internal;
using Rocks.Profiling.Internal.AdoNetWrappers;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Storage;
using SimpleInjector;

namespace Rocks.Profiling
{
    public static class ProfilingLibrary
    {
        #region Static fields

        internal static Container Container { get; private set; }

        #endregion

        #region Static methods

        public static void Setup(Func<HttpContextBase> httpContextFactory,
                                 Container externalContainer = null,
                                 Action<ProfilerConfiguration> configure = null)
        {
            if (externalContainer == null)
                externalContainer = new Container { Options = { AllowOverridingRegistrations = true } };

            RegisterAll(httpContextFactory, externalContainer, configure);

            Container = externalContainer;
            HttpContextFactory = httpContextFactory;
        }


        /// <summary>
        ///     Gets the current profiler instance.
        /// </summary>
        [NotNull]
        public static IProfiler GetCurrentProfiler()
        {
            return ProfilerFactory.GetCurrentProfiler();
        }


        /// <summary>
        ///     Starts new profile session on the current profile.
        /// </summary>
        public static void StartProfiling()
        {
            ProfilerFactory.GetCurrentProfiler().Start();
        }


        /// <summary>
        ///     Starts new profile session on the current profile.
        /// </summary>
        public static void StopProfiling([CanBeNull] IDictionary<string, object> additionalSessionData = null)
        {
            ProfilerFactory.GetCurrentProfiler().Stop(additionalSessionData);
        }

        #endregion

        #region Protected properties

        internal static Func<HttpContextBase> HttpContextFactory { get; private set; }

        #endregion

        #region Private methods

        private static void RegisterAll(Func<HttpContextBase> httpContextFactory, Container c, [CanBeNull] Action<ProfilerConfiguration> configure)
        {
            var configuration = ProfilerConfiguration.FromAppConfig();
            configure?.Invoke(configuration);

            c.RegisterSingleton<Func<HttpContextBase>>(httpContextFactory);
            c.RegisterSingleton(configuration);

            c.RegisterSingleton<ICurrentSessionProvider, CurrentSessionProvider>();
            c.RegisterSingleton<IProfiler, Profiler>();
            c.RegisterSingleton<IProfilerLogger, NullProfilerLogger>();

            c.RegisterSingleton<ICompletedSessionsProcessorQueue, CompletedSessionsProcessorQueue>();
            c.RegisterSingleton<ICompletedSessionProcessorService, CompletedSessionProcessorService>();
            c.RegisterSingleton<IProfilerResultsStorage, NullProfilerResultsStorage>();

            ReplaceProviderFactories();

            configuration.ConfigureServices(c);
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