# Cerneala language server

## Protocol contract

The server targets Language Server Protocol 3.17 over JSON-RPC 2.0. Messages use
UTF-8 JSON and the LSP `Content-Length` header framing on a pair of streams. The
host-facing executable uses stdin/stdout; tests may provide one full-duplex stream
without changing the wire protocol.

The transport implementation is StreamJsonRpc 2.25.29 with
`HeaderDelimitedMessageHandler` and `SystemTextJsonFormatter`. It is
cross-platform, transport-independent, supports request cancellation, targets the
repository's .NET 10 runtime, and is maintained independently of any editor SDK.
The server owns its LSP DTOs so protocol upgrades do not force a Visual Studio SDK
dependency into the host-agnostic process.

Protocol upgrades are explicit changes. Before changing the LSP version or the
StreamJsonRpc major/minor version, protocol tests must cover every newly announced
capability and validate framing and serialization against the supported host. The
server only advertises a capability after its protocol-level test is green.

The executable is host-agnostic and references no Visual Studio SDK. A host owns
the process and streams, sends `initialize`/`initialized`, and closes it with
`shutdown` followed by `exit`. Disconnecting the host stream also tears down the
workspace, watchers, cancellation sources, and caches.

The canonical Cerneala document extension is `.crn`. The server discovers only
`.crn` additional files and pairs a view with the sibling `.crn.cs` companion;
the retired `.cui.xml` suffix is intentionally not an alias. Build-time source
generation and editor requests both consume the shared `Cerneala.Language`
syntax and semantic model, so the server does not maintain a second parser or
binding implementation.

## Advertised capabilities

The server advertises only protocol-tested features:

- incremental text synchronization and push/pull diagnostics;
- completion/resolve and signature help;
- hover, definition, references, document highlights, prepare rename, and rename;
- full/delta semantic tokens, document/workspace symbols, folding, and selection ranges;
- document/range/on-type formatting and deterministic code actions.

Semantic tokens use the Cerneala-specific legend for element types, properties,
attached properties, events, namespaces, resources, binding sources/members,
directives, Motion, and Prism. Code actions advertise `quickfix`,
`refactor.rewrite`, and `source.fixAll.cerneala`. Fix-all is returned only when
the selected repairs are independent and non-overlapping.

Open buffers are overlays: they never write to disk, only increasing document
versions are accepted, and a newer version cancels requests for the previous one.
Project reload similarly cancels work against the old compilation. Semantic token
history is limited to 256 documents and telemetry retains at most 2,048 samples;
both are cleared during close/unload/shutdown.

Workspace reloads are debounced for 150 ms and ignore `bin` and `obj` changes.
Requests capture a workspace revision and the active overlay versions; a result
is returned only when both are still current. A deferred initial workspace load
is available to hosts that need the process to become responsive before Roslyn
project discovery completes. On the first opened project document, deferred mode
bootstraps its semantic context from in-memory copies of the latest built project
output and dependencies. The first semantic request waits only for that bootstrap;
the complete MSBuild workspace continues loading in the background and atomically
replaces it. Reading assembly images into memory prevents the language server from
locking build outputs. If no compatible output exists yet, the document remains
syntax-only until the full workspace finishes loading or the project is built.

Files not owned by a project receive syntax-only completion, diagnostics,
formatting, symbols, folding, and selection support. They also receive the single
informational diagnostic `CERNEALAWORKSPACE001`. Semantic C# features become
available after the file is included as an `AdditionalFiles` item in a loaded
project.

## Logging and crash reports

Structured logs are newline-delimited JSON written only to stderr. The supported
trace levels are `off`, `messages`, and `verbose`, controlled through LSP
`$/setTrace`. No level records document text or document URIs. Stdout is reserved
exclusively for protocol traffic.

At `verbose`, `performance.measurement` events report only the operation category,
elapsed milliseconds, managed allocation delta, and cancellation state. Categories
include parse, bind, queue, completion, diagnostics, and navigation. These events
never contain request parameters, source text, paths, or URIs.

Crash reports are disabled by default. A host may opt in by setting
`CERNEALA_LSP_CRASH_DIRECTORY`; reports contain timestamp, process id, exception
type, and stack trace, but omit exception messages and document content.

## Performance gates

The checked protocol baseline runs on Windows 11 Home `10.0.26200`, an AMD EPYC
9354 32-Core Processor, 16 GiB RAM, and .NET SDK `10.0.300`. After five warm-up
requests, the protocol suite requires diagnostics p95 below 200 ms, completion p95
below 100 ms, and hover/definition p95 below 100 ms. Every sampled request must
finish or observe cancellation within 500 ms. The same suite exercises two active
documents, rapid version changes, project reload, and 100 cancelled completion
requests. A separate lifecycle test performs 1,000 open/change/close cycles and
requires less than 32 MiB retained managed growth after a full collection.

These are regression gates, not promises for every project size or machine. Run
`dotnet test .\tests\Cerneala.Tests.LanguageServer\Cerneala.Tests.LanguageServer.csproj`
on materially different hardware before tightening them.

## Troubleshooting

- If a document reports `CERNEALAWORKSPACE001`, verify that its project includes
  the file as `AdditionalFiles`, then save or restart the host to reload the project.
- If symbols look stale, confirm the host sends increasing `didChange` versions and
  sends `didSave` after project/reference changes.
- Set `$/setTrace` to `verbose` to inspect operation timing and cancellation without
  exposing source. Keep stdout untouched because any non-LSP output corrupts framing.
- For a crash before protocol shutdown, set `CERNEALA_LSP_CRASH_DIRECTORY` to a
  writable private directory and inspect the scrubbed JSON report plus stderr logs.
