---
name: github-issue-raise
description: Raise a GitHub issue in Chevalier12/Cerneala for a Cerneala bug, or in the SDL fork Chevalier12/Graphix (native SDL) or Chevalier12/Graphix-CS (its C# bindings) when the defect is proven to be in that fork, after proving it with a faithful reproduction and writing it in the repository's contribution format (the sections of issue 3: What happens, Minimal reproduction, Expected behavior, Acceptance criteria). Invoke it automatically whenever you discover a Cerneala defect while working — during fix-bug, breaker, implement-plan, performance-gate, repo-cleanup, or any other task — and whenever the user reports a bug that should be tracked. Do not use for feature requests, questions (except the question issues that cerneala-pr-review-merge raises when the user's validation comment asks for one), or suspicions that have not been reproduced.
---

# GitHub Issue Raise

Turn a discovered Cerneala defect into a GitHub issue that meets the contribution bar: a reliable reproduction, a grounded expected behavior, literal values, and the format of [issue #3](https://github.com/Chevalier12/Cerneala/issues/3). An issue that does not meet the bar is not raised.

Follow `CLAUDE.md`, especially "The user" and "Claude Code tooling and verification commands".

## When it runs

Invoke this skill on your own, without being asked, whenever a Cerneala defect is observed:

- **Outside the current task's scope:** for example, `implement-plan` finds a crash in a neighboring subsystem, or `repo-cleanup` finds a real behavior bug. The current task does not fix it; this skill records it.
- **As a confirmed `cerneala-breaker` finding:** one issue per violated contract, not one per symptom.
- **When the bug being fixed has no issue yet:** during `cerneala-fix-bug` or `cerneala-performance-gate`, raise it after the reproduction is RED, so the fix commit can say `Fixes #N`.
- **When the user reports a bug** and wants it tracked.

Raising the issue never replaces or interrupts the current task for long. Prove, draft, queue or post, then return to the task.

## Target repository

The issue goes to the repository that owns the defective code. `<repo>` in the commands below is that repository:

| Repository | Owns | Cerneala gets it from | Example defect |
| --- | --- | --- | --- |
| `Chevalier12/Cerneala` | everything in this repository | — | a `ListBox` row keeps a stale height |
| `Chevalier12/Graphix` | native SDL, our fork of `libsdl-org/SDL` | `SDL3.dll` in the NuGet package `Graphix.Native` | Graphix #162: after `SDL_SetRelativeMouseMode`, `xrel`/`yrel` in `SDL_MOUSEMOTION` are large absolute values |
| `Chevalier12/Graphix-CS` | the C# bindings, our fork of `SDL3-CS` | `SDL3-CS.dll` in the NuGet package `Graphix-CS` | Graphix-CS #3: four SDL_mixer bindings pass `in IntPtr spec` where the SDL header takes `const SDL_AudioSpec *` |

The versions Cerneala uses are the `PackageReference` lines in `Cerneala.Platforms.Sdl3/Cerneala.Platforms.Sdl3.csproj`, for example `Graphix.Native` `3.4.16-graphix.6` and `Graphix-CS` `3.4.16.1`.

A defect belongs to a fork only when it reproduces without Cerneala code:

- `Graphix`: a small console program that calls SDL directly through `Graphix-CS` (or its own P/Invoke declarations) shows the wrong event, value, or return code, with no Cerneala project referenced.
- `Graphix-CS`: the binding disagrees with the native SDL header or behavior, shown by a direct call or by reflection over `SDL3-CS.dll`, with no Cerneala project referenced.

When the failure shows only through Cerneala code, it is a Cerneala issue, even when the cause is suspected in SDL; name the suspicion as a hypothesis in the Cerneala issue. A fork issue uses the same proof rules and the same format as a Cerneala issue (user decision, 2026-10-17). Its `## Diagnostics` section also gives the package version, the runtime `SDL_GetVersion()` value, and the SHA-256 of the `SDL3.dll` or `SDL3-CS.dll` that was tested, because the fork issue must name the exact binary.

## 1. Prove it (MANDATORY)

An issue is raised only for a defect that is proven. Before drafting:

- **Reproduce it** at the cheapest faithful level: a focused test, a CSI script, a Servo-driven runtime scenario, or a native harness, as in `cerneala-fix-bug`. Run it on the current code and record the exact code or steps, inputs, and the observed values.
- **Confirm it fails for the stated reason.** A broken fixture, a missing build output, a sandbox or environment limit, or a wrong expectation is not a Cerneala bug. Example: PreviewHost failing because Debug outputs are missing is environmental, not a bug.
- **Ground the expected behavior** in a contract: canonical docs under `docs-site/documentation/classes/`, existing tests, a guide, a plan, or an explicit user decision. If the intended behavior is genuinely ambiguous, do not raise the issue: record it as "contract unclear". Never turn it into a question issue on your own; only `cerneala-pr-review-merge` raises a question issue, when the user asks for one (see "Question issues"). Do not invent an expectation.
- **Check for duplicates.** Run `gh issue list --repo <repo> --state all --search "<key terms>"` with two or three different term sets. For a fork issue, also search `Chevalier12/Cerneala`, because the defect may already be tracked there from the Cerneala side. If an open issue already covers the contract, add the new evidence as a comment draft instead of a new issue. If a closed issue covers it, say whether this is a regression.
- **Keep the proof.** If a temporary reproduction will be deleted, put the minimal reproduction code in the issue body itself.

If any of these cannot be met, do not raise the issue. Record why in your report: for example, "not reproduced", "environmental", or "contract unclear".

## 2. Draft it in the contribution format

Title: `<Subsystem>: <concise symptom>`. Examples from the repository:

- `Motion: Transform interpolation throws mid-frame when scale is near 0`
- `Theme: mutating the installed Theme with Theme.Set is silently ignored`

Body sections, in this order (the format of issue #3):

1. `## What happens`
   - Start from what a user does and sees, with a short concrete code or markup example.
   - Then explain the cause as far as it is proven, naming the owning types and members. An unproven cause is labeled as a hypothesis.
2. `## Minimal reproduction`
   - Give runnable code or numbered literal steps, with the expected and actual values. Example: `// after == first (10, 20, 30), expected second`.
3. `## Expected behavior`
   - State what should happen, grounded in the contract. If more than one resolution is acceptable, list them.
4. `## Diagnostics`, only when it adds something: exception text, a trace, measured numbers, or the environment.
5. `## Acceptance criteria`
   - Give observable, checkable bullets. Example: "setting X then Y does not throw", "the regression test covers both orders".

Rules:

- **Language:** write in English, matching the repository.
- **Precision:** use literal values, not adjectives. "8,300 px over 96 s" beats "travels way too far".
- **No noise:** no footer like "found by an agent", and no commit permalinks as the main references. Name files and members instead.
- **Label:** `bug`. A question issue gets `question` instead.

### Question issues

A question issue asks the user to decide a contract, for example #353: `Question: should a VirtualizingStackPanel virtualize when the ScrollViewer is inside the items control's template (ScrollViewer > ItemsPresenter), as in WPF?`. Raise one only from `cerneala-pr-review-merge`, and only when the extra instructions of the user's validation comment ask for it (user decision, 2026-10-17). Example: `Validated. Write an issue about this not raised issue not as a bug, but as a question.` Every other caller, including `cerneala-fix-bug`, never raises a question issue: it records the unclear contract as "contract unclear" and does not raise it.

- **Title:** `Question: <the decision, as a question>?`
- **Body:** the same sections. `## Expected behavior` lists each possible answer with what it would change, and the comparison the user asked for, for example WPF's behavior.
- **Label:** `question`, never `bug`.

Save the draft to `.artifacts/issue-drafts/<yyyy-MM-dd>-<short-slug>.md`. That folder is git-ignored. The file's first line is `Title: ...`, followed by a blank line and the body.

In a linked worktree, the draft goes to the main checkout's folder, because the worktree may be removed. Claude Code refuses `Write` and `Edit` on main-checkout paths in a worktree session, so:

1. Write the draft with `Write` into the session's scratchpad directory, for example `<scratchpad>\2026-10-15-track-render-transform.md`.
2. Copy it at once with PowerShell run from the worktree:

   ```powershell
   $main = Split-Path (git rev-parse --path-format=absolute --git-common-dir)
   New-Item -ItemType Directory -Force "$main\.artifacts\issue-drafts" | Out-Null
   Copy-Item -LiteralPath "<scratchpad>\2026-10-15-track-render-transform.md" -Destination "$main\.artifacts\issue-drafts\"
   ```

   The draft is now at `C:\Users\lauri\Desktop\Cerneala\.artifacts\issue-drafts\2026-10-15-track-render-transform.md` and survives the worktree's removal. Later changes to that folder, such as the move to `posted/` in section 3, also use the shell.

## 3. Get approval, then post

Creating a GitHub issue publishes public content, so an issue needs the user's approval before it is posted, with two exceptions (user decision, 2026-10-15). In both, post the issue directly, without asking:

- **Bugs found by `cerneala-fix-bug`:** another defect that `cerneala-fix-bug` finds while it fixes a bug, in any repository of "Target repository", and the `Graphix` or `Graphix-CS` defect that is the root cause of the bug being fixed. The fix's pull request lists it in its `Other bugs found` section.
- **Bugs found by `cerneala-pr-review-merge`:** a defect that the review of a validated pull request proves. The skill's merge comment lists it under `Bugs found`.

In every other case, ask in a way that never stalls the work:

- **If the user is present:** show a two-line summary (title, plus one sentence of what happens), mention the draft path, and ask with `AskUserQuestion` (post / edit / drop). Post only after a "post".
- **If the user is away:** for example, during a long unattended skill run. Keep the draft in `.artifacts/issue-drafts/` (of the main checkout, as section 2 says), continue the current task, and list every pending draft at the end of the run so the user can approve them together.
- **Approval is per issue.** Approving one draft does not approve the next.

To post an approved draft:

```
gh issue create --repo <repo> --title "<title>" --body-file <body-file> --label bug
```

For a question issue (see "Question issues"), use `--label question` instead of `--label bug`.

Use a body file that contains only the body, without the `Title:` line. Then read the issue back with `gh issue view <n> --json title,body,labels`, and confirm that the sections and the label are correct. Move the draft file to `.artifacts/issue-drafts/posted/`. In a worktree, move it in the main checkout's folder with `Move-Item -LiteralPath "$main\.artifacts\issue-drafts\<draft>.md" -Destination "$main\.artifacts\issue-drafts\posted\"` (create `posted\` first with `New-Item -ItemType Directory -Force` when it is missing).

## 4. Report

Tell the user, in their language:

- **Posted issues:** the URL and title of each, which shows its repository.
- **Pending drafts:** the path and title of each, waiting for approval.
- **Defects not raised:** each one with the reason (not reproduced, environmental, duplicate of #N, contract unclear).

Do not fix the defect inside this skill. Fixing is `cerneala-fix-bug`'s job, and it can start from the issue.
