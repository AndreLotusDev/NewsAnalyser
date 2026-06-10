using System.Globalization;
using NewsAPI.Models;

namespace NewsAPI.Repositories;

public class CategoryRepository
{
    private static readonly TextInfo _textInfo = CultureInfo.InvariantCulture.TextInfo;

    private readonly LiteDbContext _db;

    public CategoryRepository(LiteDbContext db) => _db = db;

    // Canonical form: "geopolitical risk" and "Geopolitical Risk" → "Geopolitical Risk"
    public static string Normalize(string name) =>
        _textInfo.ToTitleCase(name.Trim().ToLower());

    public List<string> GetAll() =>
        _db.Categories.Query().OrderBy(c => c.Name).ToList().ConvertAll(c => c.Name);

    public void Sync(IEnumerable<string> categories)
    {
        foreach (var raw in categories.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            var name = Normalize(raw);
            _db.Categories.Upsert(new Category { Name = name, LastSeen = DateTime.UtcNow });
        }
    }
}
