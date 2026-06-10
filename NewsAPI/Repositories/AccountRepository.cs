using NewsAPI.Models;

namespace NewsAPI.Repositories;

public class AccountRepository
{
    private readonly LiteDbContext _db;

    public AccountRepository(LiteDbContext db) => _db = db;

    public List<TrackedAccount> GetAll() =>
        _db.TrackedAccounts.Query().ToList();

    public void Add(string handle) => AddWithTimestamp(handle, DateTime.UtcNow);

    public void AddWithTimestamp(string handle, DateTime addedAt)
    {
        if (!_db.TrackedAccounts.Exists(a => a.Handle == handle))
            _db.TrackedAccounts.Insert(new TrackedAccount { Handle = handle, AddedAt = addedAt });
    }

    public void Remove(string handle)
    {
        _db.TrackedAccounts.DeleteMany(a => a.Handle == handle);
        _db.Posts.DeleteMany(p => p.Account == handle);
    }
}
