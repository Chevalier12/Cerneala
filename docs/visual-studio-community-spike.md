# Spike Visual Studio Community 2026 18.9

> Verification date: 2026-08-14
> IDE: Visual Studio Community `18.9.12105.275`, Experimental Instance `Exp`
> Approved decision: classic VSSDK host (`ILanguageClient` + MEF), no
> `Microsoft.VisualStudio.Extensibility.LanguageServerProvider`

## Result

The spike confirmed a full path for the Cerneala extension:

- classic VSIX `net472`, loaded lazy through the exact content type `.crn`;
- `win-x64` self-contained server, included in VSIX and started from install root;
- LSP connection on stdin/stdout, with `initialize`, `initialized`, `shutdown` and
  the disappearance of the process when closing Visual Studio;
- TextMate available immediately and semantic tokens applied after initialization;
- push diagnostics displayed only once in the editor and Error List;
- Normal XML and similar extensions are not retrieved.

The out-of-process model loaded and executed a minimal command, but
`LanguageServerProvider.CreateServerConnectionAsync` was not called for
the associated document. The same result appeared with the official Rust sample, not only
with the code Cerneala. The problem corresponds to the still open Microsoft issue
[VSExtensibility #472](https://github.com/microsoft/VSExtensibility/issues/472).
As server activation is a binding contract, the classic VSSDK fallback
was explicitly approved.

## Spike projects

Two temporary hosts were tested:

| Host | Packages | Result |
|---|---|---|
| Out-of-process | `Microsoft.VisualStudio.Extensibility.Sdk` and `.Build` `17.14.40608` | The extension is loading; the LSP provider does not activate in 18.9. |
| Classic VSSDK | `Microsoft.VisualStudio.LanguageServer.Client` `17.14.60`, Shell `17.14.40264`, BuildTools `18.9.820` | `ILanguageClient` activates, starts the server and exchanges LSP messages. |

The classic configuration uses a `ContentTypeDefinition` derived from
`code-languageserver-preview`, one `FileExtensionToContentTypeDefinition` only
for `.crn` and the export `ILanguageClient` decorated with that content type.
Publisher registration assigns the `crn` extension the built-in Source Code editor
(Text) Editor with priority `0x64`; thus the XML editor does not win the association.

The spawn command for the classic host was:
```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' `
  .\Tools\spikes\Cerneala.VisualStudio.CrnSpike\Container\Container.csproj `
  /t:Rebuild /p:Configuration=Debug

& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' `
  /RootSuffix Exp `
  .\Tools\spikes\Cerneala.VisualStudio.CrnSpike\Fixture\Fixture.sln
```
The temporary code indicated by the commands was deleted after capturing the results, like this
as the plan requires. Stage 1 recreates only the approved classic host.

## Activate documents

Sample `IWpfTextViewCreationListener`, content type registry and process log
produced the following matrix:

| Document | Content type/editor | Customer Cerneala | Server Cerneala |
|---|---|---:|---:|
| `View.crn` | `cerneala-crn-spike`, Source Code (Text) Editor | yes | yes |
| `app.config` | `XML` | no | no |
| `foo.xml` | `XML` | no | no |
| `View.crn.cs` | `CSharp` | no | no |
| `View.cui.xml` | `XML` | no | no |

For all four negative controls `client.loaded` did not appear, o
connection or a server process. The association `.crn` does not change the XML editor.

## Lifecycle and packaging

The server was published `win-x64` self-contained. VSIX installed it under
extension version directory and started it exclusively relative to that install
root. No separate SDK or runtime installed was required.

A full session log confirmed, in order:
```text
client.loaded
connection.started
server.started
workspace.loaded
lifecycle.initialized
lifecycle.ready
client.initialized
document.opened
lifecycle.shutdown
```
The server process and the Experimental Instance process no longer existed after quit.

The first packaging attempt accidentally flattened the files from
`BuildHost-net472` and `BuildHost-netcore`, overwriting dependencies from the root
the server. The final spike target kept `RecursiveDir` for each
file. VSIX inspection confirmed `System.Text.Json.dll` of `1,890,088` bytes in
the root, the version of `777,480` bytes in `BuildHost-net472` and 23 files in
BuildHost subdirectory, no collisions.

## Coloring and diagnostics

Before the server is ready, the classifier API has already reported the scopes
TextMate `markup attribute`, `markup node` and `string`. Customer Trace a
subsequently reported `textDocument/semanticTokens/full` and
`textDocument/semanticTokens/full/delta`. Grammar remains fallback, again
semantic tokens can refine the classification after the handshake.

Visual Studio 18.9 exposed a reproducible gap when the server announces simultaneously
push and pull diagnostics:

1. The server's raw stdout contained only one
   `textDocument/publishDiagnostics`, with a single `CERNEALAUI002`;
2. the editor and the Error List initially displayed one entry each;
3. after the request `textDocument/diagnostic`, both reached two entries
   identical.

A controlled run in which the server announced only diagnostics push remained at
exactly one `IErrorTag` and exactly one Error List entry after the complete refresh cycle:
```text
error-tags count=1
error-list total=1 count=1 entries=CERNEALAUI002:...
```
The contract of the final host is therefore: Visual Studio receives the diagnostics module
push through initialization options, and the server does not announce `diagnosticProvider`
for that session. Other hosts can continue to use pull. Selection of one
single channel belongs to the protocol/host adapter; it is not artificially deduplicated in
view or in Error List.

## Client capabilities 18.9

Microsoft's official table, updated to 2026-06-01, confirms support for
`initialize`, `initialized`, `shutdown`, `exit`, cancellation, diagnostics push,
completion, completion resolve, hover, signature help, definition, references,
rename, document/range formatting and code actions. The source is
[Add a Language Server Protocol extension](https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=visualstudio).

| Contract Cerneala | Support 18.9 | Proof Stage 0 |
|---|---:|---|
| Completion | yes | Official table and handshake capability. |
| Completion resolve | yes | Official table and resolve support in client capabilities. |
| Diagnostics | yes | Push, squiggle and Error List checked; the host selects push-only. |
| Hover | yes | Official table and handshake capability. |
| Definition | yes | Official table and handshake capability. |
| References yes | Official table and handshake capability. |
| Rename | yes | Official table and prepare/rename capability handshake. |
| Formatting | yes | Document and range formatting in the official table. |
| Semantic tokens | yes | Live requests `full` and `full/delta` in trace 18.9. |
| Code actions | yes | Official table and code-action capability handshake. |

`textDocument/onTypeFormatting` does not appear as supported in the official table and no
is used as evidence for mandatory formatting. The functional matrix
end-to-end for publisher commands remains the Stage 4 gate.

## Decision for implementation

The following steps use classic VSSDK, content type MEF exactly `.crn`, the editor
built-in text, self-contained bundled server and push-only diagnostics for
Visual Studio session. The out-of-process model can be reevaluated only after
Microsoft solves the activation of `LanguageServerProvider` in target installation and
the same matrix passes without exception.