using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

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


        /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null" />.</exception>
        public ProfileResult([NotNull] ProfileSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            this.SessionData = session.AdditionalData;
            this.OperationsRoot = session.OperationsTreeRoot;
        }
    }
}