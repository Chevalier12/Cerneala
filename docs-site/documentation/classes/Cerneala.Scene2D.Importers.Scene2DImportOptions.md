# Scene2DImportOptions Class

## Definition

Namespace: `Cerneala.Scene2D.Importers`

Assembly/Project: `Cerneala.Scene2D.Importers` (optional)

Source: `Cerneala.Scene2D.Importers/Scene2DImportOptions.cs`

Configures local asset containment and bounded runtime scene import.

```csharp
public sealed class Scene2DImportOptions
```

## Examples

```csharp
using Cerneala.Scene2D.Importers;

var result = TiledScene2DImporter.Import("Content/Maps/village.tmj",
    new Scene2DImportOptions
    {
        AssetRootDirectory = "Content",
        MaxCells = 262_144,
        MaxDiagnostics = 32
    });
```

## Remarks

Options are init-only caller policy. All numeric budgets must be positive; invalid options throw `ArgumentOutOfRangeException` rather than producing a content diagnostic. Content cannot enlarge budgets. Raising an import budget does not bypass core model construction limits.

The default root is the input map's directory. An explicit root and a relative input filename resolve against the process working directory. Every input JSON file, atlas and nonempty file-valued property must resolve **below** the root. External paths resolve against the declaring file, normalize both separators and `.`/`..`, and must remain contained. Rooted external paths, URIs, network/device paths, alternate data streams and reparse points below the root are rejected.

The caller must provide a stable, trusted root tree for the duration of the synchronous import. Path checks reject existing links; they are not an OS-handle-based sandbox against a separate process concurrently replacing filesystem components. The root itself is caller-selected trust, not content-selected configuration. No source-provided URL is fetched or command executed.

Budgets are checked before allocations derived from declared dimensions. Tiled import additionally caps source-expanded collider descriptors at 65,536 overall and 4,096 per tile/entity before materialization. These limits also cover unused tile definitions; a large rejected collision object group is not fully expanded first. The parser retains bounded JSON documents, which are disposed after import. JSON limits do not describe atlas decoding or GPU allocation: those remain the existing resource system's responsibility.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DImportOptions()` | Creates the defaults below. |

## Properties

All properties have `get; init;` accessors.

| Name | Type | Default | Meaning |
| --- | --- | ---: | --- |
| `AssetRootDirectory` | `string?` | `null` | Input directory by default; optional wider local root. |
| `MaxFileBytes` | `long` | 16,777,216 | Bytes in one JSON file. |
| `MaxTotalBytes` | `long` | 67,108,864 | Aggregate bytes in JSON reads, including external files. |
| `MaxFiles` | `int` | 1,024 | Number of JSON file reads. |
| `MaxJsonDepth` | `int` | 64 | Maximum JSON object/array nesting. |
| `MaxCells` | `int` | 1,048,576 | Aggregate decoded cells; also bounds atlas definition count. |
| `MaxChunks` | `int` | 65,536 | Aggregate chunk count. |
| `MaxLayers` | `int` | 4,096 | Parsed layer count, including Tiled groups; also final core layer budget. |
| `MaxEntities` | `int` | 65,536 | Parsed objects, including tile collision objects; final core entities plus promotion references are also bounded. |
| `MaxPoints` | `int` | 4,096 | Points per source polygon/polyline. |
| `MaxDiagnostics` | `int` | 128 | Retained diagnostic count; truncation never turns failure into success. |

## See also

- [TiledScene2DImporter](Cerneala.Scene2D.Importers.TiledScene2DImporter.md)
- [Scene2DImportResult](Cerneala.Scene2D.Importers.Scene2DImportResult.md)
- [Core model validation and fixed construction limits](Cerneala.UI.Controls.Scene2DModelValidator.md)
