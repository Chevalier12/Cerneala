---
name: cerneala-fix-bug-group
description: Fix one Cerneala bug issue together with every open bug issue whose fix changes the same code files, in one worktree and one pull request, with the complete cerneala-fix-bug workflow for each issue and one group reproduction app. Takes over the code of the older pull requests of those issues and closes them after the group pull request exists. Use when the issue watcher gives a group of open pull requests that change the same code files (args `<N> --group <PRs>`, for example `479 --group 528,534`), or when the user asks to fix an issue together with its related issues. Do not use for one isolated bug the user wants fixed alone (use cerneala-fix-bug), for feature work, or for reviewing pull requests.
---

# Cerneala Fix Bug Group

Fix the target issue `#<N>` and its related open bug issues in one pull request. Each issue still gets the full `$cerneala-fix-bug` treatment (reproduction, RED regression test, root cause, fix, verification); what changes is that the issues share one worktree, one reproduction app, one commit-push, and one pull request. The point is throughput: one cold build, one context, one review, one validation for several bugs.

Read `.codex/skills/cerneala-fix-bug/SKILL.md` completely before step 1. Every rule there applies to every issue of the group, except where this file says otherwise. Follow the Codex instructions in `.codex/config.toml`, especially "Parent ownership and mandatory Luna exploration": Luna explorers own all file discovery and searches; you read known paths, edit with `apply_patch`, and run the experiments, builds, and tests. After a context compaction, re-read both files before continuing; recover the group, the RED tests and the taken-over pull requests from the repository, the pull request, and the summary.

Invoking this skill authorizes the `$git-session-commit-push` skill at the end, closing the older pull requests of the group's issues, and deleting their `repro-pr-<number>` releases, as step 7 describes (user decision, 2026-10-18). It never authorizes deleting a branch, touching the release `repro-gifs`, or closing any other pull request.

## Decisions This Skill Is Built On (user decisions, 2026-10-18)

- A group is one pull request with one `Fixes #<n>` line per issue. No new umbrella issue is created, and no issue is closed by hand: GitHub closes them all when the pull request is merged.
- Related means: the fixes change the same code files. Example: #480, #484 and #485 are related when the fix of each one changes `UI/Controls/ContentPresenter.cs`. Titles that start with the same component name are a hint, not the criterion.
- A group has no size limit.
- An issue that already has an open pull request joins the group. The group takes over that pull request's code and closes it, also when it was already validated, and deletes its release. Its branch stays.
- One reproduction app checks every issue of the group.

## 0. Worktree and Master

Do `$cerneala-fix-bug` steps 0 and 0b. The issue watcher starts this skill in its own worktree on branch `worktree-issue-<N>` or, for a given group, `worktree-group-<N>`; stay there.

## 1. Build the Group

**Given group.** When the args contain `--group <PRs>` (or the prompt says `Target: group #<N>`), the issue watcher already built the group: it is exactly the pull requests of the prompt and their issues. Skip items 1, 2, 5 and 6 (no candidate search, no diagnosis to predict files), and do this instead:

- Do item 3 (list the watcher windows). Leave out every pull request `<P>` of the prompt for which a `kind=review number=<P>` line exists, and record `not grouped: PR #<P> is under review`.
- Do item 7 with the prompt's lines: one row per issue that a remaining pull request fixes, with that pull request. The reason is the shared code files the prompt lists.
- When fewer than 2 pull requests remain, stop. Write the report (step 8) saying so, and change nothing.
- Go to step 2. Every issue of the group has a pull request, so step 2 takes over all of them, including the pull request of the target issue `#<N>`. Step 3 has nothing to do, and step 7 closes all of them, including the target's.

Example. The prompt says:

```text
Target: group #479: ...
- PR #528 (branch worktree-issue-479) fixes #479; code files: UI/Controls/ContentPresenter.cs
- PR #534 (branch worktree-issue-482) fixes #482, #483; code files: UI/Controls/ContentPresenter.cs, UI/Controls/ItemsControl.cs, UI/Controls/ItemsPresenter.cs
```

No `kind=review` line exists, so the group is #479, #482 and #483 with PR #528 and PR #534. Step 2 squashes both branches, and step 7 closes both pull requests. Had `kind=review number=534` existed, only PR #528 would remain: the report says so and nothing changes.

Otherwise (no given group):

1. Diagnose `#<N>` far enough to name the code files its fix will change: `$cerneala-fix-bug` steps 1 to 3 and the start of step 6. Write them down, for example `#480: UI/Controls/ContentPresenter.cs`.
2. List the candidates in one call each. A Luna explorer runs these two searches and returns the matches, and you decide the group from them:

   ```text
   gh issue list --repo Chevalier12/Cerneala --state open --label bug --limit 200 --json number,title,body,closedByPullRequestsReferences
   gh pr list --repo Chevalier12/Cerneala --state open --limit 100 --json number,headRefName,isDraft,files,closingIssuesReferences,comments
   ```

3. List the watcher windows that are working right now, in one call:

   ```text
   Get-CimInstance Win32_Process -Filter "Name='pwsh.exe'" | Where-Object CommandLine -match '# cerneala-watchers kind=' | ForEach-Object { $_.CommandLine -replace '.*# cerneala-watchers ', '' }
   ```

   It prints lines such as `kind=review number=467 head=fb0ca85...` (a review of PR #467) and `kind=issue-fix number=480` (this window).
4. Leave a candidate out when:
   - its open pull request is under review: a `kind=review number=<that PR>` line exists;
   - another fix window works on it: a `kind=issue-fix number=<that issue>` line other than `#<N>` exists;
   - it has more than one open pull request, or its pull request also closes an issue that is not a candidate. Record it as `not grouped: <reason>`.
5. For each remaining candidate, find the files its fix changes:
   - with an open pull request: the pull request's `files` that are not tests or docs; this is exact evidence;
   - without one: read the issue and the code it names (Luna explorers find the code with Roslyn MCP and `rg`; you read the decisive files), far enough to name the owning file, as in item 1. Do not write a reproduction yet.
6. A candidate joins the group when its files and the files of `#<N>` overlap. Repeat once with the files of the joined issues, so that a chain `#<N>` - `A` - `B` is found, then stop. When the evidence for a candidate is weak (you could not name its owning file), leave it out and record `not grouped: owning file unclear`.
7. Record the group: one line per issue with its files and, when it has one, its open pull request. Example:

   ```text
   #480 UI/Controls/ContentPresenter.cs (target)
   #484 UI/Controls/ContentPresenter.cs
   #495 UI/Controls/ContentPresenter.cs, UI/Controls/ContentControl.cs
   #479 UI/Controls/ContentPresenter.cs, PR #493 (worktree-issue-479), takes over its code
   not grouped: #486 owning file unclear
   ```

When the group is only `#<N>`, follow `$cerneala-fix-bug` alone from here on, and stop reading this file.

## 2. Take Over the Older Pull Requests

For each issue of the group with an open pull request `#<P>` on branch `<B>`:

1. `git fetch origin <B>`, then `git merge --squash origin/<B>`. A squash takes the pull request's net change, also when its branch merged master in between. Resolve conflicts by keeping both sides' intent, then commit with the message `Take over the fix of #<issue> from PR #<P>`.
2. Read the pull request body for its regression test, its RED evidence, and its `Other bugs found` section, and keep them for step 6.
3. Do not close `#<P>` yet: step 7 does that after the group pull request exists, so that no issue is ever left without a pull request.

The taken-over fix keeps its RED evidence from the older pull request: cite it, do not rerun the base build for it. Its regression test must be green in step 5.

## 3. Fix the Other Issues

For each issue of the group without a pull request, one at a time, do `$cerneala-fix-bug` steps 1, 3, 4, 6 and 7: its own reproduction, its own RED regression test, its root cause, and its fix. Where two issues share a root cause, one fix may cover both; each still has its own RED test that fails for its own reason.

`Other bugs found` (step 4 of `$cerneala-fix-bug`) is one list for the whole group. A defect that turns out to be another open issue is a candidate for step 1, not a new issue: add it to the group when its files overlap.

## 4. One Reproduction App for the Group

Do `$cerneala-fix-bug` step 5 once, for all issues of the group, with these differences:

- Generate the app with the other issues of the group:

  ```text
  pwsh -NoProfile -File Tools/scripts/New-ReproApp.ps1 -Issue <N> -AlsoIssues <a>,<b> -Destination <temp-dir>/repro-<N>/Issue<N>
  ```

  `ReproHarness.Issues` holds `<N>`, `<a>`, `<b>`. In a given group, `-AlsoIssues` lists every issue of all the group's pull requests other than `<N>` (for the example in step 1: `-Issue 479 -AlsoIssues 482,483`). The harness calls `ReproCheckInvariant(issue)` once per issue and shows one line per issue.
- Put one scenario per issue in the window, each in its own area with a heading such as `#484: ...`, and give each its own user steps under that heading. `ReproCheckInvariant(issue)` checks only that issue's invariant. The check-mode steps trigger every issue, one issue after the other.
- The verdicts of a group app: `OK: bugs #480, #484, #485 do not reproduce so far.` (exit 0), `BUG: bugs #480, #484, #485 reproduce.` (exit 1), and `PARTIAL: bugs #480, #485 reproduce; #484 does not reproduce so far.` (exit 4). Before the fix every issue must reproduce (exit 1); after the fix none may (exit 0). `PARTIAL` before the fix means an issue's scenario does not trigger its bug: fix that scenario, never the verdict. When the newest `origin/master` already fixes that issue, remove it from the group, and record it in the pull request.
- An issue whose fix changes only tests has no check in a group app with a before app, because the before app runs the same code for it; the pull request says so for that issue. When every issue of the group is test-only, publish with `-AfterOnly`.
- The taken-over issues get a scenario too: the older pull request's app source is not in the repository, so write their scenario again from their issue and their pull request's `How to check` section.

## 5. Verify

Do `$cerneala-fix-bug` step 8 for every issue of the group: every RED test is green, the group app's check mode exits 0, and the focused tests of every test project that covers the changed code pass, with `--filter`. Run each focused filter once for the whole group, not once per issue.

## 6. Commit, Push, Publish

Do `$cerneala-fix-bug` step 9 with these differences:

- Pass `$git-session-commit-push` one `Fixes #<n>` line per issue of the group, the target issue first, and a `Group` section: the table of step 1 item 7, with the reason each issue joined (`changes UI/Controls/ContentPresenter.cs`), each `not grouped` candidate with its reason, and each taken-over pull request: `Takes over PR #493 (#479), which this pull request closes.`
- Per issue: the root cause, the regression test, and its RED-to-GREEN evidence (cited from the older pull request for a taken-over fix).
- Publish the group app:

  ```text
  pwsh -NoProfile -File Tools/scripts/Publish-ReproApps.ps1 -App <temp-dir>/repro-<N>/Issue<N> -Issue <N> -AlsoIssues <a>,<b> -Pr <PR> -HowToCheckFile <temp-dir>/repro-<N>/how-to-check.md -Publish
  ```

  The `How to check` section lists the user steps per issue, with the expected red and green lines per issue.
- The `Other bugs found` section merges the older pull requests' sections with the group's own findings.

## 7. Close the Older Pull Requests

Only after the group pull request `#<G>` exists and its push succeeded, for each taken-over pull request `#<P>`:

1. Check again, right before closing: `gh pr view <P> --json state` prints `OPEN`, and the window list of step 1 item 3 has no `kind=review number=<P>` line. When it was merged meanwhile, do not close it: say so in the group pull request with `gh pr edit`. When a review window works on it now, do not close it: record `PR #<P> stays open: it is under review`, and remove its issue's `Fixes` line from the group pull request with `gh pr edit`.
2. Close it, also the pull request of the target issue `#<N>` in a given group: `gh pr close <P> --comment "Superseded by #<G>, which takes over this fix together with #<a>, #<b>. The branch stays."` Never pass `--delete-branch`.
3. Delete its release when it exists: `gh release delete repro-pr-<P> --repo Chevalier12/Cerneala --yes --cleanup-tag`. The GIFs in `repro-gifs` stay; never touch that release.

When the group pull request could not be created, close nothing: the older pull requests stay as they are.

## 8. Report

Do `$cerneala-fix-bug` step 10 for the group, plus: the group table with the reason per issue, every `not grouped` candidate with its reason, every closed pull request and deleted release, every older pull request that stayed open and why, and one verdict line per issue from the group app before and after the fix.
