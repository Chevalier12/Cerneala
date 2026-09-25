# Stage 3 SceneItems2D deterministic idle and delta evidence

Scope: the SceneItems2D idle/delta checklist item only. This is not the Stage 3
conformance or full-suite gate.

## Deterministic scenario and measured result

`SceneItems2DIncrementalContractTests.IdleFramesDoNotReenumerateOrPublishCollisionMutations`
uses one plain enumerable occurrence, an attached `SceneItems2D` in a real
`RenderSurface2D`/`UIRoot`, and an enabled `InvalidationTrace`. After the fixture's
initial frame, it switches the surface to `OnDemand`, processes one mode-change
warmup frame, records the scene, and processes one record-settling frame. It then
performs 16 `UpdateRenderTime(16 ms)` + `UIRoot.ProcessFrame()` ticks, each followed
by an actual `RecordFrame` call. There is no animation or input during sampling.

The TRX contains these exact per-run counters:

| Phase | Frames | Measured elements | Arranged elements | Measure calls | Arrange calls | Rendered elements | Invalidation requests | No-work frames |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Mode-change warmup | 1 | 0 | 0 | 0 | 0 | 1 | not sampled | 0 |
| Record-settling warmup | 1 | 0 | 0 | 0 | 0 | 0 | not sampled | 1 |
| Stable sample | 16 | 0 | 0 | 0 | 0 | 0 | 0 | 16 |

Each sampled frame independently asserts `NoWorkFrames == 1`, `HasWork == false`,
zero measure/arrange calls and phase counts, and an empty scheduler at the end.
Across the sample, source enumeration count, collision mutation version,
realized node identity, and all four SceneItems structural update counters
remain unchanged. The invalidation number counts `Request` entries in the
enabled trace after warmup; it is not an inferred number from `FrameStats`.
`OnDemand` is intentional: default `Continuous` redraw would cause a render
invalidation on each time tick even when collection contents are stable.

`ObservableDeltasOnlyCreateAndRetireAffectedOccurrences` uses an attached
`ObservableCollection<string>` and checks identity and structural counters after
each operation. `Move(2,0)` creates/retires 0 nodes; `Insert(1)` creates 1;
`RemoveAt(1)` retires exactly that inserted node; `Replace(1)` creates 1 and
retires 1; `Clear()` emits Reset and retires the remaining 3. Total deltas from
the initial counter snapshot are 2 created and 5 removed. Unaffected
occurrences retain exact node identity, and template indices remain `-1`.
Existing simple-collection tests separately cover duplicate-reference tokens,
non-empty Reset replacement, source replacement, invalid-index Reset fallback,
and one post-notification enumeration.

These measurements do not claim zero CPU work or zero allocations; they only
establish the named scheduler, layout, invalidation, materialization, and
collision-version invariants in this deterministic scenario.

## Commands and raw results

- [`narrow-command.txt`](narrow-command.txt) built the current Release Core test
  project with `-p:DefaultItemExcludesInProjectFolder=artifacts/**` and ran the
  two changed tests: **2 passed, 0 failed, 0 skipped**. Raw
  [`narrow.log`](narrow.log), [`stage3-sceneitems-narrow.trx`](stage3-sceneitems-narrow.trx).
- [`owned-command.txt`](owned-command.txt) reused the current Release binaries
  with `--no-build --no-restore` and ran both owned SceneItems test classes:
  **43 passed, 0 failed, 0 skipped**. Raw [`owned.log`](owned.log),
  [`stage3-sceneitems-owned.trx`](stage3-sceneitems-owned.trx).
- `git diff --check -- tests/Cerneala.Tests/Controls/SceneItems2DIncrementalContractTests.cs`
  exited 0. Git reported only its usual LF-to-CRLF working-copy warning.

SHA-256 of the tested incremental test source:
`819DAFDC33BADC0F05BAFAF1C86C1364975B91E8DD670621B12E180FFC251C0F`.
Raw narrow log/TRX hashes:
`60963559D979232BA9CAA2967041DC68A248172DF39AA119AD7AF78760C21092` /
`30D082376572C61C931CF532FBB18AA5BAB535761AFB400CD309D3B4E421E243`.
Raw owned log/TRX hashes:
`79F7DFECA336F3DBF1D9DE0F80BE7A9D3AE3C0B81825A4DB866DDE06F4729485` /
`7C13E6E43ADBF7C518C88787C89A0451CFC0CDF74ECD026592CFF18D86323ED9`.
