# Motion System

> Code: `UI/Motion`, `UI/Markup/GeneratedMarkupMotion.cs`, `UI/Timbre/TimbrePlaybackMotion.cs`, `UI/Detective/MotionDiagnostics.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Motion decides how a value travels from its current value to a target value over time, and writes the sampled values into element properties through the `Animation` precedence source. It does not decide the target: application code, Aspect state and input set targets; Motion only animates toward them.

The model is state-first. Example: `element.Motion().States().When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f, spec)` registers a target. When the pointer enters, Motion animates `Opacity` toward `0.5`. When no registered state matches, it animates back to the captured baseline value.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `MotionSystem` | `UI/Motion/Core/MotionSystem.cs` | Root-level facade; creates every component below and runs `Tick` | `UIRoot` (`UIRoot.Motion`, created in the `UIRoot` constructor) |
| `MotionGraph` | `UI/Motion/Core/MotionGraph.cs` | Active `MotionNode`s, groups and sequences; samples them per frame | `MotionSystem.Graph` |
| `MotionValue<T>` | `UI/Motion/Core/MotionValue{T}.cs` | One animatable value with `Current`, `Target`, `Velocity`; `AnimateTo` returns a `MotionHandle` | created by `MotionGraph.CreateValue` |
| `MotionHandle`, `MotionGroupHandle` | `UI/Motion/Core/` | Caller-side handle: `IsActive`, `IsCanceled`, `Completion`, `Completed` | caller |
| `MotionSpec<T>` subclasses | `UI/Motion/Specs/` | `TweenSpec`, `SpringSpec`, `KeyframesSpec`, `DecaySpec`, `RepeatSpec`, `PingPongSpec`; each creates a `MotionSampler<T>` | caller (immutable) |
| `ValueMixerRegistry` | `UI/Motion/Interpolation/ValueMixerRegistry.cs` | Interpolation per value type (double, float, Color, Brush, Thickness, Transform, …) | `MotionSystem.Mixers` |
| `MotionPropertyBinding<T>` | `UI/Motion/Properties/MotionPropertyBinding{T}.cs` | Connects a `MotionValue<T>` to one element property | element facade / caller |
| `MotionPropertyStore` | `UI/Motion/Properties/MotionPropertyStore.cs` | Stages property writes and flushes them once per tick | `MotionSystem.Properties` |
| `MotionFrameCoordinator` | `UI/Motion/Core/MotionFrameCoordinator.cs` | Hooks Motion into the frame phases | `MotionSystem.Frames` |
| `LayoutMotionCoordinator` | `UI/Motion/Layout/LayoutMotionCoordinator.cs` | FLIP layout motion: snapshots before/after layout, starts render-only corrections | `MotionSystem.Layout` |
| `PresenceCoordinator` | `UI/Motion/Presence/PresenceCoordinator.cs` | Exit animations for removed elements | `MotionSystem.Presence` |
| `MotionTimelineRegistry` | `UI/Motion/Core/MotionTimelineRegistry.cs` | Named timelines; root-thread-affine | `MotionSystem.Timelines` |
| `MotionTransactionContext` | `UI/Motion/Transactions/MotionTransactionContext.cs` | `BeginTransaction` scopes that give property changes a default spec | `MotionSystem.Transactions` |
| `ReducedMotionPolicy` | `UI/Motion/Core/ReducedMotionPolicy.cs` | `ReducedMotionMode`: `NoPreference`, `Reduce`, `DisableNonEssential` | passed to the `UIRoot` constructor; default `NoPreference` |
| `MotionDiagnostics` | `UI/Detective/MotionDiagnostics.cs` | Phases, warnings, counters, opt-in trace | `MotionSystem.Diagnostics`, exposed as `UIRoot.Detective.Motion` |
| `ObjectMotionRuntime` | `UI/Motion/ObjectMotionRuntime.cs` | Motion on plain objects (not elements); thread-static graph | UI thread |

## Data Flow

Example: `element.Motion().Opacity.To(0.8f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))` on an attached element.

1. The facade gets the `MotionPropertyBinding<float>` for `UIElement.OpacityProperty` from `MotionPropertyStore.GetOrCreateBinding` (one binding per element and property) and calls `AnimateTo`. The `MotionValue<float>` starts a node in `MotionGraph`. The default priority is `MotionPriority.Normal` (`MotionStartOptions`).
2. Conflict check: `MotionConflictResolver.Resolve` keeps the incoming motion when `incoming.Priority >= current.Priority`. Values: `Interactive = 0`, `Normal = 100`, `ReducedMotion = 200`. A rejected request returns a handle that is already canceled, and the active motion keeps running.
3. Next frame, `MotionFrameCoordinator.BeforeLayout` calls `MotionSystem.Tick`. `Tick` computes `delta = now - previousTimestamp`, clamped to `[0, MaxDelta]` (`DefaultMaxDelta` = 100 ms). After an idle period the first delta is `0`.
4. `MotionGraph.Tick` samples every active node. The binding stages the value in `MotionPropertyStore` (`StageSet`).
5. `MotionPropertyStore.Flush` writes each staged value through `UiObject.TrySetAnimationValueUntyped`, which sets the value with source `UiPropertyValueSource.Animation`. The normal property-change path then invalidates. `Opacity` is render-only, so the element gets Render invalidation and no Measure/Arrange.
6. When the tween completes, the binding clears the `Animation` value (`ClearValueUntyped(..., Animation)`) and the property falls back to the next source, for example `AspectBase`.

`Animation` is the second source in `UiPropertyStore.EffectiveOrder`, after `Local`. A local value set during an animation masks it; clearing the local value shows the animated value again. See [property-system.md](property-system.md) for the full order.

## Lifecycle And Ownership

- `MotionSystem` lives as long as its `UIRoot`. It is also constructible alone for tests; attached elements must use their root's clock and thread.
- Thread affinity: `MotionSystem.VerifyAccess` delegates to `UIRoot.Relay.VerifyAccess`. A handle operation from another thread throws and leaves the motion running.
- Detach: removing an element cancels its bindings on the next tick and clears its `Animation` values. Hiding it (`Visibility` leaves `Visible`, or `IsVisible` becomes `false`) calls `MotionSystem.CancelMotionForSubtree` from `UIElement`, which cancels the bindings of the whole subtree.
- Terminal handles: a handle is terminal after completion, cancellation or fault. The graph removes terminal work instead of keeping it active.
- Presence exit (`PresenceCoordinator.TryBeginExit`): removing a child from `VisualChildren` removes it from the public collection immediately, but the element stays attached in a render-only sidecar (`GetExitingVisualChildren`) until the exit motion ends. During the exit, hit testing skips it (`HitTestService` checks `IsPresenceExiting`). `CompleteExit` detaches it; a `completed` flag guards against a second detach. Re-adding the child during the exit cancels the exit.
- Layout motion (FLIP): when an element with `LayoutMotion` gets a new arranged rect, layout runs normally to the final rect. `LayoutMotionCoordinator.CaptureLastSnapshotsAndStartCorrections` then starts an inverse render transform (`SetLayoutCorrectionTransform`) that animates to `Transform.Identity`. Example: `Canvas.SetLeft(child, 40)` moves `ArrangedBounds.X` to `40` at once; the correction starts at `M31 = -40` and travels to `0`, without measure or arrange.

## Frame Integration

`UIRoot.ProcessFrameCore` passes `Motion.Frames` to `UiFrameScheduler.ProcessFrame` only when the scheduler has work or `Motion.HasActiveMotion` is true. Inside the frame:

| Scheduler step | Motion call | Effect |
|---|---|---|
| Frame start | `BeginFrame(reason)` | Clears per-frame diagnostics; phase `PreInput` or `AfterInput` |
| Before Measure | `BeforeLayout()` | Captures FLIP "first" snapshots; ticks Motion if active |
| After Arrange | `AfterLayout()` | Captures FLIP "last" snapshots; starts layout corrections |
| Before Render | `BeforeRender()` | Ticks Motion only if `BeforeLayout` did not tick this frame |
| Frame end | `EndFrame()` | Records `AfterRender` |

So Motion is sampled at most once per frame. Values written in `BeforeLayout` can still change layout in the same frame. `UiHost` also calls `ObjectMotionRuntime.TickCurrent` when render time advances (plain-object motion on its own graph).

Frame ordering is described in [invalidation-and-frame.md](invalidation-and-frame.md).

## Markup Activation

`.crn` Motion directives become calls into `GeneratedMarkup` (`UI/Markup/GeneratedMarkupMotion.cs`):

- `GeneratedMarkup.AttachMotionSession(owner)` attaches a private `MarkupMotionSession` lifecycle behavior to the element.
- `GeneratedMarkup.StartMotionProperty<T>` starts a property motion through the same `MotionPropertyBinding<T>` path as code.
- `GeneratedMarkup.StartPrismMotionProperty<T>` animates a Prism parameter (next section).

The source generator checks target type, property type and composition before it emits these calls; see [markup-and-sourcegen.md](markup-and-sourcegen.md).

## Motion On Prism And Timbre

- **Prism:** `PrismMotionBinding<T>` writes the sampled value into the Prism instance's parameter store and calls `PrismAttachment.InvalidateRenderState(target)`. The Prism target is resolved at build time (see [prism-technical-design.md](prism-technical-design.md)). Detaching the element or replacing its Prism cancels the motion exactly once.
- **Timbre:** `TimbrePlaybackMotionTarget` animates playback parameters such as `TimbrePlayback.VolumeParameter`. It uses its own `MotionGraph` with `new ReducedMotionPolicy()` (`NoPreference`), so the root's reduced-motion setting does not affect audio. Example from `TimbreMotionIgnoresTheVisualReducedMotionPreference`: the root is in `Reduce` mode; a 300 ms linear volume ramp from `0.2` to `0.8` and a 300 ms opacity tween start together. After the same frames, `Volume` is about `0.5` (still travelling), while `Opacity` is already `0` (reduced motion jumped to the end).

## Reduced Motion

`ReducedMotionPolicy` changes how motion runs, not the target:

- `Reduce`: a tween completes almost at once with its final value (a 200 ms tween ends after one 1 ms tick).
- `DisableNonEssential`: also disables FLIP layout correction; layout still jumps to the final rect.
- Infinite repeats become static under reduced motion, and `MotionDiagnostics.ReducedMotionSkipCount` counts the skips (`tests/Cerneala.Tests/UI/Motion/Core/MotionRepeatTimelineTests.cs:ReducedMotionMakesInfiniteAnimationStatic`).

`MotionPriority.ReducedMotion` is a priority value, not the policy. In production code, only `MotionStateBuilder` and state-driven Aspect motion (`AspectEngine`, when `AspectMotionSource.State` is set) pick a non-default priority (`Interactive`); nothing uses `ReducedMotion`.

## Invariants

- A lower-priority request does not replace an active motion; it returns a canceled handle. Higher or equal priority replaces it. `tests/Cerneala.Tests/UI/Motion/Core/MotionValueTests.cs:LowerPriorityAnimationCannotReplaceActiveAnimation`, `:HigherPriorityAnimationReplacesLowerPriorityAnimation`, `:ReducedMotionPriorityCannotBeReplacedByNormalAnimation`.
- Bindings write the `Animation` source and clear it on completion; a local value masks the animation and clearing it shows the animation again. `tests/Cerneala.Tests/UI/Motion/Properties/MotionPropertyBindingTests.cs:BindingWritesAnimationSource`, `:BindingClearsAnimationSourceOnCompletion`, `:BindingSurvivesLocalSourceMasking`.
- Render-only bindings cause no Measure/Arrange; layout bindings cause it only on frames where the value changes. `MotionPropertyBindingTests.cs:RenderOnlyBindingDoesNotEnqueueMeasureOrArrange`, `:LayoutAffectingBindingEnqueuesMeasureAndArrangeOnlyWhenValueChanges`.
- Detaching the target cancels the binding and clears `Animation`. `MotionPropertyBindingTests.cs:DetachedTargetCancelsAndClearsAnimationSource`.
- FLIP correction is render-only, keeps visual position on mid-flight retarget, and settles to identity without layout work. `tests/Cerneala.Tests/UI/Motion/Layout/LayoutMotionCoordinatorTests.cs:ChangingArrangedRectCreatesRenderOnlyInverseCorrection`, `:LayoutMotionTickDoesNotEnqueueMeasureOrArrange`, `:MidFlightLayoutRetargetKeepsVisualContinuity`, `:SpringLayoutCorrectionSettlesToIdentityWithoutLayoutWork`.
- An exiting element leaves the public collection, stays attached and renderable, receives no input, and is detached when the exit completes. `tests/Cerneala.Tests/UI/Motion/Presence/PresenceCoordinatorTests.cs:ExitKeepsElementAttachedAndRenderableUntilCompletion`, `:ExitRemoveDecreasesPublicCollectionCount`, `:ExitingElementDoesNotReceiveInputByDefault`, `:ExitCompletionRemovesElementOnce`.
- Frame phase order is `PreInput`, `BeforeLayout`, `AfterLayout`, `BeforeRender`, `AfterRender`. `tests/Cerneala.Tests/UI/Motion/Core/MotionSystemTests.cs:MotionFrameCoordinatorRunsBeforeAndAfterLayoutPhasesInOrder`.
- Reduced motion keeps the final target. `tests/Cerneala.Tests/UI/Motion/Core/MotionCompositionReducedMotionTests.cs:ReducedMotionCompletesTweenQuicklyWithoutBreakingFinalTarget`, `:ReducedMotionDisablesLayoutMotionCorrectionButKeepsFinalLayout`.
- A wrong-thread handle operation leaves the motion active. `tests/Cerneala.Tests/UI/Motion/Core/MotionHandleThreadAffinityTests.cs:WrongThreadLifecycleCallPreservesActiveMotion`.
- Prism motion cancels once on detach. `tests/Cerneala.Tests/UI/Prism/PrismMotionIntegrationTests.cs:DetachCancelsOnceAndReleasesThePrismBinding`.
- Timbre motion ignores the visual reduced-motion preference. `tests/Cerneala.Tests.Timbre/Motion/TimbreMotionContractTests.cs:TimbreMotionIgnoresTheVisualReducedMotionPreference`.
- Netestat: Motion is sampled at most once per frame (follows from `sampledThisFrame` in `MotionFrameCoordinator`; no test asserts a single tick).
- Netestat: `Tick` clamps the delta to `MaxDelta` (100 ms by default).

## Diagnostic

`UIRoot.Detective.Motion` is the root's `MotionDiagnostics`:

- `Phases` and `Warnings` are reset at every `BeginFrame`.
- `ReducedMotionSkipCount` counts motions skipped by reduced motion.
- The event trace (`MotionStarted`, `MotionSampled`, `MotionCompleted`, …) records only when `IsEnabled` is `true`. The default is `false`.
- `Detective.CaptureMotion()` returns a `MotionGraphSnapshot` with graph, property, layout and presence counts.

See [detective.md](detective.md) and the user guide [motion-diagnostics.md](../guides/motion-diagnostics.md).

## Known Limitations

- `MotionPriority.ReducedMotion` has no production user (search of `UI/` for `MotionPriority.ReducedMotion`).
- `MotionTrace` is an unbounded list while enabled; nothing in production code calls `MotionTrace.Clear()` (`UI/Detective/MotionTrace.cs`).
- `ScrollMotionBinding` and `DragMotionController` write the `Animation` source directly, not through `MotionPropertyStore`, so their writes are not part of the staged flush.
