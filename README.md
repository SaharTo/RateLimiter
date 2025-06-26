# RateLimiter

A thread-safe C# rate limiter that supports multiple rate limits using the **sliding window** strategy.

## Features
- Supports multiple concurrent rate limits (e.g., 10/sec, 100/min, 1000/day).
- Thread-safe: safe for multi-threaded use.
- Custom function support via `Func<TArg, Task>`.
- Clean and extensible structure.

## Usage

```csharp
var rateLimiter = new RateLimiter<string>(SimulateApiCallAsync, rateLimits);
await rateLimiter.Perform("Request");
