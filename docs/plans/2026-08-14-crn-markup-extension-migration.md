# Plan: migration of markup Cerneala to extension `.crn`

> Date: 2026-08-14
> Status: completed
> Dependencies: `docs/plans/2026-08-13-cerneala-language-core.md`, `docs/plans/2026-08-13-cerneala-language-server.md`
> Dependent plan: `docs/plans/2026-08-13-visual-studio-community-extension.md`
> Purpose: we directly and completely replace the compound extension `.cui.xml` with `.crn`, keeping the XML dialect Cerneala unchanged and without temporary compatibility for the old extension.

## 1. Summary and decisions

The Cerneala markup remains the same tolerant and source-generated XML dialect. Only the document name contract changes: `View.cui.xml` becomes `View.crn`, and the companion `View.cui.xml.cs` becomes `View.crn.cs` because the current pairing is defined as `document.Path + ".cs"`.

Migration is an immediate breaking change. The source generator, language server and projects no longer accept `.cui.xml` after the closure of this plan. We do not add transition warning, alias or permanent double support. The decision avoids the Visual Studio conflict between the compound extension and the generic XML content type; we don't change the syntax just to please the editor, as that would be another problem and another plan.

## 2. Baseline and the current problem

- `Cerneala.SourceGen/UiMarkupGenerator.cs` filters `AdditionalText` through `EndsWith(".cui.xml")`.
- `Cerneala.LanguageServer/Workspace/ProjectContext.cs` uploads only additional documents with the same suffix.
- `Cerneala.Language/Semantics/CernealaSemanticModel.cs` remove compound suffix to resolve companion type.
- Five projects include the markup through the globe `AdditionalFiles Include="**\*.cui.xml"`; the Language Server fixture is the sixth owner and explicitly includes `View.cui.xml`.
- The repo contains 16 versioned markup documents and 16 companions with the old convention.
- The Language, SourceGen and LanguageServer tests, the versioned corpus, benchmarks, public documentation and plans contain paths or examples `.cui.xml`.
- The Visual Studio 18.9 spike demonstrated that `DocumentFilter.FromGlobPattern("**/*.cui.xml")` is rejected by the compile-time evaluator of `LanguageServerProvider`, and a `DocumentTypeConfiguration` with `.cui.xml` compiles but the document is actually opened with content type `XML`. The simple extension `.crn` eliminates the ambiguity without the in-proc MEF bridge.

## 3. Objectives

- A single internal contract for extension, detection, logical name and companion path.
- The source generator and the language server exclusively accept `.crn`, case-insensitive.
- All versioned documents and their companions use `.crn`/`.crn.cs` without changing the XML or C# content.
- Build, diagnostics, generated output, semantic pairing and all LSP capabilities remain equivalent after renaming.
- All projects, tests, corpora, benchmarks, documentation and active plans describe `.crn` as canonical extension.

## 4. Non-objectives and stop conditions

- No new declarative format, new parser, new scheme or syntax changes.
- No dual support, redirect, depreciation warning or auto-conversion for `.cui.xml`.
- No changes to the Cerneala public runtime API.
- No Visual Studio MEF/content type bridge; the VSIX plan resumes only after `.crn` is the GREEN contract of the repo.
- If the renaming changes the generated C# output, diagnostics or solving the companion in a way other than the file path, the batch stops and the divergence is investigated in the layer that holds the convention.

## 5. The proposed architecture

`Cerneala.Language` becomes the owner of the convention through a unique internal helper, for example `CernealaDocumentPath`, accessible to friendly assemblies already declared in `Properties/AssemblyInfo.cs`. The helper exposes the canonical suffix `.crn`, case-insensitive checking, logical name derivation, and the companion path `.crn.cs`. `Cerneala.SourceGen`, `Cerneala.LanguageServer` and semantically I use the model instead of duplicate literals.

MSBuild remains the owner of the include files as `AdditionalFiles`, but each project uses the `**\*.crn` glob exclusively. XML content is not interpreted differently. File renaming is done with operations that preserve history and content, including existing local changes.

## 6. Estimated files

- `Cerneala.Language/` for the internal helper of the track convention.
- `Cerneala.Language/Semantics/CernealaSemanticModel.cs`.
- `Cerneala.SourceGen/UiMarkupGenerator.cs`.
- `Cerneala.LanguageServer/Workspace/ProjectContext.cs`.
- `Cerneala.csproj`, `CernealaPresentation/CernealaPresentation.csproj`, the Playground and `tests/Fixtures/LanguageServerWorkspace/LanguageServerWorkspace.csproj` projects.
- The 16 markup files and the 16 companions from `CernealaPresentation/`, `Playground/` and the Language Server fixture.
- Suites `tests/Cerneala.Tests.Language/`, `tests/Cerneala.Tests.SourceGen/` and `tests/Cerneala.Tests.LanguageServer/`.
- Corpus and language benchmarks.
- The documentation from `docs/`, `docs-site/` and the Visual Studio integration plans.

## 7. Implementation stages

### Stage 0 - Baseline and RED tests for the new contract

- [x] Inventory and freeze the list of 16 markup documents and 16 companions that must be renamed, plus all the projects that include them as `AdditionalFiles`. (Inventory: `tests/Cerneala.Tests.Language/Corpus/crn-migration-stage0-inventory.txt`.)
- [x] Add RED tests in `Cerneala.Tests.Language` for the detection of `.crn`, the derivation of the logical name `View` and the pairing `View.crn` -> `View.crn.cs`.
- [x] Add RED tests in `Cerneala.Tests.SourceGen` that request generation for `View.crn` and zero output for the same content provided as `View.cui.xml`.
- [x] Add RED tests in `Cerneala.Tests.LanguageServer` that ask for project ownership and semantic context for `View.crn`, but not for `View.cui.xml`.
- [x] Captures the baseline of the generated output and diagnostics for a representative document before renaming, so that the path change does not mask a semantic divergence. (Hint `ViewFactory.abdf9b8e.g.cs`, SHA-256 `DCD437128E0720708F736B79865B5D8C9F3A1D38C2BA580473DD655E62F4CF9F`, zero diagnostics.)
- [x] Reindex the solution.

**Gate Stage 0**

- [x] The `.crn` tests are RED exclusively because of old literals and globs, not because of the harness.
- [x] The negative tests for `.cui.xml` describe the approved breaking change and do not accept default fallback.

### Stage 1 - The single internal contract and language hosts

- [x] Add the internal path helper in `Cerneala.Language` with the `.crn` extension, case-insensitive comparison, logical name and companion path; does not introduce public API.
- [x] Replaces the private filter from `UiMarkupGenerator.IsMarkupFile` with the common helper.
- [x] Replaces additional documents filtering from `ProjectContext.CreateAsync` with the common helper.
- [x] Replaces the `.cui.xml` strip and the companion build from `CernealaSemanticModel` with the common helper.
- [x] Remove duplicate convention literals from the production code and keep the ownership in `Cerneala.Language`.
- [x] Run the RED tests from stage 0 and the affected Language, SourceGen and LanguageServer target suites. (GREEN: 8 Language, 3 SourceGen and 2 LanguageServer.)
- [x] Reindex the solution.

**Gate stage 1**

- [x] `.crn` is the only extension accepted by all three hosts, and `.cui.xml` is deterministically ignored.
- [x] The root type pairing finds `View.crn.cs`, class-name generation removes exactly `.crn`, and the semantic output remains identical to the baseline.

### Stage 2 - File renaming and MSBuild integration

- [x] Renames all 16 documents `.cui.xml` to `.crn` without changing the content.
- [x] Renames all 16 companions `.cui.xml.cs` to `.crn.cs`, fully preserving the existing local changes.
- [x] Change all six globes `AdditionalFiles` to `**\*.crn` and confirm that no project includes the old extension.
- [x] Updates the Language Server fixture, the `repository-documents.txt` corpus, golden paths and any test resource that encodes the old name.
- [x] Updates the language benchmarks to load the renamed `.crn` documents without changing the measured corpus.
- [x] Build `CernealaPresentation` and the three Playground projects and confirm that the source generator produces the same approved types/hint names. (GREEN: 15 outputs with approved stems; only the hash derived from the path changed.)
- [x] Reindex the solution.

**Gate stage 2**

- [x] There are no more `*.cui.xml` or `*.cui.xml.cs` versioned files except the temporary evidence of the spike, which are deleted before the checkpoint.
- [x] All real projects and the external fixture compile using `AdditionalFiles` `.crn` exclusively.

### Stage 3 - Complete migration of tests and corpora

- [x] Updates all path literals from the Language tests for `.crn`, including recovery, semantic scopes, navigation, formatting, completion and sourcegen parity.
- [x] Updates all path literals from the SourceGen tests, including Application, bindings, Motion, Prism and presentation regression.
- [x] Updates all path literals from the LanguageServer protocol tests, including workspace reload, diagnostics, completion, navigation, formatting, structure and hardening.
- [x] Keep `.cui.xml` examples only in explicit negative tests that verify the rejection of the breaking change.
- [x] Runs `dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj`, `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj` and `dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj`. (GREEN: 125 + 431 + 30 tests.)
- [x] Reindex the solution.

**Gate stage 3**

- [x] All parity, corpus and protocol tests are GREEN on `.crn`.
- [x] No language capability is accidentally dependent on the fact that the document had the final extension `.xml`.

### Stage 4 - Documentation and dependent plans

- [x] Update the active guides from `docs/` and the product pages from `docs-site/` so that the examples, MSBuild setup and descriptions use `.crn`.
- [x] Updates the `docs-site/documentation/classes/Cerneala.SourceGen.UiMarkupGenerator.md` API page with the `writing-api-documentation` skill; check the manifest, without the new page if the API name does not change. (The existing page remains in the manifest; it has not been added or renamed.)
- [x] Updates the other API pages that explicitly describe `.cui.xml`, without changing unrelated documentation.
- [x] Updates the Language/Core, LanguageServer and Visual Studio index plans to contract `.crn`, keeping the historical checkmarks valid.
- [x] Add this plan as an explicit dependency of `2026-08-13-visual-studio-community-extension.md` and change activation/document filters/fixtures to `*.crn`.
- [x] Document the breaking change in the startup guide: rename `View.cui.xml` -> `View.crn`, `View.cui.xml.cs` -> `View.crn.cs` and `AdditionalFiles` -> `**\*.crn`; no promise of compatibility.

**Gate Stage 4**

- [x] The public documentation describes a single contract `.crn`, and the VSIX plan no longer requires a workaround for generic XML.
- [x] Any remaining occurrence of the text `.cui.xml` is either in this migration history or in an explicit negative test and has a verifiable reason. (Intentional exceptions: the migration guide, this plan and the historical index/spike, the Stage 0 inventory, and the three negative tests.)

### Stage 5 - Final verification and closing of the breaking change

- [x] Run the builds of the `CernealaPresentation` projects, Playground and the LanguageServer fixture after the last relevant modification. (GREEN; Stage 2 proofs remained valid for untouched projects, and `CernealaPresentation` was rebuilt after the markup was updated.)
- [x] Runs the targeted Language, SourceGen and LanguageServer suites in the final state. (GREEN: 125 + 431 + 30 tests.)
- [x] Runs `dotnet test .\Cerneala.slnx` only once after the last change of code/project/renamed file. (Final GREEN: 3,507 tests; a transient allocation-gate failure passed in isolation and on full rerun without code change.)
- [x] Check through the inventory that there are 16 documents `*.crn`, 16 companions `*.crn.cs` and zero real files `*.cui.xml`/`*.cui.xml.cs`.
- [x] Run `git diff --check`, inspect rename detection and confirm that the renaming did not lose pre-existing local changes. (32 mappings: 27 blobs identical to HEAD and 5 renames with expected changed content; all 32 kept SHA-256 on move.)
- [x] Reindex the final solution and request indexing without new errors.

**Gate Stage 5**

- [x] The build, source generator, semantic model and LSP use exclusively `.crn` and all suites are GREEN.
- [x] The migration is documented as a complete breaking change; there is no alias, temporary warning or hidden dual support.

## 8. Recommended order

1. Close stages 0-5 of this plan in order, one atomic batch per stage.
2. `docs/plans/2026-08-13-visual-studio-community-extension.md` from Stage 0 resumes only after the final gate.
3. Restore the VSIX spike directly on the simple document type `.crn`; do not reuse the abandoned `.cui.xml` bridge.

## 9. The definition of ready

- [x] `.crn` is the only Cerneala markup extension supported by build, semantic model, language server and projects.
- [x] All versioned documents and companions are renamed without changing the content or behavior generated.
- [x] `.cui.xml` is explicitly rejected and appears only in historical evidence or approved negative tests.
- [x] All targeted tests, consuming projects and `dotnet test .\Cerneala.slnx` are GREEN.
- [x] The documentation, API docs and dependent plans are synchronized with the breaking change.
