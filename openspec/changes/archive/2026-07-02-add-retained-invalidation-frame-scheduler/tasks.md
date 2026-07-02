## 1. Roadmap And Contract Alignment

- [x] 1.1 Update `ROADMAPv2.md` section 4 planning checklist; done when proposal, design, specs, tasks, and validation entries for `add-retained-invalidation-frame-scheduler` are visible and accurately checked.
- [x] 1.2 Keep section 4 scoped to invalidation and scheduling; done when layout implementation, retained rendering implementation, and real hit-test geometry remain unchecked or assigned to later roadmap sections.

## 2. Core Invalidation Types

- [x] 2.1 Add `UI/Invalidation/InvalidationFlags.cs`; done when `[Flags]` includes `None`, `Measure`, `Arrange`, `Render`, `Text`, `Image`, `Resource`, `Style`, `InputVisual`, `HitTest`, and `Subtree`.
- [x] 2.2 Add `UI/Invalidation/DirtyState.cs`; done when it stores active flags, dirty version stamps, idempotent mark behavior, and phase-specific clear behavior.
- [x] 2.3 Add `UI/Invalidation/InvalidationRequest.cs`; done when requests include target element, flags, diagnostic reason, source property when available, and optional effect metadata needed by propagation.
- [x] 2.4 Add `UI/Invalidation/IInvalidationSink.cs`; done when attached elements can submit invalidation requests to root-owned scheduling without depending on concrete scheduler internals.
- [x] 2.5 Add `UI/Invalidation/FramePhase.cs`; done when phases include `Input`, `Style`, `Measure`, `Arrange`, `RenderCache`, `HitTest`, and `Idle`.
- [x] 2.6 Add `UI/Invalidation/FrameBudget.cs`; done when MVP explicitly represents no-deferral/all-work processing while leaving room for later budgeted scheduling.

## 3. Dirty Propagation And Queues

- [x] 3.1 Add `UI/Invalidation/DirtyPropagation.cs`; done when measure, arrange, render, text, image, resource, style, input visual, hit-test, and subtree propagation rules match the change spec.
- [x] 3.2 Add `UI/Invalidation/LayoutQueue.cs`; done when measure and arrange work are deduplicated by element reference and drained in deterministic order.
- [x] 3.3 Add `UI/Invalidation/RenderQueue.cs`; done when render-cache work is deduplicated by element reference and render-only invalidation does not schedule measure.
- [x] 3.4 Add `UI/Invalidation/HitTestQueue.cs`; done when hit-test rebuild work is deduplicated by element reference and can be processed separately from render work.
- [x] 3.5 Add queue ordering support using retained visual-tree traversal; done when equal-value but distinct elements remain distinct and parent/child order is stable.

## 4. Element And Root Integration

- [x] 4.1 Update `UI/Elements/UIElement.cs`; done when each element owns `DirtyState`, exposes retained invalidation entry points, and translates `IUiPropertyOwner` hooks into retained invalidation requests.
- [x] 4.2 Update `UI/Elements/UIRoot.cs`; done when the root owns the invalidation sink, scheduler, queues, and a frame-processing entry point returning `FrameStats`.
- [x] 4.3 Add detached-element behavior; done when detached elements can record local dirty state without enqueueing root work and the chosen attach behavior is documented by tests.
- [x] 4.4 Preserve backend neutrality; done when `UI/Core`, `UI/Elements`, and `UI/Invalidation` do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends.

## 5. Frame Scheduler And Diagnostics

- [x] 5.1 Add `UI/Invalidation/FrameStats.cs`; done when it reports processed measure, arrange, render-cache, hit-test, reused-cache, and no-work counts.
- [x] 5.2 Add `UI/Invalidation/UiFrameScheduler.cs`; done when it processes dirty queues through deterministic phases and no-ops on unchanged frames.
- [x] 5.3 Add phase processor contracts or delegates; done when scheduler tests can count phase work without implementing real layout, retained rendering, or hit-test geometry.
- [x] 5.4 Add `UI/Diagnostics/InvalidationTrace.cs`; done when tracing can record invalidation requests, propagation, queueing, phase processing, and clearing while staying optional.
- [x] 5.5 Ensure dirty flags clear only after successful processing; done when failures preserve matching dirty flags and queued work for retry or diagnostics.

## 6. Tests

- [x] 6.1 Add `tests/Cerneala.Tests/UI/Invalidation/InvalidationFlagsTests.cs`; done when flag combination, `None`, and specialized flags are covered.
- [x] 6.2 Add `tests/Cerneala.Tests/UI/Invalidation/DirtyStateTests.cs`; done when dirty marking, versioning, idempotency, and phase-specific clearing are covered.
- [x] 6.3 Add `tests/Cerneala.Tests/UI/Invalidation/DirtyPropagationTests.cs`; done when roadmap propagation rules are covered.
- [x] 6.4 Add `tests/Cerneala.Tests/UI/Invalidation/LayoutQueueTests.cs`; done when deduplication, reference identity, and deterministic layout order are covered.
- [x] 6.5 Add `tests/Cerneala.Tests/UI/Invalidation/RenderQueueTests.cs`; done when render-only scheduling and deterministic render order are covered.
- [x] 6.6 Add `tests/Cerneala.Tests/UI/Invalidation/HitTestQueueTests.cs`; done when hit-test queue deduplication and independent processing are covered.
- [x] 6.7 Add `tests/Cerneala.Tests/UI/Invalidation/UiFrameSchedulerTests.cs`; done when phase order, all-work MVP processing, no-work frames, and failure behavior are covered.
- [x] 6.8 Add `tests/Cerneala.Tests/UI/Invalidation/FrameStatsTests.cs`; done when stats count processed and no-work frames correctly.
- [x] 6.9 Add `tests/Cerneala.Tests/UI/Invalidation/RetainedNoWorkFrameTests.cs`; done when required no-work-frame scenarios from `ROADMAPv2.md` are covered.
- [x] 6.10 Add `tests/Cerneala.Tests/UI/Diagnostics/InvalidationTraceTests.cs`; done when enabled and disabled trace behavior is covered.
- [x] 6.11 Update existing `tests/Cerneala.Tests/UI/Core/UiPropertyInvalidationTests.cs` or add element-focused coverage; done when typed property options are proven to translate into retained invalidation through `UIElement`.
- [x] 6.12 Update existing `tests/Cerneala.Tests/UI/Elements/UIRootTests.cs` or add root-focused coverage; done when root-owned scheduler and frame stats are proven.

## 7. Validation

- [x] 7.1 Run `dotnet test`; done when the full test suite passes.
- [x] 7.2 Run `openspec validate add-retained-invalidation-frame-scheduler --strict`; done when the change validates successfully.
- [x] 7.3 Run `openspec validate --all --strict`; done when active changes and main specs validate successfully.
- [x] 7.4 Review `git status --short`; done when changed files are understood and no unrelated edits were made.
