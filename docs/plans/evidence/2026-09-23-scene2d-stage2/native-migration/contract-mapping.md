# SDL native caller migration: Stage 2 contract mapping

This ledger covers only the six SDL test source files assigned to the native
caller-migration worker. It records what each old assertion meant and what the
Stage 0-approved Stage 2 contract can still assert. It is **not** a native test
result. Compile, opt-in native execution, and independent audit are pending.

| Test source | Old contract dependency | Replacement and preserved assertions |
| --- | --- | --- |
| `NativeScenePresentationTests.cs` | An arbitrary `SceneSpatialSource2D<object>` held a cold sprite lease, drove whole-surface Loading/Error, and released on catalog reset; a second source delivered the NPC. | The NPC is one eager `SceneItems2D` occurrence with an explicitly simulated own collider. A real internal `TileMap2D.FromSource` owns the cold chunk, pending load, failure, catalog reset, and lease release. The 32 Servo click cycles still assert whole-scene withholding, movement/collision during loading, exact screenshots, retry after error, shared atlas cache count, and release on reset. `TryGetRealizedNode`, item preparation, NPC acquire/release counts are removed because simple items have no such loader; logical-node identity and detach replace them. |
| `NativeSceneImagePresentationTests.cs` | A spatial source acquired the NPC once, refreshed metadata on movement, and released it at close. | One eager NPC occurrence preserves exact Prism-inverted atlas, borrowed sibling image, async image Loading/Error/Ready across 32 cycles, Servo movement and collision, offscreen image-cache retirement/disposal, stable node identity, and close-time detach. Only the obsolete item source acquire/release and ID lookup assertions disappear. The image loader remains asynchronous and independently counted. |
| `NativeImageLeaseTests.cs` — grid/camera test | Public `TileMap2D.Source` held a model adapter; spatial item catalog carried an offscreen simulated NPC. | Public `TileMap2D.FromModel` keeps the three independent grid chunks, warm diagnostics, atlas retirement/disposal, camera/Servo/collision loop, and exact pixel/load-count assertions. The NPC is eager with `Collider2D.IsSimulated` on its own collider; stable logical identity and detach replace spatial item ID/load/release assertions. |
| `NativeImageLeaseTests.cs` — two-item camera theory | A catalog selected the NPC offscreen and released/reacquired the static prop on each pan. | An ordinary two-value item collection and existing `ContentTemplate<string>` keep **both** nodes realized while offscreen. Assertions separate logical realization (`2`, same NPC) from drawing/image residency (`0` offscreen), preserving 32 cycles of Servo movement/collision, exact pixels, image GPU disposal/reload counts, root invalidation/capture-boundary variant, and close-time cache release. Per-item payload acquire/release counts are intentionally not carried over; the approved simple collection has no item payload leases. |
| `NativeImageLeaseTests.cs` — two-window test | Shared image resource lease lifetime only; no SceneItems spatial API. | Unchanged: both windows share one atlas cache, closing one window retains the image, and 32 unload/reload cycles retire the old GPU image and preserve pixels. |
| `NativeScenePrismStreamingTests.cs` | Per-camera spatial item and custom map chunk acquisition/release, with pointwise Prism pixel parity. | The item scene has two eager sprite occurrences; the private map source retains one chunk per near/far entry with real load/release accounting. Across all camera/filter/mode combinations, exact eager/items/map pixel parity, map one-tile draw culling, source release, and Servo input remain. Item count is `2`, not viewport-only `1`; arbitrary item streaming counters are removed. |
| `NativeScenePrismDomainTests.cs` | A nine-entry item source loaded only selected sprites, disposing nested Prism attachments through item leases; map chunks streamed individually. | Nine eager sprite occurrences keep their nested Prism attachments until fixture teardown. The private map source still has nine exact chunks and asserts far chunk `8` was not acquired. The test keeps full-domain versus cropped pixel equality across filters, styles, masks, density, nested effects, camera/mode/filter Servo paths, and map `DrawnTiles` culling. Item realization is `9` even when cropped, replacing the invalid viewport-only count. |
| `Prism/ScenePrismAllocationTests.cs` | The SceneItems fixture used a spatial source for one `SolidNode`; its presence made Prism surface allocation strict. | SceneItems now receives a normal one-node array. Current source still implements `ISceneSpatialParticipant2D`, so the strict allocation failure, no unfiltered fallback, recovery, budget/raster-extent errors, and ordinary direct-node fallback remain asserted without an obsolete public source/lease. |

The private map source is used only by framework-internal behavior fixtures; it
is not a proposed public escape hatch. `TileMap2D.FromModel` free placements
would coalesce these small placement sets into one internal chunk, so it cannot
preserve the per-entry map acquisition/culling/release assertions in the Prism
tests. Grid `FromModel` remains the public static-map path where the test already
uses authored grid chunks.

Pending gates: integrated SDL compile; focused non-native allocation tests;
Windows SDL native opt-in execution of these cases (including real screenshot
and image-cache assertions); independent audit. None is claimed green here.
