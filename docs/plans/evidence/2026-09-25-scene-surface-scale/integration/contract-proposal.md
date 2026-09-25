# Scene Village scale and zoom — proposal, not approved

This is an architecture proposal, **not an implemented contract**. No production code, tests, or builds were changed or run for this feature. The source facts below come from the current worktree. The Windows SDL native result from the preceding task (6,376 passed, six skipped) is historical, not verification of this proposal.

## Current behavior that constrains the design

- `RenderSurface2D` has nullable `ViewBox` (default `null`) and `Stretch` (default `Fill`). With no ViewBox, scene recording uses an identity scene-to-target-pixel transform: scene units are target pixels, not root DIPs. With a ViewBox, the selected world rectangle is transformed into the target according to Stretch. Input conversion uses the inverse of that transform. See `UI/Controls/RenderSurface2D.cs:45–61,251–263,518–597` and `docs-site/documentation/classes/Cerneala.UI.Controls.RenderSurface2D.md:181–185`.
- The offscreen target width is `P = max(1, ceil(L × D))`, where `L` is arranged width in DIPs and `D` is root/window DPI scale. The SDL backend records into that full-resolution pixel target, then composites the complete texture over the surface's logical rectangle. See `Drawing/RenderSurface2DGeometry.cs:3–8` and `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingBackend.cs:1500–1588`. The current final image composite is linear. An explicit lower-density target would be enlarged by this path, contrary to the requested full-resolution constraint.
- For `Stretch=Fill` and explicit ViewBox width `B`, scene-to-target scale is `P/B` and target-to-layout scale is `L/P`; the product is `L/B`. The chosen ViewBox already owns world coverage and presentation scale. With no ViewBox, one scene unit remains one target pixel before final compositing. A new Core `ContentScale` property cannot independently change visible scene scale while simultaneously retaining the current null-ViewBox mapping, a fixed explicit ViewBox, and the full-resolution target. A property that merely supplies metadata to an app camera would be misleading on generic `RenderSurface2D`.
- Prism style/presentation pixel scale currently follows root DPI independently of the scene ViewBox (`UI/Rendering/DrawCommandListBuilder.cs:192–214`). An app-camera-only scale policy does not redefine Prism style distances.

## Recommended narrow contract, conditional on user approval

Keep `RenderSurface2D`, its public API, target size, null-ViewBox identity, explicit-ViewBox semantics, Stretch behavior, input inverse, and default DPI behavior unchanged. Put the requested policy in the **Village app camera**, which already owns its ViewBox. Do **not** add a metadata-only `RenderSurface2D.ContentScale` or downsample/re-enlarge its target.

Proposed app API (names/types subject to approval):

```csharp
public float Zoom { get; set; } = 1f;           // VillageGameSurface; finite and > 0
public float? ContentScale { get; set; }         // VillageGameSurface; null inherits root DPI, finite and > 0 when set
```

The application composition root sets its Village surface `ContentScale = 1f`; other surfaces and ordinary UI continue with their existing root-DPI behavior. No zoom gestures, buttons, wheel mapping, or new framework camera are implied. Property changes recompute the camera ViewBox; the implementation must preserve normal render/input/spatial invalidation rather than bypassing it.

For each axis, let `P = max(1, ceil(L × D))`, `S = ContentScale ?? D`, and `Z = Zoom`. The proposed camera chooses `B = clamp(P / (S × Z), 1, WorldSize)` and follows/clamps the player inside the unchanged world bounds. Before world-bound clamping, `P/B = S × Z` target pixels per world unit. Thus an app-configured `S=1, Z=1` requests native-size world rendering at DPI 1, 1.25, and 2 **without reducing P**; inherited `S=D` retains DPI-relative app content sizing. Root and SDL fractional origin/ceiling behavior still require native pixel evidence; this algebra alone does not prove exact pixel parity or absence of seams. An explicitly selected Core ViewBox remains the selected world area; this policy only computes the Village-owned ViewBox.

This is **not** equivalent to the requested generic per-`RenderSurface2D` content-scale API. If generic Core behavior is mandatory, the user must choose which current invariant may change: null-ViewBox pixel coordinates, fixed-ViewBox world coverage, full-resolution target/composite behavior, or the meaning of scale. We should not implement an inert Core property and call the contract fulfilled.

## Native Village sizing and compatibility scope

- Make town art 16-source-pixel cells occupy 16 world units and keep player/stress actors 32-source-pixel frames at 32 world units. Separate `TileSize=16` from a character size of 32 rather than deriving both from one constant. Current coupling is in `Playground/Cerneala.SceneVillage/VillageLayout.cs:7–9` and `VillageArt.cs:9–104`.
- Preserve `WorldSize=4096`, player speed 200 world units/s, stress pitch 32 and the existing distribution/waypoint coverage. Fill the **same world-space ground/path area** with more 16-unit tiles; changing only TileSize would halve the scene footprint. Keep authored site coordinates where geometry permits. Derive edge clamping from the actual 32-unit player size, not the new tile size. See `VillageGameSurface.cs:126–181` and `VillageLayout.cs:15–79`.
- Scale house and composite-tree visual geometry and their owned obstacle bounds coherently with source art. The current tree obstacle (`VillageGameSurface.cs:194–215`) would lie below a 16×32 tree if its old 14×10 offset were left untouched. Exact tree/site placement and collider coordinates must be established with source-cell and native interaction tests, not guessed from the old 32-unit artwork scale. The original Tiny Town PNG is not proposed for modification.
- Existing renderer conformance fixtures that intentionally render 16-source-pixel tiles as 32 world units or use a 2.5-pixel/world character view are independent test scenarios; do not mechanically rewrite them as Village app defaults.

Likely implementation files, after approval: `Playground/Cerneala.SceneVillage/VillageCamera.cs`, `VillageGameSurface.cs`, `VillageLayout.cs`, `VillageArt.cs`, composition-root Village configuration, Village README, and focused Village model/native tests. No Core production file or canonical Core API page should change under the recommended app-only contract. If the user chooses a real public Core API instead, its semantics and canonical `docs-site/documentation/classes/` page/manifest require a separate approved design.

## RED and verification plan, not yet run

First add permanent failing tests for app Zoom/scale validation, inherited versus explicit scale, unchanged Core null/explicit ViewBox behavior, native 16/32 pixel sizing at DPI 1/1.25/2, Stretch/input/world conversion, camera following and bounds, obstacle interactions, stress distribution, and renderer/Prism parity. Confirm failures are contract failures, then implement. After focused GREEN, run the original native window path with Servo input and app-owned `Window.SaveScreenshot`, affected Core/Village/SDL suites, canonical docs/manifest gate if applicable, API comparison, and the full Windows SDL solution verification. Measure target dimensions and pixel diffs; do not infer visual correctness from a green build. Human interaction, GPU-time, and performance claims remain unverified until separately measured.

**Decision needed:** Is an app-owned `VillageGameSurface.Zoom` plus nullable app camera `ContentScale` acceptable in place of a generic `RenderSurface2D` scale property? If not, specify which existing Core invariant above may change. No production/test work should start on this ambiguity.
