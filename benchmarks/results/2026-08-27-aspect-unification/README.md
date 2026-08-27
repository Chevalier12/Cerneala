# Aspect runtime unification measurements

## Environment

- Date: 2026-08-27
- Repository plan: `docs/plans/2026-08-27-unify-aspect-runtime.md`
- Benchmark project: `benchmarks/Cerneala.Benchmarks`
- Frame budget project: `benchmarks/Cerneala.PresentationFrameBudget`

## Feature-parity inventory

| Contract | Existing permanent coverage | Migration gate |
| --- | --- | --- |
| Code-first packages, layers, tokens and conditions | `AspectEngineTests`, `AspectPackageTests`, `AspectRuleSetTests`, `AspectTokenTests` | Stages 1-2 |
| Typed slots and template-owner state | `AspectSlotTests`, `ComponentTemplateTests`, `AspectTemplateCatalogIntegrationTests` | Stages 1-2 |
| Variants | `AspectVariantTests`, `AspectEngineTests.EngineReappliesWhenVariantDependencyChanges` | Stages 1-2 |
| Application `TargetType`, derived/runtime-created controls | `ApplicationResourceIntegrationTests`, `UiMarkupGeneratorApplicationTests` | Stages 2-3 |
| Element-scoped unnamed and named resources | `UiMarkupGeneratorTests.GeneratedResourcesAreStoredOnTheirActualOwnerAndFollowRuntimeLookup`, `NamedTemplatedAspectCanBeAppliedToADynamicControl` | Stages 2-3 |
| Inline and named local Aspect precedence | `ElementAspectTests`, `UiMarkupGeneratorTests.ReferencedNamedAspectRemainsLocalAfterThemeProcessing`, inline Aspect tests | Stages 2-3 |
| Component templates and replacement | `ComponentTemplateTests`, `AspectTemplateCatalogIntegrationTests`, source-generator template tests | Stages 2-4 |
| Reactive properties/data/resources and detach | source-generator `@when` tests, `AspectEngineTests`, generated condition lifecycle tests | Stage 4 |
| ItemsControl realization/container Aspect | `ItemsControlTests` and source-generated typed composition tests | Stages 2-3 |
| Motion/events/presence/layout/scroll/drag/gesture | `UiMarkupGeneratorMotion*Tests`, runtime Motion suites | Stage 4 |
| Diagnostics | `ModernAspectTraceTests`, `AspectEngineTests.EngineReportsWinnerAndRejectedDeclarations` | Stages 1 and 6 |
| Idle frames | `AspectEngineStressBudgetTests`, scheduler/frame tests, new unification RED tests | Stages 2 and 6 |

## Commands

Baseline BenchmarkDotNet:

```powershell
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --inProcess --job short --filter "*AspectResolutionBenchmarks*" --artifacts .\benchmarks\results\2026-08-27-aspect-unification\baseline-bdn
```

Baseline Presentation frame budget:

```powershell
dotnet build .\CernealaPresentation\CernealaPresentation.csproj -c Release --no-restore -p:CernealaDesktopBackend=SDL3 -p:BuildProjectReferences=false
dotnet run -c Release --no-build --project .\benchmarks\Cerneala.PresentationFrameBudget\Cerneala.PresentationFrameBudget.csproj -- --cycles 8 --frames-per-load 45 --budget-ms 16.6667
```

## Baseline results

BenchmarkDotNet completed with the in-process short job on .NET 8.0.30:

| Scenario | Mean | Allocated/op |
| --- | ---: | ---: |
| `CodeFirstCatalogResolve` | 2.204 us | 4.23 KB |
| `RootRegisteredPackageFrame` | 15.681 us | 9.32 KB |
| `ElementLocalMutationAndFrame` | 2.869 us | 1.38 KB |
| `NestedScopeAttachAndFrame` | 245.350 us | 22.48 KB |

`NestedScopeAttachAndFrame` uses one invocation per iteration because attach mutates the tree. BenchmarkDotNet warned that its approximately 220-259 us iterations are shorter than the recommended 100 ms, so that row is a directional baseline and must be compared using the identical job rather than treated as a high-confidence absolute number.

The Presentation frame-budget run completed and wrote `baseline-presentation-frame-budget.json`, but the existing 16.6667 ms gate is RED before production changes:

| Chapter | Warm p99 | Warm max | Frames over 16.6667 ms |
| --- | ---: | ---: | ---: |
| RETAINED MODEL | 19.66 ms | 21.74 ms | 140 |
| BUILD-TIME MARKUP | 19.71 ms | 22.21 ms | 142 |
| ASPECT STUDIO | 19.33 ms | 42.54 ms | 150 |
| MOTION | 23.60 ms | 30.69 ms | 133 |
| PRISM | 18.19 ms | 33.33 ms | 153 |
| FRAME PIPELINE | 19.53 ms | 34.03 ms | 86 |

Across 1,890 warm samples, scheduled Aspect time measured 0.000381 ms mean, 0.0005 ms p99 and 0.0889 ms max, with 49 processed Aspect elements. Mean per-frame allocation was 643,430 bytes across the complete Presentation scenario; this is not attributable only to Aspect.

The frame-budget RED state is baseline evidence, not a regression introduced by this plan. Stage 6 must compare against the stored report and may not relabel an unchanged failure as GREEN.

## Stage 5 API diff

The strict public API comparison against `HEAD` is stored in `stage5-api-diff.txt` and classified in `stage5-api-diff.md`. Its intentional removals are `MarkupAspectResource`, the four authoring-specific `UiPropertyValueSource` members, and the superseded six-argument `MarkupConditionRule` constructor. All reported additions belong to the unified declaration/signal/sidecar contract; no unclassified removal remains.

## Stage 6 final results

Diagnostics, optimized BenchmarkDotNet results, deterministic engine counters, Presentation frame comparison, exact `Window.SaveScreenshot` visual conformance, commands, and interpretation are recorded in `stage6-results.md`.

## Final verification

Complete project/solution test counts, Release build, formatter scope, documentation/API diff, indexer status, architecture ownership, and remaining baseline warnings are recorded in `final-verification.md`.
