# Stage 6 diagnostics, performance, and visual conformance

## Diagnostics

The canonical trace now records every considered rule, not only matched rules. Each step includes package, document, `AspectAuthoringKind`, named/inline origin name, deterministic runtime scope, layer, source order, specificity, declaration order, exact captured condition results, condition dependencies, and a deterministic outcome.

Permanent tests prove:

- target/slot filtering occurs before condition evaluation;
- condition results used by diagnostics are the same objects used by matching, with no predicate reevaluation;
- generated markup carries document plus default/named/inline origin;
- equivalent C# and generated markup rules report identical cascade coordinates, condition match state, dependency kinds, and rejection reasons.

Public diagnostic objects are materialized lazily at `GetDiagnostics`. `Resolve` does not capture trace state. This optimization was introduced only after the first final benchmark demonstrated eager diagnostics as a measured hot-path owner.

## BenchmarkDotNet comparison

Configuration remained identical to baseline: .NET 8.0.30, in-process `ShortRun`, three warmups, three measured iterations, MemoryDiagnoser, and the same machine/power-plan behavior.

| Scenario | Baseline mean | Final mean | Delta | Baseline alloc | Final alloc | Delta |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `CodeFirstCatalogResolve` | 2.204 us | 2.331 us | +5.8% | 4.23 KB | 4.04 KB | -4.5% |
| `RootRegisteredPackageFrame` | 15.681 us | 13.386 us | -14.6% | 9.32 KB | 8.57 KB | -8.0% |
| `ElementLocalMutationAndFrame` | 2.869 us | 26.653 us | +829.0% | 1.38 KB | 17.61 KB | +1,176.1% |
| `NestedScopeAttachAndFrame` | 245.350 us | 300.033 us | +22.3% | 22.48 KB | 35.86 KB | +59.5% |

`CodeFirstCatalogResolve` is within short-run noise and allocates less. The root frame path improved. The two remaining regressions are explained by the approved architectural migration: the baseline local path wrote directly, while the final local mutation rebuilds an immutable local package/catalog and resolves through the engine; nested attach now composes scoped packages and synchronizes sidecar lifetimes. Their absolute measured costs are 0.027 ms and 0.300 ms per mutation/attach, not per idle frame. The real Presentation frame run below shows no frame-level allocation regression and keeps scheduled Aspect below 0.18 ms maximum.

The pre-optimization final run is preserved under `final-bdn/`; the optimized authoritative run is `final-bdn-optimized/`.

## Deterministic engine metrics

`final-aspect-metrics.json` records 1,000 operations per scenario:

| Scenario | Rule evaluations | Matched | Condition evaluations | Declarations | Aspect invalidations |
| --- | ---: | ---: | ---: | ---: | ---: |
| `CodeFirstCatalogResolve` | 2,000 | 1,500 | 1,000 | 1,500 | 0 |
| `RootRegisteredPackageFrame` | 4,000 | 2,500 | 1,000 | 6,500 | 1,000 |
| `NestedScopeAttachAndFrame` | 3,000 | 2,000 | 0 | 6,000 | 1,000 |
| `ElementLocalMutationAndFrame` | 3,000 | 2,000 | 0 | 6,000 | 1,000 |

## Presentation frame budget

Both reports use SDL3, eight cycles, 45 frames per chapter, and the same `16.6667 ms` target. The target remains RED on this machine before and after the migration, so comparison—not relabeling—is the valid gate.

| Metric | Baseline | Final |
| --- | ---: | ---: |
| Warm scheduled Aspect mean | 0.000381 ms | 0.001877 ms |
| Warm scheduled Aspect p99 | 0.0005 ms | 0.0726 ms |
| Scheduled Aspect maximum | 0.0889 ms | 0.1766 ms |
| Processed Aspect elements | 56 | 172 |
| Mean allocation/frame | 643,430 B | 638,750 B |

The higher Aspect count is expected evidence that markup now uses the canonical engine. The absolute p99 remains 0.44% of a 16.6667 ms frame, the maximum remains below 0.18 ms, and total allocation declined 0.7%.

Chapter warm p99 deltas were: Retained Model -1.5%, Build-Time Markup +3.6%, Aspect Studio -7.1%, Motion -18.0%, Prism -0.1%, Frame Pipeline -0.8%. The isolated +3.6% chapter movement is not accompanied by Aspect time, allocation, or visual regression and is within the cross-run presentation variance visible in the other chapters.

## Visual conformance

The retained Presentation captured 1600x900 Aspect Studio and Build-Time Markup scenarios exclusively through `Window.SaveScreenshot`. `CERNEALA_PRESENTATION_SETTLED_CAPTURE=1` navigates via the real automation peer, waits for retained/Motion idle, fixes live header diagnostics, and then invokes the application-owned screenshot API.

The reference is `HEAD` built with the same versioned SDL shader artifacts as current (the Aspect plan did not modify the backend). Both scenario comparisons are exact:

| Scenario | Pixels | Changed | MAE | P99 | Max | Result |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Aspect Studio | 1,440,000 | 0 | 0 | 0 | 0 | GREEN |
| Build-Time Markup/templates | 1,440,000 | 0 | 0 | 0 | 0 | GREEN |

The diff runner uses the repository's canonical RGBA thresholds (MAE <= 1.0, P99 <= 10, max <= 49), writes JSON plus a difference PNG, and fails non-zero outside tolerance.

## Reproduction commands

```powershell
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --inProcess --job short --filter "*AspectResolutionBenchmarks*" --artifacts .\benchmarks\results\2026-08-27-aspect-unification\final-bdn-optimized
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --aspect-metrics .\benchmarks\results\2026-08-27-aspect-unification\final-aspect-metrics.json
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667 --report .\benchmarks\results\2026-08-27-aspect-unification\final-presentation-frame-budget.json
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --aspect-visual-diff <baseline.png> <actual.png> <report.json>
```
