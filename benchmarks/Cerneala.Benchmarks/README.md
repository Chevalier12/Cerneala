# Cerneala Benchmarks

The primary suite covers Queue Engine and Relay internals. The separate WPF
project under `../Cerneala.WpfDispatcherBenchmarks/` compares the overlapping
Relay and WPF Dispatcher queueing contracts on dedicated STA threads.

Run the complete Queue Engine benchmark suite from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --artifacts .\benchmarks\Cerneala.Benchmarks\artifacts\final
```

Run a focused benchmark by passing a BenchmarkDotNet filter, for example:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*QueueHasWorkBenchmarks*" --artifacts .\benchmarks\Cerneala.Benchmarks\artifacts\has-work
```

The suite records execution time, allocation volume, and garbage collections for idle `HasWork`, repeated `HasWork`, sparse snapshots, drains, shared-order snapshots, layout metadata promotion, and detached-subtree cleanup. Compare runs made with the same runtime, build configuration, and hardware. Absolute timing is intentionally not enforced in unit tests.

The archived Queue Engine 2.0 comparison is in [results/2026-07-13-queue-engine-2.md](results/2026-07-13-queue-engine-2.md). Raw BenchmarkDotNet output is written under `artifacts/` and remains a local generated artifact.
