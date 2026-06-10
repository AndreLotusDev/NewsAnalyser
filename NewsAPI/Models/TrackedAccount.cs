using LiteDB;

namespace NewsAPI.Models;

public class TrackedAccount
{
    [BsonId]
    public string Handle { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
