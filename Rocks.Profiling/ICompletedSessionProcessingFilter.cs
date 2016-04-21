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
        ///     Determines if completed session is needs to be processed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is <see langword="null" />.</exception>
        bool ShouldProcess([NotNull] ProfileSession session);
    }
}
