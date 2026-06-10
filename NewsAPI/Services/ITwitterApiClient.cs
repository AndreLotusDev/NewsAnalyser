using NewsAPI.Models;

namespace NewsAPI.Services;

public interface ITwitterApiClient
{
    Task<List<SocialPost>> GetRecentPostsAsync(string handle, int count = 10);
    Task<List<SocialPost>> GetPostsSinceAsync(string handle, string sinceId);
}
