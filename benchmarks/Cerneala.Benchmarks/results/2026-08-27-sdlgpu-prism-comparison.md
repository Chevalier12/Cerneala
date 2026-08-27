# Prism WindowsDX / SDL_GPU comparison — 2026-08-27

## Scope

The manual comparison runner renders the same retained Prism command list through both desktop backends on the same machine. The scene is 256×144 and applies `GaussianBlur`, `Emboss`, and `HueSaturation` to three filled rectangles. Both sessions use one sample, submit and present every measured frame, warm up for 12 frames, and then measure 96 frames.

Command:

```powershell
dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release --no-build --no-restore -- --prism-sdlgpu-comparison
```

Machine:

- Windows 11 Pro
- Intel Core i5-9300H
- NVIDIA GeForce RTX 2060, with Intel UHD Graphics 630 also present
- .NET SDK 10.0.303; applications target .NET 8

## Results

| Metric | WindowsDX | SDL_GPU | SDL_GPU / WindowsDX |
|---|---:|---:|---:|
| CPU frame time | 6.9028 ms | 6.8673 ms | 0.9949× |
| Frame submit count | 96 | 96 | 1.0000× |
| Managed allocation | 214,037 B/frame | 229,163 B/frame | 1.0707× |
| Peak Prism GPU resources | 2,359,056 B | 3,047,144 B | 1.2917× |
| Last retained frame Prism passes | 1 | 2 | 2.0000× |
| Last Prism CPU submit time | 126.2 µs | 284.2 µs | 2.2520× |
| Fallbacks | 0 | 0 | — |
| Active transient surfaces after frame | 0 | 0 | — |

## Assessment

SDL_GPU is not slower in end-to-end CPU frame time for this presented, retained workload: the measured ratio is 0.9949×, below the plan's 1.25× investigation threshold. Both backends perform exactly one host frame submission per measured frame and finish with no fallback or active transient Prism surface.

SDL_GPU reports one additional Prism presentation pass and uses 688,088 more peak Prism-resource bytes. That is consistent with its explicit backend-owned copy/presentation surface path and does not produce an end-to-end CPU regression in this run. Managed allocation is 7.1% higher on SDL_GPU; this is recorded for future profiling but is not accompanied by the CPU regression that would require Stage 7 remediation.

The runner emits the complete result as `cerneala-prism-backend-comparison-v1` JSON so later runs can be compared without parsing this report.
