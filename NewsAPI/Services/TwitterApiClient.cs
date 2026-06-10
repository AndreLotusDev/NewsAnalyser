using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewsAPI.Models;

namespace NewsAPI.Services;

public class TwitterApiClient : ITwitterApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TwitterApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TwitterApiClient(HttpClient http, ILogger<TwitterApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<SocialPost>> GetRecentPostsAsync(string handle, int count = 10)
    {
        var results = new List<SocialPost>();
        string? cursor = null;

        while (results.Count < count)
        {
            var (posts, hasNextPage, nextCursor) = await FetchPageAsync(handle, cursor);
            results.AddRange(posts.Take(count - results.Count));

            if (!hasNextPage || nextCursor is null)
                break;

            cursor = nextCursor;
        }

        return results;
    }

    public async Task<List<SocialPost>> GetPostsSinceAsync(string handle, string sinceId)
    {
        var results = new List<SocialPost>();
        string? cursor = null;

        while (true)
        {
            var (posts, hasNextPage, nextCursor) = await FetchPageAsync(handle, cursor);

            foreach (var post in posts)
            {
                if (CompareIds(post.PostId, sinceId) <= 0)
                    return results;
                results.Add(post);
            }

            if (!hasNextPage || nextCursor is null)
                break;

            cursor = nextCursor;
        }

        return results;
    }

    private async Task<(List<SocialPost> Posts, bool HasNextPage, string? NextCursor)> FetchPageAsync(
        string handle, string? cursor)
    {
        var url = $"twitter/user/last_tweets?userName={Uri.EscapeDataString(handle)}";
        if (cursor is not null)
            url += $"&cursor={Uri.EscapeDataString(cursor)}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for @{Handle}", handle);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching @{Handle}", handle);
            throw;
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit hit fetching @{Handle}", handle);
                throw new HttpRequestException("Twitter API rate limit exceeded", null, HttpStatusCode.TooManyRequests);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Account not found: @{Handle}", handle);
                return ([], false, null);
            }

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var page = JsonSerializer.Deserialize<TwitterPageResponse>(body, JsonOptions);

            if (page?.Status == "error")
            {
                _logger.LogWarning("API error for @{Handle}: {Message}", handle, page.Message);
                return ([], false, null);
            }

            var posts = (page?.Data?.Tweets ?? []).Select(t => MapToPost(t, handle)).ToList();
            return (posts, page?.Data?.HasNextPage ?? false, page?.Data?.NextCursor);
        }
    }

    private static SocialPost MapToPost(TwitterTweet tweet, string handle) => new()
    {
        PostId = tweet.Id,
        Account = handle,
        Text = tweet.Text,
        Timestamp = DateTime.TryParse(tweet.CreatedAt, out var dt) ? dt.ToUniversalTime() : DateTime.UtcNow,
        Link = tweet.Url ?? $"https://twitter.com/{handle}/status/{tweet.Id}",
    };

    // Twitter snowflake IDs are monotonically increasing 64-bit integers; compare numerically.
    private static int CompareIds(string a, string b)
    {
        if (ulong.TryParse(a, out var idA) && ulong.TryParse(b, out var idB))
            return idA.CompareTo(idB);
        return string.Compare(a, b, StringComparison.Ordinal);
    }

    private class TwitterPageResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public TwitterPageData? Data { get; set; }
    }

    private class TwitterPageData
    {
        public List<TwitterTweet> Tweets { get; set; } = [];
        [JsonPropertyName("has_next_page")] public bool HasNextPage { get; set; }
        [JsonPropertyName("next_cursor")] public string? NextCursor { get; set; }
    }

    private class TwitterTweet
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? CreatedAt { get; set; }
        public string? Url { get; set; }
    }
}
