# Visual Studio Community Extension

The Cerneala VSIX adds editor support for `.crn` files to 64-bit Visual Studio
Community 2026 version 18.9. The first release targets Community 18.x only; it
does not target Visual Studio 2022, Professional, or Enterprise.

## Install

1. Obtain the signed `Cerneala.VisualStudio.0.1.30.vsix` and its adjacent
   `.sha256` file from the release artifacts.
2. Close every running Visual Studio instance. The installer never forces an
   active IDE to close.
3. Double-click the VSIX, or open it through **Extensions > Manage Extensions**.
4. Let the Visual Studio Installer finish the installation and start Visual
   Studio Community normally.

The VSIX contains the self-contained Windows x64 language server and its .NET
runtime. No separate SDK, runtime, language server, or PATH change is required.
The checksum can be verified before installation with:

```powershell
Get-FileHash .\Cerneala.VisualStudio.0.1.30.vsix -Algorithm SHA256
```

## Update And Downgrade

Close Visual Studio, then install the newer VSIX over the existing extension. The stable identity
`Cerneala.Cerneala.VisualStudio` lets Visual Studio replace version N with N+1
without a second extension entry. Visual Studio refuses or otherwise leaves the
newer version active when an older VSIX is offered.

Version 0.1.30 has no user-configurable settings, so there is no settings schema
to migrate. Updates preserve the extension identity and normal enabled state.

## Uninstall

Open **Extensions > Manage Extensions**, find **Cerneala for Visual Studio**, and
choose **Uninstall**, then close Visual Studio so the installer can apply the
change. The Visual Studio Installer removes the provider and the
versioned bundled server. No separate runtime or server remains to uninstall.

## Editor Features

Opening a `.crn` document activates Cerneala support lazily. Normal XML,
`app.config`, `.crn.cs`, and the retired `.cui.xml` suffix keep their existing
editors and content types.

The tested Visual Studio Community integration includes:

- immediate TextMate highlighting and local brackets, comments, auto-closing,
  surrounding pairs, indentation, and word selection;
- diagnostics in the editor and Error List, including recovery while a document
  is incomplete;
- completion, completion details, hover, signature help, definition,
  references, highlights, safe rename, and workspace symbols;
- semantic tokens, document symbols, folding, selection ranges, formatting,
  on-type formatting, and deterministic code actions;
- unsaved buffers, undo/redo, large paste, multi-caret, simultaneous documents,
  and project reload after C# or project changes;
- a **Cerneala: Restart Language Server** command.

The extension has no designer, live preview, toolbox, property grid, build
commands, or deployment commands. Files outside a project receive syntax-only
support and one informational diagnostic because no Roslyn project context is
available.

## Troubleshooting And Logs

Use **View > Output**, then select **Cerneala**. Startup, restart, crash backoff,
missing binaries, and terminal server failures are also written to the Visual
Studio Activity Log under source `Cerneala`. Start Visual Studio with `devenv
/Log` when an Activity Log is needed.

Try **Cerneala: Restart Language Server** after a transient server failure. If a
document has syntax highlighting but no semantic features, confirm that it has
the `.crn` suffix and is included as an MSBuild additional file:

```xml
<AdditionalFiles Include="**\*.crn" />
```

For a persistent failure, include the Cerneala Output pane, the Activity Log,
the Visual Studio version, and whether the file belongs to a loaded project.
Document contents are not required for a lifecycle failure report.

## Privacy

Version 0.1.30 sends no telemetry and no document content. Source text is passed
only between the local Visual Studio process and the bundled local language
server. Future telemetry is outside this release and must be opt-in.

## Release Build

Official artifacts are built and signed from the Windows Certificate Store:

```powershell
$env:CERNEALA_VSIX_SIGNING_THUMBPRINT = '<SHA-1 certificate thumbprint>'
.\Tools\scripts\Build-CernealaVisualStudioRelease.ps1 -Version 0.1.30
```

The script creates a deterministic unsigned package, signs a copy with Sign CLI
and an RFC 3161 timestamp, and writes a SHA-256 checksum. `-SkipSigning` exists
only for local engineering validation; that output is not an official release.
This release is distributed as a VSIX artifact and is not published to the
Visual Studio Marketplace.
