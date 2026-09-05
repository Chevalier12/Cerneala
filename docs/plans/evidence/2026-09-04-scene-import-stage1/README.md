# Stage 1: core validation and geometry contracts

Verified 2026-09-05. This checkpoint covers core validation, not the optional external parsers or debug overlay.

## Implemented ownership

- Immutable `Scene2DDocument` / level / asset / entity / promotion data, with shared constructor validation and `Scene2DModelValidator` diagnostics. No UI nodes, filesystem access, GPU handles or effect systems in these data types.
- Stable structural, identity, version, tile/atlas, collider, bitset, promotion, geometry and work-limit codes. `Scene2DDiagnosticCollector` is public for reuse by the optional parsers; omitted errors cannot turn the result into success. Retained entry count and text lengths are bounded.
- Core limits reject huge dimensions before enumeration, bound excessive/infinite enumerable tails, count repeated shared components, and cap collision adapter expansion before publication. Canonical limits: [validator](../../../../docs-site/documentation/classes/Cerneala.UI.Controls.Scene2DModelValidator.md).
- Global convexity checks reject self-intersecting star polygons; extreme cross products fail with annotated argument errors rather than arithmetic exceptions.
- Exact diagonal tile transforms for all eight combinations, shared by static batches, promoted sprites and tile collider placement. Affine descriptors preserve ellipses. Two-sided zero-thickness segments use the existing support-mapping collision machinery and degenerate ray path; no extrusion or circle approximation.
- Runtime atlas validation precedes all chunk command publication. A RED two-chunk case previously built one valid chunk before failing on the second source rectangle; it now builds zero. Validation is reused while the immutable model reference and exact resolved resource-key/dimension set remain unchanged. Unresolved runtime images retain deferred loading semantics; imported documents require atlas declarations.

## Reproductions and verification

The archived `*-red.trx` files record intended failures before their corresponding fixes:

| Area | RED outcome |
| --- | --- |
| Basic model validation | 9 failures |
| Document contract | 12 failures |
| Geometry extension | 8 failures, 4 existing passes |
| Budget enforcement | 2 failures |
| Publication safety | 4 failures |
| Shared public collector | 1 failure |
| Partial graphical cache | 1 failure |

Latest code build: `dotnet build Cerneala.csproj --no-restore -m:1`, zero warnings/errors. Tests were then compiled against that freshly built core with `-p:BuildProjectReferences=false`; subsequent runs used `--no-build --no-restore` against unchanged code.

- `scene-import-stage1-gate-green.trx`: **172/172**, covering Stage 1, tilemap caches/contracts, collision, scene, sprite and animation regressions. Includes the two original segment RED cases now GREEN.
- `scene-import-stage1-original-core.trx`: **3/3**, the original atlas validator and non-UI ownership RED cases now GREEN.
- `scene-import-stage1-docs.trx`: **1/1**, the official Visual Studio API documentation manifest test. Twelve new canonical API pages are registered; affected existing pages are synchronized.
- Index refreshed after the final C# patch: 3,874 documents; 11 existing warnings, not a zero-warning index.

Reproduction commands (repository root):

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --no-build --no-restore --filter '(SceneImportStage=1|FullyQualifiedName~TileMap2D|FullyQualifiedName~Collision|FullyQualifiedName~Scene2D|FullyQualifiedName~SceneNode2D|FullyQualifiedName~SceneItems2D|FullyQualifiedName~Sprite2D|FullyQualifiedName~SpriteAnimation|FullyQualifiedName~CoreValidator|FullyQualifiedName~SegmentColliderIsTwoSided|FullyQualifiedName~CoreSceneDocument)&FullyQualifiedName!~DebugOverlayDoesNotChangeCollisionOrRealPointerRouting' -m:1
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --no-build --no-restore --filter 'FullyQualifiedName~ProgrammaticModelsUseTheSameAtlasValidator|FullyQualifiedName~CoreDataAndValidationTypesNeverBecomeUiObjectsOrOwnEffects' -m:1
dotnet test tests/Cerneala.Tests.VisualStudio/Cerneala.Tests.VisualStudio.csproj --no-build --no-restore --filter ApiDocumentationManifestIsValidAndReferencesExistingFiles -m:1
```

The initial broad filter also matched the future overlay test via the word `Collision`; its only failure was the unchanged Stage 0 missing-overlay assertion. The corrected stage filter excludes that named future-stage test, not a current-stage regression. The optional parser tests remain RED until Stages 2/3; full solution verification belongs to Stage 6.

## Bounded hostile inputs and measurements

- Seed `0x51CE`: 256 extreme dimension cases, all controlled argument failures; latest run 5.657 ms for the matrix.
- Seed `0xC11`: 512 sets of 24 rectangles match an independent exhaustive overlap oracle, including negative coordinates and touching edges.
- 65,536 disjoint horizontal chunks: original quadratic constructor **81,375.558 ms**; final integer sweep **68.580 ms**. A dense active set of 65,536 vertical intervals also passes, **236.213 ms**. The bounded tail test fails before an extra iterator step can throw.
- 16,384 sparse promotions across 16,384 chunks: original repeated scans **4,745.121 ms**; final sparse-address validation **21.852 ms**. Stored order and identities remain unchanged.
- Geometry tests cover eight flips on a rectangular 20×10 tile, static/promoted drawing command UVs and collision transforms, affine ellipses, segment endpoints/collinearity/rotation/disjointness, circle and box continuous motion from both sides at displacement 1,000, and opposing contact normals.

Times are single Debug test samples, not BenchmarkDotNet results or percentile claims. No frame-time or zero-allocation claim is made here. The sweep uses the established disjoint-active-interval invariant, not floating-point geometry. Sparse promotion validation retains only requested coordinates and scans each requested layer once.

Core asset declarations reject non-normalized/rooted/network/URI/stream paths without opening files. Actual external-root containment, reparse points, truncated JSON and decompression budgets are parser responsibilities in Stages 2/3, as frozen in the Stage 0 ordering contract. They are not claimed verified by lexical core tests.

No native screenshots or human visual validation were performed in Stage 1. Command/UV evidence does not claim native pixel parity; both-backend conformance remains a required later gate.
