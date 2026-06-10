using LiteDB;

var dbPath = Path.Combine(Path.GetTempPath(), $"smoke_test_{Guid.NewGuid():N}.db");

try
{
    using var db = new LiteDatabase(dbPath);

    var posts = db.GetCollection<SocialPost>("posts");
    posts.EnsureIndex(p => p.PostId, unique: true);
    posts.EnsureIndex(p => p.Account);

    var dummy = new SocialPost
    {
        PostId = "test-001",
        Account = "@testaccount",
        Text = "Hello, LiteDB!",
        Timestamp = DateTime.UtcNow,
        Sentiment = "positive",
        Summary = "A test post.",
        Tags = ["test", "litedb"],
        Categories = ["smoke-test"],
        Link = "https://example.com/1"
    };

    posts.Insert(dummy);

    var retrieved = posts.FindById("test-001");
    Console.WriteLine(retrieved is not null && retrieved.Account == "@testaccount"
        ? "SMOKE TEST PASSED: insert and retrieve OK"
        : "SMOKE TEST FAILED");
}
finally
{
    File.Delete(dbPath);
}

class SocialPost
{
    [BsonId] public string PostId { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Sentiment { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public string Link { get; set; } = string.Empty;
}
