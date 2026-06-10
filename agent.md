# Agent Guide — NewsAPI / Market Pulse Social Monitor

## Project Overview

Dedicated monitoring service for X/Twitter accounts relevant to the trading desk.
Backend: .NET Core 8 · Database: LiteDB · Queue: Hangfire · Frontend: Razor + jQuery · AI: OpenRouter (DeepSeek Pro) · Data: twitterapi.io

---

## Architecture

### Three-Layer Architecture (strictly enforced)

```
Controller  →  Service  →  Repository
```

- **Controllers** handle HTTP concerns only: routing, model binding, returning `IActionResult`. No business logic.
- **Services** own all business rules, orchestration, and AI calls. No direct DB access.
- **Repositories** speak to LiteDB exclusively. No business logic, no HTTP concerns.

Cross-layer rules:
- Never skip a layer. A controller must not call a repository.
- Dependencies flow inward only: outer layers depend on inner interfaces, never concrete types.
- Each layer lives in its own namespace/folder: `Controllers/`, `Services/`, `Repositories/`.

---

## .NET Core Standards

### Project Structure

```
NewsAPI/
  Controllers/
    SocialController.cs
  Services/
    IPostService.cs
    PostService.cs
    IAiService.cs
    AiService.cs
    ITwitterService.cs
    TwitterService.cs
  Repositories/
    IPostRepository.cs
    PostRepository.cs
    IAccountRepository.cs
    AccountRepository.cs
  Models/
    Post.cs
    Account.cs
    SentimentResult.cs
  Jobs/
    SocialFetchJob.cs
  DTOs/
    PostDto.cs
    AccountDto.cs
  appsettings.json
  Program.cs
```

### Dependency Injection

- Register everything through DI — no `new` for services or repositories inside other classes.
- Use `IOptions<T>` for configuration sections (TwitterOptions, OpenRouterOptions).
- Scoped lifetime for services and repositories. Singleton only for stateless infrastructure (e.g., `IHttpClientFactory` consumers).

### Async/Await

- Every I/O operation is `async`/`await`. No `.Result` or `.Wait()`.
- Method signatures: `Task<T>` return types throughout the stack.
- Cancel gracefully: accept and forward `CancellationToken` on all async methods.

### Error Handling

- Services return domain results, not raw exceptions. Use a `Result<T>` wrapper or throw typed domain exceptions only at system boundaries.
- Controllers catch domain exceptions and map them to appropriate HTTP status codes.
- Never swallow exceptions silently. Log and re-throw or surface.

---

## API Endpoints

```
GET  /api/social/posts      — paginated posts, filterable by account, tags, categories
GET  /api/social/health     — liveness check
POST /api/social/accounts   — add or remove tracked accounts
```

### Query Parameters — `GET /api/social/posts`

| Param      | Type     | Notes                                  |
|------------|----------|----------------------------------------|
| `page`     | int      | 1-based, default 1                     |
| `pageSize` | int      | default 20, max 100                    |
| `accounts` | string[] | filter by one or more account handles  |
| `tags`     | string[] | filter by AI-extracted tags            |
| `categories`| string[]| filter by AI-extracted categories      |

Filters compose with AND logic across different types, OR within the same type.

---

## Domain Model

```csharp
// Post — the core document stored in LiteDB
public class Post
{
    public string PostId       { get; init; }
    public string Account      { get; init; }
    public string Text         { get; init; }
    public DateTime Timestamp  { get; init; }
    public string Sentiment    { get; init; }   // "positive" | "negative" | "neutral"
    public string Summary      { get; init; }
    public List<string> Tags   { get; init; }
    public List<string> Categories { get; init; }
    public string Link         { get; init; }
}
```

All domain model properties use `init`-only setters. Mutate by creating new instances, not by modifying in place.

---

## Hangfire Job

- Runs every 5 minutes via a recurring job registered in `Program.cs`.
- First run per account: fetch last 10 posts to seed the database.
- Subsequent runs: fetch only posts newer than the latest stored `PostId` for that account.
- Job is also triggerable on demand via the API (manual endpoint or Hangfire dashboard).
- Hangfire dashboard at `/hangfire` — credentials: `admin` / `admin`.

```csharp
// Recurring job registration
RecurringJob.AddOrUpdate<SocialFetchJob>(
    "social-fetch",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Every(5).Minutes());
```

---

## AI Integration (OpenRouter — DeepSeek Pro)

- Every new post is enriched asynchronously after storage.
- Enrichment extracts: `sentiment`, `summary`, `tags[]`, `categories[]`.
- Model: `deepseek/deepseek-r1` via OpenRouter (`https://openrouter.ai/api/v1`).
- Prompt must request JSON output matching the domain model shape.
- Store raw AI response alongside parsed fields for debugging.
- If AI enrichment fails, store the post without enrichment and retry on next cycle.

---

## LiteDB Guidelines

- One `LiteDatabase` instance registered as Singleton.
- **Always open with `ConnectionType.Shared`** — never `ConnectionType.Direct` (the default lock mode). Use `new ConnectionString(path) { Connection = ConnectionType.Shared }` when constructing `LiteDatabase`.
- Collections: `posts`, `accounts`.
- Index `posts` by `Account` and `Timestamp` for efficient filtering.
- Index `posts` by `PostId` (unique) to prevent duplicates.
- Repositories must not expose `ILiteCollection<T>` outside the repository layer.

---

## SOLID

| Principle | Applied as |
|-----------|-----------|
| **S** — Single Responsibility | One class, one reason to change. `PostService` orchestrates; `AiService` enriches; `TwitterService` fetches. |
| **O** — Open/Closed | Extend behavior through new implementations of interfaces, not by modifying existing classes. |
| **L** — Liskov Substitution | Interfaces represent contracts. Implementations must be interchangeable without callers noticing. |
| **I** — Interface Segregation | `IPostService` only exposes what its consumers need. Split interfaces before they grow too wide. |
| **D** — Dependency Inversion | All inter-layer dependencies are on interfaces registered in DI, never on concrete types. |

---

## Object Calisthenics

These rules are enforced at code review:

1. **One level of indentation per method** — extract early if you hit nesting.
2. **No `else`** — use guard clauses and early returns.
3. **Wrap all primitives and strings** — raw `string accountHandle` becomes `AccountHandle` value object.
4. **First-class collections** — a class that holds a list does nothing else.
5. **One dot per line** — no chaining beyond one navigation step (except fluent builders by design).
6. **Do not abbreviate** — `acc` → `account`, `msg` → `message`, `svc` → `service`.
7. **Keep entities small** — max ~5 instance variables per class.
8. **No classes with more than two instance variables** — split if needed.
9. **No getters/setters on domain objects** — expose behavior, not data.

---

## Clean Code

- Methods do one thing. If you need "and" to describe it, split it.
- Method length: aim for under 20 lines. Hard limit: 40.
- Naming tells the story: `FetchNewPostsForAccountAsync`, not `Process` or `DoWork`.
- No magic strings or numbers — use constants or configuration.
- No commented-out code — delete it; git remembers.
- No default comments (do not describe what the code obviously does).

---

## Razor + jQuery Frontend

**UI Template: Fila — Multipurpose Bootstrap 5 Admin Dashboard**
All pages must conform to the Fila template structure and class conventions. Do not introduce custom CSS that duplicates what Fila already provides.

### Layout Structure (Fila)

```
<body class="layout-fixed">
  <div id="app">
    <nav class="navbar">…</nav>           <!-- top bar -->
    <aside class="sidebar">…</aside>       <!-- left nav -->
    <div class="content-wrapper">
      <div class="container-fluid">
        <div class="page-header">…</div>   <!-- breadcrumb / title -->
        <div class="row">…</div>           <!-- page content -->
      </div>
    </div>
  </div>
</body>
```

- The sidebar uses `<ul class="nav sidebar-nav">` with `<li class="nav-item">` / `<a class="nav-link">` entries.
- Active state: add `active` class to the `<li>` element.
- Page title lives in `.page-header` with an `<h4 class="page-title">` and an `<ol class="breadcrumb">`.

### Fila Component Conventions

| Element | Class pattern |
|---------|--------------|
| Card | `<div class="card">` → `<div class="card-header">` + `<div class="card-body">` |
| Sentiment badge — positive | `<span class="badge bg-success-subtle text-success">Positive</span>` |
| Sentiment badge — negative | `<span class="badge bg-danger-subtle text-danger">Negative</span>` |
| Sentiment badge — neutral | `<span class="badge bg-secondary-subtle text-secondary">Neutral</span>` |
| Tag chip | `<span class="badge rounded-pill bg-light text-dark border">tag</span>` |
| Status dot — online | `<span class="status-indicator status-online"></span>` |
| Status dot — offline | `<span class="status-indicator status-offline"></span>` |
| Avatar | `<img class="avatar avatar-sm rounded-circle" src="…">` |
| Alert banner | `<div class="alert alert-info alert-dismissible fade show">…</div>` |
| Filter dropdown | `<select class="form-select form-select-sm">` |
| Search input | `<input class="form-control form-control-sm" type="search">` |

### Razor

- Views are thin. No business logic in `.cshtml` files.
- Use `ViewModel` classes — never pass domain models directly to views.
- Partial views for the post card component (`_PostCard.cshtml`).
- `@section` for page-specific scripts; keep layout clean.
- Extend `_FilaLayout.cshtml` (or the project's shared Fila layout) — never inline the sidebar/navbar HTML per page.

### jQuery

- One `$(document).ready()` block per page. Avoid nested ready callbacks.
- Use event delegation for dynamically injected post cards: `$(document).on('event', '.selector', handler)`.
- No inline `onclick` attributes — bind all handlers through jQuery.
- Separate data-fetching from DOM manipulation: fetch → transform → render as three distinct functions.
- Poll the backend every 30 seconds for new posts; send the latest known post timestamp to receive only the diff.
- Use `data-*` attributes to carry server-side state into the DOM (e.g., `data-last-post-id`).
- Debounce filter inputs before firing a new request.

### Reactivity Pattern (jQuery)

```javascript
// Polling loop — fetch diff only
function startPolling(intervalMs) {
    setInterval(fetchNewPosts, intervalMs);
}

function fetchNewPosts() {
    const since = $('#post-feed').data('last-post-id');
    $.getJSON('/api/social/posts', { after: since }, function(data) {
        prependPosts(data.posts);
        updateLastPostId(data.latestPostId);
    });
}

function prependPosts(posts) {
    posts.forEach(post => $('#post-feed').prepend(renderPostCard(post)));
}
```

### Post Card Markup (Fila)

```html
<div class="card mb-3 post-card" data-post-id="{{postId}}">
  <div class="card-body">
    <div class="d-flex align-items-center gap-2 mb-2">
      <img class="avatar avatar-sm rounded-circle" src="{{avatarUrl}}" alt="{{account}}">
      <a class="fw-semibold text-dark" href="{{link}}" target="_blank">{{account}}</a>
      <span class="badge bg-success-subtle text-success ms-auto">{{sentiment}}</span>
    </div>
    <p class="mb-1">{{text}}</p>
    <p class="text-muted small mb-2">{{summary}}</p>
    <div class="d-flex flex-wrap gap-1 mb-2">
      <!-- tag chips -->
      <span class="badge rounded-pill bg-light text-dark border">{{tag}}</span>
    </div>
    <span class="text-muted small">{{timeAgo}}</span>
  </div>
</div>
```

---

## Initial Account List (43 accounts)

Seeded in the database on first run. Editable via `POST /api/social/accounts`.

```
@OilandGibbs @FirstSquawk @madorni @bsims1977 @EzazAhmadA_E @palmthetrader
@MarsOleochem @Saveraaintl @benjaminbodart @PalmOils @biofuelslaw @Goodvib75002247
@mgbongio @uscanola @SoybeanTrader88 @Ochefedoboss1 @lili_agri @gaurav_kochar
@Biokraftstoff @VisioCrop @DutchFarmerInUA @BiobasedDiesel @EctTan @JarrettRenshaw
@StephanieKellyM @anilbagani @PFLPetroleum @LingamSupraman2 @GrainsGorilla
@sizov_andre @EduardoVanin4 @DDFalpha @FEDIOL_EU @DeItaone @ScottIrwinUI
@ArlanFF101 @FarmPolicy @GRAINSOILSEEDS @tx_marcelo @agtradertalk @agturbobrazil
@kannbwx @realdonaldtrump @zerohedge
```

---

## External Integrations

| Service | Purpose | Docs |
|---------|---------|------|
| twitterapi.io | Fetch posts from X/Twitter accounts | https://docs.twitterapi.io/introduction |
| OpenRouter | AI enrichment via DeepSeek Pro | https://openrouter.ai |

API keys are stored in `appsettings.json` under `Twitter:ApiKey` and `OpenRouter:ApiKey`, never hardcoded.
