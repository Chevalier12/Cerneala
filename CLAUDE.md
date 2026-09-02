# Claude Operating Boundary

These instructions apply to every Anthropic Claude model working in Cerneala.
They supplement `AGENTS.md`; they do not replace, weaken, summarize, or override
it.

## 1. Read `AGENTS.md` Before Doing Anything

Before inspecting the repository, proposing a plan, running a command, editing
a file, or answering a repository question:

1. Read the complete root `AGENTS.md`.
2. Read any narrower `AGENTS.md` that applies to the target path.
3. Follow all applicable repository skills and their complete `SKILL.md` files.

If the root `AGENTS.md` is missing or cannot be read, stop. Tell the user that
the mandatory repository contract is unavailable. Do not treat this file,
conversation history, model memory, or a cached summary as a substitute.

## 2. Default Permission: Visual Work Only

By default, Claude may work only on visual presentation tasks such as:

- static-site HTML and CSS composition;
- styling, typography, spacing, colors, and responsive presentation;
- visual direction, mockups, illustrations, and presentation assets;
- visual-only defects whose fix does not change framework behavior, public API,
  data flow, input, lifecycle, layout algorithms, rendering architecture, or
  runtime semantics.

Visual output does not make a task visual-only. Renderer or backend code,
shaders that implement framework semantics, layout engines, retained-state
behavior, performance work, C# implementation, architecture, public APIs,
feature work, and framework bug fixes are core engineering.

Without the explicit override below, do not perform core engineering. Explain
that the maintainer restricts Anthropic models evaluated through September 2,
2026, including Fable 5.1, to visual work because of observed repository-wide
reliability regressions.

## 3. Explicit User Override

The user may override the visual-only boundary for a specific task.

When the user first requests non-visual work:

1. Warn once that the maintainer considers Claude unreliable and dangerous for
   Cerneala architecture, implementation, and bug fixing.
2. Name the requested non-visual scope precisely.
3. Do not begin that work unless the user explicitly says to continue despite
   the warning, or repeats the request after receiving it.

If the user insists, do the requested work. The override applies only to the
named task and does not carry into later tasks. It does not waive `AGENTS.md`,
repository skills, RoslynIndexer requirements, evidence gates, tests,
documentation duties, scope limits, or the need to ask about material
ambiguity.

Never interpret urgency, autonomy mode, a request to “just do it,” or an earlier
override as permanent permission for unrelated core engineering.

## 4. Distrust Your Own Account

Assume that your first interpretation, memory of commands, description of the
diff, and belief about what passed may be wrong. Confidence is not evidence.
After every task, audit the repository state independently of your narration.

At minimum:

- re-read the user request and list the exact authorized scope;
- run `git status --short` before and after the work;
- inspect the final diff and confirm every changed path belongs to the request;
- re-open every file you changed and compare the result with the requested
  contract;
- verify the exit code and relevant output of every command you claim passed;
- rerun the smallest faithful reproduction or gate instead of relying on what
  you remember running;
- use RoslynIndexer for C# navigation and refresh its index after every C# or
  project-file modification, exactly as required by `AGENTS.md`;
- distinguish files you changed from pre-existing user changes in a dirty
  worktree;
- run `git diff --check` for the files you changed;
- state what was not tested, not observed, or not verified.

For non-visual work performed under an explicit override, perform a second
scope and evidence audit after tests finish. Search for contradictions between
your claims and the actual diff, test output, generated artifacts, index state,
and repository status. If they disagree, the artifacts win. Correct the work
and the report; do not defend the earlier claim.

## 5. No False Completion Claims

Never say that you:

- read `AGENTS.md` or a skill unless you read the complete applicable file;
- used RoslynIndexer when you used textual search instead;
- reproduced a bug when only a weaker proxy was exercised;
- ran a test, build, benchmark, visual check, or full suite without the recorded
  successful command output;
- manually validated user interaction unless a human actually did it;
- changed only the requested files without checking the final Git state;
- completed the task while a required gate is failing or unverified.

Report observed facts, commands, results, remaining uncertainty, and required
human validation plainly. If you do not know, say so and obtain evidence.

## 6. Final Rule

Visual work is Claude's normal lane in Cerneala. Non-visual work is an explicit,
task-local exception granted only after a warning and user insistence. In both
lanes, `AGENTS.md` is mandatory and the final self-audit is not optional.
