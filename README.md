# NewsAnalyser

A social media monitoring service for trading desks that aggregates posts from curated X/Twitter accounts focused on commodities, energy, oils, agricultural markets, and biofuels. Posts are automatically enriched with AI-powered sentiment analysis, summaries, tags, and categories.

## What it does

- Fetches tweets from 43+ pre-configured industry accounts every 5 minutes
- Enriches each post with AI analysis (sentiment, summary, trader-focused observations, tags, categories, emojis)
- Serves a real-time web dashboard with filtering and auto-refresh
- Exposes a REST API for querying posts and managing tracked accounts
- Includes a Hangfire dashboard for job monitoring and manual triggers

## Tech Stack

- **Backend**: C# / .NET 10, ASP.NET Core MVC
- **Database**: LiteDB (embedded NoSQL, stored at `./data/news.db`)
- **Background Jobs**: Hangfire with in-memory storage
- **Frontend**: Razor views, Bootstrap 5 (Fila template), jQuery
- **External APIs**:
  - [twitterapi.io](https://twitterapi.io) — Twitter/X data fetching
  - [OpenRouter](https://openrouter.ai) — LLM enrichment via DeepSeek

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A **twitterapi.io** API key
- An **OpenRouter** API key

## Setup

**1. Clone and restore packages**

```bash
git clone git@github.com:AndreLotusDev/NewsAnalyser.git
cd NewsAnalyser
dotnet restore
```

**2. Configure API keys**

Edit `NewsAPI/appsettings.Development.json`:

```json
{
  "TwitterApi": {
    "ApiKey": "YOUR_TWITTERAPI_IO_KEY"
  },
  "OpenRouter": {
    "ApiKey": "YOUR_OPENROUTER_KEY",
    "Model": "deepseek/deepseek-v4-flash"
  },
  "Features": {
    "SandboxMode": true
  }
}
```

> Set `SandboxMode: true` during development to run AI enrichment on a single random account without consuming Twitter API quota.

**3. Run**

```bash
cd NewsAPI
dotnet run
```

## Accessing the App

| URL | Description |
|-----|-------------|
| `http://localhost:5138/news` | Main post feed dashboard |
| `http://localhost:5138/hangfire` | Job scheduler dashboard (admin / admin) |
| `http://localhost:5138/swagger` | REST API documentation |

On first run, 43 seed accounts are automatically inserted and an initial fetch job is queued.

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/social/posts` | Paginated posts with optional filters (accounts, tags, categories) |
| `GET` | `/api/social/health` | Health check with last job run timestamp |
| `POST` | `/api/social/accounts` | Add or remove a tracked account |
| `POST` | `/api/social/jobs/trigger` | Manually trigger the fetch job |
| `GET` | `/api/categories` | List all extracted categories |

## Architecture

The project follows a strict three-layer architecture:

```
Controller → Service → Repository
```

- **Controllers** handle HTTP routing and input validation
- **Services** contain business logic and orchestration
- **Repositories** own all database access via LiteDB

### Key Components

| Component | Responsibility |
|-----------|---------------|
| `FetchPostsJob` | Hangfire job — fetches new tweets and triggers AI enrichment |
| `TwitterApiClient` | Incremental tweet fetching (snowflake ID-based dedup) |
| `AiEnrichmentService` | OpenRouter calls — extracts sentiment, summary, tags, categories, emojis |
| `PostRepository` | Post querying, filtering, pagination, upserts |
| `SocialController` | REST API surface |
| `social-feed.js` | jQuery polling (30s), diff-based feed updates, filter state |

## AI Enrichment

Each post is analyzed by DeepSeek (via OpenRouter) and enriched with:

- **Sentiment**: `positive` / `negative` / `neutral`
- **Summary**: One concise English sentence
- **News Observation**: 2–4 sentences with trader-relevant market impact
- **Tags**: Specific topics (e.g., `RINs`, `biofuels`, `EPA`)
- **Categories**: Broad sectors (e.g., `Renewables`, `Compliance`)
- **Icons**: 2–4 emojis representing the post's topic and tone

Posts that fail enrichment are automatically retried on the next job run.

## Configuration Reference

| Key | Description | Default |
|-----|-------------|---------|
| `LiteDb:Path` | Database file path | `./data/news.db` |
| `TwitterApi:ApiKey` | twitterapi.io key | — |
| `OpenRouter:ApiKey` | OpenRouter key | — |
| `OpenRouter:Model` | LLM model ID | `deepseek/deepseek-r1` |
| `Features:SandboxMode` | Limit job to one account (dev) | `false` |

## Development Notes

- The database is auto-created on startup — no migration steps needed
- Hangfire uses in-memory storage; jobs do not persist across restarts
- The Hangfire dashboard is protected by HTTP Basic Auth (`admin` / `admin`) — change this before deploying
- `SandboxMode` is recommended during local development to avoid consuming API quota
