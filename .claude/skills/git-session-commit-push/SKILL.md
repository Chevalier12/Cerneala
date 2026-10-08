---
name: git-session-commit-push
description: Commit and push all changes attributable to the current conversation, using the repository's structured commit-message style. Use when the user invokes this skill or asks to commit and push this chat's work. Invocation authorizes both operations without another confirmation. Do not use merely to create, edit, or discuss the skill; do not pull, rebase, or publish unrelated work.
---

# Git Session Commit and Push

Commit and push the work from the entire current conversation, not just its last
turn. The user's invocation or equivalent commit-and-push request is approval:
do not ask to approve staging, the generated message, the commit, or a normal
push. Give one short progress update, execute, and report the result.

## Fast preflight and scope

- Read the repository instructions, current branch, status, configured upstream,
  and a few recent commit messages. Use the existing repository; do not invent
  a remote or branch. Stop on detached HEAD, missing/ambiguous upstream, an
  unfinished merge/rebase, or a genuine scope ambiguity.
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
4. Push the current branch explicitly to its configured upstream branch with a
   normal, non-force push. Do not rely on broad or matching push defaults.
5. Verify the resulting commit and that the remote branch tip matches local
   `HEAD`. Report success only after the push and remote-tip check succeed.

No automatic pull, rebase, merge, stash, reset, force-push, hook bypass, or retry
loop. If a push is rejected or an operation fails, retain the local commit and
report the exact blocker. If the remote advances after the push, report that
the push succeeded but the tips no longer match; do not chase it automatically.
Ask only for missing information or a materially new operation, never to
reconfirm the already authorized normal commit and push.

## Final response

Keep it short: commit hash and subject, branch/upstream, push and remote-tip
verification result, and any excluded work or failed/missing checks. Do not say
the worktree is clean when unrelated changes remain. Skill creation itself must
not commit or push the user's repository.
