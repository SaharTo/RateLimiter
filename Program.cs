using RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new();

    static async Task Main()
    {
        var rateLimits = new List<RateLimit>
        {
            new RateLimit(1, TimeSpan.FromSeconds(1)),
            new RateLimit(100, TimeSpan.FromMinutes(1)),
            new RateLimit(1000, TimeSpan.FromHours(24))
        };

        var rateLimiter = new RateLimiter<int>(CallJsonPlaceholderApiAsync, rateLimits);

        // Simulate 50 concurrent calls fetching posts with IDs 1 to 50
        var tasks = Enumerable.Range(1, 50)
            .Select(rateLimiter.Perform)
            .ToArray();

        await Task.WhenAll(tasks);

        Console.WriteLine("All tasks completed.");
    }

    private static async Task CallJsonPlaceholderApiAsync(int postId)
    {
        var url = $"https://jsonplaceholder.typicode.com/posts/{postId}";

        try
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] ✅ Executing task {postId}");
            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                return;
            }
            else
            {
                Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Failed to fetch post {postId}: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] Error fetching post {postId}: {ex.Message}");
        }
    }
}
