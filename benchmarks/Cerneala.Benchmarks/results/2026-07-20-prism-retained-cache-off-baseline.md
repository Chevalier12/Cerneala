# Prism retained cache-off baseline

Recorded on 2026-07-20 with the retained pixel cache absent (equivalent to
cache-off), WindowsDX, Debug, 16 measured frames after warmup.

Command:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --no-restore `
  --filter "FullyQualifiedName~RepresentativeScenesStayWithinMeasuredExecutionBudgets" `
  --logger "console;verbosity=detailed"
```

| Scene | Captures/frame | Passes/frame | CPU submit avg | GPU completion upper bound | Managed allocation | Peak transient surfaces |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| simple | 1 | 7 | 206.7 us | 3584.5 us | 0 B | 2 |
| chained | 1 | 9 | 149.7 us | 1018.7 us | 0 B | 2 |
| nested | 2 | 15 | 221.9 us | 1142.6 us | 0 B | 3 |

The GPU column is the existing synchronized completion/readback upper bound, not
a hardware timestamp query. Surface creation was zero after warmup; the measured
frames reused 6, 8, and 13 transient surfaces per frame respectively.

The cache-off raster oracle was also verified by
`PrismWindowsDxConformanceTests.BaselineScenesMatchVersionedWindowsDxGoldensAndUseMinimalPasses`
against the versioned WindowsDX goldens.
