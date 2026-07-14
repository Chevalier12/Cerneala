
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1
UnrollFactor=1  WarmupCount=2

 Method        | CallbackCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------- |-------------- |------------:|------------:|-----------:|------:|--------:|----------:|------------:|
 **WpfDrain**      | **1**             |    **80.96 μs** |    **43.29 μs** |  **11.242 μs** |  **1.02** |    **0.19** |    **1392 B** |        **1.00** |
 CernealaDrain | 1             |    56.65 μs |   130.24 μs |  20.154 μs |  0.71 |    0.25 |     120 B |        0.09 |
               |               |             |             |            |       |         |           |             |
 **WpfDrain**      | **100**           |   **875.14 μs** |   **600.25 μs** | **155.883 μs** |  **1.02** |    **0.23** |   **37008 B** |       **1.000** |
 CernealaDrain | 100           |    43.74 μs |    14.27 μs |   3.706 μs |  0.05 |    0.01 |     144 B |       0.004 |
               |               |             |             |            |       |         |           |             |
 **WpfDrain**      | **1024**          | **7,562.65 μs** | **3,013.33 μs** | **466.315 μs** |  **1.00** |    **0.08** |  **369648 B** |       **1.000** |
 CernealaDrain | 1024          |    96.68 μs |    32.88 μs |   8.538 μs |  0.01 |    0.00 |     144 B |       0.000 |
