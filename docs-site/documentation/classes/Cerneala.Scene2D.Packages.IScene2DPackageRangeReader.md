# IScene2DPackageRangeReader Interface

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/IScene2DPackageRangeReader.cs`

Supplies independent byte ranges from one immutable prepared CPV2 scene package revision.

```csharp
public interface IScene2DPackageRangeReader : IAsyncDisposable
```

## Examples

```csharp
// reader is an application-provided IScene2DPackageRangeReader.
await using Scene2DPackage package = await Scene2DPackage.OpenAsync(reader, cancellationToken: cancellationToken);
Scene2DPackageLevel level = package.Levels[0];
TileMap2D map = level.CreateTileMap(level.TileMapIds[0]);
```

Keep the package alive while `map` can request chunks. Detach and await `map.DisposeAsync()` before closing the package when a complete shutdown is required.

## Remarks

`Catalog` and `Payloads` must represent the same prepared CPV2 revision for the entire reader lifetime. Reported lengths and bytes at each offset remain stable. Independent range reads may run concurrently. An implementation backed by one seekable stream must serialize positioning and reading or use independent streams. This is a byte transport contract, not an importer for arbitrary formats or a public spatial-selection API.

`ReadAsync` may return a short positive count; the package continues reading until the requested range is full. Return zero only at end of data, and return a count from zero through the supplied destination length. Do not retain or access `destination` after the returned operation completes. Honor the per-call cancellation token. The package validates ranges, limits, checksums, and decoded payload types; it does not automatically retry a failed read.

Passing a non-null reader to `Scene2DPackage.OpenAsync` transfers its ownership once, including when opening fails because of invalid options or cancellation. The package awaits reader disposal on failed open. After successful open, `Scene2DPackage.Dispose()` rejects new reads and starts shutdown; `DisposeAsync()` awaits admitted reads and reader cleanup. A range reader does not supply local asset file paths or automatically load remote images. Supply image resources separately through Cerneala's resource provider/image-loader APIs.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `GetLengthAsync(Scene2DPackagePart, CancellationToken = default)` | `ValueTask<long>` | Reports the stable byte length of one package part. |
| `ReadAsync(Scene2DPackagePart, long, Memory<byte>, CancellationToken = default)` | `ValueTask<int>` | Reads bytes at an absolute offset into the supplied memory and returns the count read. |
| `DisposeAsync()` (inherited) | `ValueTask` | Releases reader resources; the owning package calls it after admitted reads drain. |

## See also

- [Scene2DPackagePart](Cerneala.Scene2D.Packages.Scene2DPackagePart.md)
- [Scene2DPackage](Cerneala.Scene2D.Packages.Scene2DPackage.md)
- [Scene2DPackageReadOptions](Cerneala.Scene2D.Packages.Scene2DPackageReadOptions.md)
