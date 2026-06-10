using LiteDB;

namespace NewsAPI.Models;

public class JobStatus
{
    [BsonId]
    public string JobName { get; set; } = string.Empty;
    public DateTime? LastRunAt { get; set; }
}
