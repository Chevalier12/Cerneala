```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Intel Core i5-9300H CPU 2.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.303
  [Host] : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method                       | InvocationCount | UnrollFactor | Mean       | Error       | StdDev     | Gen0   | Gen1   | Allocated |
|----------------------------- |---------------- |------------- |-----------:|------------:|-----------:|-------:|-------:|----------:|
| CodeFirstCatalogResolve      | Default         | 16           |   2.204 μs |   0.2048 μs |  0.0112 μs | 1.0338 |      - |   4.23 KB |
| RootRegisteredPackageFrame   | Default         | 16           |  15.681 μs |  56.8137 μs |  3.1141 μs | 2.2736 | 0.0153 |   9.32 KB |
| ElementLocalMutationAndFrame | Default         | 16           |   2.869 μs |   3.9403 μs |  0.2160 μs | 0.3357 |      - |   1.38 KB |
| NestedScopeAttachAndFrame    | 1               | 1            | 245.350 μs | 409.2901 μs | 22.4346 μs |      - |      - |  22.48 KB |
