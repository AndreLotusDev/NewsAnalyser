# Step 08 — Frontend (Razor + jQuery)

## Goal
Add a "News" tab to the Market Pulse Razor app that displays posts reactively, styled using the **Fila Bootstrap 5 Admin Dashboard** template.

## UI Template
**Fila — Multipurpose Bootstrap 5 Admin Dashboard**
All markup must use Fila layout, component classes, and color conventions. See the `## Razor + jQuery Frontend` section in `agent.md` for the full component reference.

## Checklist

### Layout & Navigation
- [ ] Add "News" entry to the Fila sidebar (`<li class="nav-item">` with `<a class="nav-link">`)
- [ ] Active state (`active` class on `<li>`) set when on the News page
- [ ] Page uses the shared Fila layout (`_FilaLayout.cshtml`) — no inline navbar/sidebar
- [ ] Page header: `<div class="page-header">` with title "Social News" and breadcrumb

### Post Card Component (`_PostCard.cshtml`)
Each card is a Fila `.card.mb-3.post-card` with `data-post-id`. Must display:
- [ ] Account avatar — `<img class="avatar avatar-sm rounded-circle">`
- [ ] Account handle — `<a class="fw-semibold text-dark">@handle</a>` linking to the original post
- [ ] Post text — plain paragraph
- [ ] Sentiment badge — `bg-success-subtle/text-success`, `bg-danger-subtle/text-danger`, or `bg-secondary-subtle/text-secondary`
- [ ] Summary — `<p class="text-muted small">`
- [ ] Tags — `<span class="badge rounded-pill bg-light text-dark border">` chips, clickable to filter
- [ ] Timestamp — `<span class="text-muted small">` formatted as "2h ago"

### Reactive Polling
- [ ] On page load: fetch all recent posts from `GET /api/social/posts`, render into `#post-feed`
- [ ] Every 30 seconds: fetch only new posts (pass `after=<lastPostId>`), prepend to feed with a CSS slide-in
- [ ] If new posts arrive while user is scrolled down: show a Fila `.alert.alert-info` banner "N new posts — click to scroll up"; dismiss on click
- [ ] No full-page reloads — all updates are DOM patches

### Filtering (Fila form controls)
- [ ] Filter by account — `<select class="form-select form-select-sm">` (multi-select)
- [ ] Filter by tag — clicking a tag chip toggles it as an active filter (chip gets `bg-primary text-white`)
- [ ] Filter by category — `<select class="form-select form-select-sm">`
- [ ] Filters compose with AND across types, OR within the same type (mirrors API behaviour)
- [ ] "Clear filters" button — `<button class="btn btn-sm btn-outline-secondary">`
- [ ] All filter changes debounce 300 ms before firing a new API request

### Health Status (top-right of page header)
- [ ] Fila status dot: `<span class="status-indicator status-online/status-offline"></span>` next to "Feed"
- [ ] "Last updated: X min ago" text using `lastRunAt` from `GET /api/social/health`
- [ ] Polls health endpoint every 60 seconds; updates dot class and text in place

## File Locations

```
Views/
  Social/
    Index.cshtml          ← News page (extends _FilaLayout)
    _PostCard.cshtml      ← partial card component
  Shared/
    _FilaLayout.cshtml    ← Fila base layout (sidebar, topbar)
wwwroot/
  js/
    social-feed.js        ← all jQuery logic for this page
  css/
    social-feed.css       ← minimal overrides only (slide-in animation, etc.)
```

## JS State Object

```javascript
const state = {
    knownPostIds: new Set(),
    filters: { accounts: [], tags: [], categories: [] },
    lastPostId: null,
    lastUpdated: null
};
```

Keep all runtime state in this object, never in the DOM.

## Notes
- Use jQuery `$.getJSON` / `$.ajax` for all API calls
- No full-page reloads — all updates are DOM patches
- Do not write custom CSS that duplicates Fila utility classes
