```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9711/25H2/2025Update/HudsonValley2)
Intel Core i5-9300H CPU 2.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.400
  [Host] : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3

Job=InProcess  Toolchain=InProcessEmitToolchain  InvocationCount=1  
IterationCount=5  UnrollFactor=1  WarmupCount=3  

```
| Method                           | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------------- |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| HomogeneousMergeAndIndexRebasing | 238.3 ns | 80.08 ns | 12.39 ns |  1.00 |    0.06 |         - |          NA |
| AlternatingEnqueue               | 158.5 ns | 46.22 ns | 12.00 ns |  0.67 |    0.05 |       1 B |          NA |
| GrowthBeyondInitialCapacity      | 191.9 ns | 10.02 ns |  1.55 ns |  0.81 |    0.04 |     452 B |          NA |
| AlternatingEnqueueAndDiscard     | 150.5 ns | 11.54 ns |  1.79 ns |  0.63 |    0.03 |       2 B |          NA |
