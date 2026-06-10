# Step 05 — Twitter API Integration (twitterapi.io)

## Goal
Connect to twitterapi.io to fetch posts for each tracked account.

## Docs
- https://twitterapi.io
- https://docs.twitterapi.io/introduction

## Checklist

- [x] Add `twitterapi.io` API key to `appsettings.json` (and `appsettings.Development.json` for local)
- [x] Create `TwitterApiClient` in `/Services` using `HttpClient`
- [x] Implement `GetRecentPostsAsync(string handle, int count = 10)`:
  - Calls the twitterapi.io endpoint for user timeline
  - Maps response fields to `SocialPost` model
  - Returns list of posts ordered by timestamp desc
- [x] Implement `GetPostsSinceAsync(string handle, string sinceId)`:
  - Fetches only posts newer than a given post ID (incremental fetch)
- [x] Handle API errors gracefully (rate limit, account not found, network timeout)
- [x] Register `TwitterApiClient` in DI
- [ ] Manual test: fetch posts for `@OilandGibbs` and print to console

## Notes
- Store the API key in an env variable for production; use `appsettings.json` locally
- The `link` field on `SocialPost` = `https://twitter.com/{handle}/status/{postId}`
