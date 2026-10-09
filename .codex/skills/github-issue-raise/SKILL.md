---
name: github-issue-raise
description: Raise a GitHub issue in Chevalier12/Cerneala for a Cerneala bug, after proving it with a faithful reproduction and writing it in the repository's contribution format (the sections of issue 3: What happens, Minimal reproduction, Expected behavior, Acceptance criteria). Invoke it automatically whenever you discover a Cerneala defect while working — during cerneala-fix-bug, cerneala-breaker, cerneala-implement-plan, cerneala-performance-gate, repo-cleanup, or any other task — and whenever the user reports a bug that should be tracked. Do not use for feature requests, questions, or suspicions that have not been reproduced.
---

# GitHub Issue Raise

Turn a discovered Cerneala defect into a GitHub issue that meets the contribution bar: a reliable reproduction, a grounded expected behavior, literal values, and the format of [issue #3](https://github.com/Chevalier12/Cerneala/issues/3). An issue that does not meet the bar is not raised.

Follow the repository instructions in `.codex/config.toml`:

- Luna explorers own discovery and searches.
- The parent reads known paths and runs the experiments.
- Edits are made with `apply_patch`.

## When it runs

Invoke this skill on your own, without being asked, whenever a Cerneala defect is observed:

- **Outside the current task's scope:** for example, `$cerneala-implement-plan` finds a crash in a neighboring subsystem, or `$repo-cleanup` finds a real behavior bug. The current task does not fix it; this skill records it.
- **As a confirmed `$cerneala-breaker` finding:** one issue per violated contract, not one per symptom.
- **When the bug being fixed has no issue yet:** during `$cerneala-fix-bug` or `$cerneala-performance-gate`, raise it after the reproduction is RED, so the fix commit can say `Fixes #N`.
- **When the user reports a bug** and wants it tracked.

Raising the issue never replaces or interrupts the current task for long. Prove, draft, queue or post, then return to the task.

## 1. Prove it (MANDATORY)

An issue is raised only for a defect that is proven. Before drafting:

- **Reproduce it** at the cheapest faithful level: a focused test, a CSI script, a Servo-driven runtime scenario, or a native harness, as in `$cerneala-fix-bug`. Run it on the current code and record the exact code or steps, inputs, and the observed values.
- **Confirm it fails for the stated reason.** A broken fixture, a missing build output, a sandbox or environment limit, or a wrong expectation is not a Cerneala bug. Examples:
  - a NuGet `NU1301` restore failure is environmental;
  - a denied `pwsh` launch inside the sandbox is environmental;
  - PreviewHost failing because Debug outputs are missing is environmental.
- **Ground the expected behavior** in a contract: canonical docs under `docs-site/documentation/classes/`, existing tests, a guide, a plan, or an explicit user decision. If the intended behavior is genuinely ambiguous, write the issue as a question about the contract, or ask the user. Do not invent an expectation.
- **Check for duplicates.** Run `gh issue list --repo Chevalier12/Cerneala --state all --search "<key terms>"` with two or three different term sets. If an open issue already covers the contract, prepare the new evidence as a comment draft instead of a new issue. If a closed issue covers it, say whether this is a regression.
- **Keep the proof.** If a temporary reproduction will be deleted, put the minimal reproduction code in the issue body itself.

If any of these cannot be met, do not raise the issue. Record why in the final report: for example, "not reproduced", "environmental", or "contract unclear".

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
- **Label:** `bug`.

Save the draft with `apply_patch` to `.artifacts/issue-drafts/<yyyy-MM-dd>-<short-slug>.md`. That folder is git-ignored. The file's first line is `Title: ...`, followed by a blank line and the body.

## 3. Get approval, then post

Creating a GitHub issue publishes public content, so every issue needs the user's explicit approval before it is posted.

- **Do not post in the same turn you draft.** End the turn, or the task's final message, with the pending drafts: title, one sentence of what happens, and the draft path. Ask the user which ones to post.
- **During a long unattended run:** keep the drafts in `.artifacts/issue-drafts/`, continue the current task, and list every pending draft in the final message so the user can approve them together.
- **Approval is per issue.** Approving one draft does not approve the next.

To post an approved draft:

```
gh issue create --repo Chevalier12/Cerneala --title "<title>" --body-file <body-file> --label bug
```

Use a body file that contains only the body, without the `Title:` line. Then read the issue back with `gh issue view <n> --json title,body,labels`, and confirm that the sections and the label are correct. Move the draft file to `.artifacts/issue-drafts/posted/`.

If the sandbox blocks `gh` (network or credentials), do not try to work around the sandbox. Report the exact error, keep the draft, and give the user the exact `gh issue create` command to run themselves.

## 4. Report

In the final message, list:

- **Posted issues:** the URL and title of each.
- **Pending drafts:** the path and title of each, waiting for approval.
- **Defects not raised:** each one with the reason (not reproduced, environmental, duplicate of #N, contract unclear).

Do not fix the defect inside this skill. Fixing is `$cerneala-fix-bug`'s job, and it can start from the issue.
