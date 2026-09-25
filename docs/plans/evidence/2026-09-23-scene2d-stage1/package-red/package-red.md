# Stage 1 package/map RED contract result

> The current-source exception-type clarification rerun supersedes the earlier source hashes and is recorded in [final-map-exception-red.md](final-map-exception-red.md). The runs below remain historical evidence.

Current-worktree Release Core/Packages binaries were frozen and the existing package characterization suite was recorded as 81/81 GREEN by the Stage 1 integration owner before these tests were added. No production source was changed in this package RED pass.

## Fixture and producer

The pre-cutover CPV2 fixture was generated **before** removing its baseline-only producer and before Stage 2 production work. The producer compiled and passed (1/1), proving the reused `Scene2DPackageTests.Fixture.Document()` and old writer/open path were valid. File sizes, SHA-256 hashes, source provenance, exact command, raw log and TRX are in [precutover-cpv2.md](precutover-cpv2.md), `baseline-producer.log`, and `baseline-producer.trx`. A prior producer attempt failed during compilation of newly written test code (`Scene2D` name resolution and xUnit async assertion overloads); it produced no fixture and is **not** counted as a contract RED. Those test-source errors were corrected before the successful producer run.

## Focused RED

```powershell
dotnet test .\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj -c Release --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Stage0_|FullyQualifiedName~Scene2DPublicBoundaryContractTests' --results-directory .\docs\plans\evidence\2026-09-23-scene2d-stage1\package-red --logger 'trx;LogFileName=package-contract-red.trx'
```

Result: **19 failed, 0 passed, 0 skipped**. Compilation succeeded. Raw output is `package-contract-red.log`; machine-readable per-test output is `package-contract-red.trx`.

All 19 failures are absent/old selected contracts, not fixture, environment or compile failures:

- 12 range-reader/map behavior cases (14 theory-expanded results) stop at the absent `IScene2DPackageRangeReader` public type, before any unimplemented fake-reader behavior can run. Those tests remain permanent and will exercise short/concurrent reads, count/length/hash validation, cancellation/retry, failed-open ownership/cleanup aggregation, async drain, remote paths, map instance/disposal and retired-generation drain after the Stage 2 API exists.
- `Stage0_PreCutoverCpv2BytesRemainReadableWithoutWireChange` successfully produced bytes matching both frozen SHA-256 hashes and opened the old package, then failed because `LoadMetadataAsync` still returns `ValueTask<SceneSpatialLease2D<Scene2DPackageMetadata>>` rather than the selected direct `ValueTask<Scene2DPackageMetadata>`.
- `Stage0_DirectValuesAndCompleteMapModelRemainCallerOwned` failed on that same current return-shape mismatch before later new-value checks; the old package fixture was created successfully.
- Three public-boundary results found the current exported `SceneSpatialEntry2D`, absent `TileMapIds`, and absent range-reader type. The remaining exact-signature assertions in those tests become observable after cutover.

The tests intentionally use reflection/`DispatchProxy` only where the approved types do not yet exist, so the baseline project compiles and reports runtime RED instead of a misleading `CS0246`. This RED run proves the **current missing contract**; it does not prove that all later fake-reader scenarios are green. They must be run after implementation. Existing GREEN characterization tests were not edited or reclassified.

## Independent-audit repair rerun (current Stage 1 source)

The original 19-result run above is historical. After the independent Stage 1 audit, the two new test files were repaired without touching production or existing characterization tests:

- `SceneSimulationContext2D` and region state are now retired on the constructing owner thread before any async drain await. The old region's cancellation is observed and its posted release pumped before reattachment; there is no implicit second context disposal after an await.
- The public-boundary tests also reject retained `SceneItems2D.TryGetRealizedNode`, `Preparation`, and `PreparationError`. The reader-shape test title no longer claims an exact member-set assertion.
- The complete-model RED now includes an authored 33×19 grid plus a separate 17×1 empty chunk (nine prepared pieces, three authored chunks), checking authored extents, versions, properties, empty cells, presentation, tile set definitions, and caller ownership after package disposal. A 257-tile free-placement fixture checks source order across the 256-item group boundary after package disposal.
- Package and map drains are asserted **inside the still-admitted, blocked reader callback** after both terminal calls have started, before that callback may return its bytes. This is a causal ordering observer, not an immediate pending-status check followed by release. The package callback also asserts the reader has not closed early. The map callback covers ordinary retired-generation read completion and repeated terminal awaits; genuine old/new-generation **release callback failures** are separately covered by the core worker's new internal-source `TileMap2DDrainContractTests.cs`. No release error is faked via package I/O.

Exact rerun command (full Release build from current test source; `--no-build` was not used):

```powershell
dotnet test .\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj -c Release --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Stage0_|FullyQualifiedName~Scene2DPublicBoundaryContractTests' --results-directory .\docs\plans\evidence\2026-09-23-scene2d-stage1\package-red --logger 'trx;LogFileName=package-contract-red-audit-rerun.trx'
```

Result: **22 failed, 0 passed, 0 skipped**; build succeeded. Raw output: `package-contract-red-audit-rerun.log`; machine-readable results: `package-contract-red-audit-rerun.trx`; individually classified results: `package-contract-red-audit-rerun-classification.csv` (one row per actual TRX result, including each of the three theory cases). Every result is a currently absent/old selected contract, not a fixture, compiler, or environment failure:

| Actual TRX failure reason | Results |
| --- | ---: |
| Public CPV2 range-reader type absent, before the fake-reader/map behavior body | 14 |
| Complete `Scene2DPackageLevel.LoadMapModelAsync` absent, after valid grid/free fixture write | 2 |
| Existing metadata read returns a spatial lease rather than the selected direct value | 2 |
| Public spatial family still exported | 1 |
| `TileMapIds` absent from the public package level | 1 |
| CPV2 reader/part public shape absent | 1 |
| Public `SceneItems2D` spatial ID/preparation members still exposed | 1 |

The two new roundtrip fixtures wrote successfully and reached the missing loader guard; the frozen CPV2 test matched both independently captured old-writer hashes and reached the old lease-return mismatch. Behavior behind missing API guards remains **unexecuted**, not GREEN. Stage 2 must run these permanent tests after implementing the selected contract.

SHA-256 for this rerun: `Stage0PackageContractTests.cs` `1F00EE6DAFBABF0A7CAFF3C175707B20B8AAC1F2F2A7E586BA880800EE162104`; `Scene2DPublicBoundaryContractTests.cs` `D87A7B8CB6B7AF83E129FA976393A305AE07F54922129AC474A154CE87538601`; TRX `EA16477C95C810C6E3950666C137DE47239DC1029409778FAF8C828DB8BE924C`; log `194114088135AA2DCDB98BAD2502448891F0B5C97501F1B8A359DB9A5B0F7024`; classification CSV `F0AF49B6FE833EDDC8807C8C9894CBD59414C49A95149BAEF9B6597BCB2F1F7E`. The pre-cutover `catalog.c2d` and `payloads.c2d` still hash to `A417E679D0ACE8599A59FCF31738C725B137A3ECD5491ADA1FB38F15A4B8815E` and `F6A5EA008D9AEF3C88C4212F3FF93B32D1AFE2BBAB2F8FE5BE5D5665878A326E` respectively.
