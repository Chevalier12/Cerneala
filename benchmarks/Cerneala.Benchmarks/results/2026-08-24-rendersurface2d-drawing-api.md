# RenderSurface2D complete Drawing API benchmark baseline

Date: 2026-08-24

## Environment

- BenchmarkDotNet 0.15.8, `ShortRun` (1 launch, 3 warmups, 3 measured iterations)
- Windows 11 25H2, .NET 8.0.30 x64 RyuJIT
- Intel Core i5-9300H, 4 physical / 8 logical cores
- Command:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*Drawing*Benchmarks*" --job short --artifacts .\tmp\drawing-api-benchmarks
```

The BenchmarkDotNet harness performs a clean build. The benchmark runner grants
that build ten minutes because the isolated build also compiles the complete
Prism shader catalog; the measured benchmark processes retain normal timeouts.

## Accepted baseline

| Family | Benchmark | Mean | Managed allocation |
|---|---|---:|---:|
| Batch | `IndividualPointCommands` (1,000 points) | 544.217 us | 0 B observed |
| Batch | `ImmutablePointBatch` (1,000 points) | 1.767 us | 0 B observed |
| Shapes | `RoundedRectangleFastCommand` | 121.8 ns | 0 B |
| Shapes | `BuildLargePolygon` (1,024 points) | 56.984 us | 234,130 B |
| Shapes | `RecordReusablePath` | 114.1 ns | 0 B |
| State | `AnalyzeNestedLayersAndClips` (64 nested groups) | 139.2 us | 342.17 KB |
| Stroke | `LargeSolidPath` (4,096 points) | 562.7 us | 1.36 MB |
| Stroke | `LargeDashedPath` | 610.1 us | 1.17 MB |
| Stroke | `LargeRoundJoinPath` | 943.4 us | 2.00 MB |
| Text | `RebuildLayout` | 41.594 us | 39,009 B |
| Text | `ReuseImmutableLayout` | indistinguishable from empty method | 0 B |

`ImmutablePointBatch` records the same 1,000 points in one command at 0.003×
the individual-command time. Recording a reusable path takes about 0.002× the
time of rebuilding the 1,024-point polygon and avoids its 234 KB allocation.
The immutable text-layout lookup is below BenchmarkDotNet's measurement floor
and allocates nothing; this is treated as a successful zero-cost reuse result,
not as a meaningful 0.0647 ns absolute latency claim.

## Accepted regression thresholds

Use the same runtime, configuration, hardware power policy, inputs, and
BenchmarkDotNet job before comparing results. A future run is accepted when:

- `ImmutablePointBatch` remains at or below 5% of `IndividualPointCommands`.
- `RecordReusablePath` remains at or below 2% of `BuildLargePolygon`, and both
  reusable command paths remain allocation-free.
- `RoundedRectangleFastCommand` remains below 1 us and allocation-free.
- `AnalyzeNestedLayersAndClips` remains below 250 us and 400 KB per operation.
- dashed stroke remains at or below 1.35× solid stroke time; round joins remain
  at or below 2.0× solid stroke time.
- dashed stroke allocation remains at or below solid allocation; round-join
  allocation remains at or below 1.75× solid allocation.
- `RebuildLayout` remains below 75 us and 64 KB, while
  `ReuseImmutableLayout` remains allocation-free.

ShortRun confidence intervals are intentionally broad for the manually invoked
point-batch methods. Relative command-count and allocation invariants are the
primary gates there; a threshold failure should be confirmed with the default
BenchmarkDotNet job before being classified as a regression.
