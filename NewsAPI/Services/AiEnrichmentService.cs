using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewsAPI.Models;

namespace NewsAPI.Services;

public class AiEnrichmentService : IAiEnrichmentService
{
    private readonly HttpClient _http;
    private readonly ILogger<AiEnrichmentService> _logger;
    private readonly string _model;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiEnrichmentService(HttpClient http, IConfiguration config, ILogger<AiEnrichmentService> logger)
    {
        _http = http;
        _logger = logger;
        _model = config["OpenRouter:Model"] ?? "deepseek/deepseek-r1";

        var apiKey = config["OpenRouter:ApiKey"] ?? "";
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://cfp.energy");
        _http.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    // A post is fully enriched only when every AI-produced field is present.
    private static bool IsFullyEnriched(SocialPost post) =>
        !string.IsNullOrEmpty(post.Summary) &&
        !string.IsNullOrEmpty(post.Sentiment) &&
        !string.IsNullOrEmpty(post.NewsObservation) &&
        post.Icons.Count > 0;

    public async Task EnrichPostAsync(SocialPost post)
    {
        if (IsFullyEnriched(post))
            return;

        var prompt = $$"""
            You are a commodities/energy market analyst. Analyze the social media post below and respond with ONLY a valid JSON object — no markdown, no code fences, no explanation, no extra text.

            IMPORTANT: Every field listed below is REQUIRED. Never omit or leave a field null or empty.

            Step 1 – Language: If the post is not in English, translate it to English before analyzing.
            Step 2 – Return this exact JSON structure (all fields mandatory):
            {
              "sentiment": "positive" | "negative" | "neutral",
              "summary": "<one concise sentence summarising the post in English>",
              "news_observation": "<2-4 sentences for a trader: market impact, affected commodities/energy assets/sectors, suggested awareness or positioning>",
              "tags": ["<tag1>", "<tag2>", "<tag3>"],
              "categories": ["<category1>", "<category2>"],
              "icons": ["<emoji1>", "<emoji2>", "<emoji3>"]
            }

            Rules for "icons": choose 2–4 Unicode emoji that represent the topic and tone. Pick from these (or similar widely-supported emoji):
            📈 price up  📉 price down  🛢️ oil  ⚡ energy/power  🌿 renewables  🌡️ weather
            💰 finance  🏦 banking  🌍 geopolitics  🚢 shipping/LNG  🔥 gas/heat  ❄️ cold/LNG
            ⚠️ risk  📰 news  🏭 industry  🧪 chemicals  🌾 agriculture  💹 markets  🔋 storage

            Example of a valid response (do not copy — only illustrates format):
            {"sentiment":"positive","summary":"RIN prices hit all-time highs amid strong biofuel demand.","news_observation":"Rising RIN prices signal tightening biofuel supply, likely pushing up blending costs for refiners. Ethanol and biodiesel producers may benefit while refiners face margin pressure.","tags":["RINs","biofuels","EPA"],"categories":["Renewables","Compliance"],"icons":["📈","🌿","⚡"]}

            Post: "{{post.Text}}"
            """;

        var requestBody = new
        {
            model = _model,
            temperature = 0.2,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("chat/completions", content);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenRouter request failed for post {PostId}", post.PostId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenRouter returned {Status} for post {PostId}", response.StatusCode, post.PostId);
            return;
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        EnrichmentResult? result = null;

        try
        {
            var completion = JsonSerializer.Deserialize<OpenRouterCompletion>(responseJson, _jsonOptions);
            var messageContent = completion?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            // Strip <think>...</think> reasoning blocks (deepseek-r1 style)
            var trimmed = messageContent.Trim();
            var thinkEnd = trimmed.LastIndexOf("</think>", StringComparison.OrdinalIgnoreCase);
            if (thinkEnd >= 0)
                trimmed = trimmed[(thinkEnd + 8)..].Trim();

            // Strip markdown code fences — find closing ``` by index to handle trailing text
            if (trimmed.StartsWith("```"))
            {
                var openEnd = trimmed.IndexOf('\n');
                var closeStart = trimmed.IndexOf("```", openEnd >= 0 ? openEnd : 3);
                trimmed = closeStart >= 0
                    ? trimmed[(openEnd >= 0 ? openEnd : 3)..closeStart].Trim()
                    : trimmed[3..].Trim();
            }

            result = JsonSerializer.Deserialize<EnrichmentResult>(trimmed, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response for post {PostId}", post.PostId);
            return;
        }

        if (result is null)
            return;

        post.Sentiment = result.Sentiment?.ToLower() switch
        {
            "positive" or "negative" or "neutral" => result.Sentiment.ToLower(),
            _ => string.IsNullOrEmpty(post.Sentiment) ? "neutral" : post.Sentiment
        };
        post.Summary = !string.IsNullOrWhiteSpace(result.Summary) ? result.Summary : post.Summary;
        post.NewsObservation = !string.IsNullOrWhiteSpace(result.NewsObservation) ? result.NewsObservation : post.NewsObservation;
        post.Tags = result.Tags is { Count: > 0 } ? result.Tags : post.Tags;
        post.Categories = result.Categories is { Count: > 0 } ? result.Categories : post.Categories;
        post.Icons = result.Icons is { Count: > 0 } ? result.Icons : post.Icons;
    }

    private sealed class OpenRouterCompletion
    {
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }

    private sealed class EnrichmentResult
    {
        public string? Sentiment { get; set; }
        public string? Summary { get; set; }
        [JsonPropertyName("news_observation")]
        public string? NewsObservation { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? Categories { get; set; }
        public List<string>? Icons { get; set; }
    }
}
