# Step 02 — Database (LiteDB)

## Goal
Set up LiteDB as the local NoSQL store and define the data models.

## Checklist

- [x] Create `SocialPost` model with fields:
  - `postId` (string, unique)
  - `account` (string)
  - `text` (string)
  - `timestamp` (DateTime)
  - `sentiment` (string: positive / negative / neutral)
  - `summary` (string)
  - `tags` (List<string>)
  - `categories` (List<string>)
  - `link` (string)
- [x] Create `TrackedAccount` model with fields:
  - `handle` (string, unique)
  - `addedAt` (DateTime)
- [x] Create `LiteDbContext` (or repository base) that opens a connection to `news.db`
- [x] Register `LiteDatabase` as a singleton in `Program.cs`
- [x] Ensure indexes on `postId` and `account` for fast queries
- [x] Smoke test: insert and retrieve one dummy post

## Notes
- LiteDB file path should be configurable via `appsettings.json`
- Default path: `./data/news.db`
