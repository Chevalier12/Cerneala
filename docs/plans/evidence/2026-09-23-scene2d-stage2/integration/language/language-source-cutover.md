# Stage 2 Language caller cutover — current evidence, not a Stage 2 checkpoint

The post-implementation caller audit found that Language still treated the
removed public `TileMap2D.Source` as an authoring alternative. This was missed
by the original Stage 0 scoped inventory and is recorded there as a dated
addendum, not retroactively attributed to that audit. The selected contract
retains direct static `<Tile>` declarations and unrelated real `Source`
properties such as `Image.Source`. No new public binding API was added.

## Reproduction and exact result

All commands use Release and the Stage 1 validated command-only
`-p:DefaultItemExcludesInProjectFolder=artifacts/**` artifact exclusion. Each
`.command.txt` in this directory records its exact invocation; the adjacent
`.log` and `.trx` are raw output. No test fixture invents a public TileMap
Source: `FixtureUsesTheCurrentPublicCoreSurface` reflects the actual current
Core DLL and verifies `TileMap2D.Source` is not public while `Image.Source` is.

| Phase / artifact prefix | Observed result | Contract interpretation |
| --- | --- | --- |
| `language-source-red` | Compiled; **3 failed, 4 passed**, 0 skipped | Property-element and Aspect assignments produced old `CERNEALAUI005` shape guidance recommending Source binding, rather than removed-member `CERNEALAUI003`. An invalid Source attribute suppressed the otherwise direct Tile symbol. Bare Source already produced an unknown-member error, and the actual-public-surface, completion-negative, and unrelated Image.Source checks were already green. |
| `old-source-completion-baseline` | **1 failed**, 0 passed | The pre-existing test wrongly expected Source completion from current Core. This is an obsolete expectation, not proof that completion was advertising it in this fixture. |
| `tile-completion-red` | Compiled; **1 failed**, 0 passed | The corrected permanent completion test required direct Tile completion even when the parent has an invalid Source attribute; the old completion branch returned no child. |
| `language-source-green-focused` | **8 passed**, 0 failed/skipped | The new semantic cases and corrected completion case passed after the owner-local change. |
| `language-full-final` | **234 passed, 1 skipped**, 0 failed | Current final Language test source; the skip is `WarmCompletionP95StaysBelowBudgetAndIndependentDocumentsDoNotBlock`, explicitly disabled by a pre-existing maintainer instruction. It does **not** validate the completion CPU P95 gate. |
| `sourcegen-after-language` | **610 passed**, 0 failed/skipped | Rebuilt SourceGen and its full test project after the Language production change. The later test-only removal of an unused `using` in the new Language test does not change SourceGen inputs or behavior. |

The production correction is exact-type/member scoped:
`IsRemovedTileMapSourceMember` rejects only `Cerneala.UI.Controls.TileMap2D`
`Source` in ordinary attributes, property elements and Aspect assignments;
completion never proposes it, and an invalid Source attribute no longer hides
static Tile semantics. Existing generic bindings, including a typed
`<Image Source="$DataContext.Picture:OneWay" />`, remain tested. No public or
protected Language member signature changed: the edited semantic model and
completion service classes are internal; the existing helper was internal and
only renamed internally. The strict reviewed Core/Package candidates remain
byte-identical after the Language builds (hashes below), so those raw API
comparisons are not silently applied to a different Core/Package candidate.

## Current input/output hashes (SHA-256)

| Path from repo root | Hash |
| --- | --- |
| `Cerneala.Language/Semantics/CernealaSemanticModel.Tiles.cs` | `9F0654CCB635F4E6341561810D6B373BBF608CC3C21FB2E289B5FAA29DD547EA` |
| `Cerneala.Language/Semantics/CernealaSemanticModel.cs` | `A0AB2507141ABA0D51DAEB4BA4CD0A2C9FE052173468A947FE8DEFB007D06E48` |
| `Cerneala.Language/Semantics/CernealaSemanticModel.Bindings.cs` | `803B71812C33DF24F53210369FFF46A4553E82350F47B55E6AFB48B5D0E38E36` |
| `Cerneala.Language/Features/CernealaCompletionService.cs` | `35E4B707D7E67A9803FF4ECA89C63367010BFA19E3C570A69F51103A591BC077` |
| `tests/Cerneala.Tests.Language/CompletionTests.cs` | `EDCE2BBD728B25B2CEEACCC4016324C8B26B64239D89FE9FC4EF236C050F550B` |
| `tests/Cerneala.Tests.Language/TileMapSourceSemanticTests.cs` | `B470D0F3294BC1335446380052862628C5F693320202466BC695D8DA983AE5B8` |
| `Cerneala.SourceGen/UiMarkupGenerator.cs` (unchanged by this Language repair) | `31AB74C77681A12C71E17B75F9BB3187B038B61F80E6918B23C7953755AB26F1` |
| `bin/Release/net8.0/Cerneala.dll` (strict Core candidate) | `C5A949EEF9C0AE37C89380AA271E0612AC429F3C37C33A7096DA3C1D7378658A` |
| `Cerneala.Scene2D.Packages/bin/Release/net8.0/Cerneala.Scene2D.Packages.dll` (strict Package candidate) | `0CAF151C87FD35F272FEB1A087985C9C3B8D329A0C65F041FFF0E96EA7EAFC1B` |
| `language-source-red.trx` | `E1DC27C60E23A1EF46E656DCB5F87BE595BDBF8266D7BE1553EE686442ADBC76` |
| `language-full-final.trx` | `8D5023968F85C59B8666DBDCB719DB057CAB9AA0351D5A4EDA08E876B47DE37A` |
| `sourcegen-after-language.trx` | `ED641787F45F0C4B969AED8FF55EAEDF8EC8A29B1EEF58BC64BBB80AA3E2E6F8` |

Stage 2 remains unchecked until integrated independent audit. Stage 3's
full-repository, native SDL, WindowsDX and non-Windows platform gates are
separate and have not been run or waived here.
