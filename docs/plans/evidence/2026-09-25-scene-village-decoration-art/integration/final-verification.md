# Scene Village decoration artwork — integration verification

Date: 2026-09-25. This is a new, app-owned bug fix. It does not amend the completed Scene2D or scene-seam plans/evidence. The user-visible report was incomplete Village tree artwork; the permanent app tests define the expected whole two-cell tree silhouette while retaining the original trunk/collider position. The shared Astra accepted the focused app candidate before this full-solution run, then accepted the current integrated source, hashes, screenshot, and raw automated Windows SDL checkpoint after the full-solution run. This does not imply human visual/play validation or execution of the six skipped cases.

## Reproduction, owner, and change

The app worker's deterministic model RED, `../app/model-red-01.log` and its TRX, failed because the tree fixture had no full two-cell artwork node. The first native attempt (`native-red-01`) captured before the root command-list commit and is explicitly **invalid evidence** for the bug. The corrected native RED (`native-red-02`) used the real window and app-owned screenshot path: at the intended canopy location it observed grass `#ff84c669` instead of source leaf `#ff479f4a`, while a shrub control remained `#ff479f4a`. The valid RED screenshot is under `artifacts/ci/scene-village/screenshots/native-20260925-102414091/decorations.png`.

The selected repair stays in the Village app: route the full tree artwork from source cells 4 and 16 rather than a truncated crop, preserve the world-space trunk/collider, and leave the separate cell-5 shrub/source artwork interpretation unchanged. No Core renderer or public API was changed. App source, credits, README, and tests were updated in the owner's allocated files. The original Tiny Town PNG remained byte-identical (SHA-256 `3A54D99ECDE790D4FDEA207A3644CF130FC56FA838F1BEB1507C185A95B8E902`). The model and native cases became GREEN (`../app/model-green-01.log`, `native-green-01.log`), and the stronger native shape/control case passed (`native-shape-green-01.log`). Exact commands, raw TRXs, source hashes, and test assertions are in `../app/verification.md`.

The final app-owned screenshot is `artifacts/ci/scene-village/screenshots/native-20260925-103407814/decorations.png`, SHA-256 `E3D618AB300C5714416277929B199107AB3D361C9E7B10473C909F97DF097949` (36,172 bytes). It was captured through the application's screenshot API after routed Servo D/S key input. The app worker's final dedicated Release native suite passed **18/18**, zero skipped (`../app/village-full-02.log` and `tests/Cerneala.Tests.SceneVillage/TestResults/decoration-village-full-02.trx`). This does not claim a human visually played the application.

## Input and dirty-work preservation

Before the mandatory FileTree regeneration, `preflight-status.txt`, `preflight-app-status.txt`, and `preflight-app-files.sha256.txt` captured the dirty worktree and hashes. The preflight hash list contains 63 files: **22 app/test source-or-asset inputs plus 41 historical TestResults TRXs**; it is not a claim of 63 source files. All candidate app/test paths were already untracked work from the previous Village task, and no integration worker edited them. Comparing the current source inputs with that preflight identified exactly seven changed app-owned text files: `Assets/CREDITS.md`, `README.md`, `VillageArt.cs`, `VillageGameSurface.cs`, `VillageLayout.cs`, `NativeVillageWindowTests.cs`, and `VillageModelTests.cs`. The other 15 app/test inputs, including both PNG assets, retained their preflight hashes. The exact final seven hashes are in `../app/verification.md`.

`full-slnx-01/inputs-before.sha256.txt` and `inputs-after.sha256.txt` compare 24 current app/test/Core input hashes immediately before and after the final build; `input-mismatches.txt` reports **none**. That includes the unchanged `UI/Controls/Sprite2D.cs` and Release `Cerneala.dll`; there was no new framework API change. The current worktree is still dirty. The status delta across the full run consisted of this task's new integration evidence outputs, not app/Core source changes (`full-slnx-01/status-before.txt`, `status-after.txt`). Unrelated concurrent map/environment changes were not overwritten or attributed to the decoration fix.

## Final build-enabled solution gate

The exact command is in `full-slnx-01/command.txt`. It ran `dotnet test .\Cerneala.slnx -c Release -m:1` with Windows SDL native opt-in (`CERNEALA_SDL_NATIVE_TESTS=1`) and the existing process-plus-command artifact exclusion (`DefaultItemExcludesInProjectFolder=artifacts/**`). It was **build enabled**, serialized, and wrote unique raw logs/TRXs to `full-slnx-01/`. Exit code: **0**. All 11 TRXs were parsed from result rows and reconciled against their counters (`trx-summary.csv`, `parse-note.txt`):

| Scope | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Entire current solution, 11 TRXs | **6,373** | **0** | **6** |
| Scene Village tests | 18 | 0 | 0 |
| SDL native tests | 935 | 0 | 5 |
| Core tests | 4,163 | 0 | 0 |
| SourceGen tests | 611 | 0 | 0 |
| PreviewHost tests | 17 | 0 | 0 |
| VisualStudio tests | 47 | 0 | 0 |

The six skipped result rows are **not** counted GREEN: one Language warm-completion P95 performance case, one SDL native ownership-lifetime case, and four disabled alpha-content data rows (1, 3, 6, 7). Exact FQNs and TRX filenames are in `full-slnx-01/trx-skips.csv`; `trx-failures.txt` is `None`. The previous seam task's 6,372-pass solution run is historical, not this decoration fix's verification.

## Limits and handoff

- Automated Windows SDL behavior and source preservation are verified. Physical OS Alt-Tab, human visual/play validation, hosted CI, other platforms, and the six skipped cases were **not** validated here.
- The separate cell-5 shrub interpretation was deliberately not redesigned. Any preference for alternative artwork remains a user/art-direction decision, not a hidden renderer or collision fix.
- Task-scoped whitespace and the completely read current FileTree were checked; the shared independent auditor accepted the integrated automated Windows SDL checkpoint. No commit, push, remote CI dispatch, or broad cleanup was requested.
