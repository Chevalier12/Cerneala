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
| CodeFirstCatalogResolve      | Default         | 16           |   2.331 μs |   0.5998 μs |  0.0329 μs | 0.9880 |      - |   4.04 KB |
| RootRegisteredPackageFrame   | Default         | 16           |  13.386 μs |   1.6105 μs |  0.0883 μs | 2.0752 | 0.0305 |   8.57 KB |
| ElementLocalMutationAndFrame | Default         | 16           |  26.653 μs |  20.3610 μs |  1.1161 μs | 4.2725 |      - |  17.61 KB |
| NestedScopeAttachAndFrame    | 1               | 1            | 300.033 μs | 446.5366 μs | 24.4762 μs |      - |      - |  35.86 KB |
