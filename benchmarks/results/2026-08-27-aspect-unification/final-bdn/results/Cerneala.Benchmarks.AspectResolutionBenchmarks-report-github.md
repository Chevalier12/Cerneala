```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
Intel Core i5-9300H CPU 2.40GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.303
  [Host] : .NET 8.0.30 (8.0.30, 8.0.3026.36720), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  
LaunchCount=1  WarmupCount=3  

```
| Method                       | InvocationCount | UnrollFactor | Mean       | Error      | StdDev     | Gen0   | Gen1   | Allocated |
|----------------------------- |---------------- |------------- |-----------:|-----------:|-----------:|-------:|-------:|----------:|
| CodeFirstCatalogResolve      | Default         | 16           |   3.101 μs |   7.879 μs |  0.4319 μs | 1.0338 |      - |   4.23 KB |
| RootRegisteredPackageFrame   | Default         | 16           |  16.206 μs |  17.344 μs |  0.9507 μs | 2.8076 | 0.0153 |  11.53 KB |
| ElementLocalMutationAndFrame | Default         | 16           |  27.819 μs |  16.905 μs |  0.9266 μs | 4.8218 |      - |  19.75 KB |
| NestedScopeAttachAndFrame    | 1               | 1            | 310.200 μs | 376.352 μs | 20.6291 μs |      - |      - |  37.43 KB |
