---
name: cerneala-fix-bug
description: Reproduce, diagnose, fix, and verify Cerneala defects end-to-end with evidence-first debugging, a focused RED regression test, CSI, Servo-driven runtime reproduction, Detective observability, or a native harness when needed, the smallest architecture-correct production change, green focused tests, and two identical reproduction apps, one built without the fix and one with it (executables published with the pull request together with a GIF of each, never committed), in which the user does the triggering actions and sees a live OK or BUG verdict. Use when the user reports broken behavior, a regression, an exception, incorrect rendering, input or layout failures, performance bugs, flaky behavior, or asks to investigate and fix a Cerneala issue rather than implement a new capability.
---

# Cerneala Fix Bug

Fix the reported behavior from reproduction through final verification. Treat the observed failure as the starting fact and theories as disposable until evidence supports them.

Follow `CLAUDE.md`, especially "Claude Code tooling and verification commands". After a context compaction, re-read this file (`.claude/skills/cerneala-fix-bug/SKILL.md`) before continuing. Recover the reproduction, the RED test, and the current hypothesis from the repository and the summary, not from memory alone.

Invoking this skill authorizes running the `git-session-commit-push` skill at the end (step 9).

**Step 0 (MANDATORY, before anything else):** start in a temporary worktree, as `CLAUDE.md` describes under "Temporary worktree for skills", and work only there. After the report, finish the worktree the same way.

## 1. Capture the Contract

- Restate the smallest known reproduction, expected behavior, actual behavior, environment, and frequency.
- Preserve diagnostics supplied by the user: exceptions, traces, screenshots, frame statistics, or custom-view details.
- Ask only when a missing detail prevents a reliable reproduction. Otherwise proceed with the most reasonable interpretation and record it.
- Do not edit production code before the failure and its intended contract are understood.

## 2. Orient in the Repository

- Follow `CLAUDE.md` and any narrower repository instructions.
- When you do not know the structure of the area, generate and read `FileTree.md` once.
- Locate the owning contract, callers, tests, documentation, and adjacent state transitions:
  - Use Roslyn MCP for C# references when it is connected, and Grep for text, markup, and non-C# uses.
  - Optional: send broad independent questions to Haiku explorers in parallel. Examples: "where is `X` raised", "which tests cover `Y`", "who calls `Z`".
  - Read the decisive files yourself. Read a full C# file before editing it.
- Do not scan unrelated subsystems for sport.
- Do not modify `CLAUDE.md` unless the bug explicitly concerns those instructions.

## 3. Reproduce Before Fixing

- Reproduce the bug with the narrowest deterministic path available.
- For runtime C# behavior, prefer a temporary CSI `.csx` experiment when it can expose the relevant state faster than a throwaway project.
- Run CSI as `dotnet-csi <script>.csx` (there is no `csi` command) with a short timeout, clean up the script, and check for a stuck `dotnet-csi.exe` process after suspicious execution.
- If CSI cannot exercise the real window, frame loop, layout, rendering, input, graphics, or Motion behavior, reproduce through the owning application with Servo and observe it through Detective when the application can host it faithfully. Otherwise use the temporary native runtime harness workflow below.
- Record the failing observation. Do not accept a theory-only reproduction.

### Servo and Detective Runtime Reproduction

Prefer this over a separate harness when the bug belongs to an application view or window that can reproduce itself inside the real application runtime.

1. Add an explicitly opt-in temporary scenario entry point in the view, its code-behind, or the owning window. Guard it with a unique environment variable, command-line switch, or internal test entry point so normal runs remain unchanged.
2. Create `Cerneala.UI.Servo.Servo` for the real `Window` or retained `UiHost`. Use `ServoTarget` queries plus Servo click, hover, drag, scroll, key, text, and wait operations so input traverses the real Cerneala pipeline; use `SaveScreenshotAsync` only from a window-backed Servo. Servo is the exclusive user-interaction path: do not invoke handlers directly or assign control state to imitate input.
3. Prefer an existing stable `Servo.Id`, semantic name, role, or scoped target. If the temporary scenario has no unambiguous semantic selector, add the narrowest opt-in `Servo.Id` needed by the probe and remove it during cleanup; do not reach generated private fields merely to drive input.
4. Inspect the root-owned `UIRoot.Detective` surface before inventing a probe. Capture the relevant built-in frame, input, layout, render, Aspect, Motion, invalidation, resource, or platform evidence. Add temporary instrumentation only for a signal that Detective and external tools cannot expose.
5. Add temporary observations at the narrowest useful boundaries when still needed: event-handler phases, model mutation, invalidation, measure/arrange/render, frame hooks, allocations and GC counts, backend state, or relevant snapshots. Capture component, trigger, and owning-layer evidence separately.
6. Run a bounded scenario, use Servo waits for the observable state actually required, write a concise structured report outside the repository, surface asynchronous failures, and close the window automatically.
7. Keep temporary instrumentation observational. Do not mix a speculative fix into the probe or permanently alter product behavior merely to gather evidence.
8. After capturing the result, remove every temporary branch, `Servo.Id`, probe, environment variable, report, and generated artifact. Rebuild after source changes and verify the temporarily instrumented production files have no remaining diff. Permanent Detective additions approved by the policy below are retained and verified instead of cleaned up.

Use a separate native runtime harness instead when direct instrumentation would distort the failing ownership boundary, cannot start the required host/backend, or would require broad product changes.

### Temporary Native Runtime Harness

Use this workflow for intermittent or hosted-runtime defects that a unit test cannot reproduce faithfully:

1. Use public Servo for in-process Cerneala UI queries, input, and waits, use a window-backed Servo for application-owned screenshots, and use the root-owned Detective surface for framework state. Do not create a parallel input or interaction path.
2. Create source only under `tests/Claude<Scenario>Harness/`. Never put harness `.cs` files under the repository-level `tmp/` directory: SDK default compile globs can pull them into an unrelated project. Use `tmp/` only for generated reports and other outputs.
3. Keep the project minimal: target the required Windows framework, reference the exact project under test, and add no reusable abstraction unless the harness proves it is needed.
4. Start the real application through its native generated application/startup descriptors. Wait for `ContentRendered` or an equivalent tree-ready signal, then create Servo for the real window and let Servo operations synchronize through the application frame lifecycle.
5. Drive controls through Servo targets and Servo input operations. Prefer existing IDs or semantic selectors; when none can identify the probe target, add and later remove a unique temporary `Servo.Id` instead of using reflection or visual-tree traversal to fake user interaction.
6. Use Servo queries for observable semantic and live-layout state. When the invariant needs retained framework state that Servo deliberately does not expose, capture it through `UIRoot.Detective`; keep a direct element reference inside the harness only when a Detective element-level capture requires one.
7. Capture the values that express the violated invariant, not screenshots alone: Servo element state and bounds, `DesiredSize`, `ArrangedBounds`, `Visibility`, dirty state, queue sizes, Detective snapshots and traces, frame statistics, hit-test/render counts, Motion writes, cache state, or trace entries as applicable.
8. Compute an explicit failure signal. For overflow, for example:

   ```text
   contentBottom = body.Y + body.Height + padding.Bottom
   overflowPixels = contentBottom - (border.Y + border.Height)
   ```

9. Turn randomness into a bounded stress sequence. Repeat the exact transition many times with deterministic short timing variations, a fixed cycle count, and a command timeout. Never use an unbounded loop.
10. Print a concise aggregate such as observed frames, failure count, and maximum delta, plus the first representative failure with enough state to diagnose it. Avoid drowning the useful frame in thousands of trace lines.
11. Save the exact command, cycle count, timing pattern, and pre-fix result. After the fix, run the identical harness and compare the same metrics.

The harness supplies high-fidelity reproduction evidence; it does not replace the permanent RED regression test. Once the owning invariant is known, encode the smallest stable version of that contract in the appropriate test project before changing production code.

### Harness Cleanup

- Close the native window on success, failure, and timeout paths. Surface asynchronous exceptions in the report instead of silently hanging.
- Delete only the exact resolved harness directory and its generated report after verification. Guard the resolved path before recursive deletion.
- Confirm with `git status` that no harness source, project, binaries, or reports remain.
- Never commit the temporary harness unless the user explicitly asks to promote it. If its observation has lasting framework value, apply the permanent Detective policy below instead of preserving an application-specific harness.
- The user reproduction app from step 5 is not a temporary harness. Keep its source outside the repository until step 9 has published it, and never commit it.

### Permanent Detective Diagnostics

The skill is authorized to add a permanent diagnostic to `Cerneala.UI.Detective` when existing Detective APIs and external tools leave a demonstrated observability gap that is reusable beyond the current one-off scenario.

- Keep the diagnostic owned by `UI/Detective/` and reachable through the root-owned `UIRoot.Detective` model when root context is required. Do not scatter permanent debug hooks across views, controls, or backends merely because that is the easiest observation point.
- Make the diagnostic observational: it may expose a snapshot, trace, counter, formatter, or dumper, but it must not change product behavior, repair the bug, invalidate state, rebuild caches, or become an input backdoor.
- Define the diagnostic contract and add the smallest focused test before implementing it. Cover enablement, bounded retention, reset/lifetime behavior, and hot-path allocation or timing risk when those apply.
- A public or protected addition must use the repository API-documentation workflow, update canonical pages under `docs-site/documentation/classes/`, synchronize the manifest when required, and pass the applicable API-compatibility review. Do not invent public surface solely to make a single harness convenient.
- Prefer disabled-by-default or snapshot-on-demand behavior when continuous collection would add hot-path cost. Measure overhead when performance can be affected; do not claim the diagnostic is cheap without evidence.
- Keep the permanent diagnostic, its tests, and its documentation during cleanup. Remove all temporary callers, switches, reports, and scenario-specific instrumentation around it.

### Repair Servo and Detective When They Fail

Servo and Detective are part of this workflow, not assumed-correct test equipment. If using either subsystem exposes its own contract violation, separate that failure from the reported defect with the smallest independent reproduction.

- When the Servo or Detective contract is established and the reproduction is RED for the subsystem defect, fix it immediately without asking the user for separate permission.
- Add a focused regression test, repair the owning invariant, run the subsystem's focused and affected verification, then repeat the blocked workflow operation and continue the original investigation.
- Do not classify documented behavior, harness misuse, or an unresolved product-semantic question as a subsystem bug. A fix without a known expected contract is guesswork.
- Report the additional Servo or Detective defect, fix, and verification separately from the original bug.

## 4. Add a RED Regression Test

- Add the smallest focused test that expresses the violated contract before changing production behavior.
- Run it and confirm it fails for the reported reason, not because the fixture, build, or assertion is broken.
- Prefer public behavior over implementation details unless the contract is intentionally internal.
- If a deterministic automated test is genuinely infeasible, state why and define a diagnostic gate plus a user-validation checklist. Do not claim automated regression coverage that does not exist.

If the bug has no GitHub issue yet, invoke `github-issue-raise` once the reproduction is RED, so that the fix can reference `Fixes #N`.

### Other Bugs Found Along the Way

While you fix the bug, you can find other Cerneala defects: in the owning code, in a neighbouring contract, in the tests, or while you build the reproduction app. For each one:

1. Prove it as `github-issue-raise` section 1 says: reproduce it on the current code, check that it fails for the stated reason, ground the expected behavior in a contract, and check for duplicates.
2. Raise it with `github-issue-raise` and post the issue directly, without asking the user (user decision, 2026-10-15). When an open issue already covers it, post the new evidence as a comment on that issue instead.
3. Do not fix it in this pull request. When it blocks the reproduction or the app, route around it with a comment. A Servo or Detective defect that blocks the workflow follows "Repair Servo and Detective When They Fail" instead.
4. Record it for the pull request: the issue link, its title, and one sentence on how you found it. Also record each suspected defect that you did not raise, with the reason, for example "not reproduced" or "contract unclear".

Step 9 lists them in the pull request. Example from #268: while building the reproduction app, the fix found #278 (a row realized again keeps the zero size it got outside the window) and #279 (Servo ignores `RenderTransform`), and PR 277 lists both under `Other bugs found`.

## 5. Build the User Reproduction App (MANDATORY)

Every fix ships a small executable in which the user performs the user validation of step 8 by themselves. The app sets up the real scenario from the issue, the user does the actions that trigger the bug (scroll, click, type, drag, resize), and the app checks the broken invariant live and says whether the bug happened. It is not a demo that plays a fixed script while the user watches: the actions are the user's. Example for #268, "rows of a virtualized list must touch after a scroll": a list of 200 rows that are 11 and 10 device pixels tall, the steps "drag the scrollbar thumb in big jumps, then scroll with the wheel", and a line that stays green while every neighbouring row pair touches and turns red at the first gap or overlap.

Build it right after the RED test, so that it shows the bug before the fix and its absence after the fix. The user gets two identical apps, compiled from the same app source: `Issue<N>-before`, built against the base commit without the fix, where the verdict must turn red, and `Issue<N>-after`, built against the fix, where it must stay green. Both run the same scenario with the same steps; only the Cerneala code under them differs. Only the compiled apps reach the user: their source is temporary and never goes into the repository, the commit, or the pull request. Step 9 publishes both apps as a GitHub pre-release, publishes an animated GIF of each one's check mode in the permanent `repro-gifs` release, and shows them in the pull request. They always go in the pull request and never in a GitHub issue: do not attach them to an issue or describe them in one, including issues raised with `github-issue-raise`.

Project:

- Create it outside the repository, in `<scratchpad>/repro-<N>/Issue<N>/`, where `<N>` is the GitHub issue number, for example `<scratchpad>/repro-268/Issue268/Issue268.csproj`. Without an issue number, use a short PascalCase scenario name, for example `<scratchpad>/repro-grid-auto-row-gap/GridAutoRowGap/`. Nothing in the repository changes for the app: no project, no `Cerneala.slnx` entry, no `Cerneala.csproj` exclusion.
- Outside the repository means outside the folder of every checkout and worktree, also when git ignores the folder. Example: `.artifacts/repro-apps/issue-311/` in the main checkout is inside the repository; MSBuild does not read `.gitignore`, so the root `Cerneala.csproj` picked up that app's `MainWindow.crn` and every build of Cerneala in that checkout failed with `CERNEALAUI003` (2026-10-16).
- Copy the project shape of `Playground/Cerneala.ComboBoxLab`, the smallest real Cerneala window app:
  - a `WinExe` project targeting `net8.0-windows`, with `ImplicitUsings` and `Nullable` enabled;
  - a `CernealaRoot` property with the absolute path of the worktree root and a trailing backslash, written as `<CernealaRoot Condition="'$(CernealaRoot)' == ''">C:\...\worktrees\issue-268\</CernealaRoot>` so that the before build can point it at the base commit with `-p:CernealaRoot=...`, and project references to `$(CernealaRoot)Cerneala.csproj`, to `$(CernealaRoot)Cerneala.Backends.SdlGpu\Cerneala.Backends.SdlGpu.csproj`, and to `$(CernealaRoot)Cerneala.SourceGen\Cerneala.SourceGen.csproj` with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"`;
  - a `ReproVariant` property (`<ReproVariant Condition="'$(ReproVariant)' == ''">dev</ReproVariant>`) passed to the app as `<AssemblyMetadata Include="ReproVariant" Value="$(ReproVariant)" />`. The app reads it with `Assembly.GetEntryAssembly()` and `AssemblyMetadataAttribute` and puts it in the window title, for example `Issue 268: before the fix` or `Issue 268: after the fix`, so the user can tell the two windows apart;
  - `<AdditionalFiles Include="**\*.crn" Exclude="bin\**;obj\**" />`;
  - a `BackendRegistration.cs` with the `ApplicationBackend` attribute for `SdlGpuApplicationBackend`;
  - a `MainWindow.crn` and a `MainWindow.crn.cs`. The source generator writes `Main`, so do not add a `Program.cs`.
- Use only public Cerneala API, as an application would. Build the scenario the way an application would: for a list bug, a real `ListBox` or `ItemsControl` in a `ScrollViewer`, not a panel that the app drives by calling its methods.

The scenario and the user's actions:

- Start from the issue's minimal reproduction and its user-validation steps. Use the same elements, sizes, values, and conditions.
- Find out which user actions really trigger the bug before you write the steps. Read the owning code, and confirm with check mode (below) before the fix. Example from #268: the wheel moves 48 units, less than one screen, so every row above the first visible row was already measured and the bug never appeared; a drag of the scrollbar thumb jumps past rows that were never measured, which is the condition of the bug.
- Write the steps in the window, as numbered literal actions, for example `1. Drag the scrollbar thumb down and up in big jumps. 2. Scroll with the mouse wheel over the list.`, and say what the user should see when the bug is absent, for example `neighbouring rows must always touch`.
- Make the symptom visible. When it is a few pixels, draw the area larger with a render-only `RenderTransform`, for example 4x with `RenderTransformOrigin` (0, 0) on the `ScrollViewer`, and give the parts strong contrasting colors. `RenderTransform` changes drawing and hit testing, not layout, so the user can still scroll and click the enlarged elements, and the check below still reads the real layout values.

The live verdict:

- Check the broken invariant from live framework state through public API, for example each realized row's `ArrangedBounds`, converted to device pixels with `MathF.Round(value * Root.Scale)`, and its item index from `ItemContainerGenerator.GetItemIndex`. Never hard-code the verdict.
- Run the check in `FrameRendered`, and only when the checked state changed since the last frame, so that standing still does not add checks. Count what was checked, for example row pairs and scroll positions.
- Show the verdict at the top of the window. It has one of these first lines:
  - `OK: bug #<N> does not reproduce so far.` in green;
  - `BUG: bug #<N> reproduces.` in red. It stays red after the first failure, even when the user moves on.
- After the first line, show the values: the first failure with measured and expected values, for example `overlap of 1 px between row 84 and row 85: row 84 = 300..311, row 85 = 310..320`, the totals, for example `29 gaps and 31 overlaps in 360 row pairs at 26 scroll positions`, and the display scale (`Root.Scale`) when the values are device pixels, because they change with it. Add a line about the current state, for example `Now: rows 40..52 are realized; every neighbouring pair touches.`
- Set a `TextBlock.Text` or brush only when its value changes, so that the window does not invalidate itself on every frame.
- When a condition from the issue cannot be set up in a real window (for example a specific DPI scale), say so in the window and in the pull request, and reproduce as much as the real window allows.

Check mode, the test in the real app:

- With the arguments `--check <report-file>`, the app performs the user's steps itself with Servo (`Cerneala.UI.Servo.Servo`, created in `ContentRendered`), then writes the verdict to `<report-file>`, closes its window, and exits with code 0 for OK, 1 for BUG, 2 when the triggering condition never happened (`NOT REACHED`, for example no jump left unmeasured rows), and 3 when the check itself threw (`ERROR`, with the exception). The generated `Main` takes no parameters and returns nothing, so read the arguments with `Environment.GetCommandLineArgs()` and set `Environment.ExitCode`.
- Use real Servo input only: `DragAsync`, `ClickAsync`, `ScrollAsync` (a delta of -120 is one wheel notch down), Servo's key and text operations, and `WaitForIdleAsync` after each action. Never set `VerticalOffset`, `SelectedIndex`, or other control state to imitate the user. Give targets an id with `Servo.SetId`, also on template parts reached through public API, for example `viewer.VerticalScrollBar.Track.Thumb`.
- Servo acts at the center of the target's layout bounds, and that point must hit the target:
  - It ignores `RenderTransform` (a known defect), so in check mode turn the zoom off with `new Transform(Matrix3x2.Identity)`. The zoom changes only drawing, so the verdict reads the same values; say so in the pull request.
  - Aim the wheel at the `ScrollViewer`, not at the scrolled content: a long list's layout center lies outside the visible area.
- With the extra arguments `--frames <folder>`, check mode also records the frames of the GIF that step 9 makes. It saves a PNG through Servo's `SaveScreenshotAsync` (Cerneala's own screenshot API) once before the first action, once after each action, and once after the verdict, named `frame-000.png`, `frame-001.png`, and so on. Each frame must show what happened:
  - the window shows the action check mode just did, for example `Check mode, step 3 of 8: drag the thumb to 70 %`, and the verdict lines are up to date, so that the GIF explains itself without a cursor;
  - the symptom is visible: when check mode turned the zoom off for Servo, turn it back on before each screenshot, wait for idle, save, and turn it off again before the next action. The zoom is render-only, so this changes no checked value.
- Build the app with `dotnet build <csproj> -c Release` and start the built `.exe` with `Start-Process -PassThru`, then `WaitForExit` with a timeout, for example 120 seconds. Kill it on timeout and report the timeout.
- Run it now, before the fix, with a report file outside the repository, for example `<scratchpad>/repro-before.txt`. It must exit with 1 and report a failure like the one in the issue. Exit 0 means the app or its actions do not trigger the bug, 2 means the actions never reached the condition, and 3 means the check broke: in every case fix the app, never the verdict.
- Step 8 runs it again after the fix and expects exit 0, and step 9 publishes it twice, before and after, and runs each published `.exe` once more. Keep every verdict line for the pull request.
- Look at the window once yourself before publishing: save a screenshot with `Window.SaveScreenshot` from temporary code, read the image, and remove that code. Check that the steps, the verdict, and the enlarged area are readable and do not overlap.

When the app does not behave as expected:

- A setup with several steps must wait for state, not count frames: a frame can render before your change has been measured and arranged. Move to the next step only when the previous step's state is visible on the elements, keep frames coming with `Invalidate(InvalidationFlags.Render, ...)` while waiting, and stop after a fixed cap, for example 300 frames, with the `NOT REACHED` verdict.
- Log the state of each frame, and of each measure and arrange through a small subclass of the element that only records, to a file outside the repository. Read the log; do not guess. Remove the logging before publishing.
- When the app runs into another Cerneala defect, handle it as "Other Bugs Found Along the Way" in step 4 says: raise it, do not fix it here, and route the app around it with a comment. Examples from #268: Servo ignores `RenderTransform`; a row that the panel had set to height 0 kept that height when it was measured again with the same width.

## 6. Diagnose the Root Cause

- Trace the failing state from the public symptom to the owning implementation.
- Distinguish the root cause from collateral symptoms and stale derived state.
- Check neighboring contracts likely to share the same invariant, but do not expand scope without evidence.
- Surface unrelated design smells separately. Do not smuggle cleanup into the fix.

## 7. Implement the Smallest Correct Fix

- Respect existing ownership, architecture, and local patterns.
- Fix the violated invariant without speculative abstractions, broad rewrites, or compatibility theater.
- A justified permanent Detective diagnostic is a separate observability deliverable, not the behavioral fix. Keep its contract and verification distinct from the defect regression.
- Preserve unrelated user changes in the working tree.
- Keep public API documentation synchronized when the fix changes public behavior or surface area. Use the repository-mandated API documentation workflow.

## 8. Verify in Layers

Run verification in this order:

1. Run the RED regression test and confirm it is now green.
2. Repeat the original CSI or runtime harness with the same inputs, cycle count, and timing pattern; compare the previously failing state and aggregate metrics.
3. Test the fix in the real app: rebuild the user reproduction app against the fixed code and run it in check mode as in step 5, with a new report file, for example `<scratchpad>/repro-after.txt`. It must exit with 0 and report OK. Exit 1 means the bug still reproduces in a real window, even when the regression test is green: go back to step 6. Exit 2 or 3 means the app no longer reaches the scenario or its check broke: fix the app and run both checks again.
4. Run the focused test group of the affected test project, with a `--filter`.
5. Run the focused tests of every other test project that covers code the fix touches or depends on, each with a `--filter`, as `CLAUDE.md` describes under "Focused tests". Never run the complete repository suite (`dotnet test Cerneala.slnx`, or a test project without `--filter`): the user chose focused tests only. Run the native SDL/GPU subset only when the fix touches that path. If another contract regresses, diagnose it, fix the damage, and rerun until green. Prove an "environmental" failure: for example, show the same failure on unchanged `master`.

When a permanent Detective diagnostic was added, also run its focused tests, documentation/manifest checks, API compatibility gate, and any measured overhead gate required by its collection model.

Do not perform or claim manual validation of the real application scenario. That gate belongs to the user. After automated verification, provide concise reproduction steps, the expected corrected result, and any diagnostics the user should inspect. The user validation happens in the two reproduction apps from step 5: download `Issue<N>-before.zip` and `Issue<N>-after.zip` from the links in the pull request's `How to check` section, unzip them, and do the actions written in the window of each `Issue<N>.exe`. The expected result is the red verdict in the before app, which shows that the steps trigger the bug, and the green verdict in the after app plus what the user should see, for example "the blue and orange rows always touch". Write the steps as literal actions with literal expected results. For example: "click X, then Y; the button should turn red and stay red". Record the manual result only when the user explicitly reports it.

Do not declare success because the code compiles or one assertion passes. The automated reproduction must be green and the repository must remain healthy. Until the user confirms the real scenario, report its status as awaiting user validation.

## 9. Commit and Push (MANDATORY)

After step 8 is green and the temporary harnesses and probes are cleaned up, invoke the `git-session-commit-push` skill. The commit never contains the reproduction app or any file from it. Pass it:

- `Fixes #N` when an issue exists;
- the root cause, the regression test, and the verification counts;
- the reproduction app's check-mode verdict lines before and after the fix;
- the other bugs found (step 4), for the `Other bugs found` section of the pull-request body;
- the pending user-validation steps.

If a required gate in step 8 failed or could not run, do not invoke it. Report the blocker and ask the user with `AskUserQuestion` whether to commit anyway.

Then publish the two reproduction apps for the pull request `<PR>` that the skill created or updated. The apps and the GIFs live in two different releases, because they live for different times (user decision, 2026-10-15):

- The release `repro-pr-<PR>` holds exactly two files, `Issue<N>-before.zip` and `Issue<N>-after.zip`. `cerneala-pr-review-merge` deletes it after the merge.
- The release `repro-gifs` is permanent and holds the GIFs of every pull request, named `Issue<N>-pr<PR>-before.gif` and `Issue<N>-pr<PR>-after.gif`, for example `Issue268-pr277-before.gif`. Nothing ever deletes it or its files, so the GIFs in a merged pull request keep showing. GitHub has no API to attach a file to a pull request, so the pull-request body shows the GIFs from this release.

1. Publish the after app from the fixed worktree, self-contained so that it runs without .NET installed:

   ```text
   dotnet publish <scratchpad>/repro-<N>/Issue<N>/Issue<N>.csproj -c Release -r win-x64 --self-contained true -p:ReproVariant=after -o <scratchpad>/repro-<N>/publish/after
   ```

2. Publish the before app from the same source against the base commit, the commit the pull request's branch started from, without the fix:
   - Create a detached worktree of it outside the repository, in a short folder such as `$env:TEMP\cb<N>`: `git worktree add --detach $env:TEMP\cb<N> $(git merge-base origin/master HEAD)`. The scratchpad path is too long: Windows path limits break the checkout there (`fatal: Could not reset index file`).
   - Copy the app folder to `<scratchpad>/repro-<N>/before/Issue<N>/` without its `bin` and `obj` folders, so the two builds never share build outputs. Change nothing else in the copy.
   - Publish the copy with `-p:CernealaRoot=$env:TEMP\cb<N>\ -p:ReproVariant=before -o <scratchpad>/repro-<N>/publish/before`, with the other arguments of the after app.
   - Remove the base worktree with `git worktree remove $env:TEMP\cb<N>` once the before app is published and checked.
3. Run each published `Issue<N>.exe` in check mode with `--frames`, for example `--check <scratchpad>/repro-<N>/after.txt --frames <scratchpad>/repro-<N>/frames-after`. The before app must exit with 1 and report the bug; the after app must exit with 0 and report OK. Otherwise publish neither, and report both verdicts.
4. Zip the contents of each publish folder with `Compress-Archive -Force` as `<scratchpad>/repro-<N>/Issue<N>-before.zip` and `Issue<N>-after.zip`. Make a GIF of each frames folder:

   ```text
   pwsh -NoProfile -File Tools/scripts/New-ReproGif.ps1 -FramesDirectory <scratchpad>/repro-<N>/frames-before -OutputPath <scratchpad>/repro-<N>/Issue<N>-pr<PR>-before.gif
   ```

   Open both GIFs with `Read` and check that each frame shows the step text, the verdict, and the symptom (before) or its absence (after).
5. Create a GitHub pre-release that holds only the two zips, on the pull request's head commit:

   ```text
   gh release create repro-pr-<PR> <before zip> <after zip> --target <head SHA> --prerelease --title "Reproduction apps: PR #<PR> (bug #<N>)" --notes-file <notes file>
   ```

   The notes say which commits the two apps were built from (the base commit and the head commit), how to run them, the actions the user does, what OK and BUG look like, and the check-mode exit codes. Uploading 160 MB can take minutes, so run it in the background. When the release already exists, for example because the pull request got new commits or it still holds an older single `Issue<N>.zip`, upload the two new zips with `gh release upload repro-pr-<PR> <files> --clobber`, delete every other asset with `gh release delete-asset repro-pr-<PR> <name> --yes`, and update the commits in its notes and in the pull request. Check with `gh release view repro-pr-<PR> --json assets` that exactly the two zips are there.
6. Upload the two GIFs to the permanent release with `gh release upload repro-gifs <before gif> <after gif> --clobber`. When `gh release view repro-gifs` says it does not exist, create it once first: `gh release create repro-gifs --target master --prerelease --title "Reproduction GIFs" --notes "GIFs of the reproduction apps' check mode, shown in pull requests. Never delete this release."`. Never delete `repro-gifs` or a file in it.
7. Add a `How to check` section to the pull-request body before its user-validation section, with `gh pr view <PR> --json body` and `gh pr edit <PR> --body-file <file>`. It contains:
   - the two GIFs side by side, each with its link, so the reviewer sees the bug and the fix without running anything:

     ```text
     | Before the fix (base `<base SHA>`) | After the fix (head `<head SHA>`) |
     | --- | --- |
     | ![Issue <N> before the fix](https://github.com/<owner>/<repo>/releases/download/repro-gifs/Issue<N>-pr<PR>-before.gif) | ![Issue <N> after the fix](https://github.com/<owner>/<repo>/releases/download/repro-gifs/Issue<N>-pr<PR>-after.gif) |
     ```

     After the edit, open the pull request page and check that both GIFs show. When they do not, link them instead and say so in the report;
   - the download links `https://github.com/<owner>/<repo>/releases/download/repro-pr-<PR>/Issue<N>-before.zip` and `.../Issue<N>-after.zip`, the release link, the two commits, and a note that the apps are self-contained for Windows x64;
   - the literal steps: download both zips, unzip them, run `Issue<N>.exe` from each folder, then the actions written in its window;
   - what the window shows in the before app (red, with example values) and in the after app (green), and the display scale the values were measured at;
   - which actions check mode performs, and a table with its exit code and verdict line before the fix, with the fix, and from both published `.exe` files;
   - every difference between check mode and the user's run, with the reason, for example the zoom that check mode turns off for Servo's actions.

   The pull request's user-validation section then says: run both apps, do the same actions in each window, and expect the red verdict in `Issue<N>-before` and the green verdict in `Issue<N>-after`. When that holds, post a comment on the pull request that starts with `Validated.`, alone or followed by extra instructions for the review, for example `Validated. Please raise issue for the other not raised issues if they are reproduced.`; `cerneala-pr-review-merge` then reviews, merges, and deletes the release `repro-pr-<PR>` with both zips. The GIFs stay in `repro-gifs`.

   After `How to check`, the body has an `Other bugs found` section with one bullet per bug from step 4, for example `- #279 Servo: target actions aim at the untransformed layout center ... Found when Servo could not drag the zoomed scrollbar thumb in the reproduction app.` Add each suspected defect that was not raised, with the reason. When there are none, the section says `None.` When a later commit finds another bug, add it to this section with `gh pr edit`.
8. Delete the reproduction app's temporary folder, `<scratchpad>/repro-<N>/`, only after the release and the pull-request body are in place, and after the base worktree is removed.
   - When the apps cannot be published, for example because `dotnet publish` fails, keep the app source for a later run: copy the app folder without its `bin` and `obj` folders to `%LOCALAPPDATA%\CernealaTools\repro-apps\issue-<N>\`, which is outside every checkout, and name that path in the pull request's `How to check` section. Never keep it in `.artifacts/` or another folder of a checkout.

## 10. Report Evidence

Summarize, in the user's language and following "The user" in `CLAUDE.md`:

- the reproduced failure and root cause;
- the focused regression test and its RED-to-GREEN result;
- the production fix;
- CSI or equivalent reproduction results before and after;
- the user reproduction apps: the release, the download links of both zips, the links of both GIFs in `repro-gifs`, whether the GIFs show in the pull request, and the check-mode verdicts before the fix, with the fix, and from both published `.exe` files;
- every other Cerneala defect found (step 4): its issue link, or the reason it was not raised, and confirmation that the pull request's `Other bugs found` section lists it;
- the focused test filters you ran with their pass/fail counts, and a note that the complete suite was not run, by the user's choice;
- the exact user-validation steps and whether the user has confirmed them;
- documentation changes, remaining uncertainty, or limitations.
- any permanent Detective diagnostic added, the evidence gap it closes, its tests and documentation/API result, and its measured or bounded runtime cost.
- any Servo or Detective defect encountered while running the workflow, its independent RED-to-GREEN result, and confirmation that the original operation was rerun.
- the branch, the commit, and the pull-request URL, or the gate that blocked them.

Never hide a failing test behind optimistic wording.

## Failure Protocol

- If the reproduction cannot be obtained, keep investigating observable state instead of guessing at a fix.
- If the reported expectation conflicts with an existing documented contract, stop and surface the conflict.
- If a focused test cannot run because of an environmental blocker, report the exact command and blocker. Do not call the work fully verified.
- If the proposed fix requires a broader architectural change, explain why the narrow fix is unsafe before expanding scope.
