# Language Tooling

> Code: `Cerneala.Language`, `Cerneala.LanguageServer`, `Cerneala.PreviewHost`, `Shared/PreviewProtocol.cs`, `Cerneala.VisualStudio` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Four projects give `.crn` markup editor support:

- `Cerneala.Language` analyzes markup: syntax, semantics, diagnostics and editor
  services. The source generator, the language server and the tests all reuse
  it, so build and editor apply the same rules.
- `Cerneala.LanguageServer` exposes that analysis over the Language Server
  Protocol (LSP).
- `Cerneala.PreviewHost` compiles and renders one `.crn` view out of process and
  returns the pixels.
- `Cerneala.VisualStudio` starts both processes from Visual Studio.

None of them is part of the Cerneala runtime. An application never loads them.
Lowering markup to C# belongs to the source generator, see
[markup-and-sourcegen.md](markup-and-sourcegen.md).

| Project | Target | Process |
|---|---|---|
| `Cerneala.Language` | `netstandard2.0`, no public types | Loaded in the compiler (as the generator's dependency), in the language server and in tests |
| `Cerneala.LanguageServer` | `net10.0` | Its own process, LSP over stdin/stdout |
| `Cerneala.PreviewHost` | `net10.0-windows` | Its own process, binary protocol over stdin/stdout |
| `Cerneala.VisualStudio` | `net472` (VSIX) | Inside `devenv.exe` |

`Cerneala.Language` is `internal` throughout. `Properties/AssemblyInfo.cs:3-7`
grants access only to `Cerneala.SourceGen`, `Cerneala.LanguageServer`,
`Cerneala.Tests.Language`, `Cerneala.Tests.LanguageServer` and
`Cerneala.Benchmarks`.

## Cerneala.Language

### Components

| Type | File | Role | Owner |
|---|---|---|---|
| `SourceText` | `Cerneala.Language/Text/SourceText.cs` | Immutable text with versioned `WithChange`. | Caller |
| `MarkupLexer` | `Cerneala.Language/Syntax/MarkupLexer.cs` | `Lex()` turns text into tokens. Trivia, comments and CDATA are tokens too, so joining the token texts gives back the input exactly. | One call |
| `MarkupParser` | `Cerneala.Language/Syntax/MarkupParser.cs` | `Parse(SourceText)` builds a `DocumentSyntax` tree. It recovers from incomplete input with zero-width "missing" tokens. Its syntax diagnostic `CERNEALASYN001` is reported to users as `CERNEALAUI001`. | One call |
| `SyntaxNode` and the syntax tree | `Cerneala.Language/Syntax/SyntaxNode.cs` | `ToFullString()` returns the stored source text (`:57`). | The document |
| `DirectiveSyntaxParser` | `Cerneala.Language/Syntax/Embedded/DirectiveSyntaxParser.cs` | Parses the embedded `@` directive syntax inside elements. | Static |
| `CernealaDocument` | `Cerneala.Language/Semantics/CernealaDocument.cs` | One `.crn` path plus its `SourceText`. | Caller |
| `CernealaCompilation` | `Cerneala.Language/Semantics/CernealaCompilation.cs` | The documents of one project plus that project's C# symbols. It creates and caches one `CernealaSemanticModel` per path. | Caller; `IDisposable` |
| `CernealaSemanticModel` | `Cerneala.Language/Semantics/CernealaSemanticModel*.cs` | Symbols, scopes, bindings, Aspect, Motion, Prism and Timbre checks for one document, plus its diagnostics. | `CernealaCompilation` cache |
| `RoslynCompilationSymbols` | `Cerneala.Language/Semantics/Symbols/RoslynCompilationSymbols.cs` | Reads C# types from a Roslyn compilation, with a `version` number. It never loads assemblies. | Caller |
| `CernealaDiagnosticCatalog`, `LanguageDiagnosticDescriptor` | `Cerneala.Language/Diagnostics/` | The shared list of 44 diagnostic IDs. Each ID has one build severity and one editor severity. | Static |
| Editor services | `Cerneala.Language/Features/`: `CernealaCompletionService`, `CernealaNavigationService`, `CernealaStructureService`, `CernealaFormattingService`, `CernealaCodeActionService` | Completion, navigation (definition, references, rename), outline and folding, formatting, code actions. | Created by the host |
| `PrismLanguageCatalog`, `TimbreMarkupBinder` | `Cerneala.Language/Prism/Catalog/`, `Cerneala.Language/Timbre/` | Language-side knowledge of Prism operations and Timbre markup. | Static |

The diagnostic catalog itself (IDs, categories and severities) is described in
[markup-and-sourcegen.md](markup-and-sourcegen.md#build-diagnostics).

### Data Flow

Example: the parser receives the unfinished text `<StackPanel><Button /></StackPanel`.

1. `MarkupLexer.Lex` produces tokens. The last `>` is absent.
2. `MarkupParser.Parse` still builds a full tree. `StackPanel` gets a
   `CloseGreaterThanToken` with `IsMissing == true` and a span of length 0.
   Parsing the same text twice gives the same span
   (`MarkupParserTests.MissingTokensUseDeterministicZeroWidthSpans`).
3. `CernealaCompilation.GetSemanticModel(path)` (`CernealaCompilation.cs:43-69`)
   returns the cached model for this path, or builds a new one. A cached model
   is reused only while `Symbols.Version` and the document version still match.
4. When the syntax already has a diagnostic, the semantic layer reports no
   further diagnostics for that file. Result: the user sees one error, not a
   cascade (`RecoveryBaselineTests.UnrecoverableSyntaxSuppressesSemanticCascades`).
5. Hosts read the diagnostics. A host chooses `AnalysisMode.Build` (the
   generator) or `AnalysisMode.Editor` (the language server). In the editor,
   `CERNEALAUI001` "could not be parsed" is shown as Information, because it is
   usually just text the user has not finished typing yet. In a build it is an
   Error.

Changes create new compilation objects:

- `WithDocument(document)` (`:71-94`) keeps the cached models of the other
  documents. All models are rebuilt only when an Application document changes.
- `WithProjectSymbols(symbols)` (`:96`) replaces the C# side, so every model is
  rebuilt.

### Lifecycle And Ownership

The host owns every `CernealaCompilation`. The source generator creates one for
each generator run, with `AnalysisMode.Build`. The language server keeps one
for each loaded project. A disposed compilation throws
`ObjectDisposedException`. A cancelled token throws
`OperationCanceledException` before any work starts.

## Cerneala.LanguageServer

### Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Program` | `Cerneala.LanguageServer/Program.cs` | `Main` runs `LanguageServerHost.RunAsync` on stdin/stdout. An unhandled exception logs `server.crashed`, writes a crash report to `CERNEALA_LSP_CRASH_DIRECTORY` and returns 1. | Process |
| `LanguageServerHost` | `Cerneala.LanguageServer/Protocol/LanguageServerHost.cs` | JSON-RPC transport: StreamJsonRpc with `HeaderDelimitedMessageHandler` and `SystemTextJsonFormatter` (`:18-29`). It waits for `exit`, a disconnect or cancellation (`:36-50`). | Process |
| `LanguageServerEndpoint` | `Cerneala.LanguageServer/Protocol/LanguageServerEndpoint.cs` | Every LSP method (`[JsonRpcMethod]`). Holds the lifecycle state and creates the workspace and the services on `initialize`. | Process |
| `CernealaWorkspace` | `Cerneala.LanguageServer/Workspace/CernealaWorkspace.cs` | Loads the solution, keeps one `ProjectContext` for each project, watches the disk and builds document snapshots. | Endpoint, from `initialize` to `shutdown` |
| `DocumentOverlayStore` | `Cerneala.LanguageServer/Workspace/DocumentOverlayStore.cs` | Unsaved text of open documents. | Workspace |
| `WorkspaceState`, `ProjectContext` | `Cerneala.LanguageServer/Workspace/` | One immutable load result. `GetOwners(path)` returns the projects that own a `.crn` path. | Workspace |
| `DiagnosticService`, `DiagnosticPublisher`, `BuildDiagnosticStore` | `Cerneala.LanguageServer/Features/` | Pull diagnostics, push diagnostics, and the `cerneala/buildDiagnostics` store. | Endpoint |
| `CompletionService`, `NavigationService`, `StructureService`, `FormattingService` | `Cerneala.LanguageServer/Features/` | Convert LSP requests into calls to the `Cerneala.Language` editor services, and convert the results back. | Endpoint |
| `CernealaInitializationOptions` | `Cerneala.LanguageServer/Protocol/LspContracts.cs:20-39` | Client options: `host`, `solutionPath`, `activeTargetFramework`, `configuration`, `diagnosticsMode`, `deferWorkspaceLoad`. | Request |

### Data Flow

Lifecycle:

1. `initialize` (`LanguageServerEndpoint.cs:29`):
   - A second `initialize` fails with error `-32600`.
   - The handler creates the workspace and the services and returns the
     capabilities.
   - Text sync: open/close, `Change = 2` (incremental), and save without text.
   - With `diagnosticsMode: "push"` the result has no `DiagnosticProvider`, so
     the client does not pull diagnostics.
   - With `deferWorkspaceLoad: true` the solution loads only when the first
     document opens.
2. Every other request before `initialize` fails with `-32002`.
3. `shutdown` (`:514`) sets the state to "shut down" and disposes the workspace.
   `exit` (`:526`) then ends the process:
   - with code 0 after `shutdown`;
   - with code 1 without `shutdown`.
4. If the client closes stdin instead, `LanguageServerHost` logs
   `server.disconnected` and returns 0. It returns 1 only when the JSON-RPC
   connection failed.

Documents:

1. `textDocument/didOpen` (`:411`) stores the overlay and starts the deferred
   solution load.
2. `didChange` (`:429`) applies the incremental change.
3. `didClose` (`:447`) removes the overlay and publishes an empty diagnostic
   list.
4. `didSave` (`:457`) reloads the whole workspace.

To build a snapshot (`CernealaWorkspace.cs:145-157`), the text of a path is
taken from the first source that has it:

1. the unsaved overlay;
2. the document in the owner project's last load;
3. the file on disk.

Workspace reload:

- `CreateAsync` (`:51`) loads the solution now or on the first `didOpen`.
- `ReloadInitialWorkspaceAsync` (`:387`) first loads a bootstrap state, then the
  full state.
- A `FileSystemWatcher` (`:372`) waits 150 ms after the last disk event, then
  reloads (`:494`).
- `ShouldReload` (`:506`) ignores `bin` and `obj`. It reacts to `.cs`,
  `.csproj`, `.props`, `.targets`, `.sln`, `.slnx`, `.dll` and `global.json`.

Example, push mode, from
`ProtocolContractTests.PushDiagnosticsUseCurrentVersionAndRetractAfterRepairAndClose`:

| Client sends | Server publishes |
|---|---|
| `didOpen` `Recovery.crn`, version 1, text `<` | `textDocument/publishDiagnostics`, version 1, contains `CERNEALAUI001` |
| `didChange` version 2, text `<Window />` | version 2, no `CERNEALAUI001`, exactly one `CERNEALAWORKSPACE001` |
| `didClose` | version `null`, empty list |

Two more endpoint behaviors:

- `textDocument/diagnostic` (`:466`) answers `-32800` (request cancelled) when a
  newer request replaces it.
- Diagnostics go out through `textDocument/publishDiagnostics` (`:583`). The
  server sends `workspace/semanticTokens/refresh` only when the client
  advertised support for it.

Transport and operation details are in the
[language server guide](../guides/language-server.md).

## Cerneala.PreviewHost

### Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Program` | `Cerneala.PreviewHost/Program.cs` | STA entry point. Runs `PreviewHostServer` on stdin/stdout, or a smoke render with `--smoke`. Returns 1 on an unhandled exception. | Process |
| `PreviewProtocol` | `Shared/PreviewProtocol.cs` | Binary framing shared by the host and the Visual Studio client (the file is linked into both projects). | Static |
| `PreviewHostServer` | `Cerneala.PreviewHost/PreviewHostServer.cs` | Request loop. Owns the current compilation, render session and active source text. | Process |
| `PreviewCompiler` | `Cerneala.PreviewHost/PreviewCompiler.cs` | Compiles the project that owns the document with `MSBuildWorkspace` (`Configuration=Debug`). The unsaved buffer replaces the `.crn` AdditionalFile. | Server |
| `PreviewMarkupHotReload` | `Cerneala.PreviewHost/PreviewMarkupHotReload.cs` | Applies literal attribute edits to the live tree without compiling. | Static |
| `PreviewRenderSession` | `Cerneala.PreviewHost/PreviewRenderSession.cs` | Loads the compiled assembly into a collectible `AssemblyLoadContext`. Creates the root element, an off-screen window and a Timbre runtime. Captures frames. | Server, one per compilation |
| `DisabledPreviewTimbreOutput` | `Cerneala.PreviewHost/DisabledPreviewTimbreOutput.cs` | Silent audio output when audio is off. | Render session |

### Protocol

Every message is an `int32` length followed by a payload.

- **Requests:** `Render = 1` to `ResetInput = 12` (`PreviewProtocol.cs:7-21`).
  Examples: pointer input, click, text, key, key state, capture, shutdown.
- **Responses:** `Frame = 1`, `Error = 2`, `Acknowledged = 3` (`:23-28`).
- **Frame:** a 42-byte header (`FrameHeaderLength`) plus the pixels.
  `MaximumFrameLength` is 128 MiB.
- **Pixel bytes:** in R, G, B, A order. `SdlGpuWindowGraphicsSession.CapturePresentedFrame`
  converts a BGRA swapchain to RGBA with `NormalizeRgba`.

### Data Flow

Example from `PreviewHostTests.LiteralPropertyEditUpdatesTheLiveTreeWithoutRecompiling`:

1. The client sends `Render` with `OpeningView.crn`, its text and 320 x 180.
2. No session exists yet. The server compiles, creates a `PreviewRenderSession`
   and answers with a `Frame`. At `RenderScale` 0.9 the frame is 288 x 162 pixels.
3. The client sends `Render` again with the same path and size, but
   `Background="#FF080A0D"` is now `Background="#FF203040"`.
4. Path, size and audio setting match, so the server first calls
   `TryApplyMarkup`, which uses `PreviewMarkupHotReload.TryApply`.
   - The result is `Applied`.
   - The server stores the new text as its active source.
   - The answer is a `Frame` with `CompileMilliseconds == 0`.

`PreviewMarkupHotReload.TryApply` gives one of four results:

| Edit | Result | What the server does |
|---|---|---|
| Same text | `Unchanged` | Captures a new frame |
| Only literal attribute values changed, for example `Opacity="0.5"` to `Opacity="0.75"` | `Applied` | Captures a new frame; the tree now has `Opacity == 0.75` |
| The new text is not valid XML, or a value does not convert, for example `Opacity="-"` | `DeferredInvalidEdit` | Captures a frame of the unchanged tree; `Opacity` stays `0.5` |
| Elements added or removed (`<Border />` to `<Border><TextBlock /></Border>`), or a change to `Aspect`, `Name` or `TargetType` | `RequiresCompilation` | Compiles again |

`TryApply` first prepares every change, then applies them all. If one apply
fails, the changes already applied are rolled back and the result is
`DeferredInvalidEdit`.

Recompiling disposes the old session before it creates the new one
(`PreviewHostServer.cs:82-89`).

Request results:

- `Click`, `Text`, `Key` and `Capture` answer with a new `Frame`.
- Pointer movement, key state and `ResetInput` answer with `Acknowledged`.
- An exception while handling a request becomes an `Error` response with the
  exception message.
- `Shutdown` ends the loop and gets no response.

### Lifecycle And Ownership

- The server holds at most one `PreviewRenderSession`.
- Each session owns:
  - its load context, which is unloaded on dispose;
  - its `TimbreRuntime`;
  - its `Application` instance. This is the project's `Application` subclass
    when the project has one, and a plain `Application` otherwise.
- `PreviewCompiler` reuses the project's existing `bin\Debug` output when the
  saved markup is unchanged. That is why a Debug build must exist before the
  preview works.

## Cerneala.VisualStudio

### Components

| Type | File | Role | Owner |
|---|---|---|---|
| `CernealaContentType` | `Cerneala.VisualStudio/CernealaContentType.cs` | Content type `cerneala-crn`, based on `CodeRemoteContent`, for `.crn`. | MEF |
| `CernealaLanguageServerProvider` | `Cerneala.VisualStudio/CernealaLanguageServerProvider.cs` | `[Export(typeof(ILanguageClient))]` for `cerneala-crn` (`:20-21`). Starts the server and returns a `Connection` over its pipes. | MEF, shared |
| `CernealaServerProcessManager` | `Cerneala.VisualStudio/Server/CernealaServerProcessManager.cs` | Starts, stops and restarts the server process. Crash backoff: 250 ms, 1 s, 4 s; restarts stop after 3 crashes in 2 minutes; shutdown timeout 3 s (`:63-71`). | Provider |
| `SystemCernealaServerProcess` | `Cerneala.VisualStudio/Server/SystemCernealaServerProcess.cs` | The real `System.Diagnostics.Process` behind the manager. | Manager |
| `CernealaPreviewMarginProvider` | `Cerneala.VisualStudio/Preview/CernealaPreviewMarginProvider.cs` | Creates one preview margin, with one preview session, for each `.crn` text view. | MEF |
| `CernealaPreviewSession` | `Cerneala.VisualStudio/Preview/CernealaPreviewSession.cs` | Sends the whole buffer 300 ms after the last edit and shows the returned frame. | Text view |
| `CernealaPreviewHostClient` | `Cerneala.VisualStudio/Preview/CernealaPreviewHostClient.cs` | Starts `Cerneala.PreviewHost.exe` and sends one request at a time. | Preview session |

### Data Flow

Language server:

1. Visual Studio opens a `.crn` file. MEF activates
   `CernealaLanguageServerProvider`.
2. The constructor resolves the server path with
   `ResolveBundledServerPath`, which gives
   `<extension install root>\Server\<version>\Cerneala.LanguageServer.exe`.
   The VSIX ships the server published self-contained for `win-x64`, and
   `PreviewHost` under `PreviewHost\<version>`.
3. `ActivateAsync` calls `CernealaServerProcessManager.StartAsync`, then
   returns `new Connection(session.Reader, session.Writer)` (`:153`). If the
   binary is missing, `StartAsync` throws `FileNotFoundException` and logs the
   expected path.
4. Visual Studio sends `initialize` with these options (`:280-288`):
   - `solutionPath`;
   - `host: "visualStudio"`;
   - `diagnosticsMode: "push"`;
   - `deferWorkspaceLoad: true`;
   - `telemetryEnabled: false`. The server does not read this option.
5. `OnAfterCloseSolution` (`:255`) stops the server.
   `StopAsync` closes the server's stdin and waits up to 3 s. If the server is
   still running, it logs "did not exit within 3 seconds; terminating it" and
   kills the process.

Preview:

1. Each edit restarts a 300 ms timer.
2. When the timer fires, the session sends `Render` with the whole buffer
   text.
3. `CernealaPreviewHostClient` allows only one request at a time. If a send
   throws, the client kills the host process. The next request starts a new
   host.
4. `CernealaPreviewSession.ApplyResponse` handles the answer:
   - for a `Frame`, it writes the pixels into a `WriteableBitmap` declared as
     `PixelFormats.Bgra32`, without converting them;
   - for an `Error`, it shows the message and keeps the last frame.
5. `Dispose` sends `Shutdown`, waits 500 ms, then kills the host.

### Lifecycle And Ownership

- `CernealaLanguageServerProvider` is one shared MEF instance. It owns one
  `CernealaServerProcessManager`, which owns at most one running server process.
  Closing the solution stops that process.
- Each `.crn` text view owns its own `CernealaPreviewSession`, and each session
  owns its own `CernealaPreviewHostClient` and preview host process. Two open
  `.crn` views mean two preview host processes. Disposing the session ends its
  host.

### History

The extension uses the classic VSSDK `ILanguageClient` with MEF. The newer
VisualStudio.Extensibility `LanguageServerProvider` was tried first, but
`CreateServerConnectionAsync` was never called. The decision record is
[archive/visual-studio-community-spike.md](../archive/visual-studio-community-spike.md).
Setup and use are in the [Visual Studio guide](../guides/visual-studio-community.md).

## Invariants

`Cerneala.Language` (tests in `tests/Cerneala.Tests.Language/`):

- Lexing is lossless: the token texts join to the exact input, including
  comments, CDATA and namespaces
  (`MarkupParserTests.cs:9 LexerIsLosslessAcrossXmlTriviaNamespacesAndEmbeddedComparators`).
- Valid documents round-trip byte for byte with no missing tokens. This holds
  for the corpus and for the real repository documents
  (`MarkupParserTests.cs:23 ValidDocumentsRoundTripByteForByteWithCompleteTree`,
  `:37 RealRepositoryDocumentsRoundTripAndHaveCompleteTrees`).
- Missing tokens have deterministic zero-width spans
  (`MarkupParserTests.cs:56 MissingTokensUseDeterministicZeroWidthSpans`).
- An unclosed element recovers at the matching ancestor close. In
  `<StackPanel><Border><Button Name="Inside" /></StackPanel><TextBlock Name="After" />`,
  both `Button` and `TextBlock` are in the tree, and only `Border` has missing
  tokens (`MarkupParserTests.cs:69 OverlappedElementsRecoverAtTheMatchingAncestorClose`).
- 10,000 random edits with a fixed seed never throw and never produce invalid
  spans (`MarkupParserTests.cs:80 TenThousandRandomIncrementalEditsNeverThrowOrProduceInvalidSpans`).
- Each recovery corpus case has at most one syntax diagnostic. When it has one,
  there are no semantic diagnostics
  (`RecoveryBaselineTests.cs:14 IncompleteDocumentsRequireTolerantRecovery`,
  `:30 UnrecoverableSyntaxSuppressesSemanticCascades`).
- An unknown binding source such as `$Missing.First.Second.Third` gives exactly
  one `CERNEALAUI007`, and no symbols for the later segments
  (`SemanticScopesTests.cs:91 UnknownBindingSourceProducesOneDiagnosticWithoutSegmentCascade`).
- `WithDocument` keeps the model of an unaffected document (same object).
  `WithProjectSymbols` with a new version rebuilds it. Disposing the original
  compilation does not break the derived one
  (`SemanticWorkspaceTests.cs:211 VersionedCachesRetainOnlyUnaffectedDocuments`).
- Cancellation and disposal are explicit: a cancelled token gives
  `OperationCanceledException`, and a disposed compilation gives
  `ObjectDisposedException` (`SemanticWorkspaceTests.cs:233 LifecycleAndCancellationAreExplicit`).
- The catalog lists every ID exactly once, and every build severity is Error
  (`DiagnosticCatalogTests.cs:20 CommonCatalogContainsEveryCurrentDescriptorExactlyOnce`).
- Editor mode lowers `CERNEALAUI001` to Information but keeps `CERNEALAUI002` an
  Error. The test checks only these two IDs
  (`DiagnosticCatalogTests.cs:42 EditorModeReducesOnlyTransientIncompleteDiagnostics`).
- Two source-text checks, which test text and not behavior:
  - `Syntax/` contains no `XText`, `XElement` or `SourceProductionContext`
    (`DiagnosticCatalogTests.cs:53 LanguageCoreContainsNoXmlOrSourceGeneratorHostTypes`);
  - `Semantics/` contains no `Assembly.Load`, `System.Reflection` or `GetType(`
    (`SemanticWorkspaceTests.cs:247 SemanticCoreDoesNotLoadAssembliesOrUseReflection`).

`Cerneala.LanguageServer` (tests in `tests/Cerneala.Tests.LanguageServer/`):

- `initialize` returns the server name `Cerneala Language Server` and the full
  capability set, and `shutdown` + `exit` ends with 0. This runs over an
  in-memory transport
  (`ProtocolContractTests.cs:10 LifecycleNegotiatesAndStopsCleanlyOverInMemoryTransport`).
- Push mode advertises no `DiagnosticProvider`
  (`ProtocolContractTests.cs:59 VisualStudioPushModeDoesNotAdvertisePullDiagnostics`).
- Pushed diagnostics carry the document version and are retracted on close
  (`ProtocolContractTests.cs:150 PushDiagnosticsUseCurrentVersionAndRetractAfterRepairAndClose`).
- A real stdio process:
  - after `shutdown` + `exit`, it exits with 0 and logs `lifecycle.exit` to
    stderr (`ProcessLifecycleTests.cs:17 StdioProcessNegotiatesShutdownAndLeavesNoServerProcess`);
  - after its stdin is closed, it exits with 0 and logs `server.disconnected`
    (`ProcessLifecycleTests.cs:83 HostDisconnectTerminatesTheServerWithoutLeavingAProcess`).

`Cerneala.PreviewHost` (tests in `tests/Cerneala.Tests.PreviewHost/PreviewHostTests.cs`):

- Saved markup that did not change reuses the existing build output (`:19`).
  Unsaved markup replaces the AdditionalFile on disk (`:60`) and compiles
  (`:78`).
- The host renders through the binary protocol (`:154`). Pointer, click, text
  and key input round-trip through the protocol (`:448`).
- A literal edit updates the live tree with `CompileMilliseconds == 0` (`:229`).
- An incomplete literal is deferred and does not change the tree (`:413`).
- A structural edit requires compilation (`:435`).
- A Timbre markup edit requires compilation instead of the attribute fast path
  (`:507`).
- Frame responses reuse the caller's image buffer (`:474`).
- The render scale does not depend on the desktop DPI (`:113`).
- The runtime can be created again after a recompile (`:134`).
- The audio policy travels in the protocol, together with its visible state
  (`:535`).
- Invalid Timbre markup is not reported as success: `CERNEALAUI032` (`:575`).

`Cerneala.VisualStudio` (tests in `tests/Cerneala.Tests.VisualStudio/CernealaServerProcessManagerTests.cs`):

- A missing server binary throws `FileNotFoundException` with the full path and
  logs "Expected path" (`:22 MissingBundledBinaryFailsWithTheInstallRelativePath`).
- Crashes wait 250 ms, then 1 s. After the third crash, `StartAsync` throws
  "restart loop disabled"
  (`:87 CrashesUseBoundedBackoffAndDisableTheRestartLoopAtTheThreshold`).
- Disable, update and uninstall each close stdin and wait first, and kill only
  after the timeout
  (`:198 DisableUpdateAndUninstallForceTerminationOnlyAfterTheTimeout`).
- The bundled path is `<install root>\Server\<version>\Cerneala.LanguageServer.exe`
  (`:219 BundledPathIsVersionedUnderTheExtensionInstallRoot`).

Netestat:

- the 150 ms watcher debounce and the `ShouldReload` filter;
- `exit` without `shutdown` returning 1;
- the collectible load context actually unloading after a recompile;
- the host's behavior on a malformed frame length;
- `CernealaPreviewHostClient` and the client side of `PreviewProtocol`, which
  have no direct test in `Cerneala.Tests.VisualStudio`;
- `SystemCernealaServerProcess`, the real process wrapper;
- the channel order of a decoded preview frame. The test
  `OpeningViewCapturesThePresentedBgraFrameWithoutPngEncoding` checks only the
  stride, the length and "not a PNG".

## Known Limitations

- The preview needs the project's Debug build output. Without it, the fast path
  has nothing to reuse (`PreviewCompiler.cs`, `bin\Debug`).
- The host writes frame pixels in R, G, B, A order. The Visual Studio client
  writes them unconverted into a `PixelFormats.Bgra32` bitmap, which reads
  byte 0 as blue. So the two sides name different channel orders. This is
  pending an issue report.
- `telemetryEnabled` is sent by Visual Studio but `CernealaInitializationOptions`
  has no such field. The LSP `processId` is only logged.
- `SystemCernealaServerProcess` logs every stderr line of the server with
  `log.Error("Cerneala language server stderr: ...")`. The server writes its
  structured log to stderr (`StructuredServerLogger.CreateForStandardError`),
  so normal log lines such as `server.started` are logged as errors.
- `CernealaPreviewHostClient` collects the preview host's stderr into a buffer,
  but no code reads that buffer, so the user never sees it.
