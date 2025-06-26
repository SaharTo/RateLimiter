using System;
using System.Collections.Generic;
using System.Linq;
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
            // Wait for all rate limits to allow execution
            var waitTasks = _limits.Select(l => l.WaitUntilAllowedAsync());
            await Task.WhenAll(waitTasks);

            // Execute the function
            await _func(arg);
        }
    }
}
