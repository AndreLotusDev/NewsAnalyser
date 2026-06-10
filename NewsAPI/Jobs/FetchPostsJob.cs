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
    private readonly IConfiguration _config;

    public FetchPostsJob(
        AccountRepository accounts,
        PostRepository posts,
        ITwitterApiClient twitter,
        IAiEnrichmentService ai,
        JobStatusTracker jobStatus,
        ILogger<FetchPostsJob> logger,
        IConfiguration config)
    {
        _accounts = accounts;
        _posts = posts;
        _twitter = twitter;
        _ai = ai;
        _jobStatus = jobStatus;
        _logger = logger;
        _config = config;
    }

    public async Task RunAsync()
    {
        if (_config.GetValue<bool>("Features:SandboxMode"))
        {
            _logger.LogInformation("FetchPostsJob skipped — SandboxMode is enabled");
            return;
        }

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

    public async Task RunSandboxAsync()
    {
        var accounts = _accounts.GetAll();
        if (accounts.Count == 0)
        {
            _logger.LogWarning("SandboxMode: no accounts available");
            return;
        }

        var account = accounts[Random.Shared.Next(accounts.Count)];
        _logger.LogInformation("SandboxMode: running AI flow for @{Handle}", account.Handle);
        await FetchForAccountAsync(account.Handle);
        _jobStatus.RecordRun();
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
