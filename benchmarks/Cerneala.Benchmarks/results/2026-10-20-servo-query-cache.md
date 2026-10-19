# Servo query cache (issue #93)

`ServoQueryBenchmarks` runs `Servo.FindAsync` on a `UiHost` whose root holds 250 chains of
20 nested elements: 5,000 elements, depth 20. `FindById` looks up `leaf-125`; `FindScoped`
looks up `leaf-125` within `chain-125`. The `AfterSemanticChange` variants change an
accessible name before each query, so the Servo semantics tree must be rebuilt every time.

Command:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*ServoQueryBenchmarks*"
```

Environment: BenchmarkDotNet v0.15.8, Windows 11 25H2, Intel Core i5-9300H, .NET 8.0.30,
Release, `SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)`. Five other agents
built and tested on the same machine during both runs, so the timings are noisy. The
allocation column is deterministic and is the primary evidence.

## Before (master 3f9f71a1: one full Servo tree build per query, copied ancestor lists)

| Method                        | Mean     | Allocated |
|------------------------------ |---------:|----------:|
| FindById                      | 13.23 ms |   5.09 MB |
| FindScoped                    | 13.48 ms |   5.10 MB |
| FindByIdAfterSemanticChange   | 13.75 ms |   5.09 MB |
| FindScopedAfterSemanticChange | 14.14 ms |   5.10 MB |

## After (root-cached Servo tree, one shared ancestor stack)

| Method                        | Mean      | Allocated  |
|------------------------------ |----------:|-----------:|
| FindById                      |  1.94 ms  |    2.25 KB |
| FindScoped                    |  1.31 ms  |    2.25 KB |
| FindByIdAfterSemanticChange   | 14.41 ms  | 3894.19 KB |
| FindScopedAfterSemanticChange | 13.18 ms  | 3894.19 KB |

A first "after" run of the same job measured 27.2 ms and 26.6 ms for the two
`AfterSemanticChange` rows with the same 3894.18 KB allocations; a rerun under lower load
gave the numbers above. The rebuild path's time is unchanged within this noise; its
allocations drop by about 1.2 MB because the walk no longer copies the ancestor list per
node. Queries without a change reuse the tree: allocations drop from 5.09 MB to 2.25 KB.
