# Scene Village character atlas lines — integration verification

Date: 2026-09-25. This is a separate Village app bug fix, not an amendment to the completed Scene2D, seam, or decoration-art ledgers. The user reported short dark lines above the character when facing right and north. The shared Astra accepted the focused app candidate and corrected Up-pose evidence **before** this final solution run, then accepted the current integrated source, diff, raw verification, and automated Windows SDL checkpoint after the run. That acceptance does not imply human play, held-key captures, GPU timing, or execution of the six skipped cases.

## Reproduction and contract

The app-owned permanent native regression exercised all 16 production character poses at calibrated 2.5 physical pixels per world unit, with exact source origins and source-row alpha checks. Before the fix, it found **134 foreign pixels** on source-transparent top rows: Down 0, Up 30, Left 52, Right 52. Right walk frames 0 and 2 have fully transparent first source rows, while the preceding atlas row contains opaque foot pixels at corresponding horizontal positions. The real production render showed 26 non-background top-row pixels for each of those Right frames; a controlled isolated-cell comparison attributed 22 dark pixels per frame to neighboring atlas content. `../app/character-production-red-01.log` and its TRX preserve this intended RED, not a fixture or build failure. A separate model test was RED because the app-created character used Linear instead of the selected Point mode.

The smallest app-owned fix sets `VillageArt.CharacterSampling` to `DrawSamplingMode.Point` for both `Player` and `StressActor`. It does **not** change the public/global `Sprite2D` Linear default, Core drawing, SDL backend, or the `ch003.png` source bytes. The model contract then passed 1/1. The identical permanent 16-pose native regression passed 1/1 with **zero** foreign top-row pixels while keeping authored neutral-frame top-row hair. `../app/verification.md` (SHA-256 `54B622209F05C2536334F6FA0D390A9041C025D3DBFE5FECE8F9DD6506BC4BDA`) links the exact RED/GREEN commands, raw logs/TRXs, per-pose observations, and app-owned captures. The final dedicated Village native project passed **21/21**, zero skipped (`../app/character-village-full-01.log` and its TRX).

The final 16-pose app-owned capture is `artifacts/ci/scene-village/screenshots/character-production-frames-20260925-163916848/production-frames.png`, SHA-256 `82D610A1C9227300A45C9D4191ADD23ABAAD942767A36A4CFEB95CE5887FD0FB`. The RED image is `.../character-production-frames-20260925-163507639/production-frames.png`, SHA-256 `8BB6BA4FD5EDD17897EE672191F03FE1EBD9A4B3DB3BD0EA42270C76547127C0`. All images were produced through the application-owned screenshot path; no OS screen-copy mechanism was used.

## North/Up evidence limitation

The earlier claim that rows 52–60 were a near-hands band with 372 exact source matches was withdrawn: that ROI was mostly head/neck. The corrected source-image comparison uses hands ROI `x=[8,58), y=[70,86)` and reports **97** near-black observed pixels mapping to source-black texels at the best fit; the 12 alpha-optimal phases leave an inexact whole-image fit and 362 observed non-background pixels outside the nearest binary source mask. This is not an exact model of Linear sampling or proof that every Up dark pixel is authored. The Point Up production capture still retains the visible near-neutral black feature. Current evidence is consistent with source-authored hand/torso details; this fix does **not** silently delete or redraw that feature. Alternative art direction remains a user decision.

Servo `PressKeyAsync` provides a discrete press/release path. Routed Right and Up screenshots are **post-tap IdleRight/IdleUp**, not held-key walk captures; the test observed one WalkRight and one WalkUp callback. The all-pose fixture freezes real production clips, but does not replace a human play session or prove behavior for every GPU/backend configuration.

## Input and dirty-work preservation

Before the mandatory tree generation, integration captured `preflight-status.txt`, `preflight-scoped-status.txt`, and `preflight-inputs.sha256.txt`: 22 app/test source-or-asset inputs plus 12 selected Core Sprite/drawing/SDL shader/backend/binary inputs, **not** historical TestResults outputs. The app/test paths were already untracked prior Village work; the Core/backend paths carried the completed seam-task dirty state. The current candidate differs from that preflight in exactly four existing app-owned text files (`Assets/CREDITS.md`, `README.md`, `VillageArt.cs`, `VillageModelTests.cs`) plus new `NativeCharacterAtlasTests.cs`. All selected Core/backend sources and binaries, the global sampling default implementation, and `Assets/ch003.png` retain their preflight hashes (`full-slnx-01/preflight-delta.txt`, `inputs-before.sha256.txt`). No integration worker edited app or Core code.

The full solution's `inputs-before.sha256.txt` and `inputs-after.sha256.txt` compare **35/35** current app/test/Core inputs byte-for-byte; `input-mismatches.txt` is `None`. The dirty worktree was preserved. The status delta during this run consists only of this task's new integration result/evidence files, not app/Core source edits (`full-slnx-01/status-before.txt`, `status-after.txt`). The separate FileTree read-only-agent scope violations are recorded in `workflow-exception-filetree.md`; no reset or rollback was performed.

## Build-enabled Windows SDL solution gate

`full-slnx-01/command.txt`, `output.log`, `exit.txt`, and 11 unique raw TRXs preserve the exact current command. It ran `dotnet test .\Cerneala.slnx -c Release -m:1` **with build**, Windows SDL native opt-in (`CERNEALA_SDL_NATIVE_TESTS=1`), and process-plus-command `DefaultItemExcludesInProjectFolder=artifacts/**`. Exit code was **0**. Every TRX result row reconciles against its total (`trx-summary.csv`, `parse-note.txt`):

| Scope | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Entire current solution, 11 TRXs | **6,376** | **0** | **6** |
| Scene Village project | 21 | 0 | 0 |
| SDL native project | 935 | 0 | 5 |
| Core tests | 4,163 | 0 | 0 |
| SourceGen tests | 611 | 0 | 0 |
| PreviewHost tests | 17 | 0 | 0 |
| VisualStudio tests | 47 | 0 | 0 |

The six skipped result rows are **not** GREEN: one Language warm-completion P95 performance case, one SDL native ownership-lifetime case, and four disabled alpha-content data rows (1, 3, 6, 7). Exact FQNs and TRX filenames are in `full-slnx-01/trx-skips.csv`; `trx-failures.txt` is `None`. The earlier decoration task's 6,373-pass solution run is historical, not verification of this character fix.

## Remaining limits

Automated Windows SDL evidence is complete for the reported foreign top-row atlas bleed. Human play/visual confirmation, a held-key screenshot of every walk pose, physical OS Alt-Tab, hosted CI, other platforms/backends, GPU timing, and the six skipped cases were **not** validated here. The Up authored-black feature was not removed, and the earlier incorrect 372-match ROI claim remains withdrawn. Task-scoped whitespace and the final generated/read FileTree were checked; the shared independent auditor accepted the integrated automated Windows SDL checkpoint. No commit, push, or unrelated cleanup was requested.
