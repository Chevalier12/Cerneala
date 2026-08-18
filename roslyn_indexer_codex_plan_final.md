# Detailed plan for Codex — Roslyn Repo Indexer

> Goal: implement a simple, performant and complete Roslyn indexer for daily usage, which can work as a local search engine for the entire repository. Do not use embedded AI, embeddings, vector DB, local/cloud LLM or external search engines. It uses Roslyn for semantic indexing of .NET code and a simple inverted index written in code for text search.

> Format: intentionally unchecked checklist. Codex must go through tasks from top to bottom and leave no TODOs, stubs or "implement later".

---

## 0. Mandatory rules for implementation

- [x] Implements everything as a local .NET tool, no server, no mandatory permanent daemon, no external services.
- [x] Do not use AI models, embeddings, vector search, ML.NET, OpenAI, Semantic Kernel, local LLM, cloud APIs or similar libraries.
- [x] Do not use ElasticSearch, Lucene, Meilisearch, Typesense, SQLite FTS or any other external search engine.
- [x] Don't send source code outside the local machine.
- [x] Do not introduce HTTP clients, telemetry, analytics, upload, sync, background network calls or external services.
- [x] Add a static check/simple test that fails if the tool project introduces obvious AI/embedding/vector/HTTP/cloud dependencies.
- [x] Use Roslyn for the semantic part: solutions/projects/documents, syntax trees, semantic models, symbols and references.
- [x] Use only a simple custom index for search: local JSON/JSONL files + in-memory dictionaries.
- [x] Search should work for the whole repo: semantic C# code + text search for relevant non-C# text files.
- [x] C# semantic indexing is mandatory and complete; non-C# text indexing is line-based, without a specialized parser.
- [x] The CLI should be easily usable by Codex or a human developer.
- [x] All important commands must have human-readable text output and stable `--json` option.
- [x] Any workspace/project/document error must be clearly logged; the tool should continue with partial index when safe.
- [x] Don't leave large duplicate code, gigantic methods or unnecessary abstractions; implementation must remain simple.
- [x] Don't do complicated premature optimizations; prioritize: correctness, simple incrementality, fast search, tests.
- [x] Does not introduce aggressive threading; limit parallelism so it doesn't blow up memory on large solutions.
- [x] Do not index `bin`, `obj`, `.git`, `.vs`, `.idea`, `.vscode`, `node_modules`, `.roslyn-index`, `TestResults`, `artifacts`, `packages`.
- [x] Do not index binary files or very large text files beyond the configured limit.
- [x] Do not write outside the repository unless the user explicitly asks.
- [x] Do not change the source of the indexed repo, other than adding the indexer project/tool ​​and its test files.

---

## 1. Expected end result

- [x] Create a tool called `ri` or `roslyn-indexer`, with an executable project packable as a .NET tool.
- [x] Tool should be able to be run from any subfolder of the repo and automatically detect root.
- [x] The tool must build the index in the `.roslyn-index/` folder at the root of the repo.
- [x] The tool must be able to do a full index on the first run.
- [x] The tool must be able to make incremental index on subsequent runs.
- [x] The tool must be able to search symbols, files, text, roughly-indexed semantic references and exact references on-demand.
- [x] The tool must be able to suggest deterministic queries for AI agents via `ri suggest`, without embedded AI.
- [x] The tool must be able to diagnose the environment through `ri doctor`.
- [x] The tool must be able to display result with path, line, column, kind, score, match reason and snippet.
- [x] Tool must support JSON output for integration with Codex.
- [x] Tool must include unit tests, integration tests and CLI tests.
- [x] The tool must include a short README with real usage.
- [x] `dotnet build` must pass.
- [x] `dotnet test` must pass.
- [x] There should be no TODOs left in the code or tests.

---

## 2. Recommended structure in the repo

- [x] Add folder `tools/RoslynRepoIndexer/`.
- [x] Create the solution `tools/RoslynRepoIndexer/RoslynRepoIndexer.sln`.
- [x] Create the project `tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/RoslynRepoIndexer.Core.csproj`.
- [x] Create the project `tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli/RoslynRepoIndexer.Cli.csproj`.
- [x] Create the project `tools/RoslynRepoIndexer/tests/RoslynRepoIndexer.Tests/RoslynRepoIndexer.Tests.csproj`.
- [x] Add all projects to the solution.
- [x] Make `RoslynRepoIndexer.Cli` refer to `RoslynRepoIndexer.Core`.
- [x] Make `RoslynRepoIndexer.Tests` refer to `RoslynRepoIndexer.Core` and where useful run the CLI as a process.
- [x] If the repo uses `Directory.Packages.props`, add the package versions there.
- [x] If the repo does not use Central Package Management, put the versions directly in `.csproj`.

### Allowed runtime packages

- [x] Add `Microsoft.CodeAnalysis.CSharp.Workspaces`.
- [x] Add `Microsoft.CodeAnalysis.Workspaces.MSBuild`.
- [x] Add `Microsoft.Build.Locator`.
- [x] If the repo does not already have central pinning, use the current stable versions verified at the plan date: `Microsoft.CodeAnalysis.CSharp.Workspaces` `5.6.0`, `Microsoft.CodeAnalysis.Workspaces.MSBuild` `5.6.0`, `Microsoft.Build.Locator` `1.11.2`.
- [x] If the repo already has Roslyn packages pinned, align the versions so you don't conflict between projects.
- [x] Do not add runtime packages for CLI parsing; implement simple parser manually.
- [x] Do not add runtime packages for logging; uses `Console.Error`, JSONL log and simple internal classes.
- [x] Do not add runtime packages for storage; use `System.Text.Json`, `FileStream`, `StreamReader`, `StreamWriter`.
- [x] Do not add runtime packages for HTTP, telemetry, AI, embeddings or vector search.

### Test packages only

- [x] Add `Microsoft.NET.Test.Sdk`.
- [x] Add `xunit`.
- [x] Add `xunit.runner.visualstudio`.
- [x] Don't add separate assertion framework unless the repo already uses it.

---

## 3. Target framework and project setup

- [x] Target `net8.0` minimum for tool projects, if the repo allows.
- [x] If the repo requires `net9.0` or `net10.0`, align the tool to the repo's standard target.
- [x] In the CLI project, set `OutputType` to `Exe`.
- [x] In the CLI project, set `PackAsTool` to `true`.
- [x] In the CLI project, set `ToolCommandName` to `ri`.
- [x] Enable `Nullable` in all new projects.
- [x] Enable `ImplicitUsings` in all new projects.
- [x] Set `TreatWarningsAsErrors` to `true` for new projects if the repo allows it.
- [x] Avoid direct references to `Microsoft.Build.*` runtime in output, except `Microsoft.Build.Locator`.
- [x] Before any use of MSBuild APIs, call `MSBuildLocator.RegisterDefaults()` in an isolated startup point.
- [x] Implements `ri --version`.
- [x] Implements `ri --help` and help per command.
- [x] Make sure the CLI project can be installed as local/global `dotnet tool`.

---

## 4. CLI — mandatory commands

### `ri index`

- [x] Implements `ri index [path]`.
- [x] If `path` is missing, use current directory.
- [x] Detect root repo starting from `path`.
- [x] Builds or updates the index from `.roslyn-index/`.
- [x] Default: incremental index if valid manifest exists.
- [x] Supports `--force` for full rebuild.
- [x] Supports `--json` for machine-readable summary.
- [x] Supports `--include-generated` for source-generated documents Roslyn, default `false`.
- [x] Supports `--include-non-csharp-text true|false`, default `true`.
- [x] Supports `--max-text-file-bytes <bytes>`, default `1048576`.
- [x] Supports `--max-degree-of-parallelism <n>`, default `min(Environment.ProcessorCount, 4)`.
- [x] Supports `--config <file>`, default `.roslyn-index.json` if it exists.
- [x] Finally, show: repo root, solutions/projects detected, docs indexed, docs skipped, symbols, references, tokens, duration, warning count.

### `ri search`

- [x] Implements `ri search <query>`.
- [x] If index is missing, display clear message: running `ri index`.
- [x] Search in symbols, text and files.
- [x] Supports `--mode all|symbol|text|file|reference`, default `all`.
- [x] Supports `--kind <kind1,kind2>` for symbols: `namespace,type,class,record,struct,interface,enum,delegate,method,constructor,property,indexer,event,field,enum-member,operator,local-function,parameter,local`.
- [x] Supports `--path <substring-or-glob-lite>`.
- [x] Supports `--project <name-or-path-substring>`.
- [x] Supports `--from-file <path>` for context-aware ranking.
- [x] Supports `--from-project <projectName>` for context-aware ranking.
- [x] Supports `--include-tests` and `--exclude-tests`.
- [x] Supports `--include-generated` for explicit search in generated indexed files.
- [x] Supports `--limit <n>`, default `50`.
- [x] Supports `--json`.
- [x] Supports query with quotes for simple phrase search: `"Customer Service"`.
- [x] Supports token search case-insensitive by default.
- [x] Supports exact symbol search if the query looks like FQN: `Namespace.Type.Member`.
- [x] The results must be sorted stably: score desc, path asc, line asc, column asc.

### `ri suggest`

- [x] Implements `ri suggest <natural-language-question>`.
- [x] Purpose: translate natural queries into deterministic command suggestions `ri search`, `ri goto` and `ri refs`.
- [x] Does not automatically execute the suggestions in the default version; just suggest the commands.
- [x] Do not use AI, embeddings, LLM, vector DB, external services or local models.
- [x] Use only existing local index: symbols, tokens, paths, references and project metadata.
- [x] Supports `--json`.
- [x] Supports `--limit <n>`, default `5`.
- [x] Supports optional `--execute-top <n>`, default `0`, to run the first N suggestions and return combined results.
- [x] Detect simple intents:
  - [x] "where is X defined?" / "where is X defined?" => suggest `ri goto X`.
  - [x] "who uses X?" / "where is X used?" / "where is X called?" => suggest `ri refs X`.
  - [x] "where is X made?" / "how is X done?" => suggest `ri search` with extracted tokens.
  - [x] "config/settings/options" => boost on config files, options classes and relevant paths.
  - [x] "controller/endpoint/route/api" => boost on Controllers, Minimal APIs and route-like code.
  - [x] "test/spec/fixture" => also includes boost on projects/test files.
- [x] Extracts tokens with the same `Tokenizer` used by search.
- [x] Eliminates Romanian/English stopwords: `unde`, `care`, `cum`, `cine`, `este`, `sunt`, `se`, `face`, `găsește`, `find`, `where`, `how`, `what`, `who`, `is`, `are`, `the`, `a`, `an`, `to`, `of`.
- [x] Keep code-like terms: CamelCase, PascalCase, snake_case, kebab-case, quoted phrases, FQNs and identifiers with `.`.
- [x] Map simple and deterministic synonyms:
  - [x] `login`, `auth`, `authentication`, `authorize`, `jwt`, `token`.
  - [x] `config`, `settings`, `options`.
  - [x] `db`, `database`, `repository`, `context`, `DbContext`.
  - [x] `endpoint`, `controller`, `route`, `api`.
  - [x] `validate`, `validation`, `validator`.
  - [x] `serialize`, `json`, `deserialize`.
  - [x] `save`, `persist`, `store`, `insert`, `update`.
- [x] For each suggestion return: `command`, `query`, `mode`, `confidence`, `reason`, `expectedResultKind`.
- [x] Sorts suggestions deterministically by `confidence desc`, then `command asc`.
- [x] If index is missing, return clear message: running `ri index`.

### `ri refs`

- [x] Implements `ri refs <symbol-query>`.
- [x] First look for the symbol in the local index.
- [x] If there are multiple candidate symbols, display the candidate list and ask for `--symbol-id` for disambiguation.
- [x] Supports `--symbol-id <id>`.
- [x] Supports `--exact` for accurate references via Roslyn `SymbolFinder.FindReferencesAsync` on-demand.
- [x] Default: use indexed semantic references, then recommend `--exact` if the result can be ambiguous.
- [x] Supports `--json`.
- [x] Displays the path, line, column, snippet and kind of the reference.

### `ri goto`

- [x] Implements `ri goto <symbol-query>`.
- [x] Returns matching statements.
- [x] Supports `--json`.
- [x] Supports `--limit`, default `20`.
- [x] For overloads, show full signature.

### `ri symbols`

- [x] Implements `ri symbols`.
- [x] Supports `--prefix <prefix>`.
- [x] Supports `--contains <text>`.
- [x] Supports `--kind <kind1,kind2>`.
- [x] Supports `--json`.
- [x] Supports `--limit`, default `100`.

### `ri doctor`

- [x] Implements `ri doctor [path]`.
- [x] Detect root repo.
- [x] Detect `.sln`, `.slnx` and `.csproj` available.
- [x] Detect installed .NET SDKs, if they can be read without build.
- [x] Check if MSBuild can be located and registered via `Microsoft.Build.Locator`.
- [x] Checks if `MSBuildWorkspace` can open the selected solution/projects.
- [x] Report unsupported or unloadable projects.
- [x] Report skipped configuration files and directories.
- [x] Reports if `.roslyn-index/` exists and if the scheme is compatible.
- [x] Do not change index or write files except output to stdout/stderr.
- [x] Supports `--json`.
- [x] Returns machine-readable diagnostics: `checks`, `status`, `message`, `severity`, `details`.

### `ri status`

- [x] Implements `ri status [path]`.
- [x] Shows if the index exists.
- [x] Show version scheme.
- [x] Show indexed root repo.
- [x] Shows the date of the last indexing.
- [x] Shows the number of documents, symbols, references, tokens.
- [x] Shows how many files appear dirty against the manifest.
- [x] Shows if the index is stale, missing, valid, corrupt or schema-incompatible.
- [x] Shows the last relevant warnings.
- [x] Supports `--json`.
- [x] Not starting Roslyn/MSBuild; must only use filesystem + manifest.

### `ri clean`

- [x] Implements `ri clean [path]`.
- [x] Delete folder `.roslyn-index/` only from detected root repo.
- [x] Request `--yes` for deletion without interactive confirmation.
- [x] Don't delete anything if the root repo is not detected for sure.

---

## 5. Mandatory exit codes

- [x] Returns `0` on success.
- [x] Returns `1` for user/input error: invalid command, invalid arguments, missing query, invalid path.
- [x] Returns `2` for critical repo/project/workspace loading error.
- [x] Returns `3` for unavailable, missing, corrupt or schema-incompatible index when the command asks for an existing index.
- [x] Returns `4` for unexpected internal error.
- [x] Returns `5` for timeout/cancelled.
- [x] `ri doctor` can return `0` if it can produce diagnostics even if some checks are warning/fail; use exit non-zero only when doctor itself cannot run.
- [x] In `--json`, always include `exitCode`, `success`, `warnings`, `errors`.
- [x] Document the exit codes in the README.
- [x] Test exit codes for common failure modes.

---

## 6. Config file `.roslyn-index.json`

- [x] If the file exists in the root repo, read it automatically.
- [x] If the file does not exist, use internal defaults.
- [x] Supports JSON with simple schema:
```json
{
  "solution": null,
  "includeGenerated": false,
  "includeNonCSharpText": true,
  "maxTextFileBytes": 1048576,
  "maxDegreeOfParallelism": 4,
  "searchResultLimit": 50,
  "suggestionLimit": 5,
  "exactRefsTimeoutSeconds": 30,
  "excludeDirectories": [
    ".git",
    "bin",
    "obj",
    ".vs",
    ".idea",
    ".vscode",
    "node_modules",
    ".roslyn-index",
    "TestResults",
    "artifacts",
    "packages"
  ],
  "excludeFileSuffixes": [
    ".dll",
    ".exe",
    ".pdb",
    ".png",
    ".jpg",
    ".jpeg",
    ".gif",
    ".webp",
    ".ico",
    ".pdf",
    ".zip",
    ".7z",
    ".tar",
    ".gz"
  ]
}
```
- [x] Validate config and show clear warning for unknown properties or invalid values.
- [x] Don't fail if a property is missing; use default.
- [x] Does not implement complex globbing; for `excludeDirectories`, compare path segment case-insensitive on Windows and case-sensitive on Linux/macOS.
- [x] For `excludeFileSuffixes`, compare case-insensitive extensions.

---

## 7. Discovering the repo

- [x] Creates class `RepositoryDiscovery`.
- [x] Starting from the received path or current directory, go up until you find `.git`.
- [x] If there is no `.git`, go up until you find `.sln`, `.slnx` or `.csproj`.
- [x] If nothing relevant is found, return error with explicit message.
- [x] Normalizes root path to full path without trailing separator.
- [x] Keep paths relative to the root repo in the index.
- [x] For non-C# text enumeration, use `git ls-files -co --exclude-standard` if `.git` exists and `git` is available.
- [x] If `git ls-files` fails, fallback to `Directory.EnumerateFiles` with configured exclusions.
- [x] Do not include files from `.roslyn-index/` in the index.

---

## 8. Discovering solutions and projects

- [x] Creates class `WorkspaceDiscovery`.
- [x] If the config specifies `solution`, use that solution.
- [x] If there is exactly one solution `.sln` or `.slnx` in root, use it.
- [x] If there are multiple solutions in the root, index them all and dedupe the docs after `fullPath + projectContext`.
- [x] If no solutions in root, recursively search for `.sln` and `.slnx`, excluding ignored directories.
- [x] If no solutions, search recursively for `.csproj`, excluding ignored directories.
- [x] Open the solutions with `MSBuildWorkspace.OpenSolutionAsync`.
- [x] Open standalone projects with `MSBuildWorkspace.OpenProjectAsync`.
- [x] Attach handler to `workspace.WorkspaceFailed` and collect warnings/errors.
- [x] If a project cannot be loaded, log in and continue with the rest of the projects.
- [x] If no C# document can be loaded, fail with exit code `4`.
- [x] For each project, keep `ProjectId`, `Name`, `FilePath`, `Language`, target framework/context if available.
- [x] Dedupe linked documents: the same `FilePath` can appear in several projects; preserves each semantic context but avoids duplicates in index text.

---

## 9. Internal data models

- [x] Create record `IndexManifest`.
- [x] Create record `ProjectEntry`.
- [x] Create record `DocumentEntry`.
- [x] Create record `SymbolEntry`.
- [x] Create record `ReferenceEntry`.
- [x] Create record `TokenPosting`.
- [x] Create record `SearchResult`.
- [x] Create record `QuerySuggestion`.
- [x] Create record `CommandResponse<T>` for uniform JSON output.
- [x] Create record `IndexDiagnostics`.

### `IndexManifest`

- [x] Includes `SchemaVersion`.
- [x] Includes `ToolVersion`.
- [x] Includes `RepoRoot`.
- [x] Includes `CreatedUtc`.
- [x] Includes `UpdatedUtc`.
- [x] Includes `ConfigHash`.
- [x] Includes `WorkspaceInputsHash`.
- [x] Includes list of indexed solutions/projects.
- [x] Includes map `DocumentsByRelativePath` with `DocumentState`.
- [x] Includes counters: `DocumentCount`, `SymbolCount`, `ReferenceCount`, `TokenCount`, `WarningCount`.

### `DocumentEntry`

- [x] Includes `DocumentId` internally stable.
- [x] Includes `ProjectId`.
- [x] Includes `ProjectName`.
- [x] Includes `RelativePath`.
- [x] Include `FullPath` only transiently, not necessarily in persistent index.
- [x] Includes `Language`.
- [x] Includes `IsGenerated`.
- [x] Includes `IsNonCSharpText`.
- [x] Includes `LengthBytes`.
- [x] Includes `LastWriteUtc`.
- [x] Includes `ContentHash`.
- [x] Includes `DeclarationHash` for C#.
- [x] Includes `LineCount`.

### `SymbolEntry`

- [x] Includes stable `SymbolId`.
- [x] Includes `DocumentId`.
- [x] Includes `ProjectId`.
- [x] Includes `Kind`.
- [x] Includes `Name`.
- [x] Includes `MetadataName`.
- [x] Includes `FullyQualifiedName`.
- [x] Includes `ContainerName`.
- [x] Includes `Signature`.
- [x] Includes `Accessibility`.
- [x] Includes relevant `Modifiers`: `static`, `abstract`, `virtual`, `override`, `async`, `partial`, `readonly`, `required`.
- [x] Includes relative `FilePath`.
- [x] Includes `StartLine`, `StartColumn`, `EndLine`, `EndColumn`.
- [x] Includes `SpanStart`, `SpanLength`.
- [x] Includes `IsDefinition`.
- [x] Includes `IsPartial`.
- [x] Includes `ParameterTypes` for overloads.
- [x] Include `ReturnType` for methods/properties where they exist.

### `ReferenceEntry`

- [x] Includes `ReferenceId` or compound `SymbolId + DocumentId + SpanStart`.
- [x] Includes `SymbolId`.
- [x] Includes `DocumentId`.
- [x] Includes `ProjectId`.
- [x] Includes relative `FilePath`.
- [x] Includes `StartLine`, `StartColumn`, `EndLine`, `EndColumn`.
- [x] Includes `SpanStart`, `SpanLength`.
- [x] Includes `ReferenceKind`: `read`, `write`, `invocation`, `type-use`, `attribute`, `object-creation`, `inheritance`, `unknown`.
- [x] Includes `ReferencedName` for fallback text.

### `TokenPosting`

- [x] Includes `Token` normalized lowercase invariant.
- [x] Includes `DocumentId`.
- [x] Includes relative `FilePath`.
- [x] Includes `Line`.
- [x] Includes `Column`.
- [x] Includes `Weight`: `symbol-name`, `identifier`, `keyword`, `string`, `comment`, `path`, `text`.
- [x] Do not store large snippets in posting; read the snippet from the file on display.

### `SearchResult`

- [x] Includes `Kind`.
- [x] Includes `Score`.
- [x] Include `MatchReason` explicit and short.
- [x] Includes `SymbolId`, when it exists.
- [x] Includes `SymbolName`, when present.
- [x] Includes `ContainingType`, when available.
- [x] Includes `FullyQualifiedName`, when present.
- [x] Includes `ProjectName`.
- [x] Includes relative `FilePath`.
- [x] Includes `StartLine`, `StartColumn`, `EndLine`, `EndColumn`.
- [x] Includes `Snippet`, only in output/render, not necessarily in the index.
- [x] Include `ReferenceKind`, when the result is a reference.

### `QuerySuggestion`

- [x] Includes full `Command` runnable by Codex.
- [x] Includes `Query`.
- [x] Includes `Mode`.
- [x] Includes `Confidence` between `0.0` and `1.0`.
- [x] Includes `Reason`.
- [x] Includes `ExpectedResultKind`.
- [x] Includes `ExecutedResults` only when `--execute-top` is used.

### `CommandResponse<T>`

- [x] Includes `Success`.
- [x] Includes `ExitCode`.
- [x] Includes `Command`.
- [x] Includes `Query`, when the order has a query.
- [x] Includes `RepoRoot`.
- [x] Includes `ElapsedMs`.
- [x] Includes `IndexUpdatedUtc`, when index exists.
- [x] Includes `Results` or payload specific to the order.
- [x] Includes `Warnings`.
- [x] Includes `Errors`.

---

## 10. Index Persistence

- [x] Create folder `.roslyn-index/v1/`.
- [x] Write `manifest.json`.
- [x] Write `documents.jsonl`.
- [x] Write `symbols.jsonl`.
- [x] Write `references.jsonl`.
- [x] Write `tokens.jsonl` or `token-postings.jsonl`.
- [x] Write `diagnostics.jsonl`.
- [x] Optionally write cache for exact refs in `.roslyn-index/v1/exact-refs-cache/`, invalidated on index change.
- [x] Use `System.Text.Json` with explicit and stable options.
- [x] Write files to `tmp-{guid}` and then do atomic replace at folder or file level.
- [x] Don't leave corrupt index if process stops in the middle.
- [x] On read, validate `SchemaVersion`.
- [x] If the scheme is incompatible, ask for `ri index --force` or rebuild automatically with a clear message.
- [x] Don't serialize absolute paths in persisted results, except `RepoRoot` in the manifest.
- [x] Normalizes the path separator in the index to `/`.
- [x] Maintain deterministic output: sort inputs by path, project, span.

---

## 11. Hashing and incremental indexing

- [x] Creates class `DocumentHasher`.
- [x] For each file, quickly read `LengthBytes` and `LastWriteUtc`.
- [x] If length and last write are identical to the manifest, consider the file unchanged without rereading.
- [x] If they have changed, calculate `ContentHash` using SHA-256 from BCL.
- [x] For C#, calculate `DeclarationHash` from the list of declarations: kind + fully-qualified name + signature + accessibility + modifiers.
- [x] If only the body of a method has changed and `DeclarationHash` remains identical, reindex only the changed document.
- [x] If `DeclarationHash` has changed, marks the current project and directly dependent projects as semantically dirty.
- [x] If `.sln`, `.slnx`, `.csproj`, `.props`, `.targets`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`, `NuGet.config`, `NuGet.config` have changed. `packages.lock.json`, marks full semantic rebuild.
- [x] If files have been deleted, completely remove their documents, symbols, references and tokens.
- [x] If files have been added, index them and update the manifest.
- [x] Reindex affected projects when project references change.
- [x] Reindex affected documents/projects when compilation options change.
- [x] Store `IndexSchemaVersion` and force full rebuild on scheme change.
- [x] Add tests for stale index prevention.
- [x] If more than 20% of C# documents are dirty, do full semantic rebuild for simplicity.
- [x] After any incremental run, rebuild global files `symbols.jsonl`, `references.jsonl`, `tokens.jsonl` from current segments to avoid stale entries.

---

## 12. Correct Roslyn loading

- [x] Creates `MSBuildRegistration` with static method `EnsureRegistered()`.
- [x] Call `MSBuildLocator.RegisterDefaults()` before any MSBuild type.
- [x] Keep code that uses MSBuild in methods called after registration.
- [x] Creates `MSBuildWorkspace` with reasonable properties, including `LoadMetadataForReferencedProjects = true` if available and useful.
- [x] Attach `WorkspaceFailed`.
- [x] Load `Solution` or `Project` asynchronously.
- [x] Use `CancellationToken` in all async operations.
- [x] Do not run `dotnet build`, `dotnet test` or repo scripts as part of indexing.
- [x] Do not crash if the restore is not perfect; index as much as possible and log the missing ones.
- [x] For source-generated docs, deploy only if `--include-generated` is set.

---

## 13. Collection of documents

- [x] From each `Project`, read `Documents`.
- [x] Ignore documents without `FilePath`.
- [x] Ignore documents under excluded directories.
- [x] Ignore documents in `bin`/`obj`, even if Roslyn accidentally exposes them.
- [x] Only include existing files on disk, except source-generated docs when explicitly requested.
- [x] For each document, create `DocumentEntry`.
- [x] For the same physical file included in several projects, create separate semantic documents per project, but only one text entry for deduplicated full-text.
- [x] For non-C# text, list repo files via git/fallback and exclude files already represented as C# documents.

---

## 14. Collecting C# Symbols

- [x] Create `SymbolCollector`.
- [x] For each C# dirty document, get `SyntaxRoot`.
- [x] For each C# dirty document, get a single `SemanticModel` and reuse it in that document.
- [x] Visit the declaration nodes and get the token declared with `semanticModel.GetDeclaredSymbol(...)`.
- [x] Don't call `GetDeclaredSymbol` on every arbitrary node if not necessary; first filter by relevant node types.
- [x] Index file-scoped and block-scoped namespaces.
- [x] Index classes.
- [x] Index record classes.
- [x] Index record structs.
- [x] Index structures.
- [x] Indexes interfaces.
- [x] Index enums.
- [x] Index delegates.
- [x] Index constructors.
- [x] Index destructors/finalizers.
- [x] Index methods.
- [x] Index local functions.
- [x] Index operators.
- [x] Index conversion operators.
- [x] Index properties.
- [x] Indexes indexers.
- [x] Index events.
- [x] Index fields.
- [x] Index enum members.
- [x] Index parameters.
- [x] Index locales where Roslyn provides stable symbol; mark them `local`.
- [x] Index type parameters for generic types/methods.
- [x] Index extension methods and mark them with modifier/flag.
- [x] Index partial declarations as separate entries that have the same FQN but different locations.
- [x] Index overloads as separate entries by full signature.
- [x] For each symbol, generate stable `SymbolId`.
- [x] Prefer `DocumentationCommentId.CreateDeclarationId(symbol)` for public/member-level symbols where it returns value.
- [x] Use `SymbolKey.Create(symbol, cancellationToken).ToString()` as semantic fallback.
- [x] For locals/parameters without global ID, use deterministic fallback: `projectId + relativePath + span + name + kind`.
- [x] Normalize display string without `global::` for UX, but keep fully qualified form internally.
- [x] Do not include symbols from external metadata as repo declarations.

---

## 15. Collection of indexed semantic references

- [x] Create `ReferenceCollector`.
- [x] Don't run `SymbolFinder.FindReferencesAsync` for each symbol during indexing; it would be too slow.
- [x] For each C# dirty document, use the same `SemanticModel` as for symbols.
- [x] Visit relevant usage nodes: identifier names, generic names, member access names, qualified names, object creation types, invocation expressions, attribute syntax, base type syntax.
- [x] For each relevant node, call `semanticModel.GetSymbolInfo(node, ct)`.
- [x] If `Symbol` is null and there is only one `CandidateSymbol`, use the candidate with internal flag `ambiguous` or low-level warning.
- [x] Ignore symbols that have nothing to do with the repo if there is no indexed local declaration.
- [x] Maps the symbol to `SymbolId` with the same strategy as declarations.
- [x] Avoid duplicates by key `symbolId + documentId + spanStart + spanLength`.
- [x] Classifies reference kind simply: invocation, object creation, attribute, inheritance, read, write, type-use, unknown.
- [x] For read/write, use parent syntax and simple operations; does not implement complex dataflow.
- [x] Keeps references roughly-indexed for quick lookups.
- [x] For exact references, implement `ri refs --exact` with `SymbolFinder.FindReferencesAsync` on-demand and optional index cache.

---

## 16. Text Indexing and Tokenization

- [x] Create `Tokenizer`.
- [x] Tokenization should work on C# and text files.
- [x] Split on whitespace, punctuation, operators and separators.
- [x] Additional split on camelCase and PascalCase.
- [x] Additional split on snake_case and kebab-case.
- [x] Normalize tokens to invariant lowercase.
- [x] Also preserves the full form for compound identifiers: `CustomerService` produces `customerservice`, `customer`, `service`.
- [x] For `IHttpClientFactory`, produces `ihttpclientfactory`, `http`, `client`, `factory`.
- [x] Include tokens of length 1 only if they are meaningful in the code: `i`, `x`, `y`, `T` for generics; otherwise filter them from plain text.
- [x] For C#, use Roslyn tokens to mark weights: identifier, keyword, string, comment.
- [x] For non-C# text, use line-by-line reading.
- [x] For paths, index path segments and filenames with weight `path`.
- [x] Don't index binary files: detect NUL bytes in the first 8KB.
- [x] Don't index files bigger than `maxTextFileBytes`, but log skip.

---

## 17. Deterministic query suggestion engine

- [x] Create `SuggestionService`.
- [x] `SuggestionService` gets the natural query, loaded index and CLI options.
- [x] Normalize text using `Tokenizer`.
- [x] Eliminates Romanian/English stopwords.
- [x] Keep the phrases in quotes as priority terms.
- [x] Detect code-like tokens and treat them as possible identifiers.
- [x] Apply configured or hardcoded synonyms simply.
- [x] Detect the main intent: definition, references, broad search, config, endpoint, tests, persistence, validation, serialization.
- [x] Generate 3-5 concrete suggestions, not dozens.
- [x] Hints must be full CLI commands, easy to run by Codex.
- [x] Do not enter long explanations in `reason`; a short sentence at most.
- [x] Do not execute `ri search` internally unless the user requested `--execute-top`.
- [x] With `--execute-top`, only run index-based commands; do not run `ri refs --exact` automatically.
- [x] The result must be deterministic for the same query and the same index.

---

## 18. Simple search engine

- [x] Creates `IndexReader` which loads manifest + jsonl into memory.
- [x] Create `SearchService`.
- [x] Build dictionaries in memory:
  - [x] `symbolsById`.
  - [x] `symbolsByLowerName`.
  - [x] `symbolsByLowerFullyQualifiedName`.
  - [x] `tokenToPostings`.
  - [x] `referencesBySymbolId`.
  - [x] `documentsById`.
- [x] Don't keep snippets in memory; read line from file to render.
- [x] Simple query parser:
  - [x] Separate phrase query between quotes.
  - [x] Separate normal tokens.
  - [x] Recognize simple prefixes `kind:`, `path:`, `project:`, `mode:`.
  - [x] Does not implement complex query language with full boolean operators.
- [x] For symbol search:
  - [x] Exact FQN match has maximum score.
  - [x] Exactly simple name match has a high score.
  - [x] Prefix simple name match has a medium-high score.
  - [x] Contains simple/FQN match has an average score.
  - [x] CamelCase acronym match has an average score.
  - [x] Token overlap with name/symbol has low-medium score.
- [x] For text search:
  - [x] Intersect postings for all query tokens where possible.
  - [x] If the intersection is empty, use union with lower score.
  - [x] Phrase search checks the actual line/snippet in the file before the result.
- [x] For file search:
- [x] Search in path segments and file name.
- [x] For reference search:
  - [x] Find candidate symbols, then read `referencesBySymbolId`.
- [x] For context-aware search:
  - [x] If `--from-file` exists, detect the document project and boost the same project.
  - [x] If `--from-project` exists, boost that project.
  - [x] Boosts projects linked by project references.
  - [x] Penalizes default test projects, except for test queries or `--include-tests`.
  - [x] Exclude test projects when `--exclude-tests` is set.
- [x] Deduplicated results after `path + line + column + kind + symbolId`.
- [x] Sort stable.
- [x] Limits to `--limit`, but calculates enough internally to get good results after filtering.

---

## 19. Recommended scoring

- [x] Start the score from `0`.
- [x] Exact FQN symbol match: `+1000`.
- [x] Exactly simple symbol name: `+800`.
- [x] Prefix symbol name: `+600`.
- [x] CamelCase acronym match: `+500`.
- [x] Contains symbol/FQN: `+350`.
- [x] Token match in symbol name: `+250` per token.
- [x] Token match in path: `+120` per token.
- [x] Token match in identifier: `+100` per token.
- [x] Token match in keyword: `+60` per token.
- [x] Token match in string/comment/text: `+40` per token.
- [x] Phrase match exactly in line: `+300`.
- [x] Boost for the same project when `--from-file`/`--from-project` is used: `+120`.
- [x] Boost for directly referenced/context referenced projects: `+60`.
- [x] Penalty for test projects: `-80`, except for explicit test/spec/fixture or `--include-tests` queries.
- [x] Penalty for generated file: `-100`, if include-generated is active.
- [x] Penalty for very deep or vendor-like path if not excluded: `-20`.
- [x] Each score must produce a short `MatchReason`: `exact-fqn`, `exact-symbol`, `prefix-symbol`, `token-overlap`, `path-match`, `reference-match`, `phrase-match`, `context-boost`.
- [x] Keep scoring in one class `SearchScorer` and test it separately.
- [x] Add tests for ranking order and match reasons.

---

## 20. Human-readable output

- [x] For each result, display a title line:
```text
[method] CustomerService.GetCustomerAsync(int id)  src/App/Services/CustomerService.cs:42:17  score=920
```
- [x] Display the snippet on the following line:
```text
    public Task<Customer> GetCustomerAsync(int id)
```
- [x] For symbols, include container/FQN when not redundant.
- [x] For reference, include `ref-kind`.
- [x] For many results, display `showing N of M`.
- [x] For warnings, show summary on stderr, don't mix in stdout when `--json` is used.

---

## 21. Stable JSON output and contract for AI agents

- [x] All commands with `--json` emit a single valid JSON object, not JSONL.
- [x] Don't write human-readable text to stdout when `--json` is active.
- [x] Warnings and logs go in JSON field `warnings`; stderr can only receive non-JSON fatal errors.
- [x] Defines a common contract for all orders:
```json
{
  "success": true,
  "exitCode": 0,
  "command": "search",
  "query": "CustomerService",
  "repoRoot": "/absolute/path",
  "elapsedMs": 12,
  "indexUpdatedUtc": "2026-07-04T00:00:00Z",
  "results": [],
  "warnings": [],
  "errors": []
}
```
- [x] For `ri search --json`, the results must include:
  - [x] `filePath`.
  - [x] `startLine`.
  - [x] `startColumn`.
  - [x] `endLine`.
  - [x] `endColumn`.
  - [x] `kind`.
  - [x] `symbolId`, when it exists.
  - [x] `symbolName`, when it exists.
  - [x] `containingType`, when it exists.
  - [x] `fullyQualifiedName`, when it exists.
  - [x] `projectName`.
  - [x] `score`.
  - [x] `matchReason`.
  - [x] `snippet`.
- [x] Example `ri search --json`:
```json
{
  "success": true,
  "exitCode": 0,
  "command": "search",
  "query": "CustomerService",
  "mode": "all",
  "repoRoot": "/absolute/path",
  "elapsedMs": 12,
  "indexUpdatedUtc": "2026-07-04T00:00:00Z",
  "totalMatches": 2,
  "results": [
    {
      "kind": "method",
      "score": 920,
      "matchReason": "exact-symbol",
      "symbolId": "...",
      "symbolName": "GetCustomerAsync",
      "containingType": "CustomerService",
      "fullyQualifiedName": "MyApp.Services.CustomerService.GetCustomerAsync(int)",
      "projectName": "MyApp",
      "filePath": "src/App/Services/CustomerService.cs",
      "startLine": 42,
      "startColumn": 17,
      "endLine": 42,
      "endColumn": 61,
      "snippet": "public Task<Customer> GetCustomerAsync(int id)",
      "referenceKind": null
    }
  ],
  "warnings": [],
  "errors": []
}
```
- [x] For `ri suggest --json`, the results must include:
  - [x] `command`.
  - [x] `query`.
  - [x] `mode`.
  - [x] `confidence`.
  - [x] `reason`.
  - [x] `expectedResultKind`.
- [x] Example `ri suggest --json`:
```json
{
  "success": true,
  "exitCode": 0,
  "command": "suggest",
  "query": "unde se validează tokenul JWT?",
  "repoRoot": "/absolute/path",
  "elapsedMs": 8,
  "results": [
    {
      "command": "ri search jwt validation token --mode all --json",
      "query": "jwt validation token",
      "mode": "all",
      "confidence": 0.86,
      "reason": "matched auth and validation terms",
      "expectedResultKind": "method-or-class"
    }
  ],
  "warnings": [],
  "errors": []
}
```
- [x] For `ri index --json`, includes counters and timings: `discoveryMs`, `workspaceLoadMs`, `semanticIndexMs`, `textIndexMs`, `persistMs`, `totalMs`.
- [x] For `ri refs --json`, include candidate ambiguity if it exists.
- [x] For `ri doctor --json`, includes the check list with `name`, `status`, `severity`, `message`, `details`.
- [x] For `ri status --json`, includes `indexState`: `missing`, `valid`, `stale`, `corrupt`, `schema-incompatible`.
- [x] Do not change the names of the fields after they are entered; add new fields without breaking change.
- [x] Add snapshot tests for the JSON form of each command.

---

## 22. Performance for daily usage

- [x] Search in existing index should not start Roslyn.
- [x] Search only needs to read the index files and lines needed for snippets.
- [x] `ri status` should not start Roslyn.
- [x] `ri clean` should not start Roslyn.
- [x] `ri index` is the only command that starts Roslyn workspace, except `ri refs --exact`.
- [x] Don't keep `SemanticModel` in global cache after the document has been processed.
- [x] Get a single `SemanticModel` per processed document and reuse it for statements + references.
- [x] Do not build `Compilation` separately for each node.
- [x] Process projects sequentially or with limited parallelism.
- [x] Process documents with limited and configurable parallelism.
- [x] Use streaming IO for JSONL files.
- [x] Do not serialize Roslyn objects.
- [x] Don't keep `Solution`, `Project`, `Compilation`, `SemanticModel` in persistent index.
- [x] For results, read the snippet directly from the file only for top results, not for all candidates.
- [x] Measures durations for: discovery, workspace load, semantic index, text index, persist, search load, search score.
- [x] Write these timings in the diagnostics and in the `ri index --json` output.

---
## 23. Performance budgets and benchmark smoke tests

- [x] Define repo classes for testing and documentation:
  - [x] small: under 500 files.
  - [x] medium: 500-5,000 files.
  - [x] large: 5,000-25,000 files.
- [x] Define measurable budgets in README/config for:
  - [x] cold index.
  - [x] warm incremental index without changes.
  - [x] warm incremental index after a file change.
  - [x] query latency for `ri search`.
  - [x] query latency for `ri goto`.
  - [x] query latency for `ri suggest`.
  - [x] approximate refs latency.
  - [x] exactly refs latency with timeout.
  - [x] approximate maximum memory.
- [x] Don't put fragile thresholds in normal unit tests.
- [x] Put separate, relaxed benchmark/smoke tests that run robustly in CI.
- [x] Each order must report `elapsedMs`.
- [x] `ri search`, `ri goto`, `ri symbols`, `ri status`, `ri suggest` should not start Roslyn/MSBuild.
- [x] `ri refs --exact` must have configurable timeout and cancellation token.

---

## 24. Robustness and edge cases

- [x] Works on Windows, Linux and macOS.
- [x] Normalize paths with `/` in the index.
- [x] Compare case-insensitive paths on Windows and case-sensitive on Linux/macOS.
- [x] Support repos with spaces in the path.
- [x] Supports files with UTF-8 BOM.
- [x] Supports files with CRLF and LF.
- [x] Support code that doesn't fully compile, while Roslyn can produce partial syntax/semantics.
- [x] Support partially unloadable projects: log + continue.
- [x] Supports multi-targeting via separate project context.
- [x] Supports linked files via separate semantic document and text after.
- [x] Supports partial classes and partial methods.
- [x] Supports top-level statements.
- [x] Supports global usings.
- [x] Support file-scoped namespaces.
- [x] Support nullable annotations in display string.
- [x] Support generics and nested types in FQN.
- [x] Supports overloads and operators.
- [x] Supports extension methods.
- [x] Supports records and primary constructors.
- [x] Support collection expressions and modern C# syntax via current Roslyn.
- [x] Don't crack on large generated files; excludes them by default.
- [x] Does not crack on long paths.
- [x] Don't crash if index is deleted while search is running; show clear error.

---

## 25. Recommended classes in `Core`

- [x] `RepositoryDiscovery` — detect root and list files.
- [x] `IndexerConfig` — config model + defaults + validation.
- [x] `ConfigLoader` — read `.roslyn-index.json`.
- [x] `MSBuildRegistration` — isolates `MSBuildLocator`.
- [x] `WorkspaceDiscovery` — find solutions/projects.
- [x] `WorkspaceLoader` — open workspace/solution/project.
- [x] `DocumentHasher` — incremental hash.
- [x] `BinaryFileDetector` — detect non-text files.
- [x] `Tokenizer` — tokenization of text and identifiers.
- [x] `SymbolIdProvider` — generate stable IDs.
- [x] `SymbolCollector` — collect statements.
- [x] `ReferenceCollector` — collect fast semantic references.
- [x] `TextIndexer` — index C# tokens + non-C# text.
- [x] `IndexBuilder` — orchestrates indexing.
- [x] `IndexStore` — read/write index.
- [x] `IndexReader` — load the index for search.
- [x] `QueryParser` — parse simple query.
- [x] `SearchScorer` — scoring.
- [x] `SearchService` — execute the search.
- [x] `ExactReferenceService` — use `SymbolFinder.FindReferencesAsync` on-demand.
- [x] `SnippetReader` — read lines/snippets.
- [x] `DiagnosticsCollector` — warnings, errors, timings.
- [x] `JsonOutputWriter` — writes stable JSON.
- [x] `HumanOutputWriter` — writes output text.

---

## 26. Simple parser CLI

- [x] Implement manual parser in `RoslynRepoIndexer.Cli`.
- [x] The first argument is the command.
- [x] The rest of the arguments are positional or options `--name value` / flags.
- [x] Supports `--help` globally.
- [x] Supports `ri <command> --help`.
- [x] For invalid arguments, display short help and exit code `2`.
- [x] Do not introduce libraries for CLI.
- [x] Do not implement hidden subcommands.

---

## 27. Algorithm `ri index` — steps explained

- [x] Parse arguments.
- [x] Detect root repo.
- [x] Load config.
- [x] Calculates `ConfigHash`.
- [x] Load the existing manifest if it exists and `--force` is not set.
- [x] Discover solutions/projects.
- [x] Calculate `WorkspaceInputsHash` from relevant `.sln/.slnx/.csproj/.props/.targets`.
- [x] Decide full vs incremental.
- [x] Register MSBuild with `MSBuildLocator`.
- [x] Open workspace/solution/project.
- [x] Collect the C# docs.
- [x] List non-C# text files.
- [x] Calculate states for documents and files.
- [x] Identifies added/changed/deleted/unchanged.
- [x] For C# dirty documents, collect root syntax and semantic model.
- [x] Collect symbols.
- [x] Collect fast semantic references.
- [x] Collect C# tokens.
- [x] For non-C# dirty files, collect text tokens.
- [x] Remove old entries for deleted/changed.
- [x] Combine existing unchanged entries with new entries.
- [x] Rebuild sorted/deterministic global indexes.
- [x] Writes the index to a temp folder.
- [x] Validates that written files can be read.
- [x] Do atomic replace.
- [x] Show summary.

---

## 28. Algorithm `ri suggest` — steps explained

- [x] Parse the arguments.
- [x] Detect root repo.
- [x] Checks for the existence of the index.
- [x] Load manifest + index files into memory.
- [x] Normalize the question.
- [x] Extracts tokens, phrases and code-like identifiers.
- [x] Remove stopwords.
- [x] Apply deterministic synonyms.
- [x] Detect intent.
- [x] Generate command suggestions.
- [x] Compute confidences for each suggestion.
- [x] Sort deterministically.
- [x] If `--execute-top` > 0, execute only top N index-based commands and append results.
- [x] Write human output or JSON.

---

## 29. `ri doctor` Algorithm — Explicit Steps

- [x] Parse the arguments.
- [x] Detect root repo.
- [x] Read the config if it exists.
- [x] Detect solutions/projects.
- [x] Check MSBuild Locator.
- [x] Try opening the workspace in safe mode with cancellation.
- [x] Collect `WorkspaceFailed` diagnostics.
- [x] Checks for index existence and schema.
- [x] Check exclude directories and configured limits.
- [x] Write human-readable or JSON report.

---

## 30. Algorithm `ri status` — steps explained

- [x] Parse the arguments.
- [x] Detect root repo.
- [x] Checks for the existence of `.roslyn-index/`.
- [x] Read the manifest without Roslyn/MSBuild.
- [x] Check the version scheme.
- [x] Check for missing/corrupted index files.
- [x] Quickly calculate dirty count based on length/last write/hash only when needed.
- [x] Returns states: missing, valid, stale, corrupt, schema-incompatible.
- [x] Write human output or JSON.

---

## 31. Algorithm `ri search` — steps explained

- [x] Parse the arguments.
- [x] Detect root repo.
- [x] Checks for the existence of the index.
- [x] Load manifest + index files into memory.
- [x] Parse query.
- [x] Apply filters.
- [x] Run symbol search if mode allows.
- [x] Run text search if mode allows.
- [x] Run file search if mode allows.
- [x] Run reference search if mode allows.
- [x] Deduplicated.
- [x] Score and sort.
- [x] Read snippets only for top results.
- [x] Write human output or JSON.

---

## 32. Algorithm `ri refs --exact`

- [x] Parse arguments.
- [x] Detect root repo.
- [x] Load the index to find the candidate symbol.
- [x] If symbol is ambiguous, return candidates and stop.
- [x] Register MSBuild.
- [x] Reopens the same solution/project from the manifest.
- [x] Resolve symbol in Roslyn using `SymbolKey` or fallback by FQN + signature + location.
- [x] Running `SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken)`.
- [x] Convert results to `SearchResult`.
- [x] Sort by path/line/column.
- [x] Write output.
- [x] Optionally cache the exact result in `.roslyn-index/v1/exact-refs-cache/` with invalidation after `UpdatedUtc`.

---

## 33. Clear referral strategy

- [x] `ri search` must be fast and index-based.
- [x] `ri refs <symbol>` uses indexed approximate references by default.
- [x] `ri refs <symbol> --exact` reopen the solution/projects with Roslyn and use `SymbolFinder.FindReferencesAsync`.
- [x] `ri refs --exact` must support cancellation.
- [x] `ri refs --exact` must support configurable timeout via `--timeout <seconds>` and config `exactRefsTimeoutSeconds`.
- [x] If timeout is reached, return partial results with clear warning and exit code `5` only if there is no usable result.
- [x] Do not call `SymbolFinder.FindReferencesAsync` in `ri search`, `ri suggest`, `ri goto`, `ri symbols` or `ri status`.

---

## 34. Mandatory unit tests

- [x] Test `RepositoryDiscovery` detects `.git` root.
- [x] Test `RepositoryDiscovery` detects root without `.git` but with `.sln`.
- [x] Directory exclusion test `bin`, `obj`, `.roslyn-index`.
- [x] Default config test.
- [x] Invalid config test produces warning, not crash.
- [x] Test `Tokenizer` for camelCase.
- [x] Test `Tokenizer` for PascalCase.
- [x] Test `Tokenizer` for snake_case.
- [x] Test `Tokenizer` for kebab houses.
- [x] Test `Tokenizer` for `IHttpClientFactory`.
- [x] Test `Tokenizer` keeps whole token + subtokens.
- [x] Test binary detector with NUL bytes.
- [x] Test path normalization Windows-like and Unix-like.
- [x] Test `SearchScorer` exact FQN > exact simple > prefix > contains.
- [x] Test `QueryParser` with phrase query.
- [x] Test `QueryParser` with `kind:` and `path:`.
- [x] Test JSON serialization for the manifest.
- [x] Test JSON output schema for search result.
- [x] Deterministic sorting test.
- [x] Test `SuggestionService` for definition questions -> `ri goto`.
- [x] Test `SuggestionService` for reference questions -> `ri refs`.
- [x] Test `SuggestionService` for questions broad -> `ri search`.
- [x] Romanian/English stopword removal test.
- [x] Test synonym expansion auth/config/database/endpoints/validation/serialization/persistence.
- [x] Deterministic confidence ordering test.
- [x] Test that JSON output respects the common contract.
- [x] Test that `matchReason` is populated for results.
- [x] Test that there are no project references to HTTP/AI/vector packages.

---

## 35. Mandatory Roslyn/integration tests

- [x] Create temporary fixture with a minimal solution and C# SDK-style project.
- [x] Indexes a simple class and checks the `class` symbol.
- [x] Index the file-scoped namespace.
- [x] Index block-scoped namespaces.
- [x] Index record class.
- [x] Indexes record struct.
- [x] Indexes struct.
- [x] Index the interface.
- [x] Index enum + enum members.
- [x] Index delegates.
- [x] Index builder.
- [x] Index async method.
- [x] Index property.
- [x] Index indexer.
- [x] Index event.
- [x] Index field.
- [x] Index operator.
- [x] Index conversion operator.
- [x] Index local function.
- [x] Index parameters.
- [x] Index generic type and generic method.
- [x] Index nested type.
- [x] Index partial class in two files.
- [x] Index overloads and check different signatures.
- [x] Index extension method and check flag/modifier.
- [x] Indexes top-level statements without crashing.
- [x] Index global usings without crashing.
- [x] Checks semantic reference to the method by invocation.
- [x] Check semantic reference to type via object creation.
- [x] Checks semantic reference to attribute.
- [x] Check semantic reference in inheritance/base type.
- [x] Check linked files in two projects.
- [x] Check project with incomplete code produces partial index, not crash.
- [x] Index attributes and check symbol/reference.
- [x] Index minimal ASP.NET Core controllers as a text/symbol fixture, without starting the application.
- [x] Indexes Minimal APIs with top-level statements.
- [x] Indexes nullable reference types without crashing.
- [x] Index aliases and using aliases.
- [x] Indexes interface implementations and implemented methods.
- [x] Index multi-targeted project without unstable duplicates.
- [x] Check `ri refs --exact` find real references for a simple symbol.

---

## 36. Incremental mandatory tests

- [x] Full index creates manifest and all index files.
- [x] The second run without changes marks 0 dirty documents.
- [x] Changing the body of a method reindexes the document, not forcing a full rebuild.
- [x] Changing the name of a method changes `DeclarationHash`.
- [x] Changing a declaration marks the semantic project dirty.
- [x] Adding a C# file adds document/symbols/tokens.
- [x] Deleting a C# file removes document/symbols/references/tokens.
- [x] `.csproj` change triggers semantic rebuild.
- [x] `Directory.Build.props` change triggers semantic rebuild.
- [x] Change `Directory.Build.targets` triggers semantic rebuild.
- [x] Change `global.json` triggers semantic rebuild.
- [x] Change `NuGet.config` triggers semantic rebuild.
- [x] Change `packages.lock.json` triggers semantic rebuild.
- [x] Modifying project references triggers reindexing affected projects.
- [x] Changing config triggers required rebuild.
- [x] Corrupt index or old schema produces clear message and rebuild/checked error.

---

## 37. Mandatory search tests

- [x] `ri search CustomerService` find the class with high score.
- [x] `ri search My.Namespace.CustomerService` finds exactly the highest scoring FQN.
- [x] `ri search CS` finds `CustomerService` by acronym/camel case if implemented.
- [x] `ri search customer service` finds separate tokens.
- [x] `ri search "Customer Service"` check phrase search in the text.
- [x] `ri search --mode symbol --kind method GetCustomerAsync` return methods.
- [x] `ri search --mode file CustomerService.cs` returns the file.
- [x] `ri search --path Services CustomerService` filters by path.
- [x] `ri goto CustomerService` returns the statement.
- [x] `ri refs GetCustomerAsync` returns indexed references.
- [x] `ri refs GetCustomerAsync --exact` returns exact references.
- [x] The results are sorted deterministically.
- [x] `--limit 1` returns a single result.
- [x] `--json` is valid JSON.
- [x] `ri suggest "unde se validează tokenul JWT?" --json` returns relevant `ri search` suggestions.
- [x] `ri suggest "where is CustomerService defined?" --json` suggests `ri goto CustomerService`.
- [x] `ri suggest "who uses GetCustomerAsync?" --json` suggests `ri refs GetCustomerAsync`.
- [x] `ri search CustomerService --from-file <path>` boosts results from the current project.
- [x] `ri search CustomerService --exclude-tests` excludes test projects.

---

## 38. Mandatory CLI tests

- [x] `ri --help` returns exit code 0.
- [x] `ri index --help` returns exit code 0.
- [x] Unknown command returns exit code 2.
- [x] Invalid argument returns exit code 2.
- [x] `ri search` without query returns exit code 2.
- [x] `ri search query` without index returns exit code 3.
- [x] `ri clean --yes` deletes the index.
- [x] `ri status` before index shows index missing.
- [x] `ri status` after index displays counters.
- [x] `ri doctor --json` returns machine-readable checks.
- [x] `ri suggest` without question returns exit code 1.
- [x] `ri suggest question` without index returns exit code 3.
- [x] `ri --version` returns exit code 0.
- [x] Controlled timeout returns exit code 5 where applicable.
- [x] CLI does not write warnings to stdout when `--json` is active.

---

## 39. Mandatory performance/smoke tests

- [x] Generate a temporary repo with at least 200 simple C# files in the test.
- [x] Full index must finish without out-of-memory and with correct counters.
- [x] Search by index should not start MSBuild/Roslyn; test with a simple seam/mock or service separation.
- [x] `ri suggest`, `ri goto`, `ri symbols` and `ri status` after index should not start MSBuild/Roslyn.
- [x] Search on the generated index must return a result below a relaxed threshold, only as a smoke test, not a strict benchmark.
- [x] Incremental after changing a file must mark below 10% dirty documents in case of body-only change.
- [x] The performance test must be robust in CI; avoid too strict time thresholds.

---

## 40. Mandatory README

- [x] Add `tools/RoslynRepoIndexer/README.md`.
- [x] Explain what the tool does in 5-8 lines.
- [x] Explicitly explains that it does not use AI, embeddings or external search engines.
- [x] Includes orders:
```bash
dotnet build tools/RoslynRepoIndexer/RoslynRepoIndexer.sln
dotnet test tools/RoslynRepoIndexer/RoslynRepoIndexer.sln
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- index .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- status .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- doctor .
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- search CustomerService
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- suggest "unde se validează tokenul JWT?"
dotnet run --project tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Cli -- refs CustomerService --exact
```
- [x] Includes example JSON output for `search`, `suggest`, `status` and `doctor`.
- [x] Includes "Troubleshooting" section.
- [x] Includes which folders are excluded by default.
- [x] Includes how to configure `.roslyn-index.json`.
- [x] Includes section to install as local/global `dotnet tool`.
- [x] Include samples `.riignore` or explain why the exclusions are only in `.roslyn-index.json`.
- [x] Includes the list of exit codes.

---

## 41. Privacy, local-only and no-network guarantee

- [x] The tool does not make HTTP requests.
- [x] Tool does not start external services.
- [x] The tool does not send source code or metadata outside the machine.
- [x] The tool does not use telemetry or analytics.
- [x] The tool does not use AI, embeddings, vector databases or LLM APIs.
- [x] Add test/static check that inspects new projects for obvious forbidden package names: `OpenAI`, `SemanticKernel`, `MLNet`, `Pinecone`, `Qdrant`, `Weaviate`, `Elasticsearch`, `Lucene`, `HttpClientFactory` runtime if not justified.
- [x] Explicitly documents in the README that the tool is local-only.

---

## 42. Definition of Done

- [x] `dotnet build tools/RoslynRepoIndexer/RoslynRepoIndexer.sln` passes.
- [x] `dotnet test tools/RoslynRepoIndexer/RoslynRepoIndexer.sln` passes.
- [x] `ri index .` works in a real C# repo.
- [x] `ri status .` shows valid index.
- [x] `ri search <class-name>` returns the class declaration.
- [x] `ri search <method-name>` returns method and relevant references/text.
- [x] `ri goto <symbol>` returns the statement.
- [x] `ri refs <symbol>` returns indexed references.
- [x] `ri refs <symbol> --exact` uses Roslyn on-demand and returns exact references.
- [x] `ri search <query> --json` returns valid JSON according to the schema.
- [x] `ri suggest <question> --json` returns useful deterministic suggestions for Codex.
- [x] `ri doctor . --json` returns useful diagnostics.
- [x] `ri --version` and `ri --help` work.
- [x] JSON contracts have snapshot tests.
- [x] Incremental indexing does not reprocess everything on a body-only change.
- [x] Deleted files are completely removed from the index.
- [x] There is no TODO/stub/not implemented.
- [x] No AI/embedding/vector/search-server/HTTP/cloud telemetry dependencies.
- [x] No unnecessary runtime dependencies other than Roslyn/MSBuildLocator.
- [x] README exists and can be followed by a new developer.

---

## 43. Useful official sources for the Codex

- [x] Roslyn Workspace model: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace
- [x] `Document` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.document
- [x] `SemanticModel` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.semanticmodel
- [x] `GetDeclaredSymbol` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.csharpextensions.getdeclaredsymbol
- [x] `SymbolFinder.FindReferencesAsync`: https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.findsymbols.symbolfinder.findreferencesasync
- [x] `Microsoft.CodeAnalysis.CSharp.Workspaces` NuGet: https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Workspaces/
- [x] `Microsoft.CodeAnalysis.Workspaces.MSBuild` NuGet: https://www.nuget.org/packages/Microsoft.CodeAnalysis.Workspaces.MSBuild/
- [x] `Microsoft.Build.Locator` NuGet: https://www.nuget.org/packages/Microsoft.Build.Locator/
- [x] MSBuild Locator guidance: https://learn.microsoft.com/en-us/visualstudio/msbuild/find-and-use-msbuild-versions
