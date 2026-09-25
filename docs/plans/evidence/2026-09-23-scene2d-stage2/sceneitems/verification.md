# Stage 2 SceneItems2D and Tetris focused verification

All commands ran from the repository root on 2026-09-24, in Release, with
`-p:DefaultItemExcludesInProjectFolder=artifacts/**`. The adjacent `.log` and
`.trx` files are the raw command and per-test evidence. No build or test job
remained running at handoff.

| Run | Command distinction | Result |
| --- | --- | --- |
| `sceneitems-simple-first` | `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~SceneItems2DSimpleCollectionContractTests` | 24 passed, 1 failed: the intended null-template `InvalidOperationException` was wrapped twice by existing `SceneNode2D.AttachSimulationContext` lifecycle aggregation. |
| `sceneitems-null-correction` | Same project, `--no-restore`, filtered to `NullWithoutNullCapableTemplateFailsWithoutNullDereference` | 1 passed. The corrected assertion requires exactly one leaf error, of type `InvalidOperationException`, with the semantic template token; it does not accept unrelated aggregate leaves. |
| `sceneitems-simple-green` | Same project, `--no-build --no-restore`, complete new collection class | 25 passed. |
| `sceneitems-legacy-render-first` | Same project, `--no-build --no-restore`, filters `SceneItems2DIncrementalContractTests`, `RenderSurface2DSceneTests`, and `RenderSurface2DSceneFoundationContractTests` | 44 passed, 5 failed. All five failures measured `ImageResourceCache.ResidentCount` 1 after offscreen record where contract expected 0: one offscreen sprite case and two cases each for pointer miss and collider-outside-visual. |
| `tetris-game-first` | `dotnet test Tetrisish/Tests/Tetris.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Cerneala.Tetris.Tests.TetrisGameTests` | 31 passed. This preceded the later presentation-only SceneItems cache fix; do not call it an integrated final-state suite result. |
| `offscreen-cache-red` | Core test project, `--no-restore`, filtered to `OffscreenRealizedSpriteReleasesItsImageAndReloadsOnReturn` after permanent phase/repetition assertions were added | 1 failed at the first offscreen `RecordFrame`: expected resident count 0, actual 1. The earlier assertion that the prior recorded frame still owns the image immediately after pan (count 1) passed. |
| `offscreen-cache-green-focused` | Core test project, `--no-restore`, filters the three cache-retirement methods (five expanded cases) | 5 passed after the local `SceneItems2D.CheckPresentation` false-intersection branch called `ReleaseRenderCaches()`. |
| `sceneitems-owned-green` | Core test project, `--no-build --no-restore`, filters new collection, incremental, and the two RenderSurface scene classes | 74 passed. |

Each command also supplied `--logger 'trx;LogFileName=<run>.trx' --results-directory docs/plans/evidence/2026-09-23-scene2d-stage2/sceneitems` and piped stdout/stderr to its matching `.log` file. All reported pass/fail counts came from those raw outputs.

The source trace for the cache failure is specific: `RenderSurface2D.UpdateRenderTime` refreshes spatial interest, then checks presentation. `SceneItems2D.UpdateSpatialInterest` retires offscreen node caches, but `SceneItems2D.CheckPresentation` queried sprite bounds through `GetLocalBounds()` / `ResolveSource(ResidentOnly)` and then skipped an offscreen child without releasing the resident image lease it could reacquire. The false-intersection branch now retires that child's render caches, consistent with this control's existing `Record` and spatial-interest branches. The refined test checks the legitimate prior-frame lease before the offscreen record, then zero residents and unchanged `loader.Loaded == 1` over 16 repeated offscreen tick/record cycles. It does **not** measure CPU time, allocations, or every possible image-cache reference path.

`git diff --check` on this worker's source/test files exited 0 at handoff. Current SHA-256 inputs at the last owned run:

| File | SHA-256 |
| --- | --- |
| `UI/Controls/SceneItems2D.cs` | `51BA5D5A60748A580F11C8A66CA441FDD329E98CEBE4A420C24C733DEEED4B1C` |
| `tests/Cerneala.Tests/Controls/SceneItems2DSimpleCollectionContractTests.cs` | `7729EC705F60236132FBCA8DE909B4C4BB7DDF4E34F79C45BBD092980189F786` |
| `tests/Cerneala.Tests/Controls/SceneItems2DIncrementalContractTests.cs` | `38A65ADB9702BDB956D7C2ED72A0D80F06D47AA921A8B7A3F4239538393A63F9` |
| `tests/Cerneala.Tests/Controls/RenderSurface2DSceneTests.cs` | `91C20AD66ACFFD243A960E7B95378A4568A96DCB092B2C9F2C24F2774803C452` |
| `tests/Cerneala.Tests/Controls/RenderSurface2DSceneFoundationContractTests.cs` | `FD379BD5710B1EE92BE3439A189176780C126871995D84C6930E7E24F458AB2D` |
| `Tetrisish/TetrisSceneModel.cs` | `690FB5B0388CDC184F7C467A359941D88BE52031F715469C731E89C409CD90FE` |
| `Tetrisish/Tests/TetrisGameTests.cs` | `BE91D4E953A2E12E32B72E9E71F0BB515D2765784AF0D79A7ECF72B697A1E8B4` |

The Stage 2 gate remains open: broader affected suites, API compatibility,
canonical documentation, native/rendering checks where applicable, and the
independent integrated audit are not established by these focused results.

## Coordinated optional-warm-preparation repair

The existing permanent
`ScenePresentationTests.PublicationDuringRecordingDiscardsEvenTheAlreadyRecordedScenePrefix`
was RED in integration's `../integration/shared-scene-focused.log/.trx` with a
`NullReferenceException` at `RenderSurface2DFrame.Complete:407`. During the
same `RecordFrame`, stale presentation caused `DiscardOptionalPreparation` to
complete queued optional requests with `prepare: false`, clear—but retain—the
warm-request list, and null the preparation surface. `Complete` then treated
the non-null **empty** list as pending work and dereferenced that null surface.

`RenderSurface2DFrame.Complete` now dispatches warm preparation only if the
retained list has `Count > 0`. Discard still runs each request's
`Complete(0, prepare: false)` path, and the list/capacity remain available
within the frame. No `RenderSurface2D.RecordFrame`, TileMap, or shared lifecycle
behavior was changed by this repair. SHA-256 of
`UI/Controls/RenderSurface2DFrame.cs` after the one-line edit:
`F6B852C40B455BCF51FC06AF1D23DEBC642CE127E9EEF639355AD4C8CB0CADCF`.

After scene-regression and Playground fixture writers had finished their
separate approved edits, one coherent Release build ran with
`--no-restore -p:DefaultItemExcludesInProjectFolder=artifacts/**`:

| Run | Filter / command distinction | Result |
| --- | --- | --- |
| `frame-discard-green-narrow` | `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~Cerneala.Tests.Controls.ScenePresentationTests.PublicationDuringRecordingDiscardsEvenTheAlreadyRecordedScenePrefix` | 1 passed, 0 failed. |
| `frame-discard-shared-classes` | Same project/current binaries with `--no-build --no-restore`, filter `ScenePresentationTests|SceneSimulationContext2DTests|ScenePrismStreamingTests|SceneWorldShowcaseTests` | 153 passed, 0 failed. |

Both commands supplied the same artifact exclusion, TRX logger, result
directory, and matching raw `.log` output as the earlier runs. This verifies
the NRE repair and the named affected classes, **not** the broader Stage 2
full-suite/API/native gates.

## Requested source snapshot versus committed realization

The independent Stage 2 audit found that an attached `SceneItems2D` could
commit the wrong model after a preflight failure: A=`[1]` was realized with an
integer template, assigning B=`["b"]` enumerated B but failed for lack of a
string template, and adding the template rebuilt from A's old `snapshot`.
`Commit` then labelled A as B and cleared collision readiness. This was not a
property rollback; `ItemsSource` had already become B under the selected
contract.

Three permanent tests were added to
`SceneItems2DSimpleCollectionContractTests.cs` **before** the production fix.
`template-recovery-red.log/.trx` is the current-source RED: 0 passed, 3
failed, all for the intended stale publication. The one-shot B case expected
model `"b"` but observed `1`. After B enumeration failed, a template edit
replaced A's node instead of retaining the unready committed tree; the same
false-ready path occurred when an explicit `Refresh()` enumeration failed for
the already assigned source.

The production invariant is now explicit: `snapshot` caches the latest
**complete source enumeration** and can be newer than `realized`, which tracks
committed node membership. A successfully enumerated B is cached before
template preflight, so a template edit can recover B without consuming a
one-shot enumerator twice. An enumeration failure invalidates the cache; a
template-only edit cannot retry enumeration or relabel committed A as B, and
the prior readiness error remains until the selected explicit retry path
(`Refresh()`, tested here) obtains a complete B snapshot. Reattach still
re-enumerates when the current source snapshot is invalid. No public API was
added.

| Run | Command distinction | Result |
| --- | --- | --- |
| `template-recovery-red` | `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~SceneItems2DSimpleCollectionContractTests.TemplateEditAfterFailed` before source fix | 0 passed, 3 failed for the intended stale-source publication. |
| `template-recovery-green-focused` | Same build/filter after source fix | 3 passed, 0 failed. |
| `template-recovery-owned-green` | Current binaries, `--no-build --no-restore`, new collection + incremental + two RenderSurface scene classes | 77 passed, 0 failed. |
| `template-recovery-scene-green` | Current binaries, `--no-build --no-restore`, ScenePresentation + SceneSimulationContext2D + ScenePrismStreaming + SceneWorldShowcase classes | 153 passed, 0 failed. |

All four supplied `-p:DefaultItemExcludesInProjectFolder=artifacts/**`, a TRX
logger, this evidence directory as results directory, and a matching raw
`.log` file. The post-fix SHA-256 inputs are
`E5E1D66569C7ABE316F32983AF4C0E134425C0CF859B1D03FF770F95394FF85C`
for `UI/Controls/SceneItems2D.cs` and
`A2580B1840F10F88E9F03E8C1B33132D4DEC9F4662B0F66C8076A42774042BAA`
for the new collection contract test. Scoped `git diff --check` exited 0.
These focused runs do not establish the broader or platform Stage 2 gate.

## Reentrant template edit of a one-shot source

A directly implicated structural callback path was checked separately. After
`ItemsSource` had been fully enumerated once, `LogicalChildren.Insert` invoked
its synchronous `Changed` callback during `SceneItems2D.Commit`. That callback
replaced the template. The old deferred-reset flag lost the request's origin:
the template edit requested `reenumerate: false`, but `ApplyDeferredChanges`
always retried with `reenumerate: true`. The permanent one-shot regression
failed with `One-shot source was enumerated again` at that exact stack path.

`SceneItems2D` now retains whether any deferred request actually requires a
new enumeration. A template-only edit reuses the complete source snapshot;
reentrant source assignment, collection notification, or explicit `Refresh()`
still asks for enumeration. Multiple deferred requests combine with logical
OR so a later template edit cannot erase an earlier request to re-enumerate.
The adjacent explicit-`Refresh()` callback test was already GREEN before the
fix and remains GREEN afterward.

| Run | Command distinction | Result |
| --- | --- | --- |
| `reentrant-template-red` | Release build/test of the two named reentrant template/Refresh regressions before source change | 1 passed (explicit Refresh), 1 failed (one-shot source enumerated twice). |
| `reentrant-template-green-focused` | Same filter after owner fix, Release rebuild | 2 passed, 0 failed. |
| `reentrant-template-owned-green` | Current binaries, `--no-build --no-restore`, new collection + incremental + two RenderSurface scene classes | 79 passed, 0 failed. |

All three used the Core test project, the artifact exclusion and TRX/result
directory described above, with matching raw `.log` and `.trx` files. Current
SHA-256: `SceneItems2D.cs`
`D4E5E394BDEC10E5693FB381AAFBD8CE7ABA85CBD79DB63CB64509E6314BF1AE`;
`SceneItems2DSimpleCollectionContractTests.cs`
`DE9E07DE5587429D682D2D81B539C4DD6AC84DC6484CC59F47D2A4BD6EB8A3C4`.
Scoped `git diff --check` exited 0. This does not replace integration's
current-state affected-suite or independent-audit gate.
