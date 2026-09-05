# Stage 3 checkpoint: LDtk importer

Verified 2026-09-05. Stage 3 is complete; overlay, native demonstration and final repository gates remain later-stage work.

## Contract and ownership

`LdtkScene2DImporter` implements the frozen JSON 1.5.3 semantic subset in the optional assembly. It reads inline/separate levels, legacy/multi-world containers and all four approved layer types. Source IDs, world placement, total layer offsets, atlas source coordinates, H/V flips, primitive fields, visual metadata and sparse promotion addresses survive normalization. IntGrid remains data and AutoLayer uses baked output. Shared role/bitset/promotion/collider construction moved from the Tiled parser into internal `ImportConventions`; both importers still use core constructors and document validation for geometry and structural invariants.

Closed-field tests cover every one of the 16 LDtk inventory scopes. Additional tests cover external references, missing files, cycles/path escape, duplicate/conflicting UID/IID, source/atlas/grid mismatches, unsupported alpha/stacking/unsnapped placement, fields, collision conventions, promotions and resource budgets. Malformed external JSON objects, arrays, null and truncated payloads fail without leaking parser exceptions or publishing partial data. Existing shared Tiled containment/compression tests remain green.

The original schema-validated Stage 0 inline/separate fixtures produce their independent semantic goldens. Additional JsonNode entity/hostile fixtures test importer semantics; they are not claimed to be complete editor exports or independently schema-validated files.

## Explicit representation refinements

- Core layer identity is the layer definition UID as an invariant string; per-level instance IID stays metadata. Global tiles are assigned from one in project atlas order and local tile order.
- An empty level with no layers uses a one-pixel grid with its declared finite pixel bounds. Non-grid-aligned nonempty level dimensions are unsupported rather than rounded or rescaled.
- The [pinned LDtk entity source](https://github.com/deepnight/ldtk/blob/v1.5.3/src/electron.renderer/data/inst/EntityInstance.hx) exports the anchor position and normalized pivot. Core origin is `px - size * pivot`; continuous geometry is not rounded to the editor's pixel-snapped left/top calculation. Source anchor remains `$SourcePx`. Tests prove pivot, geometry, layer offsets and core validation.
- External payload identity, placement and dimensions must agree with the project wrapper. Wrapper fields are checked and preserved separately under `$ProjectReference`; the payload supplies runtime level fields. IID comparison uses GUID identity, not case-sensitive spelling.
- Optional tile metadata on each field has its own `$FieldMetadata[fieldName]` bag. Entity definitions retain field UID/name/kind/nullability under `$Definition.$FieldDefinitions`.

These decisions preserve the approved core-data boundary; none adds per-cell UI, image decoding, collision inference, source-generator parsing or effect ownership to the importer.

## RED evidence

| Artifact | Observed failure |
| --- | --- |
| `scene-import-stage3-original-red.trx` | Three original LDtk cases fail because the parser type is absent. |
| `scene-import-stage3-valid-expanded-red.trx` | 39 additional cases fail for the same absent parser, after fixing fixture-only JsonNode parenting errors. |
| `scene-import-stage3-audit-red.trx` | Five valid regressions: unknown external wrapper fields and unsupported wrapper field kind accepted, case-aliased duplicate IID accepted, mandatory tile alpha missing accepted, field metadata owner lost. The other 44 cases pass. |

The initial expanded run with fixture exceptions is not used as valid RED evidence. A later temporary test compilation error (`Color.Parse`, which is not an API) was corrected to the existing `Color.TryParse` before the archived audit RED run. No production change was justified by a broken test fixture.

## Final verification

| Artifact | Result |
| --- | --- |
| `scene-import-stage3-gate.trx` | **150/150** optional importer tests: 76 Tiled + 74 LDtk, no skips. |
| `scene-import-stage3-core.trx` | **175/175** affected core scene/tilemap/collision/animation and original validator/ownership contracts. |
| `scene-import-stage3-docs.trx` | **1/1** official API documentation manifest test. |
| Final source/project index | 3,921 documents; 11 existing warnings; successful incremental index. |

Commands (repository root):

```powershell
dotnet test tests/Cerneala.Tests.Scene2DImporters/Cerneala.Tests.Scene2DImporters.csproj -m:1
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter '(SceneImportStage=1|FullyQualifiedName~TileMap2D|FullyQualifiedName~Collision|FullyQualifiedName~Scene2D|FullyQualifiedName~SceneNode2D|FullyQualifiedName~SceneItems2D|FullyQualifiedName~Sprite2D|FullyQualifiedName~SpriteAnimation|FullyQualifiedName~CoreValidator|FullyQualifiedName~SegmentColliderIsTwoSided|FullyQualifiedName~CoreSceneDocument|FullyQualifiedName~ProgrammaticModelsUseTheSameAtlasValidator|FullyQualifiedName~CoreDataAndValidationTypesNeverBecomeUiObjectsOrOwnEffects)&FullyQualifiedName!~DebugOverlayDoesNotChangeCollisionOrRealPointerRouting' -m:1
dotnet test tests/Cerneala.Tests.VisualStudio/Cerneala.Tests.VisualStudio.csproj --filter ApiDocumentationManifestIsValidAndReferencesExistingFiles -m:1
```

Canonical importer documentation and manifest are synchronized. No native rendering capture or human interaction was required or claimed in this data-parser stage. Both-backend conformance, strict API compatibility and the final full solution suite remain mandatory later gates.
