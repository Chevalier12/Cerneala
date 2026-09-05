# Stage 5 — integrated Scene World

Automated SDL conformance is green. Human visual validation was requested on
2026-09-05 and is still awaiting the user's confirmation; it is not claimed here.

## Current evidence

- `world-assets-green.trx`: the actual original Playground Tiled/LDtk assets
  import to equivalent cells and gameplay entities. Tiled retains 64 sparse
  chunks; LDtk retains two finite tile-layer chunks. Source metadata/IDs and
  finite versus sparse bounds remain format-specific.
- `detective-tilemap-red.trx` / `detective-tilemap-green.trx`: root-owned,
  observational tilemap snapshots, ownership rejection, and 10,000 reads with
  zero measured managed allocation after 256 warmup reads; 2 tests pass.
- `collection-bindings-red.trx`: three valid reference-assignable collection
  bindings failed. The generator required exact type equality; the language
  symbol adapter also discarded array types. `world-bindings-green.trx` now
  passes all 6 binding regressions, including the two init-only cases below.
- `init-only-bindings-red.trx`: OneWay emitted an illegal init-only setter and
  TwoWay failed to diagnose a non-writable endpoint. Both symbol adapters now
  classify init-only accessors as non-writable.
- `sourcegen-suite.trx`: 515 tests pass after these generator/language fixes.
- `nested-prism-batch-red.trx` / `nested-prism-batch-green.trx`: two reproductions
  (window and RenderSurface) of ordinary geometry after a nested Prism
  presentation. Presentation flush ended the batch; the enclosing command
  range now reestablishes its target before continuing. Both tests pass and
  assert the expected indexed draw count, including the following sibling.
- `sdl-suite.trx`: 121 tests pass, 5 opt-in native tests skipped. This is not a
  claim that the skipped native gates ran.
- `native-attempt1`: configuration/setup failure caused by building the SDL
  configuration with stale MonoGame restore assets. Building with restore and
  `-p:CernealaDesktopBackend=SDL3` resolved the missing `SDL3-CS` assembly.
- `native-attempt2`: real SDL failure `SDL_GPU batching requires an active
  target`, reproduced and fixed by the permanent tests above.
- `native-attempt3/01-closed.png`: application-owned capture after the batch
  fix. World loads with 40/64 visible chunks, 35 reused batches and one promoted
  door. The subsequent `Servo.ClickAsync(ById("world-player"))` fails because
  Servo's semantic projection traverses only visual children. Scene nodes are
  logical input-subtree children and must not be given fake arranged bounds.

## Reproduction entry point

Build `Playground/Cerneala.Playground/Cerneala.Playground.csproj` with
`-p:CernealaDesktopBackend=SDL3` (restore when switching configurations).
Set `CERNEALA_SCENE_WORLD_CAPTURE` to an explicit output directory and run the
generated Playground executable. The opt-in scenario uses Servo for all
interactions, captures through its `Window.SaveScreenshot` path, writes failure
details and closes the window. `CERNEALA_CONFORMANCE_CAPTURE=1` stabilizes the
existing frame-header text; it does not control gameplay state.

## Resolved integration defects

- `servo-scene-red.trx` / `servo-scene-green.trx`: three independent RED/GREEN
  tests for scene query, user-like click/focus/key routing and target capture.
  Servo's projection now includes the existing logical input subtree. Query,
  action and target capture share input bounds, with no fake layout bounds.
  Action ancestry comes from the unified input route, not only visual parents.
  Accessibility projection is deliberately unchanged.
- `scene-origin-dpi-valid-red.trx`: two RED no-ViewBox cases at DPI 1/1.25,
  alongside two already-green ViewBox cases. RenderSurface now maps its local
  physical pixels through layout/DPI and its actual origin consistently.
  `servo-scene-origin-final-green.trx`: all seven Servo scene tests pass.
  Earlier fixture runs with incorrect rounding expectations are not RED proof.
- `world-markup-matrix.trx` / `world-markup-matrix-green.trx`: the real compiled
  sample consumed MouseDown in its gameplay handler before the declared Motion
  trigger. Removing that unnecessary Handled assignment fixes the example;
  routed-event semantics were not changed. The permanent test now proves the
  whole effects matrix, including intermediate samples at 75/90 ms, collider
  OffsetX animation, seven nested visual Prism scopes (eight with debug),
  one image-cache load, Walk/Attack source frames/flips, door animation,
  collision/index invariance and incremental NPC identity.
- `native-attempt4` exposed the origin/DPI defect; `native-attempt5` completed
  the initial scenario. `native-attempt6` was a probe-only wrong backend type
  name, and `native-final` a probe-only reference-equality comparison of newly
  allocated CollisionHit2D results. The final probe checks the real assembly
  backend attribute and all contact values/owner identities instead.

## Final native evidence

`native-final-green/results.json`: **success**, actual backend
`Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`, 361 observed frames.
Nine full application captures, nine player-target crops and nine matching
Servo/Detective snapshots are archived in that directory. All images came from
the Window-owned screenshot path. Fixed scenarios are reproducible; wall-clock
frame counts and transient animation phases are not claimed pixel-identical.

| Requirement | Integrated evidence |
| --- | --- |
| #1, #7–#9 | 64 chunks, 40 visible at home, 35 reused batches; pan changes visible chunks to 48 and reuses retained batches; Plant rebuilds exactly one batch and reuses 34 |
| #2 | Original atlas declared as ImageResource; compiled-world test measures one cache load |
| #3–#6 | Six imported collider entities plus player/NPC/door declarations; Servo selection and keyboard movement; closed door travel -32, open door travel -44 to back wall, fence travel +36 |
| #10 | NPC append realizes a second item and preserves the original node identity |
| #11–#12 | LayerThenY actor group, explicit tile layer and shared camera/group transforms; inherited foundation ordering tests remain green |
| #13 | Declarative Idle/Walk/Attack and Closed/Open clips; deterministic test verifies source frames/flips and finite completion |
| #14, #16 | Actual Tiled/LDtk assets import through the same core validator; 4,096 equal cells, equivalent gameplay entities; format-specific chunk/provenance differences retained |
| #15 | User-like Debug toggle, full capture, nonzero primitives when enabled and zero when off; same collision travel/contact and index mutation counts; player picking still routes correctly |
| Effects matrix | Scene/group, map, layer, promoted door, player sprite, NPC template sprite and debug overlay attach Aspect/Motion/Prism; collider attaches Aspect/Motion only; SceneItems containers remain undecorated |

## Verification

- `stage5-core-affected.trx`: 277 affected core/scene/collision/Servo tests pass.
- `stage5-language.trx`: 186 pass.
- `stage5-sourcegen.trx`: 515 pass in the final stage state.
- `stage5-sdl.trx`: 121 pass, 5 opt-in native tests skipped. The separate real
  SDL scenario above ran; the skipped tests are not relabeled as executed.
- `stage5-importers.trx`: 151 pass, including real sample-asset equivalence.
- `stage5-docs.trx`: canonical documentation manifest verification.
- Full solution and strict API compatibility belong to stage 6, not this gate.

For human review: build/run Playground on SDL, select **Scene World**, click the
cyan player and use arrows/Space; click the house door; try Pan/Home, Plant,
NPC +, Debug and Tiled/LDtk. Expected contacts and counts are listed above.
No human validation has been reported yet.
