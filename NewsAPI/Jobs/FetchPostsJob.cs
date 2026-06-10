using NewsAPI.Repositories;
using NewsAPI.Services;

namespace NewsAPI.Jobs;

public class FetchPostsJob
{
    private readonly AccountRepository _accounts;
    private readonly PostRepository _posts;
    private readonly ITwitterApiClient _twitter;
    private readonly IAiEnrichmentService _ai;
    private readonly JobStatusTracker _jobStatus;
    private readonly ILogger<FetchPostsJob> _logger;

    public FetchPostsJob(
        AccountRepository accounts,
        PostRepository posts,
        ITwitterApiClient twitter,
        IAiEnrichmentService ai,
        JobStatusTracker jobStatus,
        ILogger<FetchPostsJob> logger)
    {
        _accounts = accounts;
        _posts = posts;
        _twitter = twitter;
        _ai = ai;
        _jobStatus = jobStatus;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("FetchPostsJob started");

        var accounts = _accounts.GetAll();
        foreach (var account in accounts)
        {
            try
            {
                var count = await FetchForAccountAsync(account.Handle);
                _logger.LogInformation("Fetched {Count} posts for @{Handle}", count, account.Handle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch posts for @{Handle}", account.Handle);
            }
        }

        await EnrichPendingPostsAsync();

        _jobStatus.RecordRun();
        _logger.LogInformation("FetchPostsJob completed");
    }

    private async Task EnrichPendingPostsAsync()
    {
        var pending = _posts.GetUnenriched();
        if (pending.Count == 0) return;
        _logger.LogInformation("Re-enriching {Count} posts with missing AI data", pending.Count);
        foreach (var post in pending)
        {
            try
            {
                await _ai.EnrichPostAsync(post);
                _posts.Upsert(post);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Re-enrichment failed for post {PostId}", post.PostId);
            }
        }
    }

    private async Task<int> FetchForAccountAsync(string handle)
    {
        var latestPostId = _posts.GetLatestPostId(handle);

        var posts = latestPostId is null
            ? await _twitter.GetRecentPostsAsync(handle, 10)
            : await _twitter.GetPostsSinceAsync(handle, latestPostId);

        foreach (var post in posts)
        {
            await _ai.EnrichPostAsync(post);
            _posts.Upsert(post);
        }

        return posts.Count;
    }
}
