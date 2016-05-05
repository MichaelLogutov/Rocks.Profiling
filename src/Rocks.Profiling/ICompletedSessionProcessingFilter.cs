using System;
using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling
{
    /// <summary>
    ///     A filter for completed sessions before they will be processed.
    /// </summary>
    public interface ICompletedSessionProcessingFilter
    {
        /// <summary>
        ///     Determines if completed session is needs to be processed.<br />
        ///     The implementation of this method should be thread safe.<br />
        ///     If <see langword="null" /> returned - the default filtering sequence will be performed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null" />.</exception>
        bool? ShouldProcess([NotNull] ProfileSession session);
    }
}
