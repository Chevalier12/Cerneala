# Plan: Cerneala common language core

> Date: 2026-08-13
> Status: completed
> Dependency: none
> Goal: we extract the parsing and semantics of `.crn` in a tolerant and editor-agnostic kernel, then we migrate the source generator to it without changing the build behavior.

## 1. Baseline and the current problem

`Cerneala.SourceGen/UiMarkupGenerator.cs` detects `*.crn`, protects comparators from directives, wraps the document in an artificial fragment and parses it through `XDocument.Parse`. An unfinished tag or quote invalidates the entire document, acceptable behavior at build, but useless for IntelliSense while the user is typing.

Diagnostics `CERNEALAUI*` are declared in the source generator, and the resolution of types, bindings, resources, templates, Aspect, Motion and Prism is shared between `UiMarkupGenerator.GenerationScope`, partials and Prism subfolders. These types are closely related to `SourceProductionContext`, `XElement` and the C# release. A language server cannot reuse them without running the generator or copying the semantics.

## 2. The target architecture

- New project `Cerneala.Language/Cerneala.Language.csproj`, compatible with `netstandard2.0` for consumption from source generator and without Visual Studio/LSP dependencies.
- Immutable text model with stable offsets, line map, versions and source spans.
- Tolerant lexer/parser for XML Cerneala and embedded languages, with missing nodes and omitted/skipped tokens explicitly represented.
- Syntax tree lossless: trivia, comments, order of attributes and the original text can be reconstructed without loss.
- Semantic model separated from the syntax tree, built on a Roslyn symbol adapter and able to respond incrementally to editor queries.
- Unique catalog for diagnostics Cerneala; the source generator and the language server only convert to the diagnostic type of the host.
- C# output remains in `Cerneala.SourceGen`, but receives already resolved nodes and symbols from the common core.

## 3. Non-objectives

- No LSP protocol, VSIX, UI editor or `Microsoft.VisualStudio.*` dependencies in this project.
- No reflection, runtime XML parser or format change `.crn`.
- No aesthetic rewriting of the generated code if the current output is semantically equivalent.
- No toleration of incomplete documents during build; recovery is for editor analysis, and the compilation of the saved document remains strict.

## 4. Estimated files

- `Cerneala.Language/Cerneala.Language.csproj`
- `Cerneala.Language/Text/` for source text, spans, line map and incremental changes
- `Cerneala.Language/Syntax/` for tokens, nodes, lexer, parser and recovery
- `Cerneala.Language/Semantics/` for compilation context, scopes, symbols and semantic model
- `Cerneala.Language/Diagnostics/` for the common catalog `CERNEALAUI*`
- `Cerneala.Language/Features/` for completing facts, symbol locations and document outline independent of LSP
- `Cerneala.SourceGen/Cerneala.SourceGen.csproj`
- `Cerneala.SourceGen/UiMarkupGenerator.cs` and its partials
- `Cerneala.SourceGen/Prism/**`
- `tests/Cerneala.Tests.Language/`
- `tests/Cerneala.Tests.SourceGen/`
- `Cerneala.slnx`
- API docs from `docs-site/documentation/classes/` for any public type required between assemblies

## 5. Implementation stages

### Stage 0 - Semantic inventory and RED corpus

- [x] Inventory all the constructions accepted by the source generator from `UiMarkupGenerator`, `UiMarkupBindingResolver`, `UiMarkupDirectiveParser`, Motion and Prism and map each construction to the existing tests.
- [x] Builds a versioned corpus from all the `.crn` files in the repo, the documented examples and the valid/invalid markups from `Cerneala.Tests.SourceGen`.
- [x] Add `tests/Cerneala.Tests.Language/Cerneala.Tests.Language.csproj` and a harness that runs the same document through the new parser, semantic model and source generator.
- [x] Add RED tests for incomplete documents by each token category: `<`, element name, attribute, quote, property element, binding, directive body, Motion and Prism.
- [x] Add RED tests that ask for a maximum of one primary diagnosis per broken syntactic area and absent semantic diagnostics under the unrecoverable node.
- [x] Captures the current sourcegen diagnostics baseline by id, severity, message and span for the invalid corpus.
- [x] Reindex the solution.

**Gate Stage 0**

- [x] Each current Cerneala construction appears in the matrix and has at least one valid and one relevant invalid example.
- [x] The recovery tests are RED because of the current dependency on `XDocument`, not because of the harness.

### Stage 1 - Text model and lossless tolerant parser

- [x] Implements source text, line map and application of incremental edits with LSP/Roslyn compatible UTF-16 offsets.
- [x] Implements the lexer for XML delimiters, names, namespaces, strings, comments, CDATA/trivia and embedded text without modifying the directive characters.
- [x] Implements syntax nodes for document, element, attribute, property element, text, comment and error/missing nodes.
- [x] Implements local recovery for missing closing tags, unfinished quotes, overlapping elements, top-level text and EOF inside a node.
- [x] Keep exact source spans for real tokens and define deterministic zero-width spans for missing tokens.
- [x] Add round-trip tests that reconstruct byte-for-byte valid documents, including whitespace and comments.
- [x] Add mutation tests that apply character-by-character edits and confirm that the parser does not throw exceptions.
- [x] Reindex the solution.

**Gate stage 1**

- [x] The parser processes the entire corpus and 10,000 randomized incremental edits without crash, hang or span outside the document.
- [x] The valid documents have a complete tree, and the incomplete ones keep the siblings after the error when the delimitation allows recovery.

### Stage 2 - Common embedded and diagnostic languages

- [x] Moves the grammar for bindings, interpolations and modes from `UiMarkupBindingResolver` to emission-independent syntax nodes.
- [x] Move `@template`, `@when`, `@if`, assignments and the other directives from `UiMarkupDirectiveParser` to an embedded parser with absolute source spans.
- [x] Moves the Motion syntax from `UiMarkupMotionSyntax`, `MotionMarkupLanguage` and related resolvers to the common core.
- [x] Moves Prism syntax and catalog from `Cerneala.SourceGen/Prism/Syntax` and `Prism/Catalog`, without emitter dependency.
- [x] Centralizes the `CERNEALAUI*` descriptors in a host-agnostic catalog with id, severity, message format, category and exact span.
- [x] Defines `Editor` and `Build` modes: same parser and same semantics, but transient incompleteness diagnostics are reduced in editor and strict in build.
- [x] Add recovery tests for each embedded language, including braces, commas, quotes, comparators and unfinished nesting.
- [x] Reindex the solution.

**Gate stage 2**

- [x] No embedded parser receives `XText`, `XElement` or `SourceProductionContext`.
- [x] Existing valid diagnostics keep their id and message; any span change is explained by a more precise localization and approved in the golden files.

### Stage 3 - Semantic Workspace and Roslyn adapter

- [x] Defines `CernealaCompilation`, `CernealaDocument` and `CernealaSemanticModel` with explicit lifecycle and cancellation.
- [x] Defines the minimum adapter over Roslyn `Compilation`, `ITypeSymbol`, members, accessibility, inheritance, XML docs and source locations.
- [x] Resolve `clr-namespace`, aliases, root type, paired `.crn.cs`, `Application`, `Window`, `UserControl` and custom controls through the project symbols.
- [x] Model content properties, normal properties, property elements, attached properties, events and existing literal conversions.
- [x] Separate the semantic bind from the emission: the result contains validated symbols and values, not C# fragments.
- [x] Add versioned caches to the compilation/document and invalidate only the projects/documents affected by the changes.
- [x] Add tests with project references, partial types, namespace aliases, duplicate types and compilations with independent C# errors.
- [x] Reindex the solution.

**Gate stage 3**
- [x] The same markup and the same `Compilation` produce the same ordered set of symbols and diagnostics regardless of the host.
- [x] The semantic core does not load assemblies and does not use reflection.

### Stage 4 - Scopes, bindings, resources, templates and Aspect

- [x] Move namescopes, resource scopes, application resources and shadowing/duplicate names rules to the semantic model.
- [x] Resolve `$DataContext`, `$root`, `$self`, named elements, resources, template owner/parts and binding modes through typed symbols.
- [x] Model the local changes of `DataContext` and validate the subsequent segments against the resulting type, including in `ContentTemplate DataType`.
- [x] Resolve `ItemsControl.Templates`, template selection according to `DataType`, `ItemsPanel`, `ItemsSource` and content ownership.
- [x] Move `Aspect` resources, `TargetType`, assignments, templates, conditions and application-site validation to the common core.
- [x] Add anti-cascade diagnostics: an unresolved binding source does not produce an error for each dependent segment.
- [x] Add parity tests for all existing binding/template/Aspect tests and for the real markup from `CernealaPresentation`.
- [x] Reindex the solution.

**Gate Stage 4**

- [x] Semantic model can respond to the type and symbol to any valid binding segment and to any resource reference.
- [x] The corpus valid for bindings, templates and Aspect has zero divergences from the source generator.

### Stage 5 - Semantics of Motion and Prism

- [x] Moves the resolution of targets, events, properties, specs, compositions and lifecycle Motion to the semantic model, leaving the emitter to only translate the result.
- [x] Move the Prism binding for directives, catalog symbols, parameters, values, nesting and Motion interop to the semantic model.
- [x] Exposes editor-agnostic facts for directive keywords, argument lists, parameter types, enum-like values ​​and symbol locations.
- [x] Add parity tests for all `UiMarkupGeneratorMotion*` and `PrismMarkupContractTests` suites.
- [x] Add recovery tests for an incomplete Motion/Prism document that preserves semantic understanding for unaffected XML elements.
- [x] Reindex the solution.

**Gate Stage 5**

- [x] Motion and Prism no longer have a private semantic binder that can diverge from the common core.
- [x] All existing Motion/Prism diagnostics have exact host-independent parity.

### Stage 6 - Migrating the generator source

- [x] Reference `Cerneala.Language` from `Cerneala.SourceGen` without changing the `netstandard2.0` compatibility of the analyzer.
- [x] Replaces `ParseDocument`, `MarkupDocument`, `XElement`-based private binding and diagnostics with common syntax tree and semantic model.
- [x] Adapt emitters for elements, bindings, resources, Aspect, Motion and Prism to common semantic results.
- [x] Eliminate duplicate parsers, descriptors and resolvers only after all parity tests are GREEN.
- [x] Keep incremental caching generator: changing a document must not semantically regenerate all independent documents.
- [x] Compare the output generated for the corpus; accept textual differences only if assembly behavior and diagnostics are identical or the improvement is explicitly approved.
- [x] Runs `dotnet test .\tests\Cerneala.Tests.Language\Cerneala.Tests.Language.csproj` and `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Reindex the solution.

**Gate stage 6**

- [x] `Cerneala.SourceGen` no longer uses `XDocument`/`XElement` for markup analysis.
- [x] All existing sourcegen tests are GREEN and the valid corpus generates functional assemblies.

### Stage 7 - Performance, API and documentation

- [x] Add benchmarks for parse cold/warm, incremental edit, semantic bind and query at-position on small, medium and `AspectChapterView.crn` documents.
- [x] Establishes hardware baselines and gates: parse/edit p95 under 50 ms for large documents, warm semantic query p95 under 25 ms and zero non-cancellable synchronous operation over 100 ms.
- [x] Profiles allocations and removes full rebuilds produced by a local edit where the benchmark demonstrates impact. (No optimization needed: the big edit has p95 1.534ms, about 32x under budget, although it allocates 369,984 B/op.)
- [x] Marks the minimal cross-assembly surface; avoid public APIs for general consumption and mandatory document any remaining public type.
- [x] Updates `docs/CernealaMarkupGuide.md`, bindings/Motion/Prism documentation and `UiMarkupGenerator` page with new common model without promising LSP before plan 2.
- [x] Runs `dotnet test .\Cerneala.slnx`, the approved benchmarks, `git diff --check` and the final reindex.

**Gate stage 7**

- [x] The common core respects budgets, has no host dependencies and has synchronized documentation/API docs.
- [x] There are no known semantic differences between build and editor-agnostic services.

## 6. The definition of ready

- [x] There is only one tolerant parser and only one semantic model for Cerneala.
- [x] Source generator uses the common kernel for all `.crn` dialects.
- [x] Incomplete documents can be analyzed incrementally without crashing and without the unnecessary cascade of diagnostics.
- [x] Diagnostics are host-agnostic and have exact build parity.
- [x] All tests and benchmarks of the plan are GREEN.
