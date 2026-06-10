using NewsAPI.Models;
using NewsAPI.Repositories;

namespace NewsAPI.Services;

public class SocialService
{
    private readonly PostRepository _posts;
    private readonly AccountRepository _accounts;
    private readonly JobStatusTracker _jobStatus;

    public SocialService(PostRepository posts, AccountRepository accounts, JobStatusTracker jobStatus)
    {
        _posts = posts;
        _accounts = accounts;
        _jobStatus = jobStatus;
    }

    public PagedResult<SocialPost> GetPosts(
        List<string>? accounts, List<string>? tags, List<string>? categories, string? after, int page, int pageSize) =>
        _posts.GetPosts(accounts, tags, categories, after, page, pageSize);

    public object GetHealth() => new
    {
        status = "ok",
        timestamp = DateTime.UtcNow,
        lastRunAt = _jobStatus.LastRunAt
    };

    public (bool success, string error, List<TrackedAccount> accounts) ManageAccount(string handle, string action)
    {
        if (string.IsNullOrWhiteSpace(handle))
            return (false, "Handle is required.", []);

        if (action == "add")
            _accounts.Add(handle);
        else if (action == "remove")
            _accounts.Remove(handle);
        else
            return (false, "Action must be 'add' or 'remove'.", []);

        return (true, string.Empty, _accounts.GetAll());
    }
}
