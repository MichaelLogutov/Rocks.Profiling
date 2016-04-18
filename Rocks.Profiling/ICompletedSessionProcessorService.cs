using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling
{
    /// <summary>
    ///     Handles the routines used for processing completed profile sessions.
    /// </summary>
    public interface ICompletedSessionProcessorService
    {
        /// <summary>
        ///     Determines if completed session is needs to be processed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null" />.</exception>
        bool ShouldProcess([NotNull] ProfileSession session);


        /// <summary>
        ///     Perform processing of completed sessions (like, storing the result).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="sessions"/> is <see langword="null" />.</exception>
        Task ProcessAsync([NotNull] IReadOnlyList<ProfileSession> sessions,
                          CancellationToken cancellationToken = default(CancellationToken));
    }
}