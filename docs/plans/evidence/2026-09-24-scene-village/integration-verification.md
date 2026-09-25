# Scene Village integration verification — 2026-09-24

This record covers only the separate Windows SDL Scene Village app, its dedicated tests, two solution entries, and the Windows-only workflow. The prior Scene2D checklist plan and the other dirty worktree changes were not edited by this integration task.

## Current executable gate

The single build-enabled final solution invocation is preserved verbatim in `full-slnx/command.txt`:

```powershell
$env:CERNEALA_SDL_NATIVE_TESTS='1'; $env:DefaultItemExcludesInProjectFolder='artifacts/**'; dotnet test .\Cerneala.slnx -c Release -m:1 --nologo -v:minimal --results-directory 'docs/plans/evidence/2026-09-24-scene-village/full-slnx' --logger 'trx;LogFilePrefix=village-full' '-p:DefaultItemExcludesInProjectFolder=artifacts/**'
```

The `artifacts/**` exclusion is a process/command-only response to existing ignored generated C# under `artifacts/rendersurface3d`; no source, project, or artifact was removed to make the build pass. `full-slnx/full-slnx.log` and `full-slnx/exit.txt` record exit **0**. All 11 TRX files were parsed and cross-checked against the 11 unique log references (`full-slnx/trx-integrity.txt`): **6,316 passed, 0 failed, 6 skipped, 6,322 total**. `full-slnx/trx-summary.csv` gives each project result. The new Village test project passed **13/13 with native opt-in and no skips**. `full-slnx/trx-skips.csv` identifies the six previously disabled non-Village cases and their stated reasons; their behavior was not verified by this run.

The app worker's earlier dedicated `native-05` gate passed **13/13** with the same opt-in. Its exact command, log, TRX, intermediate failures, and contract-level observations are in `app/verification.md` and adjacent raw artifacts. The app worker corrected the command transcript to include both environment assignments and PowerShell quoting; that correction and the README launch instruction were evidence/documentation-only, after the source-state test run.

The executable exists at `C:\Users\lauri\Desktop\Cerneala\Playground\Cerneala.SceneVillage\bin\Release\net8.0-windows\Cerneala.SceneVillage.exe` (SHA-256 `2F660A86F311A3E629D05E162232D946EA70DAA309D53129A69494893101AE7A`). For this dirty workspace, the documented safe launcher is:

```powershell
$env:DefaultItemExcludesInProjectFolder = 'artifacts/**'
dotnet build .\Playground\Cerneala.SceneVillage\Cerneala.SceneVillage.csproj -c Release '-p:DefaultItemExcludesInProjectFolder=artifacts/**'
dotnet run --project .\Playground\Cerneala.SceneVillage\Cerneala.SceneVillage.csproj -c Release --no-build
```

The native test launched the real generated SDL_GPU window. A separate interactive `dotnet run` session and human play were **not** performed or claimed.

## Captured visual and asset evidence

The full-solution native run produced four captures through the application-owned screenshot path under `C:\Users\lauri\Desktop\Cerneala\artifacts\ci\scene-village\screenshots\native-20260924-153704537\`: `village.png`, `static-10000.png`, `animated-10000.png`, and `collision-10000.png`. Their exact byte counts and SHA-256 hashes are in `full-run-screenshot-hashes.csv`. All four were opened for visual inspection: the village/house/path/player scene and the three stress presets are present, and the stress HUD displays requested and realized counts of 10,000. This is captured visual evidence, not human gameplay validation or a pixel-conformance verdict.

`built-asset-copy-hashes.csv` verifies that both licensed CC0 PNGs, the original Kenney license file, and `CREDITS.md` were copied byte-for-byte into the built app output. The 10,000 count is realized object membership, **not** 10,000 visible draw calls. No FPS, CPU, allocation, GPU-time, or frame-time threshold was measured.

## Integration surface and remaining limits

`Cerneala.slnx` contains exactly the two new Village project paths, verified by XML parsing and `dotnet sln ... list` in `solution-static-check.txt`. The new `.github/workflows/scene-village-windows.yml` is path-filtered to the two Village directories, solution file, and itself; it has one Windows job with the native opt-in and artifact upload. `workflow-static-check.txt` records structural checks. A local YAML parser/actionlint was unavailable, and no hosted CI run was dispatched; local green tests do **not** establish hosted-runner native availability.

The final full run left the Core and Scene2D Packages assembly hashes identical to the previously reviewed strict ApiCompat inputs (`core-package-binary-hashes.csv`); the prior 23 Core and 20 Packages individually reviewed intentional diagnostics were not rerun for an unchanged public surface. No Village code changes Core or public API documentation.

The pre-Village dirty worktree is archived in `pre-village-git-status.txt` and `pre-village-integrity.txt`. `final-integrity.txt` records no baseline status entries removed and no change to the 22-file Village source/asset hash ledger. It also records 27 later changes outside Village ownership (`CLAUDE.md`, `.claude/skills/**`, and `claude-skills-import/**`); their author and cause were not established here. They were not edited, reverted, or attributed to Village. `final-git-diff-check.txt` records no tracked whitespace error. The new-file text scan in `new-text-mechanical-scan.txt` reports zero issues across 20 Village/workflow/ledger text files, excluding the byte-preserved original license text's initial whitespace-only line. No Git publish or CI dispatch occurred.

Unverified user-facing behavior: physical OS Alt-Tab focus-loss, human play, any performance target at 10,000 objects, and the hosted Windows workflow. Linux, macOS, and WindowsDX are outside this task's Windows SDL platform scope and were not tested.
