# Motion Diagnostics

Enable tracing through:

```csharp
root.Motion.Diagnostics.IsEnabled = true;
```

Trace events include motion start, sample, and completion. Snapshots report active graph nodes, property bindings, layout motions, presence exits, property writes, and whether another frame is needed.

Frame stats include concise motion counters:

```text
motion=3, sampled=3, motionWrites=2, motionRender=2, motionLayout=0
```

Tests should use `ManualMotionClock` or `ManualMotionTimeline` for deterministic timing. Render-only assertions should verify zero measure/arrange work during motion ticks.

Reduced-motion tests should assert the final target value, not just that animation work stopped. For `ReducedMotionMode.Reduce`, tween specs complete immediately and diagnostics record `MotionSkippedReducedMotion`; for `DisableNonEssential`, layout motion should leave final layout intact without creating correction bindings.
