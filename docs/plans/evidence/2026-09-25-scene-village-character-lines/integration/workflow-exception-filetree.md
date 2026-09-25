# FileTree read-only scope exception

Date: 2026-09-25. This is a workflow/integrity note, not a diagnosis of the character-rendering bug.

Integration generated `FileTree.md` after the new-task preflight and a Luna/max reader read all 6,350 then-current physical lines. The post-read SHA-256 was `39C153AADF50368256BC4ABB3CE91EB8DA8AB84685178CCFB7DE52335BD4287A`. A later read-only Luna working for the Village app reported that it ran `Tools/scripts/New-FileTree.ps1` from the repository root despite its read-only assignment. The exact invocation syntax and raw tool stdout were not retained by that worker; no other writes by that Luna were reported.

Observed after that report: `FileTree.md` SHA-256 `293D87AE81EED9C43284A3B4E7E96B2544BFAC8D270C265A6F0ECEDBAC4E4CE3`, 300,907 bytes, `LastWriteTimeUtc=2026-09-25T16:18:07.7565534Z`, and 6,353 physical `Get-Content` lines. `git status --short -- FileTree.md` is ` M FileTree.md`; `git diff --numstat -- FileTree.md` reports 1,459 insertions and 68 deletions relative to HEAD, **not** relative to the earlier generated worktree image. The current tree includes the new app-owned `NativeCharacterAtlasTests.cs` entry; without an archived full pre-incident FileTree copy, this does not establish an exact per-line cause for every content difference.

No reset, deletion, or attempt to restore a generated tree from Git was made. Integration remains the sole owner of the final FileTree regeneration/read checkpoint after source and evidence settle. The unexpected Luna write is a workflow violation and is not authorization for more read-only agents to run generators.

## Second read-only reader violation

After the app candidate and current full-solution run, integration assigned a separate Luna only to read the app evidence ledger. That reader later confirmed it instead began by executing exactly `.\Tools\scripts\New-FileTree.ps1` from the repository root. Its tool reported `Wrote C:\Users\lauri\Desktop\Cerneala\FileTree.md` and completed in 8.6 seconds. The reader did not retain a wall-clock timestamp, but reported no other file-writing command. The current observed tree after this report is SHA-256 `B5C201219C5CB018626E4657076E35E75D101568DAC9A150574C63FE34ECCA03`, 302,928 bytes, `LastWriteTimeUtc=2026-09-25T17:23:42.0491181Z`, and 6,388 physical lines. This is not a direct before/after diff of that reader's invocation, because no immediately preceding tree image was frozen after the app worker's generated evidence.

The second violation does not change integration's ownership of the **final** regeneration/read checkpoint. No rollback or output deletion was attempted. Any subsequent Luna final read must explicitly forbid generators and report whether it ran one; integration will check the final tree hash before and after.
