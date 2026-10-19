---
name: cerneala-implement-issue
description: Implement one non-bug Cerneala change end to end without a checklist plan - an enhancement, a small feature, an API addition, a documentation or tooling change - from a GitHub issue or from the user's own words (the skill then creates the issue first). Decides open design choices with the user, writes tests for every acceptance criterion, implements the architecture-correct change, updates canonical docs, builds a local demo app when the change is visible in a window, opens a pull request, and merges it in the same session after the user says yes. Use when the user asks to implement an enhancement or another non-bug issue (for example "implementează #95" or "adaugă ServoModifiers.Command"). Do not use for bugs (use cerneala-fix-bug), for work the user wants planned in stages first (use cerneala-checklist-plan, then cerneala-implement-plan), or for reviewing pull requests.
---

# Cerneala Implement Issue

Implement one non-bug change from start to merge, without a plan file. The issue is the contract: its acceptance criteria say when the work is done. There is no size limit: a larger change still runs here, one issue at a time.

Follow `CLAUDE.md`, especially "Evidence and diagnosis" (feature work), "Authority and scope", "Canonical API documentation" and "Claude Code tooling and verification commands". After a context compaction, re-read this file (`.claude/skills/cerneala-implement-issue/SKILL.md`) and the issue before continuing; recover the decisions, the tests and the progress from the issue, the branch and the summary.

Invoking this skill authorizes, without another confirmation:

- creating the GitHub issue of step 1 when the request has none;
- running the `git-session-commit-push` skill in step 8;
- merging the pull request in step 9, but only after the user answers yes to the merge question.

It never authorizes force-pushing, deleting a branch, touching the release `repro-gifs`, or closing other issues and pull requests.

## Fixed Rules

- Every change has a GitHub issue. When the user gives only words, step 1 creates the issue first. Its label fits the kind of change: `enhancement` for new behavior or API, `documentation` for docs only, or another existing label that fits. Never `bug`: a bug goes to `cerneala-fix-bug`.
- No size limit, and no plan file. When the user wants stages, they ask for a plan themselves.
- A demo app only when the change is visible in a window. A change the user cannot see, for example a Servo API or a test helper, gets tests only.
- The pull request is merged in the same session, after the user says yes. It does not wait for the watcher's `Validated.` review.

## 0. Worktree and Master

1. Start in a temporary worktree, as `CLAUDE.md` describes under "Temporary worktree for skills", and work only there. Name it after the issue, for example `issue-95`; when the issue does not exist yet, name it after the change (`servo-command-key`).
2. Be up to date: `git fetch origin master`, `git merge origin/master`, and check that `git rev-list --count HEAD..origin/master` prints `0`.

The shell rules of `cerneala-fix-bug` (its paragraph "Shell commands in a worktree session", before step 1) apply here too: PowerShell with literal paths, no bash loops over variables, `robocopy` for folder copies.

## 1. The Issue

**With an issue number** (for example `#95`): read it once with `gh issue view <N> --repo Chevalier12/Cerneala --json number,title,body,labels,state,comments`.

- When it is labeled `bug`, stop and tell the user that `cerneala-fix-bug` handles it.
- When it is closed, or an open pull request already closes it (`gh pr list --repo Chevalier12/Cerneala --state open --search "<N> in:body"`), stop and tell the user which one.

**Without an issue:** write it, then create it:

- Title `<Subsystem>: <what changes>`, for example `Servo: add a Command modifier for macOS shortcuts`.
- Body sections, as in the enhancement issues #92 to #97: `## Current behavior` (what the code does now, with a short code excerpt), `## Proposal` (what should change), `## Acceptance criteria` (a bullet list of checkable results with literal values).
- Search for duplicates first with two term sets: `gh issue list --repo Chevalier12/Cerneala --state all --search "<terms>"`. When an open issue already asks for the same thing, use that issue instead.
- `gh issue create --repo Chevalier12/Cerneala --title "<title>" --label <label> --body-file <scratchpad>/issue.md`, and tell the user the number in one line.

## 2. Understand the Code and Decide

1. Find the owner of the change: Roslyn MCP (`workspace_symbol`, `find_references`, `find_callers`) and Grep, or Explorer subagents for several independent questions, as `CLAUDE.md` "Orchestrator" says. Read the complete C# files that will change, and the tests and canonical docs that cover them.
2. Check the issue against the code. The issue can be stale: when the newest master already does what the issue asks, stop and tell the user, with the evidence (file, line, test).
3. Test the assumptions the design depends on, as `CLAUDE.md` says for feature work. Example for #96: before changing the wait loop, run the existing `WaitForAsync` with two matching elements and confirm that it throws `ServoTargetAmbiguousException` today.
4. **Open choices go to the user before any production edit.** An open choice is anything that changes the result and that the issue or the existing architecture does not settle: which of several proposals, a public API name or signature, a default value, behavior that changes for existing callers, a new dependency. Ask all of them in one `AskUserQuestion` call, with a concrete example of each option (code and the literal result). Put the recommended option first. Example for #95, which says "Pick one, or combine them":
   - `Clear through the automation layer (Recommended)`: `ReplaceTextAsync` sets the text through the target's value pattern; on a `TextBox` holding `abc`, `ReplaceTextAsync(box, "xyz")` gives `Value == "xyz"` on Windows, Linux and macOS, with no key press.
   - `Platform select-all shortcut`: adds `ServoModifiers.Command`; on macOS it presses Cmd+A, then types `xyz`; a control that rebinds Cmd+A still appends.

   Mechanical choices that cannot change the result (a private helper's name, the order of tests in a file) are yours; do not ask about them.
5. Write the decisions and the files you expect to change in your scratchpad (`decisions.md`). They go into the pull request in step 8.

## 3. Tests First

Write one or more tests per acceptance criterion, in the test project that covers the owner, next to the existing tests of that type. Use real input through Servo for anything about interaction (`ClickAsync`, `PressKeyAsync`, `SendTextAsync`), never direct property assignments, as `CLAUDE.md` says.

Run them before the production change:

- A test for behavior that exists today but is wrong for the new contract must fail, for the reason the issue states. Example for #96: the transient-ambiguity test fails with `ServoTargetAmbiguousException`. Record the failure line. When it passes already, the issue's premise is wrong for that criterion: say so to the user before going on.
- A test for an API that does not exist yet cannot compile. That is expected; it is not recorded as a failing test.
- A test that keeps existing behavior (for example "`Exists`/`Missing` semantics are unchanged" in #96) must pass before and after.

## 4. Implement

- Make the architecture-correct change in the owner found in step 2, as `CLAUDE.md` "Authority and scope" says: no shims, no duplicated paths, no unrelated cleanup.
- Large or independent pieces go to an Implementer subagent with a bounded brief, as `CLAUDE.md` "Orchestrator" says; then read its `git diff` and rerun its focused tests yourself.
- A public or protected API that changes or appears updates its canonical page under `docs-site/documentation/classes/` in the same change, with the `writing-api-documentation` skill, and `docs-site/documentation/manifest.json` for an added or renamed page.
- A defect found on the way that this change does not introduce: raise it with `github-issue-raise` and do not fix it here. A defect that this change introduces is fixed here.

## 5. Verify

- Every test of step 3 passes.
- The focused tests of every test project that covers the changed or affected code pass, each with a `--filter`, as `CLAUDE.md` "Focused tests" says. Never the complete suite.
- The prerequisites of `CLAUDE.md` apply: a Debug build of `Cerneala.slnx` before PreviewHost tests, the restore before Visual Studio lane tests, `CERNEALA_SDL_NATIVE_TESTS=1` for the native subset the change touches.
- Performance-sensitive code (layout, rendering, invalidation, input per frame) gets the measurement `CLAUDE.md` "Verification and realtime behavior" asks for, with warmed-up numbers before and after. Do not claim a speed-up you did not measure.
- `git diff --check`, then read the whole `git diff` once for leftover debug code, accidental scope, and generated churn.

## 6. Demo App (only when the change is visible in a window)

The change is visible when a user can see or do it in a Cerneala window: a new control or property, a new visual state, a new input behavior. Examples: a new `TabControl` placement is visible; #95 (`ReplaceTextAsync`) and #93 (semantics tree cache) are not. When it is not visible, skip this step and say `Demo app: none, the change is not visible in a window.` in the pull request.

When it is visible:

1. Generate the app outside the repository, in change mode:

   ```text
   pwsh -NoProfile -File Tools/scripts/New-ReproApp.ps1 -Issue <N> -Change -Destination <scratchpad>/demo-<N>/Issue<N>
   ```

   Its verdict lines are `OK: #<N> works so far.` (green) and `FAIL: #<N> does not work.` (red). Fill in the scenario as `cerneala-fix-bug` step 5 describes for a reproduction app ("The scenario and the user's actions", "The live verdict", "Check mode"), with one difference: the invariant checks the new behavior, for example "the selected tab's header is on the left side" for a new placement. The user's steps in the window say what to do and what they should see.
2. Build it with `dotnet build <csproj> -c Release`, then run check mode once: `--check <scratchpad>/demo-<N>/report.txt`. It must exit with 0. Exit 1 with the change in place means the change or the app is wrong: fix it, never the verdict.
3. Copy the built app to a folder outside every checkout, so it outlives the worktree: `robocopy <scratchpad>/demo-<N>/Issue<N>/bin/Release/net8.0-windows "$env:LOCALAPPDATA\CernealaTools\demo-apps\issue-<N>" /E` (exit codes below 8 mean success).
4. The app is not published on GitHub and not committed. Step 9 gives the user its path.

## 7. Sync with Master

Right before the commit, do step 0 item 2 again. When it brings commits, resolve conflicts by keeping both sides' intent, rebuild, and rerun the tests of step 5 and the demo app's check mode.

## 8. Commit, Push, Pull Request

Invoke the `git-session-commit-push` skill. Pass it:

- `Closes #<N>`;
- the decisions of step 2 with the option the user chose, and the reason;
- per acceptance criterion: the test that covers it, and its failing line before the change when it had one;
- the docs pages changed;
- the verification lines with real counts, and that the complete suite was not run;
- the demo app line (its path, the check-mode verdict line), or `Demo app: none, ...`;
- the other bugs raised (step 4), or `None.`;
- the open pull requests that touch the same files: `gh pr list --repo Chevalier12/Cerneala --state open --json number,files --jq '.[] | "\(.number): \(.files | map(.path) | join(", "))"'`, one line per overlap, or `None.`

When a required check of step 5 failed or could not run, do not invoke it: report the blocker and ask the user with `AskUserQuestion` whether to commit anyway.

## 9. Merge in This Session

1. Tell the user, in their language and in a few lines: what changed, with one concrete example (code and the literal result), the tests and their counts, and, when there is a demo app, how to try it: `Rulează %LOCALAPPDATA%\CernealaTools\demo-apps\issue-<N>\Issue<N>.exe, apoi <the steps in the window>; sus trebuie să scrie verde OK: #<N> works so far.`
2. Ask with `AskUserQuestion` whether to merge pull request #<PR> now, with the options `Merge now (Recommended)` and `Not now`. When the user asks for changes instead, make them (steps 4 to 8; the push updates the same pull request), then ask again.
3. On yes:
   - Check that the head did not move: `gh pr view <PR> --repo Chevalier12/Cerneala --json headRefOid,mergeable`. `mergeable` must be `MERGEABLE`. On `CONFLICTING`, do step 7 in the worktree, push normally, and ask again. On `UNKNOWN`, GitHub is still computing it: run the same command once more.
   - `gh pr merge <PR> --repo Chevalier12/Cerneala --merge --match-head-commit <headRefOid>`. Never pass `--delete-branch`.
   - Check the result: `gh pr view <PR> --repo Chevalier12/Cerneala --json state,mergeCommit` prints `MERGED`, and `gh issue view <N> --repo Chevalier12/Cerneala --json state` prints `CLOSED`. When the issue stayed open, close it with `gh issue close <N> --repo Chevalier12/Cerneala --comment "Done in #<PR>."`.
4. On `Not now`, leave the pull request open and say so in the report.
5. Finish the worktree as `CLAUDE.md` describes.

## 10. Report

In the user's language, short: the issue (created or given), the decisions, the pull request URL and whether it was merged (with the merge commit), the tests with real counts, that the complete suite was not run, the demo app path or `none`, the docs pages changed, the bugs raised, and anything that still needs the user, for example trying the demo app when they said `Not now`.

## Invocation Examples

```text
/cerneala-implement-issue #96
/cerneala-implement-issue Adaugă ServoModifiers.Command, ca ReplaceTextAsync să meargă și pe macOS.
```
