using LiteDB;
using NewsAPI.Models;

namespace NewsAPI.Repositories;

public class LiteDbContext : IDisposable
{
    private readonly LiteDatabase _db;

    public ILiteCollection<SocialPost> Posts { get; }
    public ILiteCollection<TrackedAccount> TrackedAccounts { get; }
    public ILiteCollection<JobStatus> JobStatuses { get; }

    public LiteDbContext(LiteDatabase db)
    {
        _db = db;

        Posts = _db.GetCollection<SocialPost>("posts");
        Posts.EnsureIndex(p => p.PostId, unique: true);
        Posts.EnsureIndex(p => p.Account);

        TrackedAccounts = _db.GetCollection<TrackedAccount>("tracked_accounts");
        TrackedAccounts.EnsureIndex(a => a.Handle, unique: true);

        JobStatuses = _db.GetCollection<JobStatus>("job_statuses");
    }

    public void Dispose() => _db.Dispose();
}
