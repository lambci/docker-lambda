using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MockLambdaRuntime
{
    internal static class DebuggerExtensions
    {
        /// <summary>
        /// Tries to wait for the debugger to attach by inspecting <see cref="Debugger.IsAttached"/> property in a loop.
        /// </summary>
        /// <param name="queryInterval"><see cref="TimeSpan"/> representing the frequency of inspection.</param>
        /// <param name="timeout"><see cref="TimeSpan"/> representing the timeout for the operation.</param>
        /// <returns><c>True</c> if debugger was attached, false if timeout occured.</returns>
        public static bool TryWaitForAttaching(TimeSpan queryInterval, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();

            while (!Debugger.IsAttached)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    return false;
                }

                Task.Delay(queryInterval).Wait();
            }

            return true;
        }
    }
}