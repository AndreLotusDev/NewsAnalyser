using LiteDB;

namespace NewsAPI.Models;

public class Category
{
    [BsonId]
    public string Name { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
}
