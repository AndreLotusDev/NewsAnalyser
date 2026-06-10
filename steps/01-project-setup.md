# Step 01 — Project Setup

## Goal
Create the .NET Core backend service with three-layer architecture.

## Checklist

- [ ] Create a new .NET Core Web API project (e.g. `NewsAPI`)
- [ ] Add project to the existing solution (or create a new one)
- [ ] Install NuGet packages:
  - [ ] `Hangfire` + `Hangfire.AspNetCore` + `Hangfire.MemoryStorage`
  - [ ] `LiteDB`
  - [ ] `Refit` or `HttpClient` (for external API calls)
- [ ] Set up folder structure:
  ```
  /Controllers
  /Services
  /Repositories
  /Models
  /Jobs
  ```
- [ ] Register services in `Program.cs` (dependency injection)
- [ ] Configure Hangfire dashboard at `/hangfire` (user: admin / password: admin)
- [ ] Confirm the project builds and runs (`dotnet run`)

## Notes
- Three-layer: Controller → Service → Repository
- No auth for now (Hangfire dashboard is open with basic admin/admin)
