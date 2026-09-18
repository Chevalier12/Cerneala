# Scene2DImportResult Class

## Definition

Namespace: `Cerneala.Scene2D.Importers`

Assembly/Project: `Cerneala.Scene2D.Importers` (optional)

Source: `Cerneala.Scene2D.Importers/Scene2DImportResult.cs`

Reports one atomic scene import and its bounded core diagnostics.

```csharp
public sealed class Scene2DImportResult
```

## Examples

```csharp
using Cerneala.Scene2D.Importers;

var result = TiledScene2DImporter.Import("Content/village.tmj");
foreach (var diagnostic in result.Diagnostics)
    System.Console.WriteLine($"{diagnostic.Code}: {diagnostic.FilePath} {diagnostic.JsonPath}: {diagnostic.Message}");

if (result.Success)
{
    System.Console.WriteLine($"Imported {result.Document!.Levels.Count} level(s).");
    foreach (string relativePath in result.ReferencedFiles)
        System.Console.WriteLine(System.IO.Path.Combine(result.AssetRootDirectory!, relativePath));
}
```

## Remarks

Only a completely constructed and core-validated document can be returned. Fatal, Error or Unsupported diagnostics force `Success == false` and `Document == null`; no partial map is published. Known editor-only fields produce warnings, aggregated per source file, and do not prevent success.

Diagnostics are the same backend-neutral `Scene2DDiagnostic` records used by core validation. Their retained order is deterministic. The importer stops at the first blocking failure; it is not an exhaustive repair report. If an error occurs after retained warning slots are full, failure is still recorded even though that error may not appear in `Diagnostics`; `DiagnosticsTruncated` then reports the loss. Text fields are bounded by the core collector.

There is no public constructor. The result owns no UI element, GPU resource, collision world or live parser stream.

### Explicit runtime file dependencies

On success, `AssetRootDirectory` is the normalized absolute local root used by
the importer. `ReferencedFiles` is a detached, read-only snapshot of the atlas
images and nonempty Tiled `file` / LDtk `FilePath` references validated during
that import. Paths use `/`, are relative to that root, and are unique and sorted
with ordinal string comparison. Repeated normalized references occur once.
These entries complement `Document.Assets`, which describes atlas images only.

File-valued properties remain root-relative strings in the document; ordinary
text is never inferred to be a dependency. Referenced files are checked for
readability but their content is not parsed recursively. Editor map, tileset and
external-level JSON files are not runtime dependencies merely because the
importer parsed them; they appear only if also explicitly referenced as a file
property. This list does not promise a recursively self-contained dependency
graph for arbitrary file formats.

Failure publishes neither a document nor a partial dependency list:
`AssetRootDirectory` is null and `ReferencedFiles` is empty. The snapshot owns
only paths, not open handles or file contents. A later packaging operation must
recheck file availability and containment; import does not freeze the filesystem.

### Diagnostic categories

Core model codes `SCN2D003`–`SCN2D015` are listed on
[Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md). Import
also uses these categories:

| Code | Meaning |
| --- | --- |
| `SCN2D001` | Required local file cannot be read (including I/O/access failures). |
| `SCN2D002` | Malformed JSON, missing/wrong structural field, invalid encoded/compressed tile payload or incompatible layer payload structure. |
| `SCN2D016` | Invalid primitive property/field value, nullability or IntGrid value. |
| `SCN2D017` | Known editor-only fields are unused at runtime; aggregated warning per source file. |

Use `Code`, `Severity`, `FilePath` and `JsonPath` together. A version mismatch,
unsupported construct or unresolved asset prevents document publication; it is
not permission to render a partial map. The precise accepted fields and
conditional values are defined on each importer's canonical page.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Success` | `bool` | Whether a complete validated document is available. |
| `Document` | `Scene2DDocument?` | Validated core data, or null on failure. |
| `AssetRootDirectory` | `string?` | Normalized absolute asset root on success; null on failure. |
| `ReferencedFiles` | `IReadOnlyList<string>` | Immutable, ordinally sorted, unique root-relative atlas and explicitly file-valued resource paths; empty on failure. |
| `Diagnostics` | `IReadOnlyList<Scene2DDiagnostic>` | Read-only retained diagnostics, possibly including warnings on success. |
| `DiagnosticsTruncated` | `bool` | Whether additional diagnostics did not fit the retention budget. |

## See also

- [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md)
- [Scene2DDiagnostic](Cerneala.UI.Controls.Scene2DDiagnostic.md)
- [Scene2DImportOptions](Cerneala.Scene2D.Importers.Scene2DImportOptions.md)
