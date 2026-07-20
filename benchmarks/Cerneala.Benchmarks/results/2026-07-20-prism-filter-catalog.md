# Prism filter catalog: optimizer and execution profile

Date: 2026-07-20

## Environment

- Windows 10.0.26200
- .NET SDK 10.0.300
- AMD EPYC 9354 32-Core Processor
- NVIDIA RTX 2000 Ada Generation, driver 32.0.15.7216
- WindowsDX test backend, Debug build, 16 x 16 render targets

## Method

`PrismGraphExecutorTests.RepresentativeScenesStayWithinMeasuredExecutionBudgets`
warms each scene for eight frames, runs one stabilization frame after a full GC,
then measures 16 frames. CPU submit is the executor's own submit timer.

MonoGame's public WindowsDX surface does not expose portable GPU timestamp
queries. The GPU column is therefore an explicitly synchronized
`Execute + RenderTarget2D.GetData` wall-clock upper bound, not a claimed native
GPU timestamp. It includes submit, completion wait, and the test-only readback.

## Static scenes

| Scene | Passes | Peak live surfaces | New surfaces/frame | Reused surfaces/frame | Mean CPU submit | GPU completion upper bound | Managed bytes after warmup |
|---|---:|---:|---:|---:|---:|---:|---:|
| simple | 7 | 2 | 0 | 6 | 165.1 us | 3,039.4 us | 0 |
| chained | 9 | 2 | 0 | 8 | 140.5 us | 934.1 us | 0 |
| nested | 15 | 3 | 0 | 13 | 196.0 us | 935.9 us | 0 |

The synchronized completion samples are noisy by design and are recorded for
comparison, not used as portable pass/fail limits. The deterministic budgets are
zero managed allocation and zero new surfaces after warmup, no leaked leases,
and an observed live-surface peak no larger than the optimizer's lifetime plan.

## Animated and retained stress

- 2,048 alternating preplanned opacity frames created no additional surfaces,
  returned every lease, kept the same loaded `Effect` instance, and reused the
  surface pool on every frame.
- 4,096 nonstructural Prism opacity changes reused the same retained
  `DrawCommandList`, retained-cache version, and `ElementRenderCache` render
  version. Counter deltas were `0` hits, `0` misses, and `0` rebuilds: this path
  bypasses element-cache lookup entirely because the typed Prism instance
  supplies the current value through the retained scope.
- The two execution plans used by the animated GPU stress are constructed
  before the measured loop. No graph is constructed by a parameter mutation or
  retained commit. Frame-local execution planning performed by the rendering
  backend remains separate from the future retained pixel-cache work.

## Threshold decision

No public numeric threshold or limit was introduced. This measurement supports
structural, hardware-independent gates, but a single WindowsDX machine does not
justify a public CPU or GPU time promise. No quality preset, adaptive-quality
branch, or hidden degradation policy was added.

Reproduce the focused measurements with:

```powershell
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~RepresentativeScenesStayWithinMeasuredExecutionBudgets|FullyQualifiedName~ThousandsOfAnimatedFramesReuseSurfacesAndCompiledShader|FullyQualifiedName~ThousandsOfAnimatedParameterCommitsReuseRetainedCommands" --logger "console;verbosity=detailed"
```
