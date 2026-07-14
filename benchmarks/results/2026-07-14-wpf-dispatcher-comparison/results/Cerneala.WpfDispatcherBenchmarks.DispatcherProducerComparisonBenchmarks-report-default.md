
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1
UnrollFactor=1  WarmupCount=2

 Method                    | ProducerCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |-------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
 **WpfBeginInvokeTenThousand** | **1**             | **319.59 ns** |  **95.41 ns** | **24.777 ns** |  **1.00** |    **0.10** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 1             |  22.62 ns |  24.23 ns |  6.293 ns |  0.07 |    0.02 |      90 B |        0.25 |
                           |               |           |           |           |       |         |           |             |
 **WpfBeginInvokeTenThousand** | **2**             | **245.58 ns** | **117.25 ns** | **30.448 ns** |  **1.01** |    **0.16** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 2             |  48.59 ns |  38.38 ns |  9.967 ns |  0.20 |    0.04 |      91 B |        0.25 |
                           |               |           |           |           |       |         |           |             |
 **WpfBeginInvokeTenThousand** | **4**             | **279.06 ns** |  **87.95 ns** | **22.841 ns** |  **1.01** |    **0.11** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 4             |  69.60 ns |  23.08 ns |  5.993 ns |  0.25 |    0.03 |      91 B |        0.25 |
                           |               |           |           |           |       |         |           |             |
 **WpfBeginInvokeTenThousand** | **8**             | **363.92 ns** |  **40.52 ns** | **10.522 ns** |  **1.00** |    **0.04** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 8             |  81.05 ns |  16.27 ns |  4.225 ns |  0.22 |    0.01 |      91 B |        0.25 |
