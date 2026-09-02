# Cerberus planner result — 2026-09-02

## Scope and command

This result measures only Cerberus enqueue, adjacent merge, index rebasing, storage
growth, and discard. It does not execute SDL, submit GPU work, or measure GPU time.

The benchmark uses BenchmarkDotNet's in-process toolchain because the default generated
project build entered the unrelated MonoGame `mgfxc` shader compiler and did not reach
the benchmark. The in-process job is declared on `CerberusPlanningBenchmarks` with three
warmup iterations and five measurement iterations.

```powershell
dotnet build .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-restore
dotnet run --no-build -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --filter "*CerberusPlanningBenchmarks*" --exporters JSON --artifacts .\benchmarks\results\2026-09-02-cerberus\final
```

Environment: Windows `10.0.26200`, Intel Core i5-9300H, .NET `8.0.30`, BenchmarkDotNet
`0.15.8`, workstation concurrent GC.

## Planner results

| Workload | Operations per invocation | Mean per operation | Allocated per operation |
| --- | ---: | ---: | ---: |
| Homogeneous merge and index rebasing | 1,024 | 165.1 ns | 0 B |
| Alternating enqueue | 1,024 | 142.8 ns | 1 B |
| Growth beyond initial capacity | 512 | 110.7 ns | 2 B |
| Alternating enqueue and discard | 512 | 118.0 ns | 2 B |

The complete CSV, HTML, Markdown, and compressed JSON reports are in `final/results/`.
BenchmarkDotNet warned that the iteration times were below its recommended 100 ms, so
these CPU means are reported as observations, not as a stable improvement claim. The
allocation results demonstrate that the warmed planner does not allocate whole objects
per submission; the 1–2 byte values are BenchmarkDotNet's per-operation division of the
small per-invocation totals.

## Whole-frame allocation comparison

The current worktree and baseline commit `d009162ecb74164fcc7490b29de2d2588e9b0d3a`
were run consecutively with the same copied test harness, Release runtime, three warmup
frames, and fake SDL observer. The baseline commit was built in a detached temporary
worktree; no checkout or reset touched the dirty main worktree.

| Workload | Baseline commit | Current worktree | Current <= baseline |
| --- | ---: | ---: | ---: |
| 1,000 compatible quads | 12,248 B | 12,248 B | yes |
| 4,096 alternating textures | 12,973,384 B | 12,678,328 B | yes |
| 257 alternating quads | 821,448 B | 802,776 B | yes |

The fake observer's string formatting and test-process tiering make absolute values vary
between independent processes; the comparison above therefore uses consecutive runs on
the same machine and runtime. No percentage performance claim is derived from it.

## Contract audit

All Cerberus allocation call sites pass contiguous `int[]` storage (`QuadIndices`, mesh
indices, stroke indices, command-mesh indices, or stencil mesh indices), so the planner
input was narrowed from `IReadOnlyList<int>` to `ReadOnlySpan<int>` without an adapter or
copy. Core mesh/path producers already validate their indices; no unvalidated producer
was found, so no per-index validation was added to the hot enqueue loop.

## Incremental emitter result

The same warm whole-frame harness after the per-render-pass state cache measured:

| Workload | Allocated bytes | Draws | Pipeline binds | Sampler binds | Scissor sets | Stencil-reference sets |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000 compatible quads | 12,248 | 1 | 1 | 1 | 1 | 1 |
| 4,096 alternating textures | 7,993,504 | 4,096 | 1 | 4,096 | 1 | 1 |
| 257 alternating quads | 509,768 | 257 | 1 | 257 | 1 | 1 |

The deterministic result is the transition count: unchanged pipeline, scissor, and
stencil reference are emitted once per resumed pass, while every texture transition
still binds its sampler and every logical draw remains. Samplers are also rebound after
a pipeline transition because no portable SDL contract was found that permits retaining
the previous binding. CPU timing remains observational; this work does not claim a
stable CPU or GPU speedup from these counts alone.
