# Cerneala Language Core Baseline - 2026-08-13

## Environment

- Runtime: `.NET 8.0.27 X64`
- OS: `Microsoft Windows 10.0.26200`
- Logical processors: `8`
- GC mode: workstation
- Configuration: `Release`
- Warm-up iterations: `8`
- Measured iterations: `40`

Command:

```powershell
dotnet run -c Release --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- --language-core-gate
```

## Results

| Document | Characters | Operation | p95 (ms) | Max (ms) | Allocated (B/op) |
| --- | ---: | --- | ---: | ---: | ---: |
| Small | 75 | Parse cold | 0.007 | 0.008 | 4,064 |
| Small | 75 | Parse warm | 0.005 | 0.005 | 3,920 |
| Small | 75 | Incremental edit | 0.005 | 0.005 | 4,480 |
| Small | 75 | Semantic bind | 1.385 | 1.725 | 33,568 |
| Small | 75 | Warm query | 0.001 | 0.001 | 464 |
| Medium | 7,887 | Parse cold | 0.430 | 0.443 | 109,112 |
| Medium | 7,887 | Parse warm | 0.409 | 0.544 | 106,144 |
| Medium | 7,887 | Incremental edit | 0.662 | 4.370 | 140,768 |
| Medium | 7,887 | Semantic bind | 20.425 | 23.530 | 869,529 |
| Medium | 7,887 | Warm query | 0.006 | 0.019 | 464 |
| `AspectChapterView.crn` | 21,123 | Parse cold | 1.386 | 5.505 | 285,384 |
| `AspectChapterView.crn` | 21,123 | Parse warm | 1.447 | 8.017 | 279,296 |
| `AspectChapterView.crn` | 21,123 | Incremental edit | 1.534 | 1.925 | 369,984 |
| `AspectChapterView.crn` | 21,123 | Semantic bind | 14.923 | 15.999 | 2,479,080 |
| `AspectChapterView.crn` | 21,123 | Warm query | 0.062 | 2.076 | 464 |

## Gates And Allocation Review

The large-document parse and local-edit p95 values are below the 50 ms budget,
the warm semantic query p95 is below 25 ms, and every measured synchronous
operation has a maximum below 100 ms.

A local edit currently rebuilds the parser tree and allocates about 370 KiB for
the large fixture. Its 1.534 ms p95 leaves roughly 32 times the required latency
headroom, so this baseline does not justify the complexity of incremental subtree
reuse yet. Semantic binding is the largest allocator at about 2.48 MiB, but its
14.923 ms p95 and 15.999 ms maximum remain within the synchronous budget. These
figures are the reference for deciding whether a later regression warrants a
more granular cache or tree-reuse strategy.
