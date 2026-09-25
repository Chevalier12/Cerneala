# Scene Village integration baseline — 2026-09-24

This is a separate Windows SDL top-down village task. It does not reopen or edit the completed Scene2D collection plan. The application owner exclusively writes `Playground/Cerneala.SceneVillage/**` and `tests/Cerneala.Tests.SceneVillage/**`; this integration owner writes only `Cerneala.slnx`, any root-approved workflow routing, and this evidence directory. No Git reset, checkout, publish, or CI dispatch is authorized.

`pre-village-git-status.txt` is the raw worktree snapshot **before** these village evidence files were written: 98 modified, 9 deleted, 499 untracked, 0 staged. The existing broad dirty worktree belongs to the user/prior in-scope work and must be preserved. `pre-village-integrity.txt` records HEAD `443f100b86f8c4ca5f5e649651ac37c9cd5a330f`, the original `Cerneala.slnx` SHA-256 `A3D42DA4FFE1D86771B8FF2BB7EF5749ABADBB8840A36B5B04868DAD4D741CDA`, and both new project paths absent. `pre-village-solution-diff.txt` confirms `git diff --quiet -- Cerneala.slnx` exit 0.

The repository FileTree generator reported `FileTree.md` unchanged before exploration; a read-only agent read the full 5,493-line tree. No applicable ancestor/repository/Playground/tests `AGENTS.md` was found. `Cerneala.slnx` has explicit project entries under `/Playground/` and `/tests/`, not project globs. The minimal eventual additions are:

```xml
<Project Path="Playground/Cerneala.SceneVillage/Cerneala.SceneVillage.csproj" />
<Project Path="tests/Cerneala.Tests.SceneVillage/Cerneala.Tests.SceneVillage.csproj" />
```

These entries must wait until the application worker actually creates and freezes both project files. Root `Cerneala.csproj` currently excludes `Playground\**` and `tests\**` from `Compile`, `EmbeddedResource`, `None`, and its `.crn` `AdditionalFiles` glob, so the assigned paths do not enter the root Core project's SDK item set. No repository-root `Directory.Build.props` or `Directory.Build.targets` was found. The application worker corrected its preliminary target-framework guess; the village app is expected to follow the Windows SDL `net8.0-windows` pattern, but the actual new project XML will be inspected before solution integration.

No village build, test, runtime harness, or screenshot has run in this integration setup. Workflow routing remains under read-only inspection; no Linux/macOS obligation, CI dispatch, or workflow edit is inferred merely from the workflow filename.
