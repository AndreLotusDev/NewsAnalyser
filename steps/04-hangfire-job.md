# Step 04 — Hangfire Job

## Goal
Configure a recurring job that fetches new posts every 5 minutes, with an on-demand trigger via API.

## Checklist

- [ ] Create `FetchPostsJob` class in `/Jobs`
- [ ] Register recurring job in `Program.cs`:
  ```csharp
  RecurringJob.AddOrUpdate<FetchPostsJob>("fetch-posts", j => j.RunAsync(), "*/5 * * * *");
  ```
- [ ] Implement first-run logic:
  - If an account has **no posts** in DB → fetch last **10 posts**
  - If an account already has posts → fetch only posts newer than the latest stored `timestamp`
- [ ] Persist `lastRunAt` timestamp to DB after each successful run (used by `/api/social/health`)
- [ ] Add on-demand trigger endpoint:
  - `POST /api/social/jobs/trigger` → enqueues the job immediately via Hangfire
- [ ] Confirm job appears in Hangfire dashboard at `/hangfire`
- [ ] Confirm job runs on schedule and `lastRunAt` updates

## Notes
- Keep the job idempotent: re-inserting an existing `postId` should be a no-op
- Log job start/end and account-level fetch counts
