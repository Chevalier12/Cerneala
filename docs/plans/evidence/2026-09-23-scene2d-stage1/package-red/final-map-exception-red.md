# Final Stage 1 package/map exception-contract RED

The selected contract now explicitly requires `InvalidOperationException` when `TileMap2D.DisposeAsync()` is called while attached, and `ObjectDisposedException` when a terminally disposed map is reattached. Exception messages are not contractual. The package test now requires those exact types instead of accepting an arbitrary exception. Only these two assertions changed after the preceding package run; no production or existing characterization test changed.

Exact current-source command (full Release build, not `--no-build`):

```powershell
dotnet test .\tests\Cerneala.Tests.Scene2DPackages\Cerneala.Tests.Scene2DPackages.csproj -c Release --no-restore '-p:DefaultItemExcludesInProjectFolder=artifacts/**' --filter 'FullyQualifiedName~Stage0_|FullyQualifiedName~Scene2DPublicBoundaryContractTests' --results-directory .\docs\plans\evidence\2026-09-23-scene2d-stage1\package-red --logger 'trx;LogFileName=package-contract-red-final-exceptions.trx'
```

The current Release source compiled. The raw `package-contract-red-final-exceptions.trx` records **22 failed, 0 passed, 0 skipped**, with the same seven exact absence/old-contract groups and counts as [the preceding classification](package-red.md). The new `package-contract-red-final-exceptions-classification.csv` classifies each of the 22 actual TRX results, including each theory expansion; no result was fixture/environment/unclassified. `package-contract-red-final-exceptions.log` is the raw command output. The two map exception bodies still stop at the absent range-reader API on this baseline; their precise exception assertions are therefore permanent Stage 2 obligations, **not observed GREEN behavior**.

Current SHA-256: `Stage0PackageContractTests.cs` `D078F37C1DEBB9F91C02364F86BFC85BFEE30E28AF57C15C6A7D4F0969BADE9F`; `Scene2DPublicBoundaryContractTests.cs` `D87A7B8CB6B7AF83E129FA976393A305AE07F54922129AC474A154CE87538601`; final TRX `CB0A762EF18E0DC2FF24FD23109029B79313275AFF063D9F8C18A51BB5AA6516`; final log `3482C7500D6F20FD337D89789C0E0695CB4BB70747E0390B8B7450CEEF1FCC3C`; final classification CSV `F0AF49B6FE833EDDC8807C8C9894CBD59414C49A95149BAEF9B6597BCB2F1F7E`.
