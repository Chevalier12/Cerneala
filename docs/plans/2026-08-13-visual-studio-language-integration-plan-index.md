# Plan index: full integration Cerneala in Visual Studio Community

> Date: 2026-08-13
> Status: completed
> Validated target: Visual Studio Community 2026 18.9 on Windows
> Source of decision: the discussion about IntelliSense for `.crn` and the requirement that the valid markup Cerneala does not produce spurious errors in the editor
> Goal: we deliver full language support for Cerneala in the Visual Studio Community, without duplicating the parser and semantics between source generator, language server and VSIX extension.

## 1. Baseline and the current problem

`Cerneala.SourceGen/UiMarkupGenerator.cs` now consumes files `*.crn` from `AdditionalFiles`, and the final kernel and server use the same editor-agnostic semantics. Prior to VSIX, the file contract migrates through the dedicated plan to the simple extension `.crn`, because the Visual Studio 18.9 spike demonstrated that the compound extension remains classified as generic XML. The build understands Cerneala, but the Visual Studio editor cannot provide typed completions, navigation, diagnostics Cerneala or recovery without the VSIX host.

An XSD can only describe a static slice of the XML structure. It cannot correctly represent typed bindings, scoped resources, `DataContext`, `Aspect`, templates, Motion, Prism or C# project symbols. The solution must use the same core language for build and editor; otherwise we end up with two truths that swear over the fence.

Visual Studio officially offers a `LanguageServerProvider` out-of-process, document filters and LSP integration. The coloring and local behavior of the editor can be completed through Language Configuration and TextMate grammar. Implementation references: [Language Server Provider](https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/language-server-provider/language-server-provider?view=visualstudio), [LSP extension](https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=visualstudio), [Language Configuration](https://learn.microsoft.com/en-us/visualstudio/extensibility/language-configuration?view=visualstudio).

## 2. The contract for "full syntax support"

- Any `.crn` document that compiles without Cerneala diagnostics must have zero false Cerneala diagnostics in the editor.
- The source generator and the editor use the same rules, the same diagnostic codes, the same severities and the same source spans for semantic errors.
- A temporarily incomplete document during typing produces local and recoverable diagnostics, not an avalanche of secondary errors on the rest of the file.
- IntelliSense covers elements, properties, property elements, attached properties, events, values, enums, resources, namespaces, CLR types, `DataType`, `TargetType`, bindings, templates, `Aspect`, Motion and Prism.
- Visual Studio offers completion, signature help where the syntax has arguments, hover, go-to-definition, references, safe rename, document symbols, semantic colorization, folding, formatting and code actions for cases where there is a deterministic repair.
- The extension is activated exclusively for `**/*.crn`; does not confiscate files `.xml` or other documents.
- The installation is done through a single VSIX that includes the server and its dependencies; the user does not manually install a runtime or a separate process.

## 3. Architectural decisions

- `Cerneala.Language` becomes the common core: text source, tolerant parser, syntax tree, diagnostics, semantic model and editor-agnostic services.
- `Cerneala.SourceGen` consumes `Cerneala.Language` and remains the owner of the C# issue; it no longer has a second parsing or binding implementation.
- `Cerneala.LanguageServer` is an out-of-process LSP process that manages document snapshots, Roslyn workspace and IntelliSense operations.
- `Cerneala.VisualStudio` is a thin VSIX host initially based on `Microsoft.VisualStudio.Extensibility`; the fallback to classic VSSDK is allowed only if a reproducible spike demonstrates a blocking feature gap.
- Common core does not depend on Visual Studio, JSON-RPC or UI. VSIX does not contain Cerneala rules and the language server does not issue view code.
- The tolerant parser replaces `XDocument` as the common syntactic truth. `XDocument` does not remain a hidden parallel path in the source generator.
- Roslyn `Compilation` remains the source for types, members, XML documentation and C# symbols. We do not introduce reflection or runtime scanning.

## 4. Plans and dependencies

1. `docs/plans/2026-08-13-cerneala-language-core.md` - tolerant parser, common semantic model and source generator migration.
2. `docs/plans/2026-08-13-cerneala-language-server.md` - LSP server and all IntelliSense capabilities; dependent on plan 1.
3. `docs/plans/2026-08-14-crn-markup-extension-migration.md` - the breaking change from `.cui.xml` to `.crn`; dependent on plans 1-2.
4. `docs/plans/2026-08-13-visual-studio-community-extension.md` - VSIX host, integration editor, packaging and verification in Visual Studio Community; dependent on plans 1-3.

Plan 4 starts only after complete migration to `.crn`; the spike `.cui.xml` remains the architectural evidence, not the basis of the final project.

## 5. Non-objectives

- We do not implement a visual designer, live preview or drag-and-drop XAML designer.
- We do not promise general XAML/WPF/Avalonia compatibility; the extension understands the existing Cerneala dialect.
- We are not targeting Visual Studio 2022 in the first delivery. Backward compatibility gets a separate plan after the Community 2026 target is GREEN.
- We do not automatically publish the extension in the Visual Studio Marketplace. The plan produces a VSIX release-ready, installable and upgradeable; the Marketplace editorial process remains separate.
- We do not invoke the complete build or source generator for each key and we do not use XSD as a semantic model.
- We do not add VS Code support in these plans, although the LSP server must remain host-agnostic.

## 6. Global gates

- [x] After each C# or project change, run `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json`. (Reindexes are recorded in the dependent plans and the current index is valid.)
- [x] Any new or modified public API has the synchronized page in `docs-site/documentation/classes/`, created with the `writing-api-documentation` skill; `docs-site/documentation/manifest.json` is updated when a page is added or renamed. (The 2026-08-18 audit confirms the manifest and all pages/sources.)
- [x] Each IntelliSense feature has tests at the semantic core level, LSP protocol and, where Visual Studio integration can change the behavior, test in Experimental Instance. (The Language, LanguageServer and VSIX plans document green matrices at these levels.)
- [x] The corpus of all files `.crn` from the repo and valid sourcegen tests remains without false diagnostics in the editor. (Stage 4 records 12/12 documents without error tags.)
- [x] Diagnostics sourcegen and LSP are automatically compared by id, severity, message and span; divergence blocks the gate. (The contract and comparison are covered by the Language/LanguageServer plans and the VSIX matrix.)
- [x] Typing tests use incremental edits and editor commands, not direct assignments of semantic state to the model. (The VisualStudio tests and the structural gate in stage 4 confirm this contract.)
- [x] No UI check uses Computer Use or system screen capture. The correctness of IntelliSense is validated through the editor and protocol APIs; if visual proof will be requested, only a capture API provided by the application/extension is used. (The harnesses use DTE/editor APIs and no system capture.)
- [x] Full suite remains GREEN with `dotnet test .\Cerneala.slnx`. (The result is retained by the final Visual Studio integration verification.)

## 7. Delivery order

- [x] Complete plan 1 and demonstrate that the source generator uses the common core exclusively. (Plan `2026-08-13-cerneala-language-core.md` is completed.)
- [x] Complete plan 2 and demonstrate protocol-level all declared capabilities. (Plan `2026-08-13-cerneala-language-server.md` is completed.)
- [x] Complete plan 3 and demonstrate the complete breaking change to `.crn` without hidden dual support. (Plan `2026-08-14-crn-markup-extension-migration.md` is completed.)
- [x] Finish plan 4, install VSIX in a clean Visual Studio Community 2026 instance and run the end-to-end matrix. (Plan `2026-08-13-visual-studio-community-extension.md` is completed.)
- [x] Run the global gate on `CernealaPresentation`, Playground and a minimal consumer project outside the solution. (Stage 4 documents `CernealaPresentation`, `Playground` and the external fixture.)
- [x] Publish the final compatibility, performance and limitations report in the extension documentation. (`docs/visual-studio-community.md` and the Visual Studio Community report publish this information.)

## 8. The definition of ready

- [x] A user installs a single VSIX, reopens Visual Studio Community and automatically receives Cerneala support for `*.crn`. (The guide and release tests confirm lazy activation for `.crn`.)
- [x] All Cerneala constructions accepted by the build have correct colorization and semantic understanding in the editor. (The corpus matrix and grammar/semantic tests are green.)
- [x] Completion, hover, navigation, rename, diagnostics, formatting and code actions work according to dependent plans. (These capabilities are validated in the end-to-end Community matrix.)
- [x] Valid documents in the repo have zero false errors, and invalid documents indicate the relevant token without the unnecessary cascade. (Language and VisualStudio stages report a clean valid corpus and invalid-input recovery.)
- [x] Source generator and language server do not contain concurrent parsers or binders. (Both consume the shared `Cerneala.Language` core.)
- [x] Opening, typing and completing does not block the UI thread of Visual Studio and respects the budgets measured from the dependent plans. (Performance reports and tests are green.)
- [x] VSIX is installed, updated and uninstalled cleanly on Visual Studio Community 2026 18.9. (Stage 6 and the VSIX guide document the complete lifecycle.)

> Final verification: 2026-08-18. The index plan is closed after auditing its dependent plans, the API manifest and the public documentation. Marketplace publication remains out of scope.
