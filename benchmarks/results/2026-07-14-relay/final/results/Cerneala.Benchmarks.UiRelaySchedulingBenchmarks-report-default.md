
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

 Method      | Mean     | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
------------ |---------:|----------:|----------:|------:|--------:|----------:|------------:|
 Post        | 2.420 μs | 0.6888 μs | 0.1789 μs |  1.00 |    0.10 |      64 B |        1.00 |
 InvokeAsync | 2.940 μs | 1.0754 μs | 0.2793 μs |  1.22 |    0.13 |     168 B |        2.62 |
