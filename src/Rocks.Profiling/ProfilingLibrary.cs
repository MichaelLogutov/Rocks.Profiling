using System;
using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using Rocks.Helpers;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Internal.AdoNetWrappers;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;
using SimpleInjector;
#if NETFRAMEWORK
using System.Data;
using System.Linq;
using System.Reflection;
using HttpContext = System.Web.HttpContextBase;

#else
using Microsoft.AspNetCore.Http;

#endif

namespace Rocks.Profiling
{
    public static class ProfilingLibrary
    {
        private static readonly object SetupLock = new object();

        internal static Container Container { get; private set; }


        public static void Setup(Func<HttpContext> httpContextFactory, Container externalContainer = null)
        {
            lock (SetupLock)
            {
                if (externalContainer == null)
                    externalContainer = new Container { Options = { AllowOverridingRegistrations = true } };

                RegisterAll(httpContextFactory, externalContainer);

                Container = externalContainer;
                HttpContextFactory = httpContextFactory;
            }
        }


        /// <summary>
        ///     Gets the current profiler instance.
        /// </summary>
        [NotNull]
        public static IProfiler GetCurrentProfiler() => ProfilerFactory.GetCurrentProfiler();


        /// <summary>
        ///     Creates new profile session.
        /// </summary>
        [CanBeNull]
        public static ProfileSession StartProfiling([CanBeNull] IDictionary<string, object> additionalSessionData = null)
            => ProfilerFactory.GetCurrentProfiler().Start(additionalSessionData);


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="specification"/>.<br />
        ///     Uppon disposing will store the results of measurement in the current session.<br />
        ///     If there is no session started - returns dummy operation that will do nothing.
        /// </summary>
        [CanBeNull, MustUseReturnValue]
        public static ProfileOperation Profile([NotNull] ProfileOperationSpecification specification)
            => ProfilerFactory.GetCurrentProfiler().Profile(specification);


        /// <summary>
        ///     Starts new scope that will measure execution time of the operation
        ///     with specified <paramref name="specification"/>.<br />
        ///     Uppon disposing will store the results of measurement in the specified <paramref name="session"/>.<br />
        ///     If there is no session started - returns dummy operation that will do nothing.
        /// </summary>
        [CanBeNull, MustUseReturnValue]
        public static ProfileOperation Profile([NotNull] ProfileSession session, [NotNull] ProfileOperationSpecification specification)
            => ProfilerFactory.GetCurrentProfiler().Profile(session, specification);


        /// <summary>
        ///     Stops current profile session and stores the results.
        /// </summary>
        public static void StopProfiling([CanBeNull] IDictionary<string, object> additionalSessionData = null)
            => ProfilerFactory.GetCurrentProfiler().Stop(additionalSessionData);


        /// <summary>
        ///     Stops specified profile <paramref name="session"/> and stores the results.
        /// </summary>
        public static void StopProfiling([NotNull] ProfileSession session, [CanBeNull] IDictionary<string, object> additionalSessionData = null)
            => ProfilerFactory.GetCurrentProfiler().Stop(session, additionalSessionData);


        internal static Func<HttpContext> HttpContextFactory { get; private set; }


        private static void RegisterAll(Func<HttpContext> httpContextFactory, Container c)
        {
            c.RegisterSingleton<IProfilerConfiguration, ProfilerConfiguration>();

            c.RegisterInstance(httpContextFactory);

            c.RegisterSingleton<ICurrentSessionProvider, CurrentSessionProvider>();
            c.RegisterSingleton<IProfiler, Profiler>();
            c.RegisterSingleton<IProfilerLogger, NullProfilerLogger>();

            c.RegisterSingleton<ICompletedSessionsProcessorQueue, CompletedSessionsProcessorQueue>();
            c.RegisterSingleton<ICompletedSessionProcessorService, CompletedSessionProcessorService>();
            c.RegisterSingleton<IProfilerResultsStorage, NullProfilerResultsStorage>();
            c.RegisterSingleton<ICompletedSessionProcessingFilter, NullCompletedSessionProcessingFilter>();
            c.RegisterSingleton<IProfilerEventsHandler, NullProfilerEventsHandler>();

            ReplaceProviderFactories();
        }


        private static void ReplaceProviderFactories()
        {
#if NETFRAMEWORK
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

                if (factory is ProfiledDbProviderFactory)
                    continue;

                var proxy_type = typeof(ProfiledDbProviderFactory<>).MakeGenericType(factory.GetType());

                var wrapped_provider_row = table.NewRow();
                wrapped_provider_row["Name"] = row["Name"];
                wrapped_provider_row["Description"] = row["Description"];
                wrapped_provider_row["InvariantName"] = row["InvariantName"];
                wrapped_provider_row["AssemblyQualifiedName"] = proxy_type.AssemblyQualifiedName;

                table.Rows.Remove(row);
                table.Rows.Add(wrapped_provider_row);
            }
#endif

            GlobalDbFactoriesProvider.SetConstructInstanceInterceptor(
                instance =>
                {
                    if (instance is ProfiledDbProviderFactory)
                        return instance;

                    var new_instance = (DbProviderFactory) Activator.CreateInstance(typeof(ProfiledDbProviderFactory<>).MakeGenericType(instance.GetType()));

                    GlobalDbFactoriesProvider.Set(instance.GetType().Namespace, new_instance);

                    return new_instance;
                });
        }


#if NETFRAMEWORK
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

            var type = typeof(DbProviderFactories);

            var config_table_field = type.GetField("_configTable", BindingFlags.NonPublic | BindingFlags.Static)
                                     ?? type.GetField("_providerTable", BindingFlags.NonPublic | BindingFlags.Static);

            if (config_table_field == null)
                return null;

            var config_table = config_table_field.GetValue(null);

            if (config_table is DataSet config_table_dataset)
                return config_table_dataset.Tables["DbProviderFactories"];

            return (DataTable) config_table;
        }
#endif
    }
}