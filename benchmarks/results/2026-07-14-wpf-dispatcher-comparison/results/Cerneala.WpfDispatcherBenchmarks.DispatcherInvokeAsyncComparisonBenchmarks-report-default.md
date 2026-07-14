
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8655/25H2/2025Update/HudsonValley2)
AMD EPYC 9354 3.25GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4
  Job-BKNAKG : .NET 8.0.27 (8.0.27, 8.0.2726.22922), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=5  LaunchCount=1
UnrollFactor=1  WarmupCount=2

 Method              | Mean      | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
-------------------- |----------:|---------:|----------:|------:|--------:|----------:|------------:|
 WpfInvokeAsync      | 18.575 μs | 5.931 μs | 0.9179 μs |  1.00 |    0.06 |     544 B |        1.00 |
 CernealaInvokeAsync |  3.520 μs | 3.097 μs | 0.8044 μs |  0.19 |    0.04 |     168 B |        0.31 |
