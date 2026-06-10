using NewsAPI.Models;

namespace NewsAPI.Repositories;

public class PostRepository
{
    private readonly LiteDbContext _db;

    public PostRepository(LiteDbContext db) => _db = db;

    public PagedResult<SocialPost> GetPosts(
        List<string>? accounts,
        List<string>? tags,
        List<string>? categories,
        string? after,
        int page,
        int pageSize)
    {
        var query = _db.Posts.Query();

        if (accounts?.Count == 1)
        {
            var handle = accounts[0];
            query = query.Where(p => p.Account == handle);
        }

        var allPosts = query.OrderByDescending(p => p.Timestamp).ToList();

        if (accounts?.Count > 1)
            allPosts = allPosts
                .Where(p => accounts.Contains(p.Account, StringComparer.OrdinalIgnoreCase))
                .ToList();

        if (!string.IsNullOrWhiteSpace(after))
        {
            var afterPost = _db.Posts.FindById(after);
            if (afterPost != null)
                allPosts = allPosts.Where(p => p.Timestamp > afterPost.Timestamp).ToList();
        }

        if (tags?.Count > 0)
            allPosts = allPosts
                .Where(p => p.Tags.Any(t => tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                .ToList();

        if (categories?.Count > 0)
            allPosts = allPosts
                .Where(p => p.Categories.Any(c => categories.Contains(c, StringComparer.OrdinalIgnoreCase)))
                .ToList();

        var total = allPosts.Count;
        var items = allPosts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<SocialPost>(items, total, page, pageSize);
    }

    public bool HasPosts(string account) =>
        _db.Posts.Exists(p => p.Account == account);

    public string? GetLatestPostId(string account) =>
        _db.Posts.Query()
            .Where(p => p.Account == account)
            .OrderByDescending(p => p.Timestamp)
            .FirstOrDefault()
            ?.PostId;

    public List<SocialPost> GetUnenriched() =>
        _db.Posts.Query().ToList().Where(p => string.IsNullOrEmpty(p.Summary)).ToList();

    public void Upsert(SocialPost post) => _db.Posts.Upsert(post);
}
