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
built and tested on the same machine during every run, so the timings are noisy. The
allocation column is deterministic and is the primary evidence.

## Before: master 3f9f71a1

One full Servo tree build per query; matching copied the ancestor list per node and per
scope check.

| Method                        | Mean     | Allocated |
|------------------------------ |---------:|----------:|
| FindById                      | 13.23 ms |   5.09 MB |
| FindScoped                    | 13.48 ms |   5.10 MB |
| FindByIdAfterSemanticChange   | 13.75 ms |   5.09 MB |
| FindScopedAfterSemanticChange | 14.14 ms |   5.10 MB |

## After: #93 merged onto master 762223a9

The root keeps the Servo tree until a semantics invalidation, a `TreeVersion` change or a
processed frame. Matching is master's scope-set walk from #94 (no ancestor lists), with an
index loop over children.

| Method                        | Mean     | Allocated  |
|------------------------------ |---------:|-----------:|
| FindById                      |  0.92 ms |    1.19 KB |
| FindScoped                    |  2.37 ms |    1.52 KB |
| FindByIdAfterSemanticChange   | 11.16 ms | 3893.13 KB |
| FindScopedAfterSemanticChange |  9.78 ms | 3893.46 KB |

Queries without a change reuse the tree: allocations drop from 5.09 MB to 1.19 KB (by Id)
and 1.52 KB (scoped). A query after a change still builds the whole tree; its time is the
same within this noise, and its allocations drop by about 1.2 MB because the walk no longer
copies ancestor lists.

An earlier run of #93 on 3f9f71a1, before the merge (one shared ancestor stack instead of
#94's scope set), measured 1.94 ms / 2.25 KB, 1.31 ms / 2.25 KB, 14.41 ms / 3894.19 KB and
13.18 ms / 3894.19 KB for the same four rows. A first run of that build measured 27.2 ms and
26.6 ms for the two `AfterSemanticChange` rows with the same allocations, which shows the
size of the load noise.

The build count for waits is covered by `ServoSemanticsCacheTests`: a `WaitForAsync` over 10
idle frames builds the tree once (10 times before), and 12 queries without a change build it
once (12 times before).
