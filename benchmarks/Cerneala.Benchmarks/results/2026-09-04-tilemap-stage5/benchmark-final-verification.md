# TileMap2D final benchmark verification

Status: resolved on an idle workstation on 2026-09-04.

The unchanged stage 4 core benchmark was rerun after the stage 5 backend work. Its allocations and structural counters remained stable, but the absolute `chunk-mutation` P95 gate (`<= 1,135 us`) did not pass while unrelated applications kept total CPU utilization between roughly 30% and 62%.

Observed normal-priority runs:

| Condition | Warm P95 | Pan P95 | Mutation P95 | Result |
| --- | ---: | ---: | ---: | --- |
| Total CPU about 62% | 572.7 us | 428.3 us | 2,125.7 us | Fail |
| Same external workload | 437.6 us | 657.8 us | 2,910.0 us | Fail |
| Game process ended; total CPU about 31% | 318.3 us | 403.4 us | 1,387.5 us | Fail |
| Launch after three consecutive samples below 20% | 264.9 us | 387.5 us | 1,217.1 us | Fail |

One diagnostic run inherited Windows `High` process priority and still failed at 1,495.0 us; it is not accepted as gate evidence because it changes the execution condition. A bounded twelve-minute wait found no three-second window below 15% total CPU, so no further benchmark was launched.

Across the listed normal-priority runs, allocation and retained-work invariants stayed at 15,819 B/op for warm static, 18,834 B/op for pan, about 198,923 B/op for mutation, 0/36 warm rebuild/reuse, 0/48 pan rebuild/reuse, and 1/35 mutation rebuild/reuse. The failure is confined to the wall-clock P95 gate; it has not been waived.

`benchmark-loaded-attempt.json` preserves the best controlled normal-priority attempt under load (1,217.1 us). During diagnosis the standard runner overwrote the stage 4 `optimized.json` before throwing; the successful idle run below subsequently replaced it with a passing canonical report.

After the League/Riot workload was closed, an unmodified normal-priority run passed all 19 gates and replaced the canonical `optimized.json`:

| Scenario | P50 | P95 | Allocation | Commands | Rebuild / reuse |
| --- | ---: | ---: | ---: | ---: | ---: |
| Warm static | 156.6 us | 458.8 us | 15,819 B/op | 36 | 0 / 36 |
| Camera pan | 116.0 us | 189.3 us | 18,834 B/op | 48 | 0 / 48 |
| One-chunk mutation | 507.3 us | 803.2 us | 198,923 B/op | 36 | 1 / 35 |

The mutation result is below its 1,135 us P95 gate without process-priority, affinity, test, or threshold changes. The native backend profile was then regenerated in the same idle state and exited 0.
