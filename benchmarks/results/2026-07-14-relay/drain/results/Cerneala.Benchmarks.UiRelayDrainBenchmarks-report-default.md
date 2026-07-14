
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

 Method        | CallbackCount | Mean      | Error     | StdDev     | Allocated |
-------------- |-------------- |----------:|----------:|-----------:|----------:|
 **DrainOneFrame** | **0**             |  **5.780 μs** |  **3.037 μs** |  **0.7887 μs** |     **920 B** |
 **DrainOneFrame** | **1**             |  **4.875 μs** |  **3.962 μs** |  **0.6131 μs** |     **120 B** |
 **DrainOneFrame** | **100**           | **11.560 μs** |  **3.039 μs** |  **0.7893 μs** |     **120 B** |
 **DrainOneFrame** | **1024**          | **56.575 μs** |  **4.611 μs** |  **0.7136 μs** |     **120 B** |
 **DrainOneFrame** | **2048**          | **75.700 μs** | **43.788 μs** | **11.3717 μs** |     **120 B** |
