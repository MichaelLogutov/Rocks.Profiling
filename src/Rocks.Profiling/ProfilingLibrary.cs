using System;
using System.Collections.Generic;
using System.Data.Common;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Rocks.Helpers;
using Rocks.Profiling.Configuration;
using Rocks.Profiling.Internal.AdoNetWrappers;
using Rocks.Profiling.Internal.Implementation;
using Rocks.Profiling.Loggers;
using Rocks.Profiling.Models;
using Rocks.Profiling.Storage;
using SimpleInjector;

namespace Rocks.Profiling
{
    public static class ProfilingLibrary
    {
        internal static Container Container { get; private set; }


        public static void Setup(Func<HttpContext> httpContextFactory, Container externalContainer = null)
        {
            if (externalContainer == null)
                externalContainer = new Container { Options = { AllowOverridingRegistrations = true } };

            RegisterAll(httpContextFactory, externalContainer);

            Container = externalContainer;
            HttpContextFactory = httpContextFactory;
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

            c.RegisterSingleton<Func<HttpContext>>(httpContextFactory);

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
            DbFactory.CreateInstance = (type) =>
                                       {
                                           DbProviderFactory factory;
                                           try
                                           {
                                               factory = DbFactory.Get(type.FullName);
                                           }
                                           // ReSharper disable once CatchAllClause
                                           catch (Exception)
                                           {
                                               return null;
                                           }

                                           if (factory is ProfiledDbProviderFactory)
                                           {
                                               return factory;
                                           }

                                           return (DbProviderFactory) Activator.CreateInstance(typeof(ProfiledDbProviderFactory<>).MakeGenericType(factory.GetType()));
                                       };
        }
    }
}