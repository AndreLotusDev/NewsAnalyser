# Step 10 — Agent: Fill Checklist & Step Files

## Goal
Use a Claude Code agent to inspect the codebase and mark every completed item across `CHECKLIST.md` and all step files (`01-*.md` … `09-*.md`).

## How It Works

The agent reads the current source tree, runs a build/smoke-test pass, and for each checklist item it can confirm is done it flips `[ ]` → `[X]` in the relevant step file.  
It leaves items unchecked when evidence is missing or ambiguous and appends a short `## Agent Notes` block at the end of each file explaining any skipped items.

## Prompt (paste into Claude Code)

```
You are a build-verification agent for the NewsAPI / Market Pulse Social Monitor project.

Your task:
1. Read every step file in /steps (01-project-setup.md through 09-acceptance-criteria.md).
2. For each checklist item, inspect the codebase (source files, appsettings, Program.cs, Controllers/, Services/, Repositories/, Models/, Jobs/, Views/) to determine whether the item is already done.
3. Flip [ ] → [X] for every item you can confirm is complete.
4. Leave items unchecked if you cannot find clear evidence.
5. At the end of each step file add an ## Agent Notes section listing any items you left unchecked and why.
6. Finally update steps/CHECKLIST.md: flip the top-level step entry to [X] if and only if every item in that step file is now checked.

Rules:
- Do NOT modify any source (.cs, .cshtml, .json) files — only the markdown step files.
- Do NOT mark an item done if the relevant code does not exist or the feature is incomplete.
- Commit the updated markdown files with the message: "chore: agent checklist pass — mark completed steps"
```

## Checklist

- [ ] Paste the prompt above into a Claude Code session pointed at this repo
- [ ] Agent reads all nine step files
- [ ] Agent inspects source files for evidence of each checklist item
- [ ] Agent updates `[ ]` → `[X]` for confirmed items in each step file
- [ ] Agent appends `## Agent Notes` to each file with skipped-item rationale
- [ ] Agent updates top-level entries in `CHECKLIST.md`
- [ ] Changes committed under `chore: agent checklist pass — mark completed steps`
- [ ] Review agent output; manually fix any wrong calls before merging

## Notes
- Run this step again any time you add new features or fix gaps identified in Step 09.
- The agent is non-destructive: it only edits markdown, never source code.
