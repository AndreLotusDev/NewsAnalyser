using LiteDB;

namespace NewsAPI.Models;

public class SocialPost
{
    [BsonId]
    public string PostId { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Sentiment { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string Link { get; set; } = string.Empty;
}
