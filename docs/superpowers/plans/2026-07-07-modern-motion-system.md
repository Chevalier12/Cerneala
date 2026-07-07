# Modern Motion System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current MVP animation layer with a modern, deterministic, retained motion system for Cerneala: state-first, render-first, graph-driven, testable with a manual clock, friendly to layout/presence/gesture/scroll animation, and not a WPF/Avalonia-style storyboard clone wearing a fake mustache.

**Architecture:** Introduce a root-owned `MotionSystem` composed of a deterministic clock, a retained `MotionGraph`, typed `MotionValue<T>` nodes, physics/tween/keyframe specs, property bindings, implicit transactions, layout-motion FLIP, presence orchestration, input/scroll timelines, reduced-motion policy, and diagnostics. Existing `Animation<T>`, `AnimationScheduler`, `Transition<T>`, and `Storyboard` become compatibility shims or are retired after the new API is green.

**Tech Stack:** C#/.NET, existing Cerneala `UiProperty` value-source system, existing invalidation/layout/render queues, existing retained renderer, xUnit tests, playground samples, RoslynIndexer for navigation/indexing.

---

## Current Inventory

- [ ] Keep this inventory updated while implementing:
  - `UI/Animation/Animation.cs`: base duration/easing animation, single clock, no composition.
  - `UI/Animation/Animation{T}.cs`: typed from/to interpolation with `Func<T,T,float,T>`.
  - `UI/Animation/AnimationClock.cs`: positive-duration elapsed clock.
  - `UI/Animation/AnimationScheduler.cs`: rootless list scheduler; ticks entries and writes `UiPropertyValueSource.Animation`.
  - `UI/Animation/AnimatedValueSource.cs`: helper for applying/clearing animation source.
  - `UI/Animation/Easing.cs`: linear + quad easing only.
  - `UI/Animation/Transition.cs` and `Transition{T}.cs`: typed transition factory.
  - `UI/Animation/Storyboard.cs`: bag of handles with `Stop()`.
  - `UI/Styling/StyleTransition.cs`: style wrapper around `Transition<T>`.
  - `UI/Core/UiPropertyValueSource.cs`: already has `Animation = 5`, below `Local`.
  - `UI/Elements/UIRoot.cs`: owns layout/render/style/input schedulers, but not animation/motion.
  - `UI/Invalidation/UiFrameScheduler.cs`: processes invalidation work, but has no motion phase.
  - `UI/Invalidation/FrameStats.cs`: no motion counters yet.
  - `UI/Media/Transform.cs` and `Matrix3x2.cs`: render transform exists.
  - `UI/Controls/Shapes/Shape.cs`: has `RenderTransformProperty` and `OpacityProperty`; this is too narrow for app-wide motion.

## Audit Corrections Applied

- [ ] Phase 1 must not use a single vague "motion tick before layout" model. Layout motion needs a frame coordinator with pre-layout capture, property sampling, and post-layout correction phases.
- [ ] Phase 2 must include a type-erased `MotionSpec` contract. A transaction can animate multiple property types, so `BeginTransaction(MotionSpec<T>)` is generic bullshit and cannot work as the default API.
- [ ] Phase 2/4 must model typed velocity, not just `float InitialVelocity`, because springs/decay over `DrawPoint`, `Thickness`, rects, and transforms need vector-space velocity.
- [ ] Phase 7 must name the renderer integration points explicitly; adding `OpacityProperty` to `UIElement` without command/cache support would be decorative nonsense.
- [ ] Phase 9 must add a before/after property mutation seam. `UiObject.PropertyChanged` fires after effective value changes and does not expose enough source/base-target information for robust implicit animation.
- [ ] Phase 1 must create every type referenced by the initial `MotionSystem` API. `MotionTimelineRegistry`, `MotionDiagnostics`, `ReducedMotionPolicy`, and `MotionTokens` cannot magically appear in later phases while Phase 1 still compiles. Nu merge cu "las' ca vine el".
- [ ] Phase 1 must explicitly integrate with `UiFrameScheduler` or `UIRoot.ProcessFrame`; otherwise motion is root-owned in theory and dead in practice.
- [ ] Phase 7 must define transform-channel properties used by examples and composition rules. `UIElement.ScaleProperty` cannot appear in API examples unless the plan actually creates it.
- [ ] Phase 1 must define UI-thread/root affinity. Motion graph mutation from arbitrary threads would turn deterministic animation into o ciorba cu suruburi.
- [ ] Phase 5 must define `MotionHandle` lifetime, completion waiting, and disposal semantics. Otherwise canceled/completed handles and callbacks can leak roots/elements.
- [ ] Phase 7 must define `RenderTransformOrigin` coordinate semantics, not just add a property and hope pixels read minds.
- [ ] Phase 20 must define `MotionGroup` as real files/API/tests if it is replacing `Storyboard`.

## Design Non-Negotiables

- [ ] No primary `Storyboard` mental model. Storyboards may exist only as a compatibility facade.
- [ ] No magic global timers. Motion is per `UIRoot`, deterministic, and frame-owned.
- [ ] No layout churn for transform/opacity/color-only motion. These must stay render-only.
- [ ] Layout animation uses FLIP-like visual correction, not constant measure/arrange every tick.
- [ ] State changes are the API surface: hover, press, focus, selected, mounted, unmounted, and layout changes should be animatable without imperative spaghetti.
- [ ] Explicit cancellation and replacement semantics. No undead animation handles.
- [ ] Manual-clock tests for every timing-sensitive behavior.
- [ ] Reduced-motion behavior is part of the core contract, not a later checkbox of shame.
- [ ] Diagnostics tell us why a frame was animated, what values changed, and whether layout/render/hit-test were dirtied.
- [ ] The system must make simple things simple:

```csharp
button.Motion()
    .Animate(Control.BackgroundProperty)
    .To(hoverColor)
    .With(root.Motion.Tokens.FastOut);
```

- [ ] The system must make advanced things possible without API contortions:

```csharp
using (root.Motion.BeginTransaction(Motion.Spring(stiffness: 520, damping: 38)))
{
    panel.Width = expanded ? 320 : 64;
    details.IsVisible = expanded;
}
```

## External Ideas To Learn From, Not Copy

- [ ] Motion for React: use the idea of declarative, prop/state-driven motion, layout animation, gestures, and scroll-linked motion, but not React component semantics.
  - Reference: https://motion.dev/docs/react
- [ ] React Native Reanimated: use the idea of shared mutable motion values and worklet-like separation between state mutation and frame sampling, but keep Cerneala single-runtime and deterministic.
  - Reference: https://docs.swmansion.com/react-native-reanimated/docs/2.x/fundamentals/shared-values/
- [ ] Jetpack Compose Animation: use the idea of choosing focused APIs by use case: visibility, content size, value-as-state, gestures, infinite transitions.
  - Reference: https://developer.android.com/develop/ui/compose/animation/choose-api
- [ ] SwiftUI Animation/Transactions: use the idea that animations propagate through a transaction/context and the framework determines which changed values animate.
  - Reference: https://developer.apple.com/documentation/swiftui/animation
  - Reference: https://developer.apple.com/videos/play/wwdc2023/10156/

## Target Folder Layout

- [ ] Create `UI/Motion/` as the new namespace root: `Cerneala.UI.Motion`.
- [ ] Keep `UI/Animation/` only for compatibility wrappers during migration.
- [ ] Add these production folders:
  - `UI/Motion/Core/`
  - `UI/Motion/Specs/`
  - `UI/Motion/Interpolation/`
  - `UI/Motion/Properties/`
  - `UI/Motion/Transactions/`
  - `UI/Motion/Layout/`
  - `UI/Motion/Presence/`
  - `UI/Motion/Input/`
  - `UI/Motion/Styling/`
  - `UI/Motion/Diagnostics/`
- [ ] Add tests under:
  - `tests/Cerneala.Tests/UI/Motion/Core/`
  - `tests/Cerneala.Tests/UI/Motion/Specs/`
  - `tests/Cerneala.Tests/UI/Motion/Properties/`
  - `tests/Cerneala.Tests/UI/Motion/Transactions/`
  - `tests/Cerneala.Tests/UI/Motion/Layout/`
  - `tests/Cerneala.Tests/UI/Motion/Presence/`
  - `tests/Cerneala.Tests/UI/Motion/Input/`
  - `tests/Cerneala.Tests/UI/Motion/Diagnostics/`
- [ ] Add playground samples:
  - `Playground/Cerneala.Playground/Samples/MotionSample.cs`
  - `Playground/Cerneala.Playground/Samples/LayoutMotionSample.cs`
  - `Playground/Cerneala.Playground/Samples/PresenceMotionSample.cs`
  - `Playground/Cerneala.Playground/Samples/ScrollMotionSample.cs`

---

## Phase 0: Freeze The Current Contract

- [x] Add `tests/Cerneala.Tests/UI/Animation/LegacyAnimationCompatibilityTests.cs`.
- [x] Test that current `AnimationScheduler` behavior is captured before migration:
  - [x] Animation source outranks style sources.
  - [x] Local source masks animation source.
  - [x] Completing an old animation clears `UiPropertyValueSource.Animation`.
  - [x] Replacing same target/property stops and clears the old entry.
  - [x] Stopping a handle is idempotent.
- [x] Add test proving current weakness:
  - [x] `Storyboard` cannot express sequencing.
  - [x] `AnimationScheduler` has no root/frame integration.
  - [x] `AnimationScheduler` has no diagnostics beyond tick counts.
- [x] Do not fix these tests by expanding legacy API. Mark them as characterization tests where needed.

Verification:

```powershell
dotnet test .\Cerneala.slnx --filter "FullyQualifiedName~Animation"
```

---

## Phase 1: Core Clock And Frame Ownership

### Files

- [x] Add `UI/Motion/Core/IMotionClock.cs`.
- [x] Add `UI/Motion/Core/SystemMotionClock.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Motion/Core/ManualMotionClock.cs`.
- [x] Add `UI/Motion/Core/MotionFrame.cs`.
- [x] Add `UI/Motion/Core/MotionFramePhase.cs`.
- [x] Add `UI/Motion/Core/MotionFrameCoordinator.cs`.
- [x] Add `UI/Motion/Core/MotionFrameResult.cs`.
- [x] Add `UI/Motion/Core/MotionSystem.cs`.
- [x] Add `UI/Motion/Core/MotionThreadGuard.cs`.
- [x] Add `UI/Motion/Core/ReducedMotionMode.cs` with minimal `NoPreference`/`Reduce` values; Phase 17 expands platform behavior.
- [x] Add `UI/Motion/Core/ReducedMotionPolicy.cs` with default `NoPreference`; Phase 17 wires platform/accessibility.
- [x] Add `UI/Motion/Core/MotionTimelineRegistry.cs` as an empty registry shell; Phase 19 adds real timeline types.
- [x] Add `UI/Motion/Diagnostics/MotionDiagnostics.cs` as a disabled/minimal sink; Phase 18 expands trace/snapshots.
- [x] Add `UI/Motion/Styling/MotionTokens.cs` with hardcoded default specs; Phase 10 wires theme tokens.
- [x] Modify `UI/Elements/UIRoot.cs`.
- [x] Modify `UI/Invalidation/UiFrameScheduler.cs`.
- [x] Modify `UI/Invalidation/FrameStats.cs`.

### `IMotionClock`

- [x] Define the minimal deterministic clock abstraction:

```csharp
namespace Cerneala.UI.Motion.Core;

public interface IMotionClock
{
    TimeSpan Now { get; }
}
```

- [x] `SystemMotionClock` should:
  - [x] Use monotonic time, not wall clock time.
  - [x] Return non-decreasing `Now`.
  - [x] Be replaceable in tests.

### `MotionFrame`

- [x] Implement as readonly value type:

```csharp
public readonly record struct MotionFrame(
    TimeSpan Now,
    TimeSpan Delta,
    int FrameIndex,
    MotionFrameReason Reason,
    MotionFramePhase Phase);
```

- [x] Add `MotionFrameReason` enum:
  - [x] `Initial`
  - [x] `Scheduled`
  - [x] `Input`
  - [x] `Layout`
  - [x] `Manual`

- [x] Add `MotionFramePhase` enum:
  - [x] `PreInput`
  - [x] `AfterInput`
  - [x] `BeforeLayout`
  - [x] `AfterLayout`
  - [x] `BeforeRender`
  - [x] `AfterRender`

### `MotionFrameCoordinator`

- [x] Implement `MotionFrameCoordinator` as the only class allowed to decide when motion runs inside a root frame.
- [x] It should expose:

```csharp
public sealed class MotionFrameCoordinator
{
    public MotionFrameCoordinator(UIRoot root, MotionSystem motion);
    public MotionFrameResult BeginFrame(MotionFrameReason reason);
    public MotionFrameResult BeforeLayout();
    public MotionFrameResult AfterLayout();
    public MotionFrameResult BeforeRender();
    public MotionFrameResult EndFrame();
}
```

- [x] `BeforeLayout()` should:
  - [x] Flush non-layout-affecting sampled values only when safe.
  - [x] Capture first layout snapshots for elements participating in layout motion.
  - [x] Avoid starting FLIP correction because final layout is not known yet.
- [x] `AfterLayout()` should:
  - [x] Capture final layout snapshots.
  - [x] Ask `LayoutMotionCoordinator` to compute inverse visual corrections.
  - [x] Start or retarget render-only correction animations.
- [x] `BeforeRender()` should:
  - [x] Flush render-only sampled values.
  - [x] Flush layout-motion correction transforms.
  - [x] Produce final render invalidation counters.

### `MotionSystem`

- [x] Own one `MotionGraph`.
- [x] Own one `MotionTimelineRegistry`.
- [x] Own one `MotionDiagnostics`.
- [x] Track previous timestamp and frame index.
- [x] Enforce root/thread affinity through `MotionThreadGuard`.
- [x] Capture the creating managed thread id unless platform services expose a stronger UI dispatcher identity.
- [x] All graph mutation APIs should call `MotionThreadGuard.VerifyAccess()`.
- [x] Document that cross-thread animation requests must be marshaled through platform/UI dispatch before calling motion APIs.
- [x] Expose:

```csharp
public sealed class MotionSystem
{
    public MotionSystem(UIRoot root, IMotionClock clock, ReducedMotionPolicy reducedMotion);
    public MotionGraph Graph { get; }
    public MotionTimelineRegistry Timelines { get; }
    public MotionDiagnostics Diagnostics { get; }
    public MotionFrameCoordinator Frames { get; }
    public MotionTokens Tokens { get; }
    public bool HasActiveMotion { get; }
    public MotionFrameResult Tick(MotionFrameReason reason = MotionFrameReason.Scheduled, MotionFramePhase phase = MotionFramePhase.BeforeRender);
}
```

- [x] `Tick()` should:
  - [x] Compute delta from clock.
  - [x] Clamp huge deltas to a configurable maximum, e.g. 100 ms, to avoid tab-resume chaos.
  - [x] Sample graph.
  - [x] Flush property bindings.
  - [x] Produce counters.
  - [x] Return whether another frame is needed.

### `MotionThreadGuard`

- [x] Implement as a tiny root-owned guard:

```csharp
public sealed class MotionThreadGuard
{
    public MotionThreadGuard(int ownerThreadId);
    public bool CheckAccess();
    public void VerifyAccess();
}
```

- [x] In tests, allow constructing the guard with a fake/current thread id.
- [x] Do not add a full dispatcher abstraction in Phase 1 unless existing platform services already provide one.

### `UIRoot` Integration

- [x] Add `public MotionSystem Motion { get; }`.
- [x] Construct it after queues and before `ElementLifecycle.AttachSubtree`.
- [x] Add optional constructor parameter only if tests need it; otherwise add internal setter/factory for tests.
- [x] Do not let motion run during no-work frames unless `Motion.HasActiveMotion` is true.
- [x] Add a clear motion insertion seam:
  - [x] Preferred: extend `UiFrameScheduler.ProcessFrame(...)` to accept an optional `MotionFrameCoordinator`.
  - [x] Acceptable: wrap `Scheduler.ProcessFrame(...)` in `UIRoot.ProcessFrame(...)` with explicit before/after calls, but only if tests prove phase ordering.
  - [x] Do not hide motion ticking in render processors or style processors. That would be a cursed side effect.
- [x] Decide frame order and document it:
  - [x] Input updates state.
  - [x] Style/command state resolves new target values.
  - [x] `Motion.Frames.BeforeLayout()` captures layout-motion first snapshots and flushes any explicit layout-affecting motion values that must participate in layout.
  - [x] Layout runs from real property values, not visual correction transforms.
  - [x] `Motion.Frames.AfterLayout()` computes FLIP correction from first/last layout snapshots.
  - [x] `Motion.Frames.BeforeRender()` samples/flushed render-only values and correction transforms.
  - [x] Render cache updates if sampled values affect render.
  - [x] `Motion.Frames.EndFrame()` clears per-frame staging and emits diagnostics.

### `FrameStats`

- [x] Add counters:
  - [x] `MotionFrames`
  - [x] `MotionNodesSampled`
  - [x] `MotionValuesChanged`
  - [x] `MotionPropertyWrites`
  - [x] `MotionCompleted`
  - [x] `MotionRenderInvalidations`
  - [x] `MotionLayoutInvalidations`
  - [x] `MotionSkippedByReducedMotion`
- [x] Add `HasWork` integration so active motion is visible in stats.

### Tests

- [x] `MotionSystemUsesManualClockDeterministically`.
- [x] `MotionSystemDoesNotTickWhenNoActiveMotion`.
- [x] `MotionSystemRequestsAnotherFrameWhileGraphActive`.
- [x] `MotionSystemClampsHugeDelta`.
- [x] `MotionSystemRejectsGraphMutationFromWrongThread`.
- [x] `MotionFrameCoordinatorRunsBeforeAndAfterLayoutPhasesInOrder`.
- [x] `UiFrameSchedulerInvokesMotionCoordinatorAroundLayoutAndRender`.
- [x] `UIRootDoesNotProcessMotionWhenSchedulerAndMotionAreIdle`.
- [x] `LayoutMotionSnapshotsAreCapturedBeforeAndAfterLayout`.
- [x] `UIRootFrameStatsIncludeMotionCounters`.

---

## Phase 2: Typed Motion Specs

### Files

- [x] Add `UI/Motion/Specs/MotionSpec.cs` for the type-erased base spec.
- [x] Add `UI/Motion/Specs/MotionSpec{T}.cs` for the typed spec.
- [x] Add `UI/Motion/Specs/MotionSpecContext.cs`.
- [x] Add `UI/Motion/Specs/MotionSampler.cs`.
- [x] Add `UI/Motion/Specs/MotionVelocity.cs`.
- [x] Add `UI/Motion/Specs/RetargetMode.cs`.
- [x] Add `UI/Motion/Interpolation/IValueMixer.cs` as the minimal non-generic contract needed by type-erased specs.
- [x] Add `UI/Motion/Interpolation/ValueMixer.cs` as the generic base needed by `MotionSpec<T>`.
- [x] Add `UI/Motion/Interpolation/ValueMixerRegistry.cs` as a shell registry; Phase 4 registers built-ins.
- [x] Add `UI/Motion/Specs/TweenSpec.cs`.
- [x] Add `UI/Motion/Specs/SpringSpec.cs`.
- [x] Add `UI/Motion/Specs/DecaySpec.cs`.
- [x] Add `UI/Motion/Specs/KeyframesSpec.cs`.
- [x] Add `UI/Motion/Specs/MotionCompletion.cs`.
- [x] Add `UI/Motion/Specs/Motion.cs`.
- [x] Add tests under `tests/Cerneala.Tests/UI/Motion/Specs/`.

### `MotionSpec<T>`

- [x] Define a type-erased base spec first, because transactions and theme tokens need specs that can apply to multiple property types:

```csharp
public abstract class MotionSpec
{
    public abstract MotionSampler CreateSamplerUntyped(
        object? from,
        object? to,
        IValueMixer mixer,
        MotionSpecContext context);
}
```

- [x] Define as the base for all typed animation specs:

```csharp
public abstract class MotionSpec<T> : MotionSpec
{
    public abstract MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context);
}
```

- [x] Add non-generic `MotionSampler`:
  - [x] `object? CurrentUntyped { get; }`
  - [x] `bool IsComplete { get; }`
  - [x] `void Advance(TimeSpan delta)`
  - [x] `void RetargetUntyped(object? to, RetargetMode mode)`
- [x] Add `MotionSampler<T>`:
  - [x] `T Current { get; }`
  - [x] `bool IsComplete { get; }`
  - [x] `MotionVelocity<T>? Velocity { get; }`
  - [x] `void Advance(TimeSpan delta)`
  - [x] `void Retarget(T to, RetargetMode mode)`
- [x] `MotionSpecContext` should contain:
  - [x] `ReducedMotionPolicy ReducedMotion`
  - [x] `ValueMixerRegistry Mixers`
  - [x] `MotionDiagnostics? Diagnostics`
  - [x] `TimeSpan Now`
  - [x] `string? DebugName`
- [x] `MotionVelocity<T>` should:
  - [x] Wrap a value-space velocity `T Value`.
  - [x] Be produced/consumed only by mixers that support vector operations.
  - [x] Fail clearly when velocity is requested for non-vector types.

### `TweenSpec`

- [x] Properties:
  - [x] `TimeSpan Duration`
  - [x] `TimeSpan Delay`
  - [x] `IEasing Easing`
  - [x] `FillMode FillMode`
- [x] Behavior:
  - [x] Delay does not write target value until elapsed unless fill mode says so.
  - [x] Duration zero is allowed only through reduced-motion conversion, not general public creation.
  - [x] Completion is exact and deterministic.

### `SpringSpec`

- [x] Properties:
  - [x] `float Stiffness`
  - [x] `float Damping`
  - [x] `float Mass`
  - [x] `float RestSpeed`
  - [x] `float RestDelta`
  - [x] `SpringVelocityMode VelocityMode`
- [x] Behavior:
  - [x] Retarget preserves typed velocity by default when the mixer supports vector operations.
  - [x] Retarget falls back to no-velocity continuity for non-vector mixers and records a diagnostic.
  - [x] Spring can complete without fixed duration.
  - [x] Max iteration safety exists to avoid numerical nonsense.
  - [x] Use semi-implicit Euler or RK4, document the chosen integrator.

### `DecaySpec`

- [x] Properties:
  - [x] `MotionVelocity<T> InitialVelocity`
  - [x] `float Deceleration`
  - [x] `T? Min`
  - [x] `T? Max`
  - [ ] `MotionSpec<T>? Bounce` (deferred from Phase 2; fake unused API was removed instead of shipped)
- [x] Use for inertia and scroll/drag handoff later.
- [x] `DecaySpec<T>` must require a mixer with vector operations; otherwise construction or sampler creation fails with a clear exception.

### `KeyframesSpec`

- [x] Supports:
  - [x] Explicit offsets 0..1.
  - [x] Per-segment easing.
  - [x] Mixed hold/interpolate segments.
  - [x] Validation that offsets are sorted and endpoints are valid.

### Static Factory

- [x] `Motion` static class should expose:

```csharp
public static TweenSpec<T> Tween<T>(TimeSpan duration, IEasing? easing = null);
public static SpringSpec<T> Spring<T>(float stiffness = 520, float damping = 38, float mass = 1);
public static KeyframesSpec<T> Keyframes<T>(params MotionKeyframe<T>[] frames);
public static DecaySpec<T> Decay<T>(MotionVelocity<T> initialVelocity, float deceleration = 0.998f);
public static MotionSpec Tween(TimeSpan duration, IEasing? easing = null);
public static MotionSpec Spring(float stiffness = 520, float damping = 38, float mass = 1);
```

### Tests

- [x] Tween samples start/mid/end.
- [x] Tween applies delay.
- [x] Tween retarget can either restart or preserve progress based on `RetargetMode`.
- [x] Spring approaches target and completes under rest thresholds.
- [x] Spring retarget preserves velocity.
- [x] Spring retarget over non-vector mixer records fallback diagnostic instead of pretending velocity exists.
- [x] Decay clamps at bounds.
- [x] Decay rejects non-vector mixers.
- [x] Keyframes validate offsets.
- [x] Keyframes sample exact endpoint values.

---

## Phase 3: Easing As First-Class API

### Files

- [x] Add `UI/Motion/Specs/IEasing.cs`.
- [x] Add `UI/Motion/Specs/Easings.cs`.
- [x] Add `UI/Motion/Specs/CubicBezierEasing.cs`.
- [x] Add `UI/Motion/Specs/StepEasing.cs`.
- [x] Keep `UI/Animation/Easing.cs` as legacy adapter.

### Requirements

- [x] `IEasing` exposes:

```csharp
public interface IEasing
{
    float Transform(float progress);
}
```

- [x] `Easings` includes:
  - [x] `Linear`
  - [x] `Standard`
  - [x] `Emphasized`
  - [x] `EaseIn`
  - [x] `EaseOut`
  - [x] `EaseInOut`
  - [x] `Sharp`
- [x] `CubicBezierEasing`:
  - [x] Validates x control points in [0, 1].
  - [x] Uses deterministic Newton/bisection fallback.
  - [x] Clamps NaN to 0.
- [x] `StepEasing`:
  - [x] Supports `JumpStart`, `JumpEnd`, `JumpBoth`, `JumpNone`.

### Tests

- [x] Bezier endpoints are exact.
- [x] Bezier monotonicity holds for valid curves.
- [x] Step easing behavior matches mode.
- [x] Legacy `Easing.Linear` delegates safely or remains unchanged until removal.

---

## Phase 4: Value Mixing And Typed Interpolation

### Files

- [x] Extend the Phase 2 interpolation contracts instead of creating duplicate files.
- [x] Add `UI/Motion/Interpolation/FloatMixer.cs`.
- [x] Add `UI/Motion/Interpolation/DoubleMixer.cs`.
- [x] Add `UI/Motion/Interpolation/ColorMixer.cs`.
- [x] Add `UI/Motion/Interpolation/ThicknessMixer.cs`.
- [x] Add `UI/Motion/Interpolation/DrawPointMixer.cs`.
- [ ] Add `UI/Motion/Interpolation/DrawSizeMixer.cs` (N/A for now: no `DrawSize` type exists in the repo).
- [x] Add `UI/Motion/Interpolation/DrawRectMixer.cs`.
- [x] Add `UI/Motion/Interpolation/TransformMixer.cs`.

### Contract

- [x] `ValueMixer<T>` should handle:
  - [x] `T Mix(T from, T to, float progress)`
  - [x] `bool EqualsWithinTolerance(T left, T right, float tolerance)`
  - [x] `bool SupportsVectorOperations { get; }`
  - [x] `T Add(T left, T right)` only when `SupportsVectorOperations` is true.
  - [x] `T Subtract(T left, T right)` only when `SupportsVectorOperations` is true.
  - [x] `T Scale(T value, float scalar)` only when `SupportsVectorOperations` is true.
- [ ] For non-vector types, either:
  - [x] Do not support spring/decay, and fail clearly.
  - [ ] Or provide a vector adapter.
- [x] `ValueMixerRegistry`:
  - [x] Is root/system owned, not static-only.
  - [x] Registers built-ins during `MotionSystem` creation.
  - [x] Allows local custom mixers for app-specific structs.

### Transform Rules

- [x] Avoid naive matrix lerp as default if it gives ugly transforms.
- [x] Add `TransformComponents`:
  - [x] `TranslationX`
  - [x] `TranslationY`
  - [x] `ScaleX`
  - [x] `ScaleY`
  - [x] `RotationRadians`
  - [x] `SkewX`
  - [x] `SkewY`
- [x] `TransformMixer` should decompose/recompose where possible.
- [x] Fallback to matrix lerp only with explicit `TransformInterpolationMode.Matrix`.

### Tests

- [x] All built-in mixers return exact endpoints.
- [x] Color interpolation handles alpha.
- [x] Thickness interpolation handles each edge.
- [x] Rect interpolation handles x/y/width/height.
- [x] Transform interpolation preserves identity endpoints.
- [x] Missing mixer produces actionable exception with property/type name.

---

## Phase 5: Motion Values And Graph

### Files

- [x] Add `UI/Motion/Core/MotionValue.cs`.
- [x] Add `UI/Motion/Core/MotionValue{T}.cs`.
- [x] Add `UI/Motion/Core/DerivedMotionValue{T}.cs`.
- [x] Add `UI/Motion/Core/MotionNode.cs`.
- [x] Add `UI/Motion/Core/MotionGraph.cs`.
- [x] Add `UI/Motion/Core/MotionHandle.cs`.
- [x] Add `UI/Motion/Core/MotionCompletionSource.cs`.
- [x] Add `UI/Motion/Core/MotionCancellation.cs`.
- [x] Add `UI/Motion/Core/MotionPriority.cs`.

### `MotionValue<T>`

- [x] Design it as a mutable, observable motion cell:

```csharp
public sealed class MotionValue<T> : MotionValue
{
    public T Current { get; }
    public T Target { get; }
    public bool IsAnimating { get; }
    public MotionHandle AnimateTo(T target, MotionSpec<T> spec, MotionStartOptions? options = null);
    public void JumpTo(T value);
    public IDisposable Subscribe(Action<MotionValueChanged<T>> listener);
}
```

- [x] It should:
  - [x] Store current value.
  - [x] Store target value.
  - [x] Store active sampler.
  - [x] Store optional velocity vector.
  - [x] Notify only when effective sampled value changes.
  - [x] Allow retarget without tearing.

### `MotionGraph`

- [x] Responsibilities:
  - [x] Own active nodes.
  - [x] Advance nodes in deterministic insertion order.
  - [x] Remove completed nodes after notification flush.
  - [x] Avoid mutation while iterating by staging graph changes.
  - [x] Provide counters for diagnostics.
- [x] API:

```csharp
public sealed class MotionGraph
{
    public MotionValue<T> CreateValue<T>(T initial, ValueMixer<T>? mixer = null);
    public MotionFrameResult Tick(MotionFrame frame);
    public void Register(MotionNode node);
    public void Unregister(MotionNode node);
}
```

### `MotionHandle`

- [x] Expose:
  - [x] `bool IsActive`
  - [x] `bool IsCompleted`
  - [x] `bool IsCanceled`
  - [x] `ValueTask Completion { get; }`
  - [x] `void Cancel(MotionCancelBehavior behavior = MotionCancelBehavior.KeepCurrent)`
  - [x] `void Complete()`
  - [x] `void Dispose()`
  - [x] `event EventHandler<MotionCompletedEventArgs>? Completed`
- [x] Implement `IDisposable` so app code can unregister callbacks and release graph references before natural completion.
- [x] `Completed` callbacks should be cleared when the handle completes, cancels, or disposes.
- [x] `Completion` should complete successfully for natural completion and `Complete()`.
- [x] `Completion` should complete as canceled for cancellation if .NET cancellation plumbing is available; otherwise document the result type in `MotionCompletedEventArgs`.
- [x] `Cancel()` and `Dispose()` must be idempotent.
- [x] Completion semantics:
  - [x] Natural completion applies final value.
  - [x] Cancel keep-current leaves sampled value.
  - [x] Cancel revert restores pre-animation value if explicitly requested.
  - [x] Cancel complete jumps to target if explicitly requested.

### Derived Values

- [x] Add `DerivedMotionValue<T>` for computed transforms:

```csharp
MotionValue<float> x = root.Motion.Value(0f);
MotionValue<float> y = root.Motion.Value(0f);
MotionValue<Transform> transform = MotionValue.Combine(x, y, (cx, cy) => Transform.Translate(cx, cy));
```

- [x] Derived values should:
  - [x] Subscribe to dependencies.
  - [x] Recompute only when dependency values changed.
  - [x] Dispose subscriptions.

### Tests

- [x] Motion value `JumpTo` notifies once.
- [x] `AnimateTo` updates over manual ticks.
- [x] Retarget preserves active motion.
- [x] Cancel keep-current stops future ticks.
- [x] Complete jumps to target and fires completion once.
- [x] Disposing a handle unregisters completion callbacks and stops retaining target element.
- [x] Awaiting `Completion` resolves after natural completion.
- [x] Canceling a handle resolves/marks completion as canceled according to the documented contract.
- [x] Derived values recompute when dependencies change.
- [x] Graph tolerates nodes adding/removing nodes during callbacks.
- [x] Reentrant value callbacks cannot crash ticks or let an old handle finish the active new handle.
- [x] Replacement/retarget completion callbacks cannot leave callback-started handles orphaned.
- [x] Throwing `Completed` callback still clears callbacks/actions and releases retained targets.
- [x] Cancel revert and cancel complete behaviors are covered by tests.
- [x] Disposing a derived value unsubscribes from dependencies.

---

## Phase 6: Property Binding Layer

### Files

- [ ] Add `UI/Motion/Properties/MotionPropertyBinding.cs`.
- [ ] Add `UI/Motion/Properties/MotionPropertyBinding{T}.cs`.
- [ ] Add `UI/Motion/Properties/MotionPropertyStore.cs`.
- [ ] Add `UI/Motion/Properties/MotionPropertyKey.cs`.
- [ ] Add `UI/Motion/Properties/MotionPropertyOptions.cs`.
- [ ] Add `UI/Motion/Properties/AnimatablePropertyRegistry.cs`.
- [ ] Add `UI/Motion/Properties/MotionPropertyInvalidationClassifier.cs`.
- [ ] Modify `UI/Core/UiPropertyMetadata.cs` if metadata needs animation hints.
- [ ] Modify `UI/Core/UiPropertyOptions.cs` if render-only/layout-affecting classification is incomplete.

### Contract

- [ ] `MotionPropertyBinding<T>` connects a `MotionValue<T>` to a `UiObject` + `UiProperty<T>`.
- [ ] It writes with `UiPropertyValueSource.Animation`.
- [ ] It clears animation source on natural completion unless configured to hold.
- [ ] It tracks the property source before animation starts.
- [ ] It handles local value masking without losing sampled animation state.
- [ ] It stops when target element detaches from root.
- [ ] It must ignore writes where `UiPropertyValueSource.Animation` is already the source to avoid transaction feedback loops.
- [ ] It must stage writes through the frame coordinator, not write directly while graph nodes are being sampled.

### API

```csharp
public sealed class MotionPropertyBinding<T> : IDisposable
{
    public UiObject Target { get; }
    public UiProperty<T> Property { get; }
    public MotionValue<T> Value { get; }
    public MotionHandle AnimateTo(T to, MotionSpec<T> spec, MotionPropertyStartOptions? options = null);
    public void Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase);
}
```

### `AnimatablePropertyRegistry`

- [ ] Register built-in animatable properties:
  - [ ] `Control.BackgroundProperty`
  - [ ] `Control.BorderBrushProperty`
  - [ ] `Control.BorderThicknessProperty`
  - [ ] common width/height/margin/padding if present.
  - [ ] `UIElement.RenderTransformProperty` after Phase 7.
  - [ ] `UIElement.OpacityProperty` after Phase 7.
- [ ] Store:
  - [ ] Mixer type.
  - [ ] Default spec.
  - [ ] Invalidation category: render, layout, hit-test, semantics.
  - [ ] Whether property is safe for implicit animation.

### Tests

- [ ] Binding writes animation source.
- [ ] Binding clears on completion.
- [ ] Binding survives local source masking.
- [ ] Binding does not invalidate when sampled value equals effective value.
- [ ] Render-only binding does not enqueue measure/arrange.
- [ ] Layout-affecting binding enqueues measure/arrange only when value changes.
- [ ] Detached target cancels and clears source.

---

## Phase 7: Render-Layer Motion Foundation

### Files

- [ ] Add or move properties to `UI/Elements/UIElement.cs`:
  - [ ] `RenderTransformProperty`
  - [ ] `RenderTransformOriginProperty`
  - [ ] `OpacityProperty`
  - [ ] `TranslateXProperty`
  - [ ] `TranslateYProperty`
  - [ ] `ScaleProperty`
  - [ ] `ScaleXProperty`
  - [ ] `ScaleYProperty`
  - [ ] `RotationProperty`
  - [ ] `SkewXProperty`
  - [ ] `SkewYProperty`
  - [ ] `ClipToBoundsProperty` if needed for presence/layout effects.
- [ ] Modify `UI/Rendering/IRenderableElement.cs` if render metadata needs to expose element-level transform/opacity.
- [ ] Modify `UI/Rendering/DrawCommandListBuilder.cs` so every `UIElement` can push/pop transform and opacity around its render commands.
- [ ] Modify `UI/Rendering/RetainedRenderer.cs` so retained traversal preserves transform/opacity scopes.
- [ ] Modify `UI/Rendering/RetainedRenderCache.cs` so cache invalidation includes transform/opacity render versions.
- [ ] Modify hit-test cache/input route generation if transformed hit testing is selected.
- [ ] Audit `UI/Controls/Shapes/Shape.cs` and decide migration:
  - [ ] Remove shape-only duplicate properties after compatibility period.
  - [ ] Or make shape properties aliases to element-level properties.

### Requirements

- [ ] `RenderTransform` must be render-only by default.
- [ ] `Opacity` must be render-only by default.
- [ ] `RenderTransformOrigin` must use normalized element-local coordinates:
  - [ ] `(0, 0)` means top-left of arranged bounds.
  - [ ] `(0.5, 0.5)` means center.
  - [ ] `(1, 1)` means bottom-right.
  - [ ] Values outside `[0, 1]` are allowed only if explicitly documented; otherwise validate and reject.
- [ ] Transform composition must apply origin translation around the final arranged bounds, not desired size.
- [ ] Hit-testing must have an explicit policy:
  - [ ] Either transformed visual bounds participate in hit-test.
  - [ ] Or hit-test remains layout bounds and this is documented.
  - [ ] Pick one and test it. Do not let it be accidental.
- [ ] Retained render cache keys must include transform/opacity render version.
- [ ] Render scopes must compose in this order unless explicitly changed:
  - [ ] Layout correction transform.
  - [ ] Presence transform/opacity.
  - [ ] User render transform/opacity.
  - [ ] Child render scopes.

### Tests

- [ ] Animating opacity dirties render but not measure/arrange.
- [ ] Animating render transform dirties render but not measure/arrange.
- [ ] Render transform affects actual draw commands.
- [ ] Render transform origin changes pivot point deterministically.
- [ ] Invalid transform origin is rejected if policy clamps/rejects out-of-range values.
- [ ] Hit-test behavior under transform matches chosen policy.
- [ ] Retained cache invalidates only required subtree.

---

## Phase 8: Public Motion Facade

### Files

- [ ] Add `UI/Motion/MotionExtensions.cs`.
- [ ] Add `UI/Motion/MotionElementFacade.cs`.
- [ ] Add `UI/Motion/MotionAnimationBuilder.cs`.
- [ ] Add `UI/Motion/MotionStateBuilder.cs`.
- [ ] Add `UI/Motion/MotionDefaults.cs`.

### API Shape

- [ ] Extension:

```csharp
public static MotionElementFacade Motion(this UIElement element);
```

- [ ] Fluent property animation:

```csharp
element.Motion()
    .Animate(Control.BackgroundProperty)
    .From(current)
    .To(target)
    .With(Motion.Tween<DrawColor>(TimeSpan.FromMilliseconds(160), Easings.Standard));
```

- [ ] Shortcuts:

```csharp
element.Motion().Opacity.To(0.6f, Motion.Tween<float>(TimeSpan.FromMilliseconds(120)));
element.Motion().TranslateX.To(24f, Motion.Spring<float>());
```

- [ ] The facade should:
  - [ ] Resolve root.
  - [ ] Resolve value mixer.
  - [ ] Create/reuse a binding per target/property.
  - [ ] Fail clearly if element is detached and no root exists.

### Tests

- [ ] Facade creates one binding per element/property.
- [ ] Facade reuses existing binding on repeated calls.
- [ ] Facade throws clear error for missing mixer.
- [ ] Detached element behavior is deterministic.

---

## Phase 9: Motion Transactions For Implicit Animation

### Files

- [ ] Add `UI/Motion/Transactions/MotionTransaction.cs`.
- [ ] Add `UI/Motion/Transactions/MotionTransactionScope.cs`.
- [ ] Add `UI/Motion/Transactions/MotionTransactionContext.cs`.
- [ ] Add `UI/Motion/Transactions/MotionTransactionOptions.cs`.
- [ ] Add `UI/Core/UiPropertyMutation.cs`.
- [ ] Add `UI/Core/UiPropertyMutationObserver.cs`.
- [ ] Modify `UI/Core/UiObject.cs` to expose a source-aware before/after mutation seam.
- [ ] Modify style application path to optionally participate in transactions.

### Contract

- [ ] A transaction captures property changes during a scope.
- [ ] `MotionTransactionScope` implements `IDisposable` and always pops the transaction in `Dispose()`.
- [ ] A transaction disposed during exception unwinding must restore the previous transaction stack before propagating the exception.
- [ ] Transaction state must be root/thread-affine through `MotionThreadGuard`.
- [ ] The property mutation seam must capture:
  - [ ] Target `UiObject`.
  - [ ] `UiProperty`.
  - [ ] Mutating source.
  - [ ] Old effective value and old effective source.
  - [ ] New effective value and new effective source.
  - [ ] Old source-slot value and new source-slot value when available.
  - [ ] Whether coercion changed the requested value.
- [ ] If a changed property is animatable and allowed, it animates from previous effective value to new effective value.
- [ ] The target value remains the resolved non-animation value.
- [ ] The sampled animation value is written through `UiPropertyValueSource.Animation`.
- [ ] Local values still outrank animation.
- [ ] Writes from `UiPropertyValueSource.Animation` do not start new implicit animations.
- [ ] Style applicator should batch base and visual-state mutations so one style pass creates one target animation per property.
- [ ] Transactions can be nested:
  - [ ] Inner transaction overrides spec for changes inside it.
  - [ ] Outer transaction remains active after inner disposal.
- [ ] Transactions can disable animation:

```csharp
using (root.Motion.Disable())
{
    element.Width = 100;
}
```

### API

```csharp
public MotionTransactionScope BeginTransaction(MotionSpec defaultSpec);
public MotionTransactionScope BeginTransaction(MotionTransactionOptions options);
public MotionTransactionScope Disable();
```

### Tests

- [ ] Transaction animates animatable property changes.
- [ ] Non-animatable properties set immediately.
- [ ] Nested transaction uses inner spec.
- [ ] Transaction scope pops correctly when property mutation throws.
- [ ] Disposing the same transaction scope twice is harmless.
- [ ] Disabled transaction suppresses animation.
- [ ] Transaction does not animate initial attach/default population.
- [ ] Transaction works with style visual-state changes.
- [ ] Animation-source writes inside a transaction do not recursively create new animations.
- [ ] Style pass that applies and clears visual-state setters in one frame creates one final target, not two back-to-back animations.

---

## Phase 10: Style And Theme Motion Tokens

### Files

- [ ] Extend `UI/Motion/Styling/MotionTokens.cs` from Phase 1 with theme-aware token resolution.
- [ ] Add `UI/Motion/Styling/ThemeMotionTokens.cs`.
- [ ] Add `UI/Motion/Styling/StyleMotion.cs`.
- [ ] Add `UI/Motion/Styling/MotionStateRule.cs`.
- [ ] Modify `UI/Styling/Theme.cs` or `ThemeProvider` as needed.
- [ ] Modify `UI/Styling/DefaultTheme.cs`.
- [ ] Modify `UI/Styling/StyleRule.cs` only if style rules need motion metadata.

### Token Names

- [ ] `Instant`
- [ ] `FastOut`
- [ ] `FastIn`
- [ ] `Standard`
- [ ] `Emphasized`
- [ ] `GentleSpring`
- [ ] `SnappySpring`
- [ ] `LayoutSpring`
- [ ] `Enter`
- [ ] `Exit`

### Style Integration

- [ ] Do not put huge timeline definitions into style setters.
- [ ] Let styles specify:
  - [ ] Which property can animate.
  - [ ] Which motion token/spec to use.
  - [ ] Whether transition applies to base changes, visual-state changes, or both.
- [ ] Replace or adapt `StyleTransition<T>` with `StyleMotion<T>`.

### Tests

- [ ] Default theme provides motion tokens.
- [ ] Style visual-state change uses configured motion token.
- [ ] Missing token fails clearly or falls back to documented default.
- [ ] Theme change can change future motion specs without mutating active samplers.

---

## Phase 11: Visual State Motion

### Files

- [ ] Add `UI/Motion/Styling/MotionVisualStateController.cs`.
- [ ] Add `UI/Motion/Styling/MotionVisualStateSnapshot.cs`.
- [ ] Modify style visual state processing carefully.

### Behavior

- [ ] Hover, focus, pressed, disabled transitions animate by default only for safe properties:
  - [ ] Background color.
  - [ ] Border color.
  - [ ] Opacity.
  - [ ] Render transform.
- [ ] Layout properties must opt in. No surprise bouncing layout because hover changed padding.
- [ ] Pressed state can compose scale + color:

```csharp
button.MotionStates()
    .When(PseudoClass.Pressed)
    .Set(UIElement.ScaleProperty, 0.97f, Motion.Spring<float>(700, 44));
```

- [ ] If multiple pseudo-classes change in one frame, resolve a single target state and animate once.

### Tests

- [ ] Hover state animates background from old style value to new style value.
- [ ] Pressed state retargets active hover animation without jumping.
- [ ] Disabled state can cancel lower-priority interactive state motion.
- [ ] Multiple state changes in one style pass produce one property animation.

---

## Phase 12: Layout Motion With FLIP

### Files

- [ ] Add `UI/Motion/Layout/LayoutMotionCoordinator.cs`.
- [ ] Add `UI/Motion/Layout/LayoutSnapshot.cs`.
- [ ] Add `UI/Motion/Layout/LayoutMotionId.cs`.
- [ ] Add `UI/Motion/Layout/LayoutMotionOptions.cs`.
- [ ] Add `UI/Motion/Layout/LayoutMotionBinding.cs`.
- [ ] Modify `UI/Layout/LayoutManager.cs`.
- [ ] Modify `UI/Elements/UIElement.cs` with:
  - [ ] `LayoutMotionIdProperty`
  - [ ] `LayoutMotionOptionsProperty`

### Contract

- [ ] Capture "first" layout rect before layout changes.
- [ ] Run normal layout to produce "last" layout rect.
- [ ] Compute inverse transform from last to first.
- [ ] Apply inverse transform as render-only correction.
- [ ] Animate correction transform back to identity.
- [ ] Do not repeatedly measure/arrange during layout motion ticks.
- [ ] If actual layout changes again mid-flight, retarget from current visual position.
- [ ] If element detaches, hand off to presence exit if configured.

### API

```csharp
element.LayoutMotionId = "settings-panel";
element.LayoutMotion = LayoutMotionOptions.Spring(root.Motion.Tokens.LayoutSpring);
```

### Tests

- [ ] Changing arranged rect creates render-only transform animation.
- [ ] Layout motion tick does not enqueue measure/arrange.
- [ ] Mid-flight layout retarget keeps visual continuity.
- [ ] Layout motion completes by clearing correction transform.
- [ ] Same `LayoutMotionId` can animate element relocation across parents if parent coordinate conversion exists.
- [ ] If coordinate conversion is not ready, document and test same-parent only for v1.

---

## Phase 13: Presence And Enter/Exit

### Files

- [ ] Add `UI/Motion/Presence/PresenceCoordinator.cs`.
- [ ] Add `UI/Motion/Presence/PresenceState.cs`.
- [ ] Add `UI/Motion/Presence/PresenceOptions.cs`.
- [ ] Add `UI/Motion/Presence/PresenceHandle.cs`.
- [ ] Modify element removal/lifecycle path in `UI/Elements/ElementLifecycle.cs`.
- [ ] Modify relevant panels/items controls only if they remove children directly.

### Contract

- [ ] Enter:
  - [ ] Element attaches.
  - [ ] Initial visual state applies.
  - [ ] Motion animates to present state.
- [ ] Exit:
  - [ ] Removal request marks element as exiting.
  - [ ] Element stays in tree/render list until exit completes.
  - [ ] Input/hit-test excludes exiting elements by default.
  - [ ] Layout policy is explicit: keep space, collapse space, or overlay.
- [ ] Completion:
  - [ ] Final removal happens once.
  - [ ] Canceled exit can restore element to present state.

### API

```csharp
element.Presence = PresenceOptions.FadeAndScale(
    enter: root.Motion.Tokens.Enter,
    exit: root.Motion.Tokens.Exit);
```

### Tests

- [ ] Exit keeps element renderable until completion.
- [ ] Exiting element does not receive input by default.
- [ ] Exit completion removes element once.
- [ ] Re-adding while exiting cancels exit and animates back to present.
- [ ] Presence works with layout motion without double transforms.

---

## Phase 14: Gesture, Drag, And Input Timelines

### Files

- [ ] Add `UI/Motion/Input/GestureMotionController.cs`.
- [ ] Add `UI/Motion/Input/PointerMotionState.cs`.
- [ ] Add `UI/Motion/Input/DragMotionController.cs`.
- [ ] Add `UI/Motion/Input/VelocityTracker.cs`.
- [ ] Modify `UI/Input/ElementInputBridge.cs` only through clear integration hooks.

### Behavior

- [ ] Hover and pressed should become motion inputs, not ad-hoc style-only transitions.
- [ ] Drag should expose:
  - [ ] `MotionValue<float> DragX`
  - [ ] `MotionValue<float> DragY`
  - [ ] velocity.
  - [ ] constraints.
  - [ ] inertia handoff through `DecaySpec`.
- [ ] Input state changes happen before motion tick in a frame.

### Tests

- [ ] Pointer press retargets scale/color motion.
- [ ] Pointer release retargets back.
- [ ] Drag updates motion values without layout invalidation.
- [ ] Drag end starts decay with captured velocity.
- [ ] Pointer capture loss cancels or settles drag deterministically.

---

## Phase 15: Scroll-Linked Motion

### Files

- [ ] Add `UI/Motion/Input/ScrollTimeline.cs`.
- [ ] Add `UI/Motion/Input/ScrollMotionBinding.cs`.
- [ ] Add `UI/Motion/Input/MotionRange.cs`.
- [ ] Modify `UI/Controls/ScrollViewer.cs` to expose scroll values/timeline hooks.

### Behavior

- [ ] Scroll position is a source timeline, not a normal animation.
- [ ] Supports:
  - [ ] Absolute offset.
  - [ ] Normalized progress.
  - [ ] Range mapping.
  - [ ] Sticky/parallax transforms.
- [ ] Scroll-linked changes should be render-only unless mapped property affects layout.
- [ ] Avoid feedback loops: scroll changing motion must not change scroll offset unless explicitly wired.

### API

```csharp
ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
header.Motion().Opacity.Bind(timeline.Progress.Map(1f, 0f));
```

### Tests

- [ ] Vertical scroll updates timeline progress.
- [ ] Horizontal scroll updates separate timeline.
- [ ] Timeline mapping clamps correctly.
- [ ] Scroll-linked opacity does not enqueue measure/arrange.
- [ ] Scroll-linked layout property is explicit opt-in.

---

## Phase 16: Composition, Priority, And Conflict Rules

### Files

- [ ] Add `UI/Motion/Core/MotionComposition.cs`.
- [ ] Add `UI/Motion/Core/MotionChannel.cs`.
- [ ] Add `UI/Motion/Core/MotionConflictResolver.cs`.

### Rules

- [ ] Same target/property/default channel:
  - [ ] New animation replaces old animation.
  - [ ] Retarget policy determines continuity.
- [ ] Different transform channels can compose:
  - [ ] TranslateX
  - [ ] TranslateY
  - [ ] ScaleX
  - [ ] ScaleY
  - [ ] Rotate
  - [ ] Skew
- [ ] Layout correction transform composes before user transform.
- [ ] Presence transform composes after layout correction but before user transform unless documented otherwise.
- [ ] Interactive state has lower priority than explicit imperative animation.
- [ ] Reduced motion has highest priority.

### Tests

- [ ] Same property replacement cancels old handle.
- [ ] Transform channels compose deterministically.
- [ ] Layout correction and user transform both render.
- [ ] Presence and layout transforms do not overwrite each other.
- [ ] Explicit animation outranks hover state animation.

---

## Phase 17: Reduced Motion And Accessibility

### Files

- [ ] Add `UI/Motion/Core/ReducedMotionPolicy.cs`.
- [ ] Add `UI/Motion/Core/ReducedMotionMode.cs`.
- [ ] Add `UI/Motion/Core/IReducedMotionSource.cs`.
- [ ] Wire through `IPlatformServices` if platform can expose it.

### Modes

- [ ] `NoPreference`
- [ ] `Reduce`
- [ ] `DisableNonEssential`

### Behavior

- [ ] Opacity/color short transitions may be shortened.
- [ ] Large transform/layout motion should be replaced by instant or crossfade depending on policy.
- [ ] Infinite animations pause or become static.
- [ ] Diagnostics count skipped/reduced animations.

### Tests

- [ ] Reduced motion converts tween duration.
- [ ] Reduced motion disables layout motion.
- [ ] Reduced motion does not break final target values.
- [ ] Policy changes affect future animations and optionally active ones based on documented behavior.

---

## Phase 18: Diagnostics And Developer Surfaces

### Files

- [ ] Extend `UI/Motion/Diagnostics/MotionDiagnostics.cs` from Phase 1.
- [ ] Add `UI/Motion/Diagnostics/MotionTrace.cs`.
- [ ] Add `UI/Motion/Diagnostics/MotionTraceEvent.cs`.
- [ ] Add `UI/Motion/Diagnostics/MotionGraphSnapshot.cs`.
- [ ] Modify `UI/Diagnostics` surfaces if there is a central diagnostics page.
- [ ] Modify playground frame stats overlay.

### Diagnostics

- [ ] Trace events:
  - [ ] `MotionStarted`
  - [ ] `MotionRetargeted`
  - [ ] `MotionSampled`
  - [ ] `MotionCompleted`
  - [ ] `MotionCanceled`
  - [ ] `MotionPropertyWritten`
  - [ ] `MotionInvalidatedRender`
  - [ ] `MotionInvalidatedLayout`
  - [ ] `MotionSkippedReducedMotion`
- [ ] Snapshot fields:
  - [ ] Active node count.
  - [ ] Active property bindings.
  - [ ] Active layout motions.
  - [ ] Active presence exits.
  - [ ] Values sampled this frame.
  - [ ] Properties written this frame.
  - [ ] Next-frame-needed flag.

### Frame Stats Text

- [ ] Add concise counters to existing frame stats:

```text
motion=3, sampled=3, motionWrites=2, motionRender=2, motionLayout=0
```

- [ ] Avoid making the playground stats text wrap into unreadable soup by keeping labels short.

### Tests

- [ ] Diagnostics record start/sample/complete.
- [ ] Diagnostics can be disabled with near-zero overhead.
- [ ] Frame stats count motion work.
- [ ] No-work frame with no active motion reports no work.
- [ ] Active motion frame reports work even if other queues are empty.

---

## Phase 19: Infinite And Timeline-Driven Motion

### Files

- [ ] Add `UI/Motion/Specs/RepeatSpec.cs`.
- [ ] Add `UI/Motion/Specs/PingPongSpec.cs`.
- [ ] Add `UI/Motion/Core/MotionTimeline.cs`.
- [ ] Extend `UI/Motion/Core/MotionTimelineRegistry.cs` from Phase 1.
- [ ] Add `UI/Motion/Core/ManualMotionTimeline.cs`.

### Behavior

- [ ] Infinite animations must be opt-in.
- [ ] Infinite animations must appear in diagnostics as permanent frame requesters.
- [ ] Timelines can be:
  - [ ] Time-based.
  - [ ] Scroll-based.
  - [ ] Input/gesture-based.
  - [ ] Manual/test-based.
- [ ] Repeating animation must not leak handles or graph nodes.

### Tests

- [ ] Repeat loops exact cycle boundaries.
- [ ] Ping-pong reverses correctly.
- [ ] Infinite animation keeps requesting frames.
- [ ] Canceling infinite animation stops frame requests.
- [ ] Manual timeline drives sampled value without clock delta.

---

## Phase 20: Compatibility And Migration

### Files

- [ ] Modify `UI/Animation/AnimationScheduler.cs`.
- [ ] Modify `UI/Animation/Animation{T}.cs`.
- [ ] Modify `UI/Animation/Transition{T}.cs`.
- [ ] Modify `UI/Animation/Storyboard.cs`.
- [ ] Modify `UI/Styling/StyleTransition.cs`.
- [ ] Add `UI/Animation/AnimationCompatibility.cs` if needed.
- [ ] Add `UI/Motion/Core/MotionGroup.cs`.
- [ ] Add `UI/Motion/Core/MotionSequence.cs`.
- [ ] Add `UI/Motion/Core/MotionStagger.cs`.
- [ ] Add `UI/Motion/Core/MotionGroupHandle.cs`.

### Strategy

- [ ] Mark legacy types `[Obsolete]` only after all internal callers move.
- [ ] Implement legacy scheduler in terms of `MotionSystem` only if there is a root-aware path.
- [ ] If legacy scheduler remains rootless, keep it separate and clearly documented as test/compat only.
- [ ] Replace `Storyboard` with `MotionGroup` concept:
  - [ ] Parallel group.
  - [ ] Sequence group.
  - [ ] Stagger group.
  - [ ] Cancellation behavior propagates from group to child handles.
  - [ ] Child failure/cancellation policy is explicit: cancel siblings, continue, or complete group as canceled.
  - [ ] Group completion is awaitable through `MotionGroupHandle.Completion`.
- [ ] Do not let compatibility APIs shape the new model.

### Tests

- [ ] Existing animation tests still pass.
- [ ] Legacy scheduler behavior remains stable until removal.
- [ ] New motion tests do not rely on legacy classes.
- [ ] Motion group parallel waits for all child handles.
- [ ] Motion sequence starts next child only after previous completion.
- [ ] Motion stagger starts children with deterministic offsets.
- [ ] Canceling group cancels active children and prevents future sequence children from starting.
- [ ] Obsolete warnings are not emitted inside framework source after migration.

---

## Phase 21: Playground Samples

### Files

- [ ] Add `Playground/Cerneala.Playground/Samples/MotionSample.cs`.
- [ ] Add `Playground/Cerneala.Playground/Samples/LayoutMotionSample.cs`.
- [ ] Add `Playground/Cerneala.Playground/Samples/PresenceMotionSample.cs`.
- [ ] Add `Playground/Cerneala.Playground/Samples/ScrollMotionSample.cs`.
- [ ] Add tabs/routes for these samples.

### Sample Coverage

- [ ] Motion sample:
  - [ ] Hover button color.
  - [ ] Pressed scale.
  - [ ] Explicit animate button.
  - [ ] Cancel/restart controls.
- [ ] Layout motion sample:
  - [ ] Reorder list.
  - [ ] Expand/collapse panel.
  - [ ] Frame stats proving no measure/arrange spam during correction ticks.
- [ ] Presence sample:
  - [ ] Add/remove items with exit animation.
  - [ ] Toggle reduced motion.
- [ ] Scroll sample:
  - [ ] Header fade.
  - [ ] Parallax transform.
  - [ ] Progress indicator.

### Runtime Verification

- [ ] Open playground.
- [ ] Verify active motion updates frame stats.
- [ ] Verify idle frames return to no-work.
- [ ] Verify scroll-linked motion does not cause layout storm.
- [ ] Verify layout motion does not move hit-test into nonsense.

---

## Phase 22: Performance And Stress Gates

### Files

- [ ] Add `tests/Cerneala.Tests/UI/Motion/MotionStressTests.cs`.
- [ ] Add `tests/Cerneala.Tests/UI/Motion/MotionAllocationTests.cs` if allocation measuring infra exists.
- [ ] Add diagnostics budget constants to `MotionSystem`.

### Budgets

- [ ] 1 active opacity animation:
  - [ ] No measure calls per tick.
  - [ ] No arrange calls per tick.
  - [ ] 1 render invalidation max per tick.
- [ ] 100 active color animations:
  - [ ] Deterministic completion.
  - [ ] No per-frame list-copy explosion beyond the graph snapshot strategy.
- [ ] 100 layout motions:
  - [ ] No repeated measure/arrange after initial layout change.
  - [ ] Render-only transform corrections during ticks.
- [ ] Infinite animation:
  - [ ] No handle leak after cancel.
  - [ ] No graph node leak after cancel.

### Tests

- [ ] Stress test 100 simultaneous render-only animations.
- [ ] Stress test retargeting every frame for 60 frames.
- [ ] Stress test layout reorder with 100 elements.
- [ ] Stress test presence exit cancellation.
- [ ] Stress test reduced-motion toggling during active motion.

---

## Phase 23: Documentation

### Files

- [ ] Add `docs/motion-system.md`.
- [ ] Add `docs/motion-api.md`.
- [ ] Add `docs/motion-diagnostics.md`.
- [ ] Update `docs/developer-preview-checklist.md`.

### Docs Must Include

- [ ] Mental model:
  - [ ] Motion values.
  - [ ] Specs.
  - [ ] Property bindings.
  - [ ] Transactions.
  - [ ] Layout motion.
  - [ ] Presence.
  - [ ] Scroll timelines.
- [ ] What not to do:
  - [ ] Do not animate layout properties every frame unless intentional.
  - [ ] Do not build giant storyboard trees.
  - [ ] Do not use infinite motion without diagnostics visibility.
- [ ] Examples:
  - [ ] Hover/press visual state.
  - [ ] Explicit property animation.
  - [ ] Implicit transaction.
  - [ ] Layout reorder.
  - [ ] Exit animation.
  - [ ] Scroll-linked header.
- [ ] Testing guide:
  - [ ] Manual clock.
  - [ ] Frame stats assertions.
  - [ ] Reduced motion assertions.

---

## Suggested Implementation Order

- [x] 1. Phase 0: freeze legacy behavior.
- [x] 2. Phase 1: root-owned clock/system/frame stats.
- [x] 3. Phase 2 and 3: specs/easing.
- [x] 4. Phase 4: mixers.
- [x] 5. Phase 5: motion graph/values.
- [ ] 6. Phase 6: property bindings.
- [ ] 7. Phase 7: render-layer properties on `UIElement`.
- [ ] 8. Phase 8: public facade.
- [ ] 9. Phase 9 and 10: transactions/style tokens.
- [ ] 10. Phase 11: visual states.
- [ ] 11. Phase 12: layout motion.
- [ ] 12. Phase 13: presence.
- [ ] 13. Phase 14 and 15: input/scroll timelines.
- [ ] 14. Phase 16 and 17: composition/reduced motion.
- [ ] 15. Phase 18: diagnostics.
- [ ] 16. Phase 19: repeat/timeline advanced cases.
- [ ] 17. Phase 20: compatibility cleanup.
- [ ] 18. Phase 21 to 23: playground, stress, docs.

---

## Definition Of Done

- [ ] New motion system is root-owned and deterministic.
- [ ] All timing tests use manual clocks or manual timelines.
- [ ] Render-only animations do not enqueue measure/arrange.
- [ ] Layout motion uses visual correction, not layout spam.
- [ ] Visual-state transitions retarget without jumps.
- [ ] Scroll-linked motion works without scroll feedback loops.
- [ ] Presence exit keeps elements alive only until completion.
- [ ] Reduced motion has real behavior and diagnostics.
- [ ] Frame stats explain motion work clearly.
- [ ] Playground has samples proving idle frames return to no-work.
- [ ] Legacy `UI/Animation` no longer dictates architecture.
- [ ] The public API feels like Cerneala in 2026, not like someone photocopied WPF docs at 3 AM.
