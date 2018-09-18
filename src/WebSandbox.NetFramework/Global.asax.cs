using System;
using System.Threading;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Rocks.Profiling;
using Rocks.Profiling.Storage;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using SimpleInjector.Lifestyles;

namespace WebSandbox.NetFramework
{
    public class MvcApplication : HttpApplication
    {
        /// <summary>
        ///     DI контейнер.
        /// </summary>
        public static Container Container { get; protected set; } =
            new Container
            {
                Options =
                {
                    AllowOverridingRegistrations = true,
                    DefaultScopedLifestyle = new AsyncScopedLifestyle()
                }
            };


        private static readonly AsyncLocal<HttpContextBase> HttpContextAsyncLocal = new AsyncLocal<HttpContextBase>();


        protected void Application_Start()
        {
            var config = GlobalConfiguration.Configuration;

            Container.RegisterWebApiControllers(config);
            Container.RegisterSingleton<IProfilerResultsStorage, ProfilerJsonResultsStorage>();
            
            // ReSharper disable once ConvertToLocalFunction
            Func<HttpContextBase> http_context_factory = () =>
                                                         {
                                                             var result = HttpContextAsyncLocal.Value;
                                                             if (result == null)
                                                             {
                                                                 var http_context = HttpContext.Current;
                                                                 if (http_context != null)
                                                                 {
                                                                     result =  new HttpContextWrapper(http_context);
                                                                     HttpContextAsyncLocal.Value = result;
                                                                 }
                                                             }
                                                             
                                                             return result;
                                                         };
            
            ProfilingLibrary.Setup(http_context_factory, Container);
            
            Container.RegisterSingleton<IProfilerResultsStorage, ProfilerJsonResultsStorage>();

            config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(Container);
            config.MapHttpAttributeRoutes();

            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.JsonFormatter.SerializerSettings.Converters.Add(new StringEnumConverter());
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;

            config.EnsureInitialized();
            Container.Verify();
        }


        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpContextAsyncLocal.Value = new HttpContextWrapper(HttpContext.Current);
            ProfilingLibrary.StartProfiling();
        }


        protected void Application_EndRequest(object sender, EventArgs e)
        {
            ProfilingLibrary.StopProfiling();
            HttpContextAsyncLocal.Value = null;
        }
    }
}