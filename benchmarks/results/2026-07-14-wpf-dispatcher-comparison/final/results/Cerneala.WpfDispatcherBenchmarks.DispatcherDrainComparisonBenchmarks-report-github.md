```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1
UnrollFactor=1  WarmupCount=2

```
| Method        | CallbackCount | Mean        | Error       | StdDev       | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |-------------- |------------:|------------:|-------------:|------:|--------:|----------:|------------:|
| **WpfDrain**      | **1**             |    **80.24 μs** |    **29.07 μs** |     **7.548 μs** |  **1.01** |    **0.12** |    **1392 B** |        **1.00** |
| CernealaDrain | 1             |    33.72 μs |    10.09 μs |     2.620 μs |  0.42 |    0.05 |     144 B |        0.10 |
|               |               |             |             |              |       |         |           |             |
| **WpfDrain**      | **100**           |   **745.48 μs** |   **132.81 μs** |    **20.553 μs** |  **1.00** |    **0.04** |   **37008 B** |       **1.000** |
| CernealaDrain | 100           |    42.73 μs |    22.10 μs |     3.420 μs |  0.06 |    0.00 |     144 B |       0.004 |
|               |               |             |             |              |       |         |           |             |
| **WpfDrain**      | **1024**          | **9,361.66 μs** | **9,290.08 μs** | **2,412.605 μs** |  **1.05** |    **0.34** |  **369648 B** |       **1.000** |
| CernealaDrain | 1024          |    95.74 μs |    25.06 μs |     6.507 μs |  0.01 |    0.00 |     144 B |       0.000 |
