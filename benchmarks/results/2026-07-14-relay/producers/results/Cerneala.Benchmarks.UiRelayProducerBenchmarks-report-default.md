
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

 Method             | ProducerCount | Mean     | Error    | StdDev    | Allocated |
------------------- |-------------- |---------:|---------:|----------:|----------:|
 **EnqueueTenThousand** | **1**             | **21.97 ns** | **14.48 ns** |  **2.241 ns** |      **90 B** |
 **EnqueueTenThousand** | **2**             | **52.12 ns** | **40.44 ns** | **10.502 ns** |      **91 B** |
 **EnqueueTenThousand** | **4**             | **70.68 ns** | **26.51 ns** |  **6.886 ns** |      **91 B** |
 **EnqueueTenThousand** | **8**             | **77.34 ns** | **10.25 ns** |  **2.662 ns** |      **91 B** |
