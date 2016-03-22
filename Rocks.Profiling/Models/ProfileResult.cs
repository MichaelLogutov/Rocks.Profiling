using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Rocks.Profiling.Internal.Implementation;

namespace Rocks.Profiling.Models
{
    /// <summary>
    ///     Represents a profiled session result.
    /// </summary>
    [DataContract]
    public class ProfileResult
    {
        /// <summary>
        ///     Operations tree root node.
        /// </summary>
        [NotNull, DataMember]
        public ProfileOperation OperationsRoot { get; }

        /// <summary>
        ///     Additional session data passed to <see cref="IProfiler.Stop"/> method.
        /// </summary>
        [CanBeNull, DataMember]
        public IDictionary<string, object> SessionData { get; set; }


        internal ProfileResult([NotNull] CompletedSessionInfo completedSessionInfo)
        {
            if (completedSessionInfo == null)
                throw new ArgumentNullException(nameof(completedSessionInfo));

            this.SessionData = completedSessionInfo.AdditionalData;
            this.OperationsRoot = completedSessionInfo.Session.OperationsTreeRoot;
        }
    }
}