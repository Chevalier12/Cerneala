
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1  
UnrollFactor=1  WarmupCount=2  

 Method             | Mean      | Error    | StdDev   | Allocated |
------------------- |----------:|---------:|---------:|----------:|
 SimpleBindingBurst |  95.83 ns | 13.49 ns | 2.087 ns |      81 B |
 InterpolationBurst | 109.69 ns | 48.83 ns | 7.557 ns |      82 B |
