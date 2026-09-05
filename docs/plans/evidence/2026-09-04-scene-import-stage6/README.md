# Stage 6 — documentation, API and final verification

The delivery target is SDL GPU, per the explicit user decision recorded in the
source plan. MonoGame-only failures remain visible, not silently skipped or
reclassified as passing. Human visual review is still awaiting confirmation.

## Documentation and API

The canonical `docs-site/documentation/classes/` pages cover all 21 new public
types (17 core types and four optional importer types), with corresponding
manifest entries. Existing tile flip/collider, scene, generator, Servo and
Detective pages describe the affected contracts. The documentation skill and its
API formula were applied; no API pages were added under `docs/documentation/`.

Tiled/LDtk pages publish the exact closed field matrices, versions, relative-path
and stable-root policy, atomic failures, budgets, diagnostics and non-goals.
Scene2DDocument and TilePromotion2D link the actual imported-world composition,
explain sparse declarative promotion and show the interactive door's real
Aspect/Motion/Prism markup. SceneItems2D effects stay on template nodes.

See [strict API report](api-compat.md): Release build has zero warnings/errors;
strict ApiCompat passes with exactly 17 reviewed new-type approvals, alongside
the separate historical approval files. The report explicitly documents the
limits of whole-type suppressions and reviews affected members independently.

## Restored-build regression

The first full solution run (`full-suite.log`) passed eight test projects but
failed to compile core tests with CS0118: `Scene2D` resolved to the optional
importer's parent namespace instead of `Cerneala.UI.Controls.Scene2D`.
The restored focused build reproduced this (`core-restored-compile.log`).

The isolated C# experiment (`namespace-experiment.log`) proves that an ordinary
using or global alias still loses that lookup, while an alias inside the test
namespace selects the intended type. Twenty-two affected test files now use
that namespace-local alias. No assertions, public namespace, production type or
project-reference visibility were changed to make the build pass.

## Current-state evidence reused

- [Stage 5](../2026-09-04-scene-import-stage5/README.md): 151 importer, 186
  Language, 515 SourceGen, 121 SDL tests pass; five SDL opt-in tests remain
  unexecuted. Separate real SDL conformance succeeds for all nine world scenarios
  (361 observed frames, Window-owned full and target captures, collision/input
  and retained-work measurements).
- [Stage 4](../2026-09-04-scene-import-stage4/README.md): twelve native SDL overlay
  captures, per-flag conformance, exact off restoration, unchanged picking and
  collision/index state, and measured CPU recording/allocation costs.
- The final namespace aliases modify test lookup only; they cannot change the
  already captured application/backend binaries or invalidate native results.
- `index.json`: refreshed after all namespace aliases; no command warnings or
  errors. No C# or project edits follow this index at this checkpoint.

## Final suite — 2026-09-05

```powershell
dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj --filter 'FullyQualifiedName~Scene|FullyQualifiedName~Servo|FullyQualifiedName~Collision|FullyQualifiedName~TileMap|FullyQualifiedName~SpriteAnimation' --logger 'trx;LogFileName=core-affected-green.trx' --results-directory docs/plans/evidence/2026-09-04-scene-import-stage6
dotnet test .\Cerneala.slnx --no-restore --logger trx --results-directory docs/plans/evidence/2026-09-04-scene-import-stage6/final-suite -v:minimal
git diff --check
```

The restored focused run passes all 277 tests. The complete solution run uses
the restored graph and rebuilds as needed; it does not use `--no-build`.
`final-suite.log` and all nine TRX files in `final-suite/` preserve the result:

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Core tests | 3482 | 2 | 2 |
| Scene2D importers | 151 | 0 | 0 |
| Language | 186 | 0 | 0 |
| SourceGen | 515 | 0 | 0 |
| LanguageServer | 40 | 0 | 0 |
| PreviewHost | 13 | 0 | 0 |
| VisualStudio | 47 | 0 | 0 |
| SDL GPU | 121 | 0 | 5 |
| Tetris | 29 | 0 | 0 |
| **Total** | **4584** | **2** | **7** |

The command exits **1**, not zero. Both failures are the already isolated,
explicitly waived MonoGame hardware MSAA cases of
`OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent`: content 1 measures
47/255 and content 2 measures 25/255, exactly the stage-4 observations. The
no-interleaved-content control passes. No common/core/importer/Servo/SDL failure
is excluded under this waiver, and neither failing assertion was changed.

The two core skips are opt-in cross-backend Drawing/Prism pixel conformance;
the five SDL skips are opt-in native tests. They are not claimed executed.
The mandatory scene-specific SDL native corpus was executed separately, as
linked above. No applicable optional importer test project is missing from the
solution; both the importer and its test project are explicitly included.

The final VisualStudio run includes a passing
`ApiDocumentationManifestIsValidAndReferencesExistingFiles` after the last
canonical-page edit. `git diff --check` exits 0 under repository Git settings
(line-ending normalization warnings are archived separately). Temporary CSI
source and its empty directory were removed; no CSI process remains.

All applicable automated plan gates pass with the explicit MonoGame exception.
This does not claim a wholly green solution command or human visual validation.
