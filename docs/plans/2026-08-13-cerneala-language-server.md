# Plan: Cerneala Language Server and complete IntelliSense

> Date: 2026-08-13
> Status: completed
> Dependency: `docs/plans/2026-08-13-cerneala-language-core.md`
> Goal: we build a host-agnostic LSP language server that transforms the syntax tree and common semantic model into complete, fast and deterministic IntelliSense.

## 1. Baseline and contract

The repo does not contain a language server today. The source generator receives `Compilation` and `AdditionalFiles` only during the build, and Visual Studio does not have a process to maintain document snapshots, to map `.crn` to the project or to respond to editorial requests.

The server must work on the unsaved buffer, load the project's C# context, cancel stale requests, and not run the full generator on every keystroke. Any semantic result comes from `Cerneala.Language`; the server only translates to the LSP.

## 2. Mandatory skills

- Diagnostics push/pull with sourcegen parity and recovery during typing.
- Completion and resolve completion for XML structure, C# symbols, bindings, resources, templates, Aspect, Motion and Prism.
- Hover, signature help, go-to-definition, references, document highlight and rename for sure.
- Document symbols, workspace symbols for Cerneala symbols, folding ranges and selection ranges.
- Semantic tokens for XML Cerneala and embedded languages; TextMate remains the instant fallback, not the semantic source.
- Full/range formatting, on-type formatting and deterministic code actions.

## 3. The target architecture

- Executable project `Cerneala.LanguageServer` with stream-based transport and lifecycle controlled by the host.
- `Cerneala.LanguageServer.ProtocolTests` project that starts the real server via JSON-RPC/LSP, does not directly call handlers in end-to-end tests.
- Workspace service that maps document -> project -> Roslyn `Compilation`, tracks reloads and keeps versioned snapshots.
- Thin feature handlers over editor-agnostic services from `Cerneala.Language`.
- Scheduler with cancellation, coalescing and latest-document-version wins.

## 4. Estimated files

- `Cerneala.LanguageServer/Cerneala.LanguageServer.csproj`
- `Cerneala.LanguageServer/Protocol/`
- `Cerneala.LanguageServer/Workspace/`
- `Cerneala.LanguageServer/Features/`
- `Cerneala.LanguageServer/Program.cs`
- `tests/Cerneala.Tests.LanguageServer/`
- `tests/Fixtures/LanguageServerWorkspace/`
- `Cerneala.slnx`
- API docs from `docs-site/documentation/classes/` only for the inevitable public surface

## 5. Implementation stages

### Stage 0 - LSP contract and RED harness

- [x] Selects an LSP implementation maintained and compatible with the delivered runtime; documents protocol version, framing, serialization and upgrade policy.
- [x] Add the server and the tests protocol project to the solution, with in-memory transport for tests and stdio/duplex stream for the host.
- [x] Add a RED test that initializes the server, opens a `.crn`, applies incremental `didChange` and asks for diagnostics/completion.
- [x] Defines declared capabilities exactly; the server does not announce a feature until the feature's protocol-level test is GREEN.
- [x] Defines structured logging, trace levels and crash reports without default document content.
- [x] Reindex the solution.

**Gate Stage 0**

- [x] The server process starts, negotiates initialize/shutdown/exit and can be terminated without process leak.
- [x] The functional test is RED for lack of feature handlers, not for transport or fixture.

### Stage 1 - Workspace, projects and document synchronization

- [x] Map the URIs `.crn` to the projects that include them as `AdditionalFiles`, including `.slnx`, `.sln`, project references and linked files.
- [x] Builds Roslyn compilations for the owner project and updates the context for changes `.cs`, `.csproj`, references, configuration or target framework.
- [x] Defines the multi-target policy: selects the active context provided by the host and deduplicates identical results; do not mix incompatible symbols between TFMs.
- [x] Maintain overlay for the unsaved buffer without writing to disk and apply only changes with a newer version.
- [x] Cancel parse/bind/feature requests for stale versions and publish results only if the document version is still current.
- [x] Manages standalone files with syntax-only support and a unique informational diagnosis regarding the lack of semantic project.
- [x] Add tests for project reload, rename/delete, document in two projects, broken C# compilation and server restart.
- [x] Reindex the solution.

**Gate stage 1**

- [x] The unsaved buffer and the saved source generator use the same semantic context after save/build.
- [x] No old request can overwrite diagnostics or completion for a new version.

### Stage 2 - Diagnostics without false errors

- [x] Implement diagnostics on syntax and semantics using the common catalog, with exact UTF-16 line/column mapping.
- [x] Publish strict diagnostics for the analyzed document/version and withdraw diagnostics that disappeared after repair.
- [x] Suppress dependent semantic diagnostics under incomplete syntax nodes and limit duplicates to the same cause/span.
- [x] Dedupes LSP diagnostics from build/sourcegen diagnostics from the Error List by id, document and span, without hiding distinct errors.
- [x] Add golden tests for all `CERNEALAUI*`, Motion and Prism and compare the LSP result with the source generator.
- [x] Add character-by-character typing scenarios for opening/closing tag, attribute, binding, directive and template.
- [x] Runs the repo corpus and asks for zero editor diagnostics for each document that compiles validly.
- [x] Reindex the solution.

**Gate stage 2**

- [x] id/severity/message/span parity is exact for stable semantic errors.
- [x] `CernealaPresentation` and Playground have zero false diagnostics in the editor.

### Stage 3 - Completion, resolve and signature help

- [x] Completes root elements and child elements allowed by parent/content property, with custom types accessible through namespace aliases.
- [x] Complete attributes, property elements, attached properties and events, eliminating already used members where the contract does not allow duplicates.
- [x] Fill in boolean values, numeric values, enum, colors/brushes, thickness, cursor, alignment and other conversions recognized by the build.
- [x] Complete `xmlns` aliases, CLR namespaces, `DataType`, `TargetType`, templates `DataType` and valid assignable types.
- [x] Complete visible resources by scope, element names, Aspect names, Motion specs/clips, Prism symbols and parameters.
- [x] Complete binding sources and each segment according to the result type, including after local changes of `DataContext`, with modes only where they are legal.
- [x] Complete directive keywords, blocks, argument names and Motion/Prism values ​​based on the exact syntactic context.
- [x] Implements completion resolve with signature, declaring type, XML documentation, deprecation and source assembly without loading everything upfront.
- [x] Implement signature help for directives/functions/specs/filter parameters with correct active parameter after incomplete edits.
- [x] Add negative tests that demonstrate that impossible suggestions do not appear.
- [x] Reindex the solution.

**Gate stage 3**

- [x] The completion matrix covers all the categories in the corpus and each inserted item produces valid markup in the tested context.
- [x] Completion warm p95 is below 100 ms on the large document and does not block other documents.

### Stage 4 - Hover, navigation, references and rename

- [x] Displays hover for elements, properties, events and types with signature, inherited/declaring type, default value and available XML docs.
- [x] Displays typed hover for binding segments, resources, Aspect/Motion/Prism symbols and diagnostics explanation without duplicating the raw message.
- [x] Implement go-to-definition to C# type/member, paired `.crn.cs`, named element, resource, template, Aspect, Motion clip/spec and locally defined Prism symbol.
- [x] Implement references for names/resources/declarative symbols with correct scopes and for C# symbols via Roslyn.
- [x] Implement document highlights for declaration and usage in the current file.
- [x] Allows renaming only when all references are resolved exactly and editors do not touch arbitrary text; explicitly refuse ambiguous cases.
- [x] Add cross-file, cross-project, shadowing, duplicate names, generated companion and partially invalid documents tests.
- [x] Reindex the solution.

**Gate Stage 4**

- [x] Navigation does not lead to generated `.g.cs` when there is a better user-authored source.
- [x] Rename produces compileable workspace edits and does not modify symbols with the same text from another scope.

### Stage 5 - Semantic tokens, symbols, folding and selection

- [x] Defines semantic token legend for element type, property, attached property, event, namespace, resource, binding source/member, directive, Motion and Prism.
- [x] Issue full and delta semantic tokens, versioned and cancelable, without invalid overlap.
- [x] Issue hierarchical document symbols for root, named elements, resources, templates, Aspects, Motion and Prism declarations.
- [x] Issue workspace symbols for navigable Cerneala statements without indexing literals or generated noise.
- [x] Issue folding ranges for elements, resources, templates and directive blocks, keeping XML comments/regions.
- [x] Issue selection ranges from token to expression, attribute, element and document.
- [x] Add tests on mixed and incomplete documents.
- [x] Reindex the solution.

**Gate Stage 5**

- [x] Semantic tokens cover the Cerneala syntax that TextMate cannot distinguish and remain stable after local edits.
- [x] Symbols/folding do not disappear completely due to a local recoverable error.

### Stage 6 - Formatting and code actions

- [x] Defines a canonical formatter that preserves comments, literal text, directive semantics and the order of user-authored attributes if there is no semantic reason for reordering.
- [x] Implements document/range formatting, indentation of property elements and directive blocks and on-type formatting for `>`, newline and closing delimiters.
- [x] Ensures idempotency: two consecutive formats produce zero edits.
- [x] Add code actions only for deterministic repairs: namespace alias missing, closing tag missing, typo with unique candidate, event handler companion and attribute/property-element conversion where valid.
- [x] Add organize/fix-all only for independent diagnostics; refuse fix-all when edits overlap or change semantics.
- [x] Add snapshot tests for real markup, comments, Motion, Prism and partial documents.
- [x] Reindex the solution.

**Gate stage 6**

- [x] The formatter is semantically lossless, idempotent and does not produce diff on the corpus already formatted after approval of the baseline.
- [x] Each action code applied removes the target diagnosis and leaves the document parseable.

### Stage 7 - Competition, performance and hardening

- [x] Instrument parse, bind, completion, diagnostics, navigation, queue time, cancellation and allocation without collecting user-authored text.
- [x] Adds stress tests with fast typing, two active documents, project reload and 100 canceled completion requests.
- [x] Enforce latest-version wins, cache limits and cleanup at close/solution unload/shutdown.
- [x] Establishes gates on documented hardware: diagnostics warm p95 under 200 ms, completion p95 under 100 ms, hover/navigation p95 under 100 ms and non-cancellable zero request over 500 ms.
- [x] Check memory plateau after 1,000 open/change/close cycles and absence of child processes after shutdown/crash host.
- [x] Runs `dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj`, `dotnet test .\Cerneala.slnx`, `git diff --check` and final reindexing.
- [x] Documents protocol capabilities, logging, troubleshooting and syntax-only limitation for standalone files.

**Gate stage 7**

- [x] All the capabilities announced by the server are protocol-level tested and respect the budgets.
- [x] The server is host-agnostic, does not reference the Visual Studio SDK and closes cleanly.

## 6. The definition of ready

- The [x] Language server offers all the mandatory capabilities for the entire Cerneala dialect.
- [x] Diagnostics are identical to the build and tolerant during typing.
- [x] Workspace correctly tracks the unsaved buffer, Roslyn projects and builds.
- [x] Completion and navigation are typed, scoped and fast.
- [x] All protocol tests, stress tests and full suite are GREEN.