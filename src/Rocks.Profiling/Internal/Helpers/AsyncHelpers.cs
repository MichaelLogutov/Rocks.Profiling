using System.Threading.Tasks;

namespace Rocks.Profiling.Internal.Helpers
{
    internal static class AsyncHelpers
    {
        public static Task Silent(this Task task)
        {
            return task.ContinueWith(_ =>
            {
            });
        }
    }
}