using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RateLimiter
{
    /// <summary>
    /// Coordinates multiple rate limits and executes the provided function
    /// only when all limits allow it. Supports concurrent usage.
    /// </summary>
    /// <typeparam name="TArg">The type of argument to pass to the function.</typeparam>
    public class RateLimiter<TArg>
    {
        private readonly Func<TArg, Task> _func;
        private readonly List<RateLimit> _limits;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public RateLimiter(Func<TArg, Task> func, IEnumerable<RateLimit> limits)
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
            _limits = limits?.ToList() ?? throw new ArgumentNullException(nameof(limits));
        }

        /// <summary>
        /// Waits until all rate limits permit execution, then runs the provided function.
        /// </summary>
        /// <param name="arg">The input to pass to the function.</param>
        public async Task Perform(TArg arg)
        {
            await _semaphore.WaitAsync(); // Only one task may proceed at a time, bascially controlling concurrency

            try
            {
                while (true)
                {
                    // Check delays across all rate limits, then check the maximum delay
                    var delays = _limits.Select(l => l.CheckDelay()).ToList();
                    var maxDelay = delays.Max();

                    // All limits allow execution at this moment
                    if (maxDelay == TimeSpan.Zero)
                    {
                        // Proceed to record timestamps and execute the function only when all limits are ready
                        foreach (var limit in _limits)
                            limit.RecordTimestamp(); // Add a timestamp to each limiter

                        await _func(arg); // Execute the user-provided function
                        return;
                    }

                    // Wait once for the largest delay required among all rate limits
                    await Task.Delay(maxDelay);
                }
            }
            finally
            {
                _semaphore.Release(); // Release the current task from semaphore and allow the next task to begin
            }
        }
    }
}
