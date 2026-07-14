
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

 Method      | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------ |---------:|----------:|----------:|------:|--------:|----------:|------------:|
 Post        | 1.920 μs | 0.8348 μs | 0.2168 μs |  1.01 |    0.15 |      64 B |        1.00 |
 InvokeAsync | 2.640 μs | 1.4561 μs | 0.3782 μs |  1.39 |    0.24 |     168 B |        2.62 |
