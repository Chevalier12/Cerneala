# Shared-scene focused run — current failure classification

Command: [`shared-scene-focused.command.txt`](shared-scene-focused.command.txt). Raw console: [`shared-scene-focused.log`](shared-scene-focused.log). Raw per-case results: [`shared-scene-focused.trx`](shared-scene-focused.trx).

Release `--no-build --no-restore` used the Core test binary built after the current SceneItems/Map compile pass. Result: **136 passed, 17 failed, 0 skipped, 153 total**. This is not a Stage 2 gate pass. The following groups come from all 17 TRX `UnitTestResult[@outcome='Failed']` records, not the truncated console output:

| Owner | Cases | Observed failure | Status |
| --- | ---: | --- | --- |
| Playground showcase/package caller | 12 | Seven tests/theory families reach `SceneWorldPackage.OpenAsync` guard at line 57: `InvalidDataException` says the village lacks expected zero offset or ordered Ground/Buildings/Doors maps. Several async UI tests surface that same error through `Assert.Null`. | Sent to Playground owner for source-backed contract/fixture diagnosis. No root cause assigned yet. |
| Scene presentation regression | 4 | Two offscreen-simulation theory cases report expected 1/actual 0; complete-visible case expected 2/actual 1; publication-during-recording throws `NullReferenceException` in `RenderSurface2DFrame.Complete` line 407. | Sent to scene-regressions owner; fixture vs production owner unresolved. |
| Simulation context regression | 1 | Nested simulated collider case expects one retained terrain collision entry but observes none (`Assert.Single`, line 355). | Sent to scene-regressions owner; marker/geometry/map ownership not presumed. |

All selected `ScenePrismStreamingTests` cases in this filtered run passed. The 136 passing cases are reusable only if their relevant implementation/test inputs remain unchanged; this failed aggregate is not evidence that the shared-scene gate is green. No test expectation has been updated merely to erase these failures.

Subsequent coordinated repairs and a current-source rerun are recorded under `stage2/sceneitems/frame-discard-green-narrow.*` and `stage2/sceneitems/frame-discard-shared-classes.*`: the narrow frame-discard case passed 1/1 and the same four-class aggregate passed 153/153. The present document preserves the original failure classification; it is superseded as a current result by those raw rerun artifacts, not erased.
