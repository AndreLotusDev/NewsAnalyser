using NewsAPI.Models;
using NewsAPI.Repositories;

namespace NewsAPI.Services;

public class JobStatusTracker
{
    private const string FetchPostsJobName = "fetch-posts";

    private readonly LiteDbContext _db;

    public JobStatusTracker(LiteDbContext db) => _db = db;

    public DateTime? LastRunAt =>
        _db.JobStatuses.FindById(FetchPostsJobName)?.LastRunAt;

    public void RecordRun() =>
        _db.JobStatuses.Upsert(new JobStatus
        {
            JobName = FetchPostsJobName,
            LastRunAt = DateTime.UtcNow
        });
}
