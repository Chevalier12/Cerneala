# Collision Stage 2 production gate

Command:

```powershell
dotnet run --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -c Release -- --collision-stage2 .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-collision-stage2\results.json
```

The runner uses the frozen Stage 0 seed (`0xC0111D3`), corpus, eight warmup passes, and 48 measured passes against the production `SparseCollisionGrid2D`. Every measured query is checked against an exhaustive AABB oracle after the timing pass.

Final measurements on the archived host:

| Scenario | Update P95 | Query P95 | Retained bytes | False negatives | Gate |
| --- | ---: | ---: | ---: | ---: | --- |
| `large-sparse` | 107.6 us | 268.6 us | 1,425,544 | 0 | PASS |
| `high-churn` | 293.1 us | 100.2 us | 403,040 | 0 | PASS |
| `long-fence` | 16.2 us | 51.4 us | 205,184 | 0 | PASS |

The authoritative full result, including the remaining deterministic scenarios and host metadata, is `results.json`. The numeric gates are those frozen in `../2026-09-04-collision-stage0/stage0-contract-and-gates.md`.
