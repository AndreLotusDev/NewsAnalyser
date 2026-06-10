## Goal
Dedicated monitoring of X/Twitter accounts relevant to the trading desk, with health check alerting.

### General architecture
- Backend service in .NET Core
- Use hangfire job to run every 5min to get the news and allow to trigger the job on demand via the API
- Frontend integration in Market Pulse (Razor)
- Uses JQuery in Razor
- Makes the front end be reactive even though using JQuery
- Use three layer architecture (Controller, Service, Repository) for better separation of concerns and maintainability

## Hangfire job configuration
- Runs every 5 minutes to fetch latest posts from the tracked accounts
- User: admin | password: admin (for simplicity, no auth for now)

### Frontend configuration
- Uses Razor
- A dedicated "News" filter/tab in the feed
- Post card shows: account name, avatar, post text, timestamp, engagement metrics if available
- Hit periodically the backend endpoint to fetch latest posts (just add the difference between what we have)

### Backend configuration
- `GET /api/social/posts` — recent social media posts (paginated and filterable by account, tags, categories and can be mixed together)
- `GET /api/social/health` — to verify if the backend is alive
- `POST /api/social/accounts` — add/remove tracked accounts

### Database configuration
- Use lite DB as .NET option to store as Non SQL

### Integration configuration
- https://twitterapi.io
- https://docs.twitterapi.io/introduction
- https://openrouter.ai

### AI Configuration
- Use openrouter to summarize the posts and extract the sentiment (positive, negative, neutral) and store it in the database
- The AI model should be: [deep seek pro ](https://openrouter.ai)

### Feature configuration
- Iterate trough hangfire job every 5min to get the news and allow to trigger the job on demand via the API (the first run get the last 10 posts for each account to populate the database, then only the new ones)
- Uses the AI to summarize the posts and extract the sentiment (positive, negative, neutral) and store it in the database, uses openrouter for this
    - {
        "postId": "1234567890",
        "account": "@OilandGibbs",
        "text": "Oil prices surge as demand rebounds",
        "timestamp": "2024-06-01T12:00:00Z",
        "sentiment": "positive",
        "summary": "Oil prices are rising due to increased demand.",
        "tags": ["oil", "prices", "demand"],
        "categories": ["energy", "commodities"],
        "link": "https://twitter.com/OilandGibbs/status/1234567890"
    }

## Initial account list (43 accounts)
Stored in the database, editable at any time via the editor flow or via the `POST /api/social/accounts` endpoint.

@OilandGibbs, @FirstSquawk, @madorni, @bsims1977, @EzazAhmadA_E, @palmthetrader, @MarsOleochem, @Saveraaintl, @benjaminbodart, @PalmOils, @biofuelslaw, @Goodvib75002247, @mgbongio, @uscanola, @SoybeanTrader88, @Ochefedoboss1, @lili_agri, @gaurav_kochar, @Biokraftstoff, @VisioCrop, @DutchFarmerInUA, @BiobasedDiesel, @EctTan, @JarrettRenshaw, @StephanieKellyM, @anilbagani, @PFLPetroleum, @LingamSupraman2, @GrainsGorilla, @sizov_andre, @EduardoVanin4, @DDFalpha, @FEDIOL_EU, @DeItaone, @ScottIrwinUI, @ArlanFF101, @FarmPolicy, @GRAINSOILSEEDS, @tx_marcelo, @agtradertalk, @agturbobrazil, @kannbwx, @realdonaldtrump, @zerohedge

## Acceptance Criteria
- Posts from curated accounts appear in the UI
- Health check hits the backend endpoint and shows the status in the UI
- Last successful integration tentative timestamp visible in Market Pulse
- Accounts can be added/removed via API (if removed, remove the retroactive posts)
