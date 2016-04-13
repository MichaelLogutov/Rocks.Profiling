using JetBrains.Annotations;
using Rocks.Profiling.Models;

namespace Rocks.Profiling.Internal
{
    internal interface ICurrentSessionProvider
    {
        /// <summary>
        ///     Returns current profile session instance.<br />
        ///     If no session was set - returns null.
        /// </summary>
        [CanBeNull]
        ProfileSession Get();


        /// <summary>
        ///     Sets current profile session.
        /// </summary>
        void Set([NotNull] ProfileSession session);


        /// <summary>
        ///     Deletes current profile session.
        /// </summary>
        void Delete();
    }
}