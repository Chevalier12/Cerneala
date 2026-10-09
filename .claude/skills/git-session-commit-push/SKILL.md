---
name: git-session-commit-push
description: Commit, push, and open a pull request for all changes attributable to the current conversation, always on a branch and never directly on master, using the repository's structured commit-message style. Use when the user invokes this skill or asks to commit and push this chat's work. Invocation authorizes the branch, a fast-forward of local master to origin/master, the commit, the push, and the pull request without another confirmation. Do not use merely to create, edit, or discuss the skill; do not rebase, merge without asking, or publish unrelated work.
---

# Git Session Commit and Push

Commit and push the work from the entire current conversation, not just its last
turn, on a branch, and open a pull request for it. The user's invocation or
equivalent commit-and-push request is approval: do not ask to approve the
branch, staging, the generated message, the commit, a normal push, or the pull
request. Give one short progress update, execute, and report the result.

The invocation does not authorize merging, enabling auto-merge, force-pushing,
or committing directly on `master`.

## Fast preflight and scope

- Read the repository instructions, current branch, status, configured upstream,
  and a few recent commit messages. Use the existing repository and its `origin`
  remote; do not invent a remote. Stop on detached HEAD, a missing `origin`
  remote, an unfinished merge/rebase, or a genuine scope ambiguity.
- Reconstruct conversation-owned changes from the chat's edits, initial status
  snapshots and diffs. Include both user and assistant work when attributable
  to this conversation. A dirty file or recent timestamp alone is not evidence.
- Inspect the relevant staged, unstaged and new-file contents. Preserve work
  that predates or falls outside this conversation. Do not stage secrets or
  temporary investigation artifacts. If a file mixes scopes, stage only the
  reviewed conversation-owned hunks; stop if they cannot be separated safely.
- Do not absorb or unstage unrelated changes already in the index. Report that
  blocker instead. Stage owned changes with explicit paths or a reviewed cached
  patch, never an indiscriminate `git add .` or `git add -A`.
- Check the commits that a push would publish, not just the new diff. Refresh
  the configured upstream with one targeted fetch when needed for an accurate
  comparison. A fetch does not integrate changes into the working branch.
  Stop if publication would include unrelated unpublished commits or if the
  upstream has diverged and requires integration.

## Branch (MANDATORY)

Never commit directly on `master`. Choose the branch before staging:

- **On `master`:** start the branch from the newest `origin/master`:
  1. Run `git fetch origin master`.
  2. Check `git log --oneline origin/master..master`. If local `master` holds
     unpublished commits that this conversation did not make, stop and report
     them, because the new branch would publish them.
  3. Run `git merge --ff-only origin/master`. This only moves local `master`
     forward to the GitHub version; it never creates a merge commit.
     Uncommitted changes stay in place. If Git refuses, for example because an
     uncommitted file would be overwritten or the histories diverged, stop and
     report Git's message. Do not stash, reset, or merge to work around it.
  4. Run `git switch -c session/<slug>`. The slug names the conversation's main
     change: lowercase, hyphen-separated, at most five words, for example
     `session/skills-branch-policy`. If that name already exists locally or on
     `origin`, choose a more specific slug. Uncommitted changes move with the
     switch.
- **On any other branch:** stay on it and commit there, for example on a
  `session/…` branch from an earlier run whose pull request is still open. Do
  not update it from `master` here; step 7 below checks it for conflicts.

## Structured commit message

Follow the actual recent repository history. In Cerneala, use an English subject
in this form, normally at most 72 characters:

```text
type(scope): concise summary
```

Use the purpose as `type` (`fix`, `feat`, `perf`, `refactor`, `docs`, `test`,
`build`, or `chore`) and the narrowest truthful owner as `scope`.

For a substantial change, group the body into meaningful ownership sections
with concrete bullets, followed by `Verification:`. Small changes need only
short bullets and verification; do not pad the message. For example:

```text
fix(sdl-gpu): correct retained surface invalidation

Rendering and ownership:
- Describe the actual invariant repaired and its owning layer.

Coverage and documentation:
- Describe the regression coverage and documentation actually changed.

Verification:
- Record the checks actually run, their outcomes and relevant limitations.
```

End the message with the attribution trailer that the session's instructions
specify for commits (for example a `Co-Authored-By:` line from a system
reminder), after a blank line, unless the user's own instructions say otherwise.
Do not invent a trailer the session does not specify.

When another skill invokes this one at its end (`cerneala-fix-bug`,
`cerneala-implement-plan`, `cerneala-performance-gate`, or `repo-cleanup`),
include the facts it passes in both the commit body and the pull-request body,
for example `Fixes #N`, the plan path, or a baseline-versus-final table.

Derive every statement from the staged diff and recorded evidence. Do not copy
example bullets, old test counts, or unrelated accomplishments. State failures,
skips and missing manual/platform validation honestly; never turn a failed suite
into a passing claim. Add a breaking-change note only when supported by the diff.

## Execute without reconfirmation

1. Reuse verification already completed for the unchanged staged content. Run
   only missing applicable checks, honoring mandatory repository pre-commit
   gates. Do not introduce a new full-suite requirement merely to make a commit.
   Invocation does not waive a mandatory gate or a tool security restriction.
2. Review `git diff --cached --stat`, `git diff --cached`, and
   `git diff --cached --check`. Confirm the index contains exactly the intended
   conversation changes. Do not silently fix unrelated whitespace or code.
3. Create the structured commit using multiple `-m` arguments or a temporary
   message file outside the repository. Do not amend an existing commit.
   If there is nothing new to commit, do not make an empty or duplicate commit;
   push existing conversation-owned commits if that is the remaining work.
4. Push the current branch explicitly with a normal, non-force push: to its
   configured upstream branch when it has one, otherwise with
   `git push -u origin <branch>`. Do not rely on broad or matching push
   defaults.
5. Verify the resulting commit and that the remote branch tip matches local
   `HEAD`.
6. Check for an open pull request with
   `gh pr list --head <branch> --state open --json number,url`.
   - If one exists, the push already updated it. Reuse its URL.
   - If none exists, write the body to a file outside the repository and run
     `gh pr create --base master --head <branch> --title "<commit subject>" --body-file <file>`.
     The body summarizes the branch's commits, the verification actually run,
     and any excluded work or missing checks. It ends with the pull-request
     attribution line that the session's instructions specify, if any.
   - Follow the host's post-PR instructions, for example binding the pull
     request and reading its CI. Do not merge or enable auto-merge unless the
     user asks.
   Report success only after the push, the remote-tip check, and the pull
   request succeed.
7. Check the pull request for conflicts with `master`:
   - Run `git fetch origin master`, then
     `git merge-tree --write-tree --name-only origin/master HEAD`. This command
     only computes the merge; it changes no file, branch, or index.
   - Exit code 0 means no conflict. Report the pull request as mergeable.
   - Exit code 1 means a conflict. The output's first line is a tree hash; the
     conflicting files follow, one per line, until the first blank line, and
     Git's `CONFLICT (...)` messages come after it. Report those files, then ask the user with
     `AskUserQuestion` whether to bring `master` into the branch with
     `git merge origin/master` and resolve the conflicts. Example of the
     question: "PR-ul are conflict cu master în `CanonicalConverter.cs`. Aduc
     master în branch și rezolv conflictul?"
   - Only after a yes: run `git merge origin/master`, resolve each conflict by
     keeping both sides' intent, and show the user the resolved hunks. Run the
     checks that cover the conflicting files, commit the merge, and push
     normally. If the right resolution is unclear, for example because both
     sides changed the same logic differently, ask the user instead of choosing.
     Abort with `git merge --abort` when the user declines the resolution.

No automatic pull, rebase, stash, reset, force-push, hook bypass, or retry
loop. The only automatic integration is the fast-forward of `master` in
"Branch"; a real merge happens only after the user's yes in step 7. If a push is rejected or an operation fails, retain the local commit and
report the exact blocker. If the remote advances after the push, report that
the push succeeded but the tips no longer match; do not chase it automatically.
Ask only for missing information or a materially new operation, never to
reconfirm the already authorized normal commit and push.

## Final response

Keep it short: commit hash and subject, branch/upstream, push and remote-tip
verification result, the pull-request URL (new or existing), the conflict check
result (no conflict, or the conflicting files and what the user decided), and
any excluded work or failed/missing checks. Do not say
the worktree is clean when unrelated changes remain. Skill creation itself must
not commit or push the user's repository.
