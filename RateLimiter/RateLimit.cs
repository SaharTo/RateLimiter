using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/* RateLimiter – Design Decision & Implementation Notes
------------------------------------------------------------------------------

This class implements a rate limiter that supports multiple rate limits
(e.g., 10 requests per second, 100 per minute, 1000 per day), and ensures
that a given action won't exceed any of them — even under concurrent usage.

────────────────────────────
Strategy Chosen: Sliding Window
────────────────────────────
I chose the **sliding window** approach over the absolute window approach.

The main reason is that sliding windows are much more accurate when it comes
to preventing bursts that can break the actual intent of a limit.

For example, imagine we allowed 1000 requests per day, and used the "absolute"
window — meaning: 1000 requests from midnight to midnight. If a client sends
1000 requests at 23:59, and then another 1000 requests at 00:01, we've allowed
2000 requests within 2 minutes. That’s obviously not aligned with the real
intent of "1000 per day."

In contrast, the sliding window always checks the *last N seconds/minutes/hours*
relative to now — so you can’t “cheat” by waiting for the clock to reset.

────────────────────────────
How It Works
────────────────────────────
Each RateLimit exposes two steps:
1. `CheckDelay()` — checks if the rate limit would allow an action right now.
2. `RecordTimestamp()` — is called only after the action is confirmed to run.

All rate limits are checked first, and only if all are ready, we commit and execute.

────────────────────────────
Pros:
- It provides a much more accurate way to enforce rate limits over time.
- Prevents burst traffic across artificial reset boundaries (e.g., midnight).

Cons:
- Slightly more complex to implement than absolute windowing “reset every X minutes” approach.
- Requires tracking execution history (e.g., using a queue of timestamps).
- Can consume a bit more memory depending on request volume.

────────────────────────────
Thread Safety
────────────────────────────
Each RateLimit instance holds a queue of timestamps and uses a private lock to
safely check and update that queue when multiple threads are calling in parallel.

────────────────────────────
Usage
────────────────────────────
You can pass in any function (like an API call) to the RateLimiter. Once
constructed with the relevant rate limits, just call `.Perform(arg)` from
anywhere — even from many threads at once. The limiter will make sure all
rules are followed before executing the function.
*/

namespace RateLimiter
{
    /// <summary>
    /// Represents a single sliding window rate limit.
    /// Limits the number of actions allowed within a specific time window.
    /// </summary>
    public class RateLimit
    {
        private readonly int _maxCount;
        private readonly TimeSpan _window;
        private readonly Queue<DateTime> _timestamps = new(); // Stores timestamps of previous executions
        private readonly object _lock = new(); // Ensures thread-safe access to the queue

        public RateLimit(int maxCount, TimeSpan window)
        {
            _maxCount = maxCount;
            _window = window;
        }

        /// <summary>
        /// CheckDelay is checking the queue of timestamps before allowing execution.
        /// </summary>

        public TimeSpan CheckDelay()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Remove timestamps outside the current sliding window
                while (_timestamps.Count > 0 && (now - _timestamps.Peek()) > _window)
                    _timestamps.Dequeue();

                if (_timestamps.Count < _maxCount)
                    return TimeSpan.Zero;

                return _timestamps.Peek() + _window - now; // Calculate how long to wait until the next allowed execution
            }
        }

        // Records the current time only after all rate limits approve execution
        public void RecordTimestamp()
        {
            lock (_lock)
            {
                _timestamps.Enqueue(DateTime.UtcNow);
            }
        }
    }
}
