# Step 06 — AI Integration (OpenRouter)

## Goal
Use OpenRouter + DeepSeek Pro to enrich each post with a summary, sentiment, tags, and categories.

## Docs
- https://openrouter.ai

## Model
- `deepseek/deepseek-r1` (DeepSeek Pro via OpenRouter)
- Set `temperature: 0.2` if the model/API supports it (deterministic, consistent structured output)

## Checklist

- [ ] Add `openrouter.ai` API key to `appsettings.json`
- [ ] Create `AiEnrichmentService` in `/Services`
- [ ] Implement `EnrichPostAsync(SocialPost post)`:
  - Sends the post text to OpenRouter
  - Prompt should ask for:
    - `sentiment`: one of `positive`, `negative`, `neutral`
    - `summary`: one-sentence summary of the post
    - `tags`: list of relevant keywords (e.g. ["oil", "prices"])
    - `categories`: list of high-level categories (e.g. ["energy", "commodities"])
  - Parse structured JSON response back into the `SocialPost` fields
- [ ] Call `EnrichPostAsync` for each new post fetched by the Hangfire job
- [ ] Handle OpenRouter errors gracefully (store post without enrichment, retry on next run)
- [ ] Register `AiEnrichmentService` in DI
- [ ] Manual test: enrich one post and verify the DB record has all fields populated

## Prompt Template
```
Analyze this social media post from a commodities/energy trading context.
Return a JSON object with exactly these fields:
{
  "sentiment": "positive" | "negative" | "neutral",
  "summary": "<one sentence>",
  "tags": ["<tag1>", "<tag2>", ...],
  "categories": ["<cat1>", ...]
}

Post: "{post.Text}"
```

## Notes
- Only enrich posts where `summary` is null/empty (avoid re-processing)
- Keep enrichment async so it doesn't block the fetch loop
