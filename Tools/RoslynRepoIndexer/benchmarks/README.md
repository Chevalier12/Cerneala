# RoslynRepoIndexer benchmarks

Run from the repository root:

```powershell
dotnet run -c Release --project Tools/RoslynRepoIndexer/benchmarks/RoslynRepoIndexer.Benchmarks -- --filter "*"
```

The default corpus is the indexed Cerneala repository. Set `RI_BENCH_REPO` to run the same benchmark suite against another deterministic local corpus. BenchmarkDotNet writes p50-style distribution statistics, allocations, and artifacts under `BenchmarkDotNet.Artifacts/`.

`baseline-2026-07-12.json` records the pre-upgrade measurements from the implementation plan. It is comparison data, not a machine-dependent unit-test threshold.
