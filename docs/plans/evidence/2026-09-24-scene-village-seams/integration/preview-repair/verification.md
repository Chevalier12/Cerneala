# PreviewCompiler current-build target-name repair

Date: 2026-09-25. This is the user's explicitly authorized narrow repair of the sole failure in the first seam full-solution run. It is not a change to the completed Scene2D plan or to the seam renderer implementation.

## Observed failure and contract

- The build-enabled full solution run in `../final-full-slnx-01/` exited 1. Its 11 TRXs contain 6,368 passed, **one failed**, and six skipped tests. The sole failure was `RenderSurface3DPreviewTests.PreviewInstantiatesPairedThreeDimensionalSurfaceWithSampleGeometry`: `PreviewCompilation.TargetTypeName` was bare `RenderSurface3DPreview` instead of `Cerneala.SdlGpuSmoke.RenderSurface3DPreview`. The failure occurred before that test loaded the assembly or captured/rendered the 3D marker. `../final-previewhost-focused-01/` repeated the same failure 1/1 from current binaries.
- For a paired `.crn` and `.crn.cs` type, current-build-output reuse must return the companion type's qualified name, consistent with the existing dynamic semantic path. An existing unpaired saved-markup characterization separately requires reuse of its built PE and assembly identity; it did not promise a qualified target name. Root selected preservation of that unpaired behavior rather than silently changing the test or declaring an unsupported new contract.
- Source trace: `TryUseCurrentBuildOutput` previously returned `Path.GetFileNameWithoutExtension(documentPath)` while `ResolveTargetTypeNameAsync` selected the first companion-declared `INamedTypeSymbol` whose simple name matched the document and returned its semantic display name. A timestamp-current Debug Smoke DLL made the fast branch eligible in this run. Its absence/state in the earlier green run was not frozen, so this does **not** establish why the earlier run passed.

## Permanent RED and repair

- New `tests/Cerneala.Tests.PreviewHost/PreviewCompilerFastPathTargetTypeTests.cs` uses GUID-owned isolated test temp projects; it never changes the repository Smoke DLL or its timestamps. Its paired fixture copies a real PE containing two `View` types in different namespaces, chooses the later metadata token as the target, and proves the dynamic semantic name and built-output bytes before asserting fast-path parity. `red-03/` compiled and failed this exact name assertion (bare `View`) for the intended reason. The conditional-companion case also failed as bare `View`, after proving the dynamic project path resolved the configured namespace. Earlier `red-01/` and `red-02/` are preserved test-development evidence; `red-03/` is the final paired RED.
- The full PreviewHost project then exposed an existing unpaired reuse case failing under the first conservative fallback (`green-project-01/`: 15 passed, one failed). A third permanent test encoded that established branch, and `unpaired-red-01/` failed for the intended reason: it received dynamically emitted `Sample` instead of the copied PE's assembly identity.
- `Cerneala.PreviewHost/PreviewCompiler.cs` now shares the same companion declaration-selection predicate with the dynamic resolver. For a plain, unique, top-level, non-generic companion without directives, the fast branch resolves its semantic name in a small Roslyn declaration compilation and verifies that exact namespace/name as a top-level type in the current built PE. If identity cannot be established, it falls through to the existing project semantic compilation rather than guessing another qualified name. A document with **no** companion keeps its previous built-PE reuse, assembly name, and bare filename target; no full-name guarantee is claimed for that legacy unpaired case. No new dependency or public API was added.

## Verification and limits

| Evidence | Result |
| --- | --- |
| `green-focused-03/` (new paired, conditional, unpaired tests; original 3D preview; existing unpaired characterization) | 5 passed, 0 failed/skipped, exit 0 |
| `green-project-02/` (whole `Cerneala.Tests.PreviewHost` project) | 17 passed, 0 failed/skipped, exit 0 |
| `green-visualstudio-01/` (dependent `Cerneala.Tests.VisualStudio` project/VSIX build) | 47 passed, 0 failed/skipped, exit 0 |

Each named directory contains its exact command, raw output, exit code, and TRX. `green-focused-01/` is a superseded mechanical compile failure from an ambiguous `Document` type name introduced by the metadata import; it was fixed by qualifying `Microsoft.CodeAnalysis.Document` before the green runs. Current `PreviewCompiler.cs` SHA-256 is `8E84606012822FCF3E3871DC3B83200EC6D34111E40715695F638B7C93BEA929`; new test SHA-256 is `82EA2203EBB9EB747C1AF6313856773FE3656E96DE0947822565BCCA6DDFCDC3`. Scoped tracked `git diff --check` and full-line whitespace scans of both files found no whitespace error; the only output was Git's LF-to-CRLF advisory. The dedicated temporary fixture parent had no remaining child when inspected.

This is **not** a final full-solution GREEN claim. The build-enabled full solution must be rerun on current source after independent repair audit. No human manual preview validation, fast-path latency benchmark, or universal nested/generic/conditional type-resolution guarantee is claimed.
