
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1
UnrollFactor=1  WarmupCount=2

 Method                    | ProducerCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------------- |-------------- |----------:|-----------:|----------:|------:|--------:|----------:|------------:|
 **WpfBeginInvokeTenThousand** | **1**             | **354.16 ns** | **344.723 ns** | **89.524 ns** |  **1.05** |    **0.33** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 1             |  19.99 ns |   5.739 ns |  1.490 ns |  0.06 |    0.01 |      90 B |        0.25 |
                           |               |           |            |           |       |         |           |             |
 **WpfBeginInvokeTenThousand** | **2**             | **302.20 ns** | **139.909 ns** | **36.334 ns** |  **1.01** |    **0.17** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 2             |  49.15 ns |  52.773 ns | 13.705 ns |  0.16 |    0.05 |      91 B |        0.25 |
                           |               |           |            |           |       |         |           |             |
 **WpfBeginInvokeTenThousand** | **4**             | **321.63 ns** |  **92.035 ns** | **23.901 ns** |  **1.00** |    **0.09** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 4             |  73.73 ns |  34.471 ns |  8.952 ns |  0.23 |    0.03 |      91 B |        0.25 |
                           |               |           |            |           |       |         |           |             |
 **WpfBeginInvokeTenThousand** | **8**             | **331.55 ns** |  **72.689 ns** | **18.877 ns** |  **1.00** |    **0.07** |     **360 B** |        **1.00** |
 CernealaPostTenThousand   | 8             |  80.76 ns |  20.973 ns |  5.447 ns |  0.24 |    0.02 |      91 B |        0.25 |
