# Sprite animation: scheduling and lifecycle

## Reproduction

```powershell
dotnet run --project benchmarks/Cerneala.Benchmarks/Cerneala.Benchmarks.csproj -c Release --no-restore -- --sprite-animation benchmarks/Cerneala.Benchmarks/results/2026-09-04-sprite-animation-stage3/baseline.json
```

`baseline.json` was captured before optimizing playback's per-tick clip scan.
Do not overwrite it; use `optimized.json` for the comparison run.

The runner measures the production `TimeSensitiveRenderInvalidator` traversal,
including its scene boundary, and separately the traversal plus `UIRoot.ProcessFrame`
and dirty-surface recording. It does not measure input, GPU execution, rasterization,
or presentation. Numbers are not whole-application frame times.

Each fixture warms up 128 iterations and measures 256 at a fixed 16 ms delta.
The two frames last 100 ms each. Sprite counts are 1/100/10,000, with zero,
approximately 10%, and 100% active; other instances are paused. All sprites are
inside a 1600x1600 viewport. All instances share one immutable clip definition
and one image. The tile fixture has 1,024 cells in one 32x32 chunk and 0/1/100
promotions, spread every three cells. Prism variants attach one Blur layer to
each promoted instance. Allocation counters cover the executing thread after
warmup; percentile timing includes GC pauses occurring during measurement.

Tile counters describe the last actual recording. For an unchanged static
surface that is the initial recording, not a per-tick rebuild. Zero promotions
produce one static command and zero splits. Changed animation frames reuse the
ordinary neighboring batches.

## Baseline and pre-optimization acceptance

The 10,000-active temporal-only baseline is P95 7,825.3 microseconds and
2,720,008.96875 allocated bytes/tick. The all-paused temporal baseline has zero
allocations and zero invalidations, regardless of scene size. The all-paused
root-commit path still allocates 856 bytes/tick: this is not a zero-allocation
claim for the entire UI frame.

The source audit identifies two LINQ-based presentation-change checks per active
instance per tick, allocating 272 bytes/instance/tick in the large warmed fixture.
The optimization is limited to deriving presentation transitions once in the
immutable clip. Before editing that path, its acceptance is fixed as:

- retain all deterministic/lifecycle results and 41 surface invalidations per
  256 measured ticks for the active looping fixture;
- remove the 272-byte per-active-instance allocation component, without adding
  a different instance-proportional allocation;
- keep the 10,000-active temporal P95 at or below the measured 7,825.3 microseconds;
- preserve zero temporal invalidations/allocations for inactive scenes and
  unchanged static tile batching.

No absolute whole-frame or GPU performance promise is introduced by this gate.

## Regression evidence

The initial three scheduling tests were RED: OnDemand did not advance the
selected source rectangle or invalidate at frame boundaries. The first scheduling
implementation passed 20/21 sprite-animation tests; the remaining test established
that an offscreen sprite was still recorded. Sprite recording now uses the
existing scene viewport intersection and conservatively retains offscreen inputs
when a sprite or scene-ancestor Prism scope can expand their influence.

The expanded seven-case scheduling corpus reproduced two further failures:
redundant identical tail frames kept a non-loop registered after its last visual
change, and a scene-group Prism scope retained its content version after an
animated descendant changed. `sprite-animation-stage3-tail-red.trx` records both.
The latter is a scene ownership issue: UIElement's Prism aggregation followed
only visual/presence parents. Scene nodes now use their logical rendering owner
as the fallback parent for that same aggregation; there is no surface-wide cache
flush or duplicate scene version system. Ordinary visual/presence ancestry is
unchanged. An initial compile-only test attempt needed `.Value` on the nullable
Prism scope; it is not counted as RED behavior evidence.

## Optimized results

`optimized.json` uses the same runner, configuration, warmup and measurement
sequence. The 10,000-active temporal P95 is 5,818.1 microseconds, below the
7,825.3 baseline. Allocation is 8.96875 bytes/tick: exactly 272 bytes per active
instance per tick were removed. The remainder is not zero; the surface still
allocates its coalesced invalidation request on frame changes. The active fixture
still invalidates 41 times, and every inactive temporal fixture retains zero
allocations/invalidations. Smaller fixtures report higher residual allocations
(up to 201 bytes/tick); the report preserves these results rather than claiming
all runtime paths have reached identical JIT warmup behavior.

The separate 10,000-active clock+commit+record P95 is 33,479.8 microseconds and
2,038,470.59375 allocated bytes/tick. Recording all 10,000 sprites is not covered
by a 60 FPS claim, and this stage does not optimize the existing drawing pipeline.

| Promoted tiles | Individual Prism | Clock+commit+record P95 (us) | Bytes/tick | Splits | Neighbor batches rebuilt / reused |
| --- | --- | ---: | ---: | ---: | ---: |
| 0 | No | 0.8 | 856 | 0 | Initial record only; no later redraw |
| 1 | No | 40.2 | 2214.12 | 1 | 0 / 2 |
| 1 | Yes | 101.8 | 2604.91 | 1 | 0 / 2 |
| 100 | No | 876.2 | 59772.81 | 100 | 0 / 101 |
| 100 | Yes | 1314.1 | 98862.38 | 100 | 0 / 101 |

All 25 sprite-animation tests pass in `sprite-animation-stage3-green.trx`.
The seven stage-3 cases include hidden/offscreen progress, nested registration,
pause/rate-zero removal, completion and restart, duplicate tail presentation,
detach/reattach/scene replacement, overlay clock traversal, and individual/group
Prism content versions. The canonical documentation manifest passes 1/1.
The first broader core run had 3,386 passes, two failures and two opt-in backend
conformance skips. Both failures expected offscreen draw commands in transform/
ordering fixtures. `NestedSceneGroupsComposeTransformsInsideViewBoxClip` translated
the sprite below a ViewBox whose height was only 10; its Y translation is now 1
instead of 11. `LayerThenYAppliesTheParentSceneTransformToItsAnchor` mirrored both
sprites to negative Y outside its viewport; a +50 Y translation keeps the same
reversed order visible. The exact transform/order assertions are preserved for
visible geometry, while stage-3 tests own the explicit culling contract. No
production bypass for static sprites was introduced.

Both corrected foundation tests pass (2/2) in
`sprite-animation-stage3-foundation-rerun.trx`. The other 3,386 passing cases from
the broader run remain valid: no production code changed afterward. This is
cross-run evidence, not a claim that the archived broader TRX has no failures.
The final solution run remains a stage-4 gate, including the explicitly enabled
backend conformance work. The index's eighth warning is the archived 4.7 MB core
TRX exceeding its 2 MB text-index limit; the other seven warnings are baseline.
