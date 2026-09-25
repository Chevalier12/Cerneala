# Stage 2 strict public API comparison — exact-member review

The original strict Stage 1 project first compares Core and aborts on its intentional breaking differences, so its raw nonzero result **does not compare Packages**. The separate [`api-compat-packages.proj`](api-compat-packages.proj) uses the same SDK `ValidateAssembliesTask` and parameters (`EnableStrictMode=true`, parameter-name rule enabled, `RespectInternals=false`, suppression generation disabled) for the package pair only. No frozen binary, original project, or suppression file was changed.

| Pair | Frozen SHA-256 | Current SHA-256 | Final raw command/log | Strict result |
| --- | --- | --- | --- | --- |
| Core | `F5F16215D563E712337DE83D4ACF1FC8CE1D2A0E5F2BB94E94EDE6B78BB6EA83` | `EC8BEE87C67F0558E85B4D637903AA087C980931EDDE9DD4922B58498774D87B` | [`command`](api-compat-core-final-repair.command.txt), [`log`](api-compat-core-final-repair.log), [`exit`](api-compat-core-final-repair.exitcode.txt) | exit 1; 23 CP diagnostics |
| Packages | `7D4B36BD358FEB21889BB4828DDE7B27BF72ECD153B0549B5C2B1D507DD97F2F` | `DD8790173B27740D1B3819528AE0BA9C5088E3D70B32523D1C247CACC4002DC8` | [`command`](api-compat-packages-final-repair.command.txt), [`log`](api-compat-packages-final-repair.log), [`exit`](api-compat-packages-final-repair.exitcode.txt) | exit 1; 20 CP diagnostics |

Every diagnostic's **exact symbol, code, direction, and selected-contract reason** is a separate row in [`api-compat-exact-classification.tsv`](api-compat-exact-classification.tsv), IDs C01–C23 and P01–P20. There are 43 rows and no unclassified diagnostic in those two final raw logs. This is an evidence classification, **not** an ApiCompat suppression or a claim the comparison exited zero. The selected contract is the Stage 0 decision companion and contract/platform matrix; public-boundary/behavior tests and canonical documentation supply semantic proof beyond this signature diff.

The post-audit SceneItems implementation repair changed both candidate DLL
hashes. The refreshed strict Core and Package comparisons were run as **two
separate tasks** against the same frozen baseline. A read-only exact-sequence
comparison of all `error CP....:` lines found Core **23/23 identical** and
Packages **20/20 identical** to the previous reviewed candidate, in the same
order and with the same exact members; no new or missing diagnostic appeared.
The TSV therefore still classifies every final diagnostic. The prior
`api-compat-after-public-retention.*` and
`api-compat-packages-after-public-retention.*` are historical and are not the
current DLL-hash proof.

The initial Core raw comparison had 24 diagnostics, including CP0001 for `TileMapChunkData2D`. That was **not** approved. The map owner restored this existing public payload value without reinstating a source/catalog/lease facade; the package boundary assertion was RED while internal and GREEN after the one-keyword repair, full package tests passed 104/104, and the canonical payload page and manifest were restored. The final Core raw log has **23** diagnostics and no `TileMapChunkData2D` CP0001. The earlier raw [`Core RED`](api-compat-current.log) and [`Package initial`](api-compat-packages.log) files remain archived, not overwritten.

ApiCompat reports newly added types at the type level rather than listing every member of `IScene2DPackageRangeReader` or `Scene2DPackagePart`. Their exact selected signatures and behavior are separately checked by the package contract/boundary tests and the canonical class pages. The raw diff contains no unexpected remaining public/protected removal on this candidate. The independent Stage 2 audit must confirm this classification against current source and the frozen decision record before any Stage 2 checklist gate is checked.
