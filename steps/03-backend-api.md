# Step 03 — Backend API Endpoints

## Goal
Implement the three API endpoints with full Controller → Service → Repository wiring.

## Checklist

### GET /api/social/posts
- [ ] Accepts query params: `account`, `tags`, `categories`, `page`, `pageSize`
- [ ] Filters can be combined (mixed filter support)
- [ ] Returns paginated list of `SocialPost`
- [ ] Returns most recent posts first (ordered by `timestamp` desc)

### GET /api/social/health
- [ ] Returns `{ status: "ok", timestamp: "...", lastRunAt: "..." }`
- [ ] `lastRunAt` = timestamp of the last successful Hangfire job run

### POST /api/social/accounts
- [ ] Accepts `{ "handle": "@SomeAccount", "action": "add" | "remove" }`
- [ ] `add`: inserts into `TrackedAccount` collection if not already present
- [ ] `remove`: deletes the account AND all posts from that account
- [ ] Returns updated account list

### General
- [ ] Add CORS policy to allow requests from the Market Pulse Razor app
- [ ] Return proper HTTP status codes (200, 400, 404)
- [ ] Test all three endpoints with Swagger or Postman

## Notes
- `SocialController` in `/Controllers`
- Business logic in `SocialService` in `/Services`
- Data access in `PostRepository` + `AccountRepository` in `/Repositories`
