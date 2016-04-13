using System;
using System.Threading;
using System.Web;
using JetBrains.Annotations;
using Rocks.Profiling.Models;
using Rocks.SimpleInjector.Attributes;

namespace Rocks.Profiling.Internal.Implementation
{
    internal class CurrentSessionProvider : ICurrentSessionProvider
    {
        private const string HttpContextCurrentSessionKey = "RocksProfiling_CurrentSession";

        private readonly Func<HttpContextBase> httpContextFactory;

        [ThreadSafe]
        private static readonly AsyncLocal<ProfileSession> CurrentSession = new AsyncLocal<ProfileSession>();


        public CurrentSessionProvider(Func<HttpContextBase> httpContextFactory)
        {
            this.httpContextFactory = httpContextFactory;
        }


        /// <summary>
        ///     Returns current profile session instance.<br />
        ///     If no session was set - returns null.
        /// </summary>
        public ProfileSession Get()
        {
            var http_context = this.httpContextFactory();
            if (http_context != null)
                return http_context.Items[HttpContextCurrentSessionKey] as ProfileSession;

            return CurrentSession.Value;
        }


        /// <summary>
        ///     Sets current profile session.
        /// </summary>
        public void Set(ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            this.SetCurrentSession(session);
        }


        /// <summary>
        ///     Deletes current profile session.
        /// </summary>
        public void Delete()
        {
            this.SetCurrentSession(null);
        }


        private void SetCurrentSession([CanBeNull] ProfileSession session)
        {
            var http_context = this.httpContextFactory();
            if (http_context != null)
            {
                http_context.Items[HttpContextCurrentSessionKey] = session;
                return;
            }

            CurrentSession.Value = session;
        }
    }
}