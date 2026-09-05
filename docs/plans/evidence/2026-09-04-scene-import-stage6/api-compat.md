# Import/debug validation — strict API audit

Date: 2026-09-05. Strict comparison: **PASS**, exit 0.

## Reproduction

```powershell
dotnet build Cerneala.Scene2D.Importers/Cerneala.Scene2D.Importers.csproj -c Release --no-restore
dotnet msbuild docs/plans/evidence/2026-09-04-scene-import-stage6/api-compat.proj -t:Compare -v:minimal
```

The Release build has zero warnings/errors. SDK 10.0.303's ApiCompat task runs
strict mode and parameter-name validation. Unnecessary suppressions are forbidden.
`api-compat-red.log` inventories the unsuppressed additions; `api-compat-green.log`
is the ordinary strict run, with suppression generation disabled.

The detached Servo baseline is preserved at commit
`fed724b954bc2823c4799db69c94b92e2790b2b5`. SHA-256 values:

| Assembly | SHA-256 |
| --- | --- |
| Baseline core Release | `2D20937BFC0783CEC52CB8F8328C4F27625E56C99A808C736C28FD822B8297F1` |
| Current core Release | `ABE7DDF989D935A82D8E3D48D8AF26E100B95D97096820DFCD308FC8C50D089F` |
| New optional importer Release | `3BEA8570853CEF72A5AD3C3434ACF6DE155E1665975ADA910A641B5BE9F02380` |

## Reviewed differences

Historical Servo/Detective, foundation, tilemap, collision and sprite-animation
approval files remain separate inputs. This plan adds exactly 17 type-addition
suppressions, matching the source and canonical manifest:

- Five document/data types: asset, promotion, entity, level and document.
- Six shared validation/diagnostic types: severity, diagnostic, options, result,
  validator and collector.
- SegmentCollider2D, implementing the approved exact polyline-segment contract.
- Four overlay types: flags, read-only navigation provider, counters and node.
- Root-owned Detective TileMapDiagnosticsSnapshot.

No removal, unrelated signature change, wildcard or unnecessary suppression is
added by this plan. The initial run reported no unresolved reference errors.

## Coverage limits and explicit member review

ApiCompat suppresses an entire newly added type when that type is absent from
the old baseline. It does **not** enumerate every member change within types
already covered by historical type-addition approvals. The following were
therefore reviewed explicitly against the approved contract, implementation,
tests and canonical pages:

- TileFlip2D.Diagonal = 4, all eight normalized-axis combinations.
- TileColliderShape2D.Segment, exactly two points, zero thickness/two-sided.
- TileColliderDescriptor2D.LocalTransform and the affine constructor overload;
  the original constructor remains and uses identity, so no old overload is
  removed. Geometry/source validation and fixed construction budgets are
  intentional behavior changes, not binary changes claimed by ApiCompat.
- Detective.CaptureTileMap(TileMap2D), validated root ownership and value-only
  observational snapshots. The earlier Detective type-addition suppression
  means this method is checked by its permanent tests/docs, not by a separate
  ApiCompat method diagnostic.
- Servo's logical scene projection and shared root bounds, plus RenderSurface's
  origin/DPI mapping, preserve public signatures. Their behavioral changes are
  independently covered by RED/GREEN tests and native target-crop evidence.
- SourceGen reference-compatible OneWay collections and init-only accessors
  change validation/emission behavior without changing public signatures.

The optional importer assembly is wholly new, so there is no historical assembly
to compare. Its four exported entry/result/options types are documented and
covered by 151 importer tests. It references core and the approved strict
DEFLATE dependency, not a graphics backend. Import paths, atomic publication,
bounded diagnostics and exact format versions remain explicit contracts.

The source-level collision between the optional namespace `Cerneala.Scene2D`
and the imported `Cerneala.UI.Controls.Scene2D` type in nested `Cerneala.Tests`
namespaces is not hidden as an ApiCompat success. The restored core test build
reproduces CS0118. `namespace-experiment.log` shows ordinary/global alias lookup
failing and a namespace-local type alias succeeding; affected tests use that
explicit type identity without changing the framework's public namespaces.
