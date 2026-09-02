# Cerberus baseline — 2026-09-02

## Repository state

The implementation started from `master` with these pre-existing or ownership-uncertain
changes, which are outside this plan and remain untouched:

```text
 M .vscode/launch.json
 M .vscode/tasks.json
 D AGENTS.md
 M FileTree.md
 M tests/Cerneala.Tests/UI/Relay/UiRelayCoreTests.cs
?? .codex/config.toml
?? AGENTS_DEPRECATED.md
```

The untracked plan `docs/plans/2026-09-02-private-sdlgpu-cerberus.md` is the requested
plan and is therefore in scope. Regenerating `FileTree.md` reported `Unchanged`; its
existing modification is not attributed to this work.

## Environment

- OS: Windows `10.0.26200`, `win-x64`.
- .NET SDK selected by `global.json`: `10.0.303`.
- .NET test runtime: `Microsoft.NETCore.App 8.0.30`.
- CPU identifier: `Intel64 Family 6 Model 158 Stepping 10, GenuineIntel`.
- Configuration: `Release`.
- SDL implementation: deterministic `FakeSdlApi`; these measurements do not include
  native driver or GPU time.

Environment command:

```powershell
dotnet --info
```

## Focused test baseline

Commands:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter "FullyQualifiedName~SdlGpuDrawingBackendTests|FullyQualifiedName~SdlGpuDrawingFrameCountersTests" --logger "console;verbosity=normal"
dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj -c Release --filter FullyQualifiedName~DrawingImageMeshBatchTests --logger "console;verbosity=normal"
```

Results before the stage-0 test additions:

| Suite | Passed | Failed | Skipped | Test time | Wall time |
| --- | ---: | ---: | ---: | ---: | ---: |
| SDL GPU drawing backend + counters | 24 | 0 | 0 | 4.1206 s | 9.189 s |
| Core `DrawingImageMeshBatchTests` | 13 | 0 | 0 | 5.9836 s | 29.245 s |

## Warm allocation and state baseline

The permanent harness is
`SdlGpuDrawingBackendTests.WarmCerberusWorkloadsRecordMeasuredAllocationAndStateBaselines`.
Each workload creates a fresh fake SDL device/session and command list, renders the same
pre-analyzed frame three times as warmup, clears only the fake API's observation lists
between frames while retaining their capacity, then measures the fourth render with
`GC.GetAllocatedBytesForCurrentThread`. Counts below are from that same measured frame.

Command:

```powershell
dotnet test .\tests\Cerneala.Tests.SdlGpu\Cerneala.Tests.SdlGpu.csproj -c Release --filter "FullyQualifiedName~SdlGpuDrawingBackendTests|FullyQualifiedName~SdlGpuDrawingFrameCountersTests" --logger "console;verbosity=detailed"
```

Measured results:

| Workload | Warmup frames | Allocated bytes | Draw calls | Pipeline binds | Sampler binds | Scissor sets | Stencil-reference sets |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 1,000 compatible quads | 3 | 11,288 | 1 | 1 | 1 | 1 | 1 |
| 4,096 alternating textures | 3 | 11,301,304 | 4,096 | 4,096 | 4,096 | 4,096 | 4,096 |
| 257 alternating quads (exceeds 1,024 vertices, 1,536 indices, and 256 draws) | 3 | 715,656 | 257 | 257 | 257 | 257 | 257 |

The allocation figures include managed work performed by the deterministic test harness
and fake SDL observer. They are whole-frame managed allocations, not isolated Cerberus
planner allocations and not GPU measurements.

## RED architecture contract

The new architecture test was run separately and failed only because the expected
top-level type does not exist yet:

```text
Expected a top-level Cerneala.Backends.SdlGpu.Cerberus type; the current
owner-coupled implementation is still nested.
```

The public-surface characterization passed independently: the only exported type is
`SdlGpuApplicationBackend`, whose only declared public static method is
`void EnsureRegistered()`.
