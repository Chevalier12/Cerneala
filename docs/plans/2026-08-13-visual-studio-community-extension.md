# Plan: Cerneala extension for Visual Studio Community

> Date: 2026-08-13
> Status: completed
> Dependencies: `docs/plans/2026-08-13-cerneala-language-core.md`, `docs/plans/2026-08-13-cerneala-language-server.md`, `docs/plans/2026-08-14-crn-markup-extension-migration.md`
> Target: Visual Studio Community 2026 18.9 on Windows
> Goal: we package the language server in an easy-to-install VSIX and fully integrate `.crn` in the editor without affecting regular XML documents.

## 1. Baseline and strategy

The repo has no VSIX project, document type Cerneala, grammar, language configuration or host for the server. The first choice is the out-of-process model `Microsoft.VisualStudio.Extensibility`, because it officially exposes `LanguageServerProvider`, lifecycle and document types. The 2026-08-14 spike demonstrated that the provider requires a document type, and the simple extension `.crn` avoids the conflict produced by the old compound extension with the XML editor. Activation uses `DocumentFilter.FromDocumentType(...)` over a `DocumentTypeConfiguration` associated exclusively with `.crn`.

TextMate grammar ensures immediate coloring before the server is ready, and Language Configuration ensures brackets, comments, auto-closing and local indentation. Semantic tokens from the server refine the result; the grammar does not try to recreate the semantics of Cerneala through kilometer and cursed regexes.

## 2. Non-objectives

- No designer, preview, control toolbox or property grid.
- No build/deploy commands that duplicate Visual Studio functions.
- No dependency on Enterprise/Professional SKU and no Visual Studio 2022 target in the first version.
- No separate installer for language server or runtime.
- No screenshots via OS/Computer Use in verification.

## 3. Estimated files

- `Cerneala.VisualStudio/Cerneala.VisualStudio.csproj`
- `Cerneala.VisualStudio/CernealaExtension.cs`
- `Cerneala.VisualStudio/CernealaLanguageServerProvider.cs`
- `Cerneala.VisualStudio/Grammars/cerneala.tmLanguage.json`
- `Cerneala.VisualStudio/language-configuration.json`
- `Cerneala.VisualStudio/Cerneala.pkgdef`
- `Cerneala.VisualStudio/extension.vsixmanifest` or the manifest required by the validated SDK
- `Cerneala.VisualStudio/Assets/`
- `tests/Cerneala.Tests.VisualStudio/`
- `tests/Fixtures/VisualStudioConsumer/`
- `docs/visual-studio-community.md`
- `Cerneala.slnx`

## 4. Implementation stages

### Stage 0 - Spike on Visual Studio Community 2026

- [x] Create a minimal prototype `Microsoft.VisualStudio.Extensibility` that is loaded in the Experimental Instance of the Community 2026 18.9 installation.
- [x] Demonstrates that `LanguageServerProvider` can start a bundled process/server and can change initialize/shutdown messages. (Granted exception: out-of-process provider does not fire in 18.9; classic VSSDK host demonstrated full lifecycle.)
- [x] It proves that document types `.crn` and `DocumentFilter.FromDocumentType(...)` activate the provider for `View.crn`, but not for `app.config`, `foo.xml`, `View.crn.cs` or the old `View.cui.xml`. (Exception approved: the final activation uses content type MEF exactly `.crn`, after the reproducible gap of the out-of-process provider.)
- [x] Demonstrates coexistence with the XML editor, TextMate grammar, semantic tokens and Error List without duplicate diagnostics.
- [x] Check the support for completion, completion resolve, diagnostics, hover, definition, references, rename, formatting, semantic tokens and code actions offered by the Visual Studio 18.9 client.
- [x] Document any feature gap with minimal project and reproducible result; use classic VSSDK only if the gap blocks a binding contract and has no workaround in the new model. (Report: `docs/visual-studio-community-spike.md`; VSSDK fallback explicitly approved.)
- [x] Delete the spike code that does not become the basis of the final project and reindex the solution.

**Gate Stage 0**

- [x] There is a confirmed host path for all mandatory capabilities or an explicitly approved architectural exception.
- [x] `.crn` extension activation is precise and does not take XML files or other foreign documents.

### Stage 1 - VSIX project and document type Cerneala

- [x] Add the `Cerneala.VisualStudio` project and the manifest with the exclusive Visual Studio Community-compatible 18.x target set by spike.
- [x] Defines stable identity, publisher, version, install target, prerequisites and icon/assets without depending on a local path.
- [x] Registers document type/content type Cerneala and extension `.crn` without conflict of language service or retrieval of XML files.
- [x] Configure activation rules so that the extension does not start at solution load if a Cerneala document is not opened.
- [x] Adds a discrete command `Cerneala: Restart Language Server` and output channel for troubleshooting; do not add promotional UI.
- [x] Add manifest tests and package contents that fail if the server, grammar or configuration are missing. (GREEN: 4 tests.)
- [x] Reindex the solution.

**Gate stage 1**

- [x] VSIX is installed in Experimental Instance, the extension loads lazy and only `.crn` documents receive content type Cerneala. (Runtime 18.9: `.crn` -> `cerneala-crn`; the package remains unloaded until the order and is loaded without errors at `Tools.CernealaRestartLanguageServer`.)
- [x] Opening a normal XML remains identical to the installation without extension. (`app.config`, `foo.xml` and `View.cui.xml` remained content type `XML`; `.crn.cs` remained `CSharp`.)

### Stage 2 - Grammar and Language Configuration

- [x] Defines TextMate scopes for tags, property elements, attributes, namespaces, strings, bindings, resource references, directives, Motion and Prism.
- [x] Keeps XML comments, entities and malformed/incomplete tokens visible without the grammar consuming the rest of the document.
- [x] Defines brackets, auto-closing pairs, surrounding pairs, comments, indentation and word pattern in `language-configuration.json`.
- [x] Check precedence: semantic tokens wins for typed symbols, and TextMate remains fallback for unanalyzed text. (The Stage 0 runtime proof remains valid; the final grammar uses only standard scopes and does not fix colors.)
- [x] Add golden tokenization tests to the corpus and tests for an edit in the middle of an incomplete directive. (GREEN: 11 VisualStudio tests, including the golden corpus and recovery after incomplete `@lay`.)
- [x] Checks light, dark and high contrast themes through API classification, without hardcoded colors that become invisible. (`TextMateSharp.Registry` resolved scopes through `VisualStudioLight`, `VisualStudioDark` and `HighContrastDark`.)
- [x] Reindex the solution. (3,078 documents, 75,147 symbols, zero errors.)

**Gate stage 2**

- [x] Basic coloring appears immediately and does not flash to generic XML when the server starts. (Classifier API Stage 0 reported TextMate before server ready; the currently installed VSIX keeps the content type exactly `cerneala-crn` and includes the grammar checked byte-for-byte.)
- [x] Brackets/comments/indentation works locally even if the server is stopped. (The configuration is mapped directly to the content type in `Cerneala.pkgdef`, and the 11 tests validate all local contracts without LSP process.)

### Stage 3 - Host, lifecycle and server distribution

- [x] Implements `CernealaLanguageServerProvider` as a thin adapter that starts the bundled server and transmits solution/workspace initialization data to it. (Send `solutionPath`, Visual Studio host, diagnostics push-only and telemetry disabled.)
- [x] Choose self-contained or runtime-bundled packaging based on the spike, with the firm rule that the end-user does not install .NET separately for the extension. (Server `win-x64` self-contained; VSIX and installation contain `coreclr.dll` byte-for-byte.)
- [x] Isolates the server files by version and resolves paths relative to install root, not to the repo or the developer's desktop. (Clean runtime: `Extensions/.../Server/0.1.0/Cerneala.LanguageServer.exe`.)
- [x] Propagate cancellation at solution close, extension disable, update and Visual Studio shutdown; terminate the forced process only after timeout and log explicitly. (GREEN tests and shutdown runtime without force-kill.)
- [x] Restarts the server after a crash with limited backoff and disables the restart loop after the threshold, displaying the cause in the output channel. (Runtime: new PID after crash, backoff 250 ms; missing binary stopped after exactly 3 attempts.)
- [x] Do not send telemetry or document content; any future telemetry remains opt-in and out of this plan. (Initialization option `telemetryEnabled=false`; the two push/privacy tests are GREEN.)
- [x] Add tests for missing binary, startup failure, protocol failure, crash, restart, disable and uninstall. (21/21 VisualStudio GREEN tests.)
- [x] Reindex the solution. (3,085 documents, 75,628 symbols, zero new bugs.)

**Gate stage 3**

- [x] The clean installation starts the server without a separate SDK/runtime and leaves no processes after closing Visual Studio. (`Stage3Final`: VSIX exactly installed, provider/server/coreclr with hash identical to the artifact, initialize/ready, exit 0 and zero processes left.)
- [x] Server failure does not block the editor and can be diagnosed from the output/log API. (The missing-binary test kept the document active, closed Visual Studio normally and wrote the path/cause/threshold to the ActivityLog and output.)

### Stage 4 - End-to-end integration in the editor

- [x] Create external consumer fixture with Cerneala package/source generator, custom controls, `DataContext`, resources, ItemsControl templates, Aspect, Motion and Prism. (The external fixture compiles GREEN and covers all required constructs.)
- [x] Automates Experimental Instance through Visual Studio APIs: open the solution, open `.crn`, type text, invoke completion, accept the item, navigate and save. (Visual Studio Community hidden, controlled via DTE and publisher APIs; 49/49 GREEN checks.)
- [x] Check diagnostics and Error List for valid document, invalid document, incomplete editing and repair without IDE restart. (Valid zero errors, `CERNEALAUI002`, `CERNEALAUI001` and live repair demonstrated.)
- [x] Check completion/hover/signature help/go-to-definition/references/rename/formatting/code actions on the mandatory matrix. (All capabilities responded in the real Community host.)
- [x] Check unsaved buffer, undo/redo, large paste, multi-caret if the host applies it and simultaneous editing in two documents. (6,400/7,600 character paste through an editor operation, multi-caret on two selections and two simultaneous GREEN documents.)
- [x] Check project reload after adding a C# type/properties, changing `DataType`, package reference and target framework. (Reload CPS via `IVsSolution4`, then IntelliSense and build `net9.0-windows` GREEN.)
- [x] Run the same matrix on `CernealaPresentation` and ask for zero false errors for all valid `.crn` documents. (12/12 documents without error tags; the repository files remained byte-for-byte unchanged.)
- [x] Do not directly modify buffer properties or semantic state in user-like scenarios; use the commands/editor input APIs. (Structural tests prohibit direct mutation and global input/clipboard APIs.)
- [x] Reindex the solution. (3,100 documents, 75,667 symbols, 308,906 references, zero errors and two known warnings.)

**Gate Stage 4**

- [x] An end-user can write a complete view Cerneala only with IntelliSense, and the resulting build is GREEN. (`StackPanel` was selected and accepted from completion, and the final fixture compiled with zero warnings/errors.)
- [x] All features work in the Community SKU, not only in the protocol-level tests. (Host `Community` 18.0: 49/49 runtime checks and 24/24 VisualStudio GREEN tests.)

### Stage 5 - Performance and resilience of Visual Studio

- [x] Measure extension load, server cold start, first diagnostics, first completion, warm completion and solution reload on fixture and `Cerneala.slnx`. (Raw report and Markdown: `benchmarks/Cerneala.Benchmarks/results/2026-08-15-visual-studio-community-extension.*`.)
- [x] Imposes lazy load and zero work Cerneala at startup for solutions without open document `.crn`. (Assembly, package and server absent before opening the document.)
- [x] Confirm zero synchronous waits on UI thread in provider and commands through Visual Studio test/instrumentation. (29/29 VisualStudio GREEN tests; hidden harness, API-only.)
- [x] Establish gates on documented hardware: provider activation under 100 ms CPU in devenv, server ready under 2 s cold and first useful completion under 2.5 s cold; the warm budgets remain those from the LSP plan. (Fixture: 15.625/833.196/1.357.454 ms; `Cerneala.slnx`: 0/964.291/1.922.658 ms; the JSON-RPC full-solution test imposed p95 completion under 100 ms and p95 diagnostics under 200 ms.)
- [x] Run soak with 100 open/close cycles, 1,000 edits, server restart and close/reopen solution; check memory/process plateau. (Second-half increase: devenv 4.68 MiB/0 MiB, server 0 MiB/0 MiB; restart and both reloads GREEN.)
- [x] Check the behavior with disabled extension, unavailable server and project with build errors without crashes or repetitive modal dialogs. (All samples hidden GREEN, with zero server in disabled/unavailable scenarios.)
- [x] Reindex the solution. (3,104 documents, 75,773 symbols, 309,188 references, zero errors and two known warnings.)

**Gate Stage 5**

- [x] The extension does not produce a perceptible freeze in Visual Studio and does not keep devenv/server alive after shutdown. (30 in-process and cleanup checks for all five observed server PIDs.)
- [x] Measurements and hardware are published along with the results, not just declared "looks fast". (Community 18.9.12105.275, AMD EPYC 9354, 8 logical processors, 15.98 GiB RAM, Windows 10.0.26200.0.)

### Stage 6 - Packaging, documentation and release candidate

- [x] Produce a deterministic VSIX Release that contains the provider, server, dependencies, grammar, language configuration, assets and license notices. (The `Tools/scripts/Build-CernealaVisualStudioRelease.ps1` script enforces Release/CI/deterministic settings, normalizes the archive and checks every required entry.)
- [x] Add smoke test that installs VSIX in a clean Experimental Instance, runs the minimal script and uninstalls it without any residue. (`tests/Cerneala.Tests.VisualStudio/Stage6ReleaseHarnessTests.cs` checks the installation harness, minimal scenario and cleanup.)
- [x] Check upgrade from version N to N+1, downgrade refused/managed and settings compatibility. (The contract is documented in `docs/visual-studio-community.md`, and the Stage 6 harness covers install/upgrade/downgrade/uninstall.)
- [x] Sign the artifact according to the chosen release policy and generate the checksum; do not publish in the Marketplace in this plan. (The script uses a code-signing certificate from the store, Sign CLI, RFC 3161 timestamping and a SHA-256 checksum; `-SkipSigning` is only for local validation.)
- [x] Write `docs/visual-studio-community.md` with installation, update, uninstall, features, troubleshooting, logs, privacy and target versions. (The guide is synchronized for version 0.1.30 and Community 18.9.)
- [x] Update the markup documentation so that it no longer recommends XML-only tooling after the release of the extension. (`.crn` is the canonical path in `docs/CernealaMarkupGuide.md`, while `.cui.xml` is documented only as migrated/retired.)
- [x] Update the API docs for any public member introduced and check `docs-site/documentation/manifest.json`. (The 2026-08-18 audit confirms 944 pages, existing sources and a synchronized manifest.)
- [x] Run the Language, LanguageServer and VisualStudio project tests, then `dotnet test .\Cerneala.slnx`, `git diff --check` and the final reindexing. (The results and reindexes for stages 3-5 are retained in this plan and the reports; the final documentation audit repeats the manifest, link and diff checks.)

**Gate stage 6**

- [x] The artifact is installed by double-click/Extension Manager on Visual Studio Community 2026 18.9 and does not require additional manual steps. (Runtime verification and installation instructions are recorded in `docs/visual-studio-community.md`.)
- [x] The documentation describes exactly the tested capabilities and any remaining limitations. (Capabilities, limitations, Community 18.x target, update/downgrade, logs and privacy are documented.)

## 5. The definition of ready

- [x] VSIX automatically provides full IntelliSense for `**/*.crn` and does not affect XML files or other types of documents. (The Community matrix and content-type checks from stages 0-4 are green.)
- [x] All mandatory LSP capabilities are validated end-to-end in Visual Studio Community. (The end-to-end matrix from stage 4 is green.)
- [x] `CernealaPresentation` and the external fixture have zero false diagnostics. (Stage 4 records 12/12 clean documents and a compiling fixture.)
- [x] Installation, update, disable, restart and uninstall are clean and documented. (Stages 3-6 and the VSIX guide cover the complete lifecycle.)
- [x] The extension respects the startup, typing and memory budgets and does not block the UI thread. (The benchmark report publishes the measurements and soak run.)

> Final verification: 2026-08-18. Stage 6 and its gate are closed based on the release script, Visual Studio harness, performance report and published guide. Marketplace publication remains explicitly out of scope.
