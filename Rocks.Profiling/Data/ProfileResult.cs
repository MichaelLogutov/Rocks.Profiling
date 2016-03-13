using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Rocks.Profiling.Internal.Implementation;

namespace Rocks.Profiling.Data
{
    /// <summary>
    ///     Represents a profiled session result.
    /// </summary>
    public class ProfileResult
    {
        /// <summary>
        ///     Operations tree root node.
        /// </summary>
        [NotNull]
        public IProfileOperation OperationsTreeRoot { get; }

        /// <summary>
        ///     Additional session data passed to <see cref="IProfiler.Stop"/> method.
        /// </summary>
        [CanBeNull]
        public IDictionary<string, object> SessionData { get; set; }

        /// <summary>
        ///     Total time of all operations.
        /// </summary>
        public TimeSpan TotalTime { get; set; }


        internal ProfileResult([NotNull] CompletedSessionInfo completedSessionInfo)
        {
            if (completedSessionInfo == null)
                throw new ArgumentNullException(nameof(completedSessionInfo));

            this.SessionData = completedSessionInfo.AdditionalData;
            this.OperationsTreeRoot = completedSessionInfo.Session.OperationsTreeRoot;
            this.TotalTime = completedSessionInfo.Session.GetTotalDuration();
        }
    }
}