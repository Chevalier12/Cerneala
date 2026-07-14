```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

```
| Method             | ProducerCount | Mean     | Error     | StdDev   | Allocated |
|------------------- |-------------- |---------:|----------:|---------:|----------:|
| **EnqueueTenThousand** | **1**             | **17.47 ns** |  **9.019 ns** | **2.342 ns** |      **90 B** |
| **EnqueueTenThousand** | **2**             | **46.00 ns** | **35.799 ns** | **9.297 ns** |      **91 B** |
| **EnqueueTenThousand** | **4**             | **71.70 ns** | **21.197 ns** | **5.505 ns** |      **91 B** |
| **EnqueueTenThousand** | **8**             | **76.52 ns** | **38.950 ns** | **6.028 ns** |      **91 B** |
