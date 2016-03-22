using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal.Implementation
{
    /// <summary>
    ///     Represents information about completed profile session.
    /// </summary>
    internal class CompletedSessionInfo
    {
        /// <summary>
        ///     Session that has been completed.
        /// </summary>
        [NotNull]
        public ProfileSession Session { get; }

        /// <summary>
        ///     Additional data, passed to <see cref="IProfiler.Stop"/> method.
        /// </summary>
        [CanBeNull]
        public IDictionary<string, object> AdditionalData { get; }


        public CompletedSessionInfo([NotNull] ProfileSession session,
                                    [CanBeNull] IDictionary<string, object> additionalData = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            this.Session = session;
            this.AdditionalData = additionalData;
        }
    }
}