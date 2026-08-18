# Plan: integration, diagnostics and hardening for Motion markup

> Date: 2026-07-15
> Status: completed
> Dependency: all other plans `2026-07-15-motion-markup-*`
> Purpose: dogfood-we surprise the language, close diagnostics/lifecycle/performance and promote the proposal to the implemented documentation.

## 1. Objectives

- One coherent surface, not six feature islands that greet each other from afar.
- Diagnostics with precise source spans and readable generated code.
- Proof by CernealaPresentation that a complex Motion showcase can be predominantly written in markup.
- Stress gates for leaks, allocations, idle frames and cleanup.

## 2. Non-objectives

- No full VS/LSP extension in this plan.
- No new Motion semantics compared to the proposal and Decay decisions explicitly accepted.
- No unrelated rewriting of CernealaPresentation.

## 3. Implementation stages

### Audit stage 0 - coverage matrix

| Public construction | Proprietary plan | Positive test | Relevant negative diagnosis | Audit result |
| --- | --- | --- | --- | --- |
| `Tween`, `Spring`, Layout named/inline, `@when`, `@if`, `@on`, `@animate`, `@from`, `@to`, `current`, `$part`, start options | foundation | `UiMarkupGeneratorMotionTests` | same fixture: missing `@to`, property/type/mixer/resource/context/options/event | implemented |
| `@set`, `@parallel`, `@sequence` and nesting | composition-and-clips | `UiMarkupGeneratorMotionClipTests`, `UiMarkupGeneratorMotionCompositionTests` | invalid discrete values, empty groups, siblings without composition, lifecycle cancel | implemented |
| `MotionClip`, `@run` | composition-and-clips | `UiMarkupGeneratorMotionClipTests` | same fixture: body count/context, missing/wrong target, recursion/direct assignment | implemented |
| `@parameter` and typed arguments | composition-and-clips | `UiMarkupGeneratorMotionParameterTests` | same fixture: duplicate/missing/wrong type/unsupported use | implemented |
| `@handle`, `as`, `@cancel` | composition-and-clips | `UiMarkupGeneratorMotionHandleTests` | same fixture: undeclared/duplicate/use-before-declaration/clip context | implemented |
| `@keyframes`, ranges, `hold`, `Step`, `Repeat`, `PingPong`, `@stagger`, spec options | timelines-and-specs | `UiMarkupGeneratorMotionTimelineTests` | same fixture: invalid ranges/overlap/nesting/spec/count/context | implemented |
| `Decay` | timelines-and-specs | the rejection tests from `UiMarkupGeneratorMotionTimelineTests` | the resource and the inline constructor are rejected | deferred: the runtime has no typed execution without decorative `@to` |
| `@presence` | presence-and-layout | `UiMarkupGeneratorMotionPresenceTests` | same fixture: duplicate/custom endpoints/body/retroactive attach | implemented |
| `@layout` | presence-and-layout | `UiMarkupGeneratorMotionLayoutTests` | same fixture: mode/crossfade/shared-element/custom sequence | implemented |
| `@scroll`, `@drag`, `@gesture press` | scroll-and-input | `MotionInputTimelineTests` plus stress/runtime tests from the proprietary plan | parser/resolver rejecting ranges/easing/non-float, drag options and unsupported gestures | implemented; dedicated sourcegen contracts are strengthened in stage 1 |
| `@else`, `$event`, transactions, direct resource `Motion`, layout programming extensions | explicitly outside the language | n/a | unknown/illegal directives and diagnostics of capability/context | intentionally absent; there is no lowering or example declared delivered |

Public API audit for worktree against `HEAD` is empty. The right pages
revised/synchronized in stage 4 for the aggregate surface are
ZZZ BLACK10ZZZ,
`Cerneala.UI.Markup.MarkupMotionExecution`,
`Cerneala.UI.Motion.Input.ScrollMotionBinding<T>` and
`Cerneala.UI.Motion.Input.DragMotionController`; all four pages already exist
in `docs-site/documentation/classes/`. The proposal is explicitly marked as deferred
Decay, seek/reverse/scrubbing, extended Stagger ordering, Presence/layout/input options
without runtime support and does not present any of them as the delivered syntax.

### Stage 0 - Surface audit

- [x] Build a matrix between each example/directive from `motion-markup-syntax-proposal.md`, the plan that implements it, the positive test and the relevant negative diagnosis.
- [x] Remove or explicitly mark deferred any example that cannot be downloaded at the current runtime; leave no decorative syntax.
- [x] Confirm the absence of `@else`, `$event`, transactions, direct `Motion` resources and layout-programming extensions.
- [x] Run the public API diff and inventory all `docs-site/documentation/classes/` pages that need to be synchronized.

**Gate Stage 0**

- [x] Each construction in the proposal is implemented, rejected with diagnosis or marked explicitly deferred with a runtime reason.

### Stage 1 - Diagnostics and generated source quality

- [x] Establish distinct IDs and messages for syntax, target resolution, event resolution, property/spec typing, composition, lifecycle-only directives and unsupported runtime capability.
- [x] Map each diagnosis to the exact token/directive in `.crn`, including resources referenced from another scope.
- [x] Issue generated members with stable names and `#line`/source mapping where the generator infrastructure allows, without sacrificing C# debugging. (Names are deterministic and contractually covered; `#line` was not added over the common factory method, because it would incorrectly map statements from interleaved XML nodes and degrade C# debugging.)
- [x] Add snapshot/contract tests for generated code: without reflection, dynamic, per-frame lookup by string or closures recreated at each tick.
- [x] Add diagnostics suggestions for too general `TargetType` and custom event found on the concrete type.
- [x] Reindex the solution.

**Gate stage 1**

- [x] A user can locate the error in the message and source span without reading generated C# like coffee grounds.

### Stage 2 - Dogfood in CernealaPresentation

- [x] Migrate a small hover/event behavior to foundation syntax and check the visual equivalence.
- [x] Migrate Motion view/showcase to `Aspect`, `MotionClip`, `@set`, composition and handles as the surfaces become available; including the replay and its discrete states are fully in the markup, without code-behind orchestration.
- [x] Use at least one custom event to demonstrate static `@on` wiring and a real attach/detach cycle.
- [x] Use Layout or Presence only if the showcase has a natural case; don't add decorations just to tick off APIs. (It wasn't necessary: Motion Lab doesn't insert/remove or reparent elements.)
- [x] Run the existing automation from `PresentationWindow.Automation.cs` and compare screenshots/behavior with the accepted baseline. (2 complete cycles, 15 samples, no error; the Motion Lab 1125x765 capture kept the geometry/palette and has no overlap.)
- [x] Reindex the solution.

**Gate stage 2**

- [x] The complex showcase does not require manual handle orchestration in the code-behind for the things representable in the markup.

### Stage 3 - Lifecycle and memory stress

- [x] Add an integrated test with 100 attach/detach/reattach cycles for Aspect with `@when`, custom `@on`, active clip, handle, Presence, Scroll and Drag where applicable.
- [x] Check after settle/GC that Motion graph active nodes, event subscriptions, observations, controllers and retained elements return to the baseline.
- [x] Quickly add Next/Previous-style restart/cancel stress to prevent memory regression seen in CernealaPresentation.
- [x] Add idle-frame assertions: without active/infinite motion, markup behavior does not require frames and does not produce layout/render invalidation.
- [x] Add allocation budget after warmup for hover/event restart and scroll update. (Warmup 64, 1,000 measured interactions, 40 MB ceiling and stability between the two halves; observed local baseline ~28.2 MB.)
- [x] Reindex the solution.

**Gate stage 3**

- [x] Stress tests are deterministic and GREEN; the transient growth of the heap stabilizes and does not correspond to retained owners. (27 relevant GREEN lifecycle/runtime tests, including the 2 new integrated gates.)

### Stage 4 - Documentation and tooling contract
- [x] Transform the proposal into implemented language documentation or keep separately a clearly marked deferred section; eliminate the formulation of the proposal for what is delivered. (The historical file remains at the same path for link compatibility, but the title and content are language reference; the undelivered surface is isolated under `Deferred Surface`.)
- [x] Documents grammar, ownership, event semantics, no-reflection lowering, lifecycle, cancellation and all runtime limitations.
- [x] Update Motion/API docs and all public API pages using the `writing-api-documentation` skill; synchronize the manifest. (The four inventoried pages were synchronized; their entries already existed in the manifest, so no JSON modification was necessary.)
- [x] Add a machine-readable table or a unique grammar used/tested by the generator that can later feed syntax highlighting/completion; do not manually duplicate keywords in two unvalidated sources. (`MotionMarkupLanguage.DirectiveNames` is consumed by the parser and fully traversed by the contractual test.)
- [x] Document requirements for future tooling: completion, hover types, go-to-definition, rename, quick fixes and generated-code preview, without declaring them implemented.
- [x] Reindex the solution.

**Gate Stage 4**

- [x] The documentation and the generator do not contradict each other for any public example. (161 sourcegen Motion GREEN tests, including directive table contract.)

### Stage 5 - Final check

- [x] Runs `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`. (329 passed, 0 failed, 0 skipped.)
- [x] Runs `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`. (1,899 passed, 0 failed, 0 skipped.)
- [x] Runs `dotnet test .\Cerneala.slnx`. (1,899 runtime + 329 sourcegen passed, 0 failed, 0 skipped.)
- [x] Runs the CernealaPresentation smoke and visual inspection/automation of the migrated showcase. (The final cycle has 8 samples/8 chapters without error report; Motion in-flight capture 1650x990 confirms stable layout, discrete states generated by `@set` and active animations without overlap.)
- [x] Run `git diff --check`, public API diff and RoslynIndexer `doctor/status` after final indexing. (`diff --check` clean, runtime public API diff empty, indexing has 0 warnings; `doctor/status` reports expected `stale` only because the worktree has modified uncommitted files.)
- [x] Confirm that there are no new skipped tests, new warnings or generated source with reflection/dynamic. (0 skipped, build/test without warnings; the generated source contract prohibits reflection, `dynamic`, string lookup and tick closures.)

## 4. The definition of ready

- [x] All supported syntax is typed, source-generated, diagnosed and demonstrated in a real application.
- [x] Custom events, observations, clips, handles and input controllers are completely detachable.
- [x] Performance gates demonstrate zero work in idle and lack of uncontrolled growth at repeated interaction.
- [x] The future tooling has a stable grammar/symbols/diagnostics contract that it can build on, without guessing the language.
- [x] Motion markup is coherent enough that WPF can start crying regularly.