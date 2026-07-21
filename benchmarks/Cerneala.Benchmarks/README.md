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

## Prism retained-composition benchmark

Run the deterministic Prism matrix from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --prism-retained-cache
```

The runner exercises static and animated compositions, 24 common instances,
many layers, filter chains, styles, nested groups, a shared backdrop, resource
changes, and a deliberately undersized retained cache at 256 x 144 and
640 x 360. Each cache-off/cache-on row reports CPU graph-build and submission
time, a synchronized GPU-completion upper bound, managed allocations, passes,
captures, surface pressure, retained hit/miss counters, evictions, and estimated
GPU surface bytes. It fails immediately on a static allocation, missing retained
hit, unexpected capture, fallback, or leaked active surface.

The integration and budget reference is in
[results/2026-07-21-prism-integration-hardening.md](results/2026-07-21-prism-integration-hardening.md).
The earlier retained-cache-only baseline remains in
[results/2026-07-21-prism-retained-cache.md](results/2026-07-21-prism-retained-cache.md).
