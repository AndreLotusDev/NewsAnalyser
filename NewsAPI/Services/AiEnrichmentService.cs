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

    public async Task EnrichPostAsync(SocialPost post)
    {
        if (!string.IsNullOrEmpty(post.Summary))
            return;

        var prompt = $$"""
            Analyze this social media post from a commodities/energy trading context.
            Return a JSON object with exactly these fields:
            {
              "sentiment": "positive" | "negative" | "neutral",
              "summary": "<one sentence>",
              "tags": ["<tag1>", "<tag2>", ...],
              "categories": ["<cat1>", ...]
            }

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

        post.Sentiment = result.Sentiment ?? post.Sentiment;
        post.Summary = result.Summary ?? post.Summary;
        post.Tags = result.Tags ?? [];
        post.Categories = result.Categories ?? [];
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
        public List<string>? Tags { get; set; }
        public List<string>? Categories { get; set; }
    }
}
