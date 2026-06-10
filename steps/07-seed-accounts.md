# Step 07 — Seed Initial Accounts

## Goal
Populate the database with the 43 curated trading accounts on first startup.

## Checklist

- [ ] Create a `DatabaseSeeder` class (or seed logic in `Program.cs`)
- [ ] On app startup, check if `TrackedAccount` collection is empty
- [ ] If empty, insert all 43 accounts:

```
@OilandGibbs, @FirstSquawk, @madorni, @bsims1977, @EzazAhmadA_E,
@palmthetrader, @MarsOleochem, @Saveraaintl, @benjaminbodart, @PalmOils,
@biofuelslaw, @Goodvib75002247, @mgbongio, @uscanola, @SoybeanTrader88,
@Ochefedoboss1, @lili_agri, @gaurav_kochar, @Biokraftstoff, @VisioCrop,
@DutchFarmerInUA, @BiobasedDiesel, @EctTan, @JarrettRenshaw, @StephanieKellyM,
@anilbagani, @PFLPetroleum, @LingamSupraman2, @GrainsGorilla, @sizov_andre,
@EduardoVanin4, @DDFalpha, @FEDIOL_EU, @DeItaone, @ScottIrwinUI,
@ArlanFF101, @FarmPolicy, @GRAINSOILSEEDS, @tx_marcelo, @agtradertalk,
@agturbobrazil, @kannbwx, @realdonaldtrump, @zerohedge
```

- [ ] After seeding, trigger the Hangfire job immediately to fetch the first 10 posts per account
- [ ] Confirm 43 accounts are visible in the DB
- [ ] Confirm posts start appearing after the first job run

## Notes
- Seeding is one-time (guarded by empty-collection check)
- The `addedAt` field for seeded accounts = app startup timestamp
