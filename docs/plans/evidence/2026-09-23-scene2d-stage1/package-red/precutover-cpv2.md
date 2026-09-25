# Stage 1 pre-cutover CPV2 fixture

Captured on 2026-09-24 from the unchanged current-worktree package writer before any Stage 2 production edit. The temporary `Scene2DPackageTests.BaselineOnly_WritePreCutoverCpv2Fixture` test used the existing deterministic `Fixture.Document()` and `Scene2DPackageWriter.WriteAsync`, then opened the result with the pre-cutover `Scene2DPackage.OpenAsync(string)` reader. The writer refused to overwrite an existing destination. The generated package is at `../cpv2-precutover/`.

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `catalog.c2d` | 1,012 | `A417E679D0ACE8599A59FCF31738C725B137A3ECD5491ADA1FB38F15A4B8815E` |
| `payloads.c2d` | 263,634 | `F6A5EA008D9AEF3C88C4212F3FF93B32D1AFE2BBAB2F8FE5BE5D5665878A326E` |

Source SHA-256 at capture:

| File | SHA-256 |
| --- | --- |
| `Cerneala.Scene2D.Packages/Scene2DPackageWriter.cs` | `105CDA5E45182ABF120BB3A30A31D3EB3CB9BB32FEEB70F8FA797BC583C7E37C` |
| `Cerneala.Scene2D.Packages/PackageValueCodec.cs` | `E92BFD076EFFAB40A4EDD44E624CA090B037573F511B98FECF524419794BA38E` |
| `tests/Cerneala.Tests.Scene2DPackages/Scene2DPackageTests.cs` | `3944B273AC5F484B2D4F1E6582F00AFA74CD140DF9118088506BA7D5B8F7864E` |

Command, with `CERNEALA_STAGE1_CPV2_EVIDENCE_ROOT` set to the existing absolute `docs/plans/evidence/2026-09-23-scene2d-stage1` directory:

```powershell
dotnet test .\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj -c Release --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~BaselineOnly_WritePreCutoverCpv2Fixture' --results-directory .\docs\plans\evidence\2026-09-23-scene2d-stage1\package-red --logger 'trx;LogFileName=baseline-producer.trx'
```

Result: 1 passed, 0 failed, 0 skipped. Raw build/test output is `baseline-producer.log`; TRX is `baseline-producer.trx`. The first invocation failed to compile the newly added RED test file due test-source errors and produced no fixture; those errors were repaired before the successful capture. It is not counted as a RED contract result.
