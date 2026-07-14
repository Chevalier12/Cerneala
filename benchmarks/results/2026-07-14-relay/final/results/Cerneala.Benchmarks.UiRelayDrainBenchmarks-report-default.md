
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

 Method        | CallbackCount | Mean      | Error     | StdDev     | Median    | Allocated |
-------------- |-------------- |----------:|----------:|-----------:|----------:|----------:|
 **DrainOneFrame** | **0**             |  **5.420 μs** |  **3.627 μs** |  **0.9418 μs** |  **4.800 μs** |     **920 B** |
 **DrainOneFrame** | **1**             |  **3.900 μs** |  **5.329 μs** |  **1.3838 μs** |  **3.100 μs** |     **120 B** |
 **DrainOneFrame** | **100**           | **11.725 μs** |  **6.765 μs** |  **1.0468 μs** | **11.650 μs** |     **120 B** |
 **DrainOneFrame** | **1024**          | **71.500 μs** | **29.201 μs** |  **7.5835 μs** | **71.600 μs** |     **120 B** |
 **DrainOneFrame** | **2048**          | **67.200 μs** | **45.538 μs** | **11.8260 μs** | **60.100 μs** |     **120 B** |
