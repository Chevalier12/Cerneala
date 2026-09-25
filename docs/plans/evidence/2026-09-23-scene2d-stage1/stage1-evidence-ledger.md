# Stage 1 evidence ledger — baseline and pre-cutover RED

Status: **integrated evidence accepted by the independent auditor; Stage 1
checklist checkpoint recorded, awaiting its separate audit**. The five
Stage 1 tasks and gate in
`docs/plans/2026-09-23-scene2d-simple-collections-and-internal-spatial.md`
are now checked. No production cutover, public API documentation migration, or
Stage 2 verification has run.

## Source and ownership

- Captured from worktree HEAD `443f100b86f8c4ca5f5e649651ac37c9cd5a330f`
  (`master`), **including** the pre-existing user edits, notably
  `UI/Controls/TileMap2D.cs`/`FromModel`. The worktree was not reset to HEAD.
  The original dirty-file identities and hashes are in
  `../2026-09-23-scene2d-stage0/worktree-baseline.md`.
- The baseline Release build used the command-only SDK default-item exclusion
  `-p:DefaultItemExcludesInProjectFolder=artifacts/**`. The first unadjusted
  build failed because ignored generated `artifacts/` C# files entered Core's
  compile items. The adjusted list removed exactly 17 `artifacts/` files and
  no intended source, then the package/Core build succeeded. Raw logs and
  compile-item lists are linked in `baseline-and-characterization.md`. No
  project file was changed to hide that input issue.
- Frozen pre-cutover binaries in `binary-baseline/` are worktree artifacts, not
  arbitrary older release binaries: `Cerneala.dll` SHA-256
  `F5F16215D563E712337DE83D4ACF1FC8CE1D2A0E5F2BB94E94EDE6B78BB6EA83`
  (5,483,008 bytes), and `Cerneala.Scene2D.Packages.dll` SHA-256
  `7D4B36BD358FEB21889BB4828DDE7B27BF72ECD153B0549B5C2B1D507DD97F2F`
  (78,336 bytes). Full capture command, input comparison, and hashes are in
  `baseline-and-characterization.md`.

## Stage 1 item-to-evidence map

| Stage 1 item | Current-state evidence | Result and limitation |
| --- | --- | --- |
| Existing characterization GREEN | `baseline-and-characterization.md`; `core-characterization.log`, `sourcegen-characterization.log`, `packages-characterization.log`, `importers-characterization.log`, `tetris-characterization.log`; matching `test-results/*.trx` | Core **focused filter**, 410 passed; whole SourceGen 610, packages 81, importers 173, Tetris 30 passed: **1,304 passed, 0 failed/skipped**. These are baseline runs before new RED files. Existing streaming tests were not deleted or reclassified as RED. `sdl-native-no-optin-characterization.log`/TRX records **six skipped** without native opt-in, **not** SDL conformance execution. Exact commands are in `baseline-and-characterization.md`. |
| Isolated `ItemsSource` compile RED | `isolated-compile-red.md`; `sceneitems-compile-red/SceneItems2DCollectionCompileRed.csproj` and `.cs`; `sceneitems-compile-red.log`; `compile-items-after-isolated-probe.json` | The isolated consumer assigns both `IEnumerable` and `ObservableCollection<TetrisSpriteModel>`; build exits 1 with exactly two intended CS0266 assignments to the existing `ISceneSpatialSource2D<object>` property, no other compiler diagnostic. Probe source is outside ordinary SDK compile items (zero matching Core items). Exact command and diagnostics are in `isolated-compile-red.md`. |
| Simple-collection, collider, and TileMap lifecycle RED | `tests/Cerneala.Tests/Controls/SceneItems2DSimpleCollectionContractTests.cs`; `Collider2DSimulationInterestContractTests.cs`; `TileMap2DDrainContractTests.cs`; `core-red/command-final6.txt`, `dotnet-test-final6.log`, `core-red-final6.trx`, `red-classification-final6.csv`, `repair-notes.md` | Focused Release test project compiles; **28 failed, 0 passed/skipped**. All 28 TRX rows independently match the per-case CSV by name/outcome/message/top guard stack: 22 old `ItemsSource` type, four absent `Collider2D.IsSimulated`, two absent `TileMap2D : IAsyncDisposable`; zero unclassified or caught fixture failures. The latest exact command is in `command-final6.txt`. Earlier `final3` had a swallowed guard/fixture-path failure; `final4` and `final5` are also **superseded** by current repaired source and final6. Eight xUnit1031 analyzer warnings, zero C# compiler errors. Behavioral assertions behind API guards remain unexecuted. |
| Public spatial/package replacement RED | `tests/Cerneala.Tests.Scene2DPackages/Stage0PackageContractTests.cs`; `Scene2DPublicBoundaryContractTests.cs`; `package-red/package-red.md`, `package-red/final-map-exception-red.md`, `package-contract-red-final-exceptions.log`, `package-contract-red-final-exceptions.trx`, `package-contract-red-final-exceptions-classification.csv` | Focused package project compiles; **22 failed, 0 passed/skipped**. Every result independently matches the CSV/log: 14 absent approved range-reader type; two missing selected `LoadMapModelAsync`; two old metadata lease return shapes; four public-boundary failures. The 33×19/empty and 257-placement fixture paths write before the missing map-loader guard, so those specific fixture constructions are not the RED cause. Exact command is in `package-red.md`. The historical 19-result `package-contract-red.trx` and audit-rerun 22-result TRX are **superseded** by the current exact-exception test source/run. Reader/map behavior behind guards remains unexecuted. |
| Current-worktree binary/API baseline and strict comparison preparation | `binary-baseline/*.dll`; `api-compat.proj`; `api-compat-baseline-selfcompare.log`; `baseline-and-characterization.md` | Fresh installed SDK 10.0.400 `Microsoft.DotNet.ApiCompat.Task.ValidateAssembliesTask` parameters were inspected. The dedicated project compares matching Core and Packages assembly pairs in strict mode, checks parameter names, and imports no historical suppression file or absolute SDK/baseline path. Baseline-to-identical-current self-compare exits 0. **The post-cutover API comparison has not run.** |

## Frozen CPV2 compatibility input

Before any production writer change, the temporary producer test ran **1/1
passed** using the existing `Fixture.Document()` and old package writer. Its
command, raw log/TRX and final fixture hashes are in
`package-red/precutover-cpv2.md`, `baseline-producer.log`, and
`baseline-producer.trx`. The temporary producer was removed from the final
test source. The permanent package RED test retains golden-byte/readability
assertions against the hashes of the archived `cpv2-precutover/` bytes:

- `catalog.c2d`: 1,012 bytes; SHA-256
  `A417E679D0ACE8599A59FCF31738C725B137A3ECD5491ADA1FB38F15A4B8815E`.
- `payloads.c2d`: 263,634 bytes; SHA-256
  `F6A5EA008D9AEF3C88C4212F3FF93B32D1AFE2BBAB2F8FE5BE5D5665878A326E`.

The fixture is a pre-cutover byte/readability input for later compatibility
verification; it does not prove new-reader compatibility yet.

## Audit-repair record and remaining limits

The first independent Stage 1 audit rejected specific new-test oracles, so the
Core and package owners repaired only their allocated new files and reran
their focused RED subsets. The current artifacts are `core-red/*final6*` and
`package-red/*final-exceptions*`; earlier RED runs remain archived but are not
current-state acceptance evidence. The selected Stage 0 companion and matrix
were clarified under the user's delegated design authority:

- Framework-produced no-template/wrong-result/duplicate-node validation is
  `InvalidOperationException` without an exact-message contract; arbitrary
  application template-factory exceptions propagate unchanged.
- Internal `TileMap2D.StopSource` retirement tracks its own sync/late release
  errors without breaking structural detach; terminal `DisposeAsync` reports
  all generations. Attached dispose is `InvalidOperationException` without
  consumption; reattach after terminal disposal is `ObjectDisposedException`.
  Arbitrary application lifecycle-hook exceptions are not swallowed.

Core `repair-notes.md` records one deliberately removed **new and never-valid**
`CleanupFailureDoesNotHidePrimaryStructuralFailure` test: plain `IEnumerable`
realizations have no resource-release callback that could produce its claimed
cleanup failure, so retaining it would require inventing a hook or the removed
spatial lease path. This is not deletion or reclassification of an existing
characterization test. Terminal structural-fault readiness, primary-fault
visibility, and observable unsubscription remain in the new Core test source.
The Core owner added `TileMap2DDrainContractTests.cs` using the actual map
residency owner for old/current generation release failures.

The package owner strengthened blocked-read disposal ordering, added
map-model subdivision/free-placement and removed-auxiliary-member API cases,
and changed attached-dispose and terminal-reattach assertions to the exact
selected `InvalidOperationException` and `ObjectDisposedException` types.
Those exception assertions have **not** executed in RED because the reader
API guard fails first.
Both owners' repaired test bodies still stop at intentional missing API
guards on this baseline; deeper assertions require GREEN verification after
Stage 2. The archived CPV2 bytes/hash provenance are fixed before cutover;
the permanent hash test writes with the current writer and opens its freshly
written package, rather than opening the archived file directly. The shared
auditor accepted this golden-hash path as Stage 1 evidence; it must not be
inflated into proof that the post-cutover reader accepts old bytes.

Other observers in the frozen matrix belong to later conformance or existing
characterization suites; absence from these five **new** RED files alone is
not evidence of a Stage 1 gap. The same independent auditor re-reviewed the
repaired current source and raw runs and accepted the integrated Stage 1
candidate before the checklist was changed.

## Gate and remaining uncertainty

The Stage 1 gate was checked only after that audit acceptance and root
authorization. The same auditor must still inspect the **checklist checkpoint**
before Stage 2 begins. No Stage 2 source, existing test, or
API documentation change was made here. A passing focused baseline is not a
full current-state suite. Native SDL was skipped without opt-in; WindowsDX,
Linux, and macOS platform gates are not claimed. No platform waiver is inferred.

Current post-repair mechanical checks and source-hash reconciliation are
recorded in `stage1-integrity-current.txt`. The pre-existing user-dirty files
are compared to the Stage 0 baseline separately from the intentionally
updated plan and regenerated `FileTree.md`. Neither these checks nor the RED
counts justified the Stage 1 checkpoint by themselves; the independent audit
and subsequent authorized checklist edit supplied that gate.
