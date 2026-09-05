# Stage 2 checkpoint: optional Tiled importer

Verified 2026-09-05. Stage 2 is complete; LDtk, overlay and native integration remain later-stage work.

## Ownership and behavior

`Cerneala.Scene2D.Importers` and its dedicated test project are in the solution. Core excludes the optional project from source, resources and `.crn` discovery. Stage 0 parser contracts moved out of the core test assembly; LDtk cases remain explicitly assigned to Stage 3. Parsers return core data, not UI objects or backend resources.

The Tiled 1.11 implementation covers the frozen finite/infinite, atlas, layer/group/object, primitive-property, flip, collider and sparse-promotion subset. Unknown fields are rejected at all ten Tiled inventory scopes. Kind-specific layer fields cannot be silently discarded. Declared geometry uses core constructors and the shared document validator; no invalid/unsupported partial document is returned.

External files and file properties are checked under the configured local root. Tests exercise normalized separators and internal `..`, missing assets, rooted/network/alternate-stream paths, source cycles, external file budgets, explicit drive roots, and a real Windows junction pointing outside the root. The junction is rejected before opening its target. Temporary test files/directories are removed non-recursively. This is a stable-tree containment policy, not an OS-handle sandbox against concurrent filesystem replacement.

Strict bounded compression required an additional optional-project-only dependency; see [compression-decision.md](compression-decision.md). DEFLATE completion and compressed-input consumption are explicit. Gzip concatenated members, optional headers and header checksums are tested; malformed/truncated framing, checksums and trailing garbage fail. Decompressed under/overflow remains a separate stable dimension diagnostic.

## RED evidence and corrections

- `scene-import-stage2-migrated-red.trx`: 24 original Tiled contracts failed because importer options/parser types were absent.
- `scene-import-stage2-hostile-red.trx`: 27 additional hostile-input cases failed for the same intended missing API.
- `scene-import-stage2-coverage-red.trx`: four wrong-layer-kind silent losses and two truncated-compression acceptances reproduced; six other coverage cases passed.
- `scene-import-stage2-final-hostile-red.trx`: five silent structure/convention acceptances, three unhandled external-root JSON-kind exceptions, and one allocation-bound failure reproduced. Three compression corpus cases were already green.
- `scene-import-stage2-drive-root-red.trx`: a caller-selected drive root was incorrectly rejected by a doubled separator in the containment prefix.

The 32-object × 4,096-point tile-collider fixture allocated **482,011,224 bytes** before late rejection. Early per-owner/aggregate descriptor checks reduced it to **154,215,776 bytes**, still failing the unchanged 100,000,000-byte gate (`scene-import-stage2-final-hostile-green.trx` is this intermediate **8 pass / 1 fail** run, despite its provisional filename). Reusing duplicate-member name sets per depth and constructing diagnostic paths only on failure closed the same bound; the final gate passes. No final exact allocation number or frame-time percentile is claimed.

## Final verification

| Evidence | Result |
| --- | --- |
| `scene-import-stage2-gate-final.trx` | **76/76** Tiled/hostile/semantic/containment tests |
| `scene-import-stage2-core-regressions.trx` | **175/175** affected core scene/tilemap/collision/animation and original validator/ownership tests |
| `scene-import-stage2-docs.trx` | **1/1** official API manifest test |
| Last code build through `dotnet test` | Successful, no reported compiler warnings/errors |
| Final C#/project index | 3,902 documents; 11 existing warnings |
| `dotnet list ... package --vulnerable --include-transitive` | No vulnerabilities reported by configured NuGet sources at verification time |

Canonical pages for importer, options and result are in `docs-site/documentation/classes/` and registered in the manifest. They document the exact subset, limits, root trust assumptions, diagnostics and composition boundary.

Reproduction commands (repository root):

```powershell
dotnet test tests/Cerneala.Tests.Scene2DImporters/Cerneala.Tests.Scene2DImporters.csproj --filter 'SceneImportStage!=3'
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter '(SceneImportStage=1|FullyQualifiedName~TileMap2D|FullyQualifiedName~Collision|FullyQualifiedName~Scene2D|FullyQualifiedName~SceneNode2D|FullyQualifiedName~SceneItems2D|FullyQualifiedName~Sprite2D|FullyQualifiedName~SpriteAnimation|FullyQualifiedName~CoreValidator|FullyQualifiedName~SegmentColliderIsTwoSided|FullyQualifiedName~CoreSceneDocument|FullyQualifiedName~ProgrammaticModelsUseTheSameAtlasValidator|FullyQualifiedName~CoreDataAndValidationTypesNeverBecomeUiObjectsOrOwnEffects)&FullyQualifiedName!~DebugOverlayDoesNotChangeCollisionOrRealPointerRouting' -m:1
dotnet test tests/Cerneala.Tests.VisualStudio/Cerneala.Tests.VisualStudio.csproj --filter ApiDocumentationManifestIsValidAndReferencesExistingFiles -m:1
```

The named future overlay test and Stage 3 LDtk cases are not claimed green. Full solution, strict API compatibility and both-backend integration/conformance remain mandatory later gates. No native screenshot or human visual validation was performed in this parser stage.
