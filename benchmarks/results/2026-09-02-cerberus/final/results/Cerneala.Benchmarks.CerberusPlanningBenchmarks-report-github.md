```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Intel Core i5-9300H CPU 2.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.303
  [Host] : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  InvocationCount=1
IterationCount=5  UnrollFactor=1  WarmupCount=3

```
| Method                           | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------- |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| HomogeneousMergeAndIndexRebasing | 165.1 ns | 108.06 ns | 28.06 ns |  1.02 |    0.21 |         - |          NA |
| AlternatingEnqueue               | 142.8 ns | 122.90 ns | 31.92 ns |  0.88 |    0.22 |       1 B |          NA |
| GrowthBeyondInitialCapacity      | 110.7 ns |  31.19 ns |  4.83 ns |  0.68 |    0.10 |       2 B |          NA |
| AlternatingEnqueueAndDiscard     | 118.0 ns |  84.43 ns | 13.07 ns |  0.73 |    0.12 |       2 B |          NA |
