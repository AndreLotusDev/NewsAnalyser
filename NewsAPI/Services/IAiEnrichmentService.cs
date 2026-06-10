using NewsAPI.Models;

namespace NewsAPI.Services;

public interface IAiEnrichmentService
{
    Task EnrichPostAsync(SocialPost post);
}
