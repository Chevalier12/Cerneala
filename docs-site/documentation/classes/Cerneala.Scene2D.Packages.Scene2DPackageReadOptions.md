# Scene2DPackageReadOptions Class

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/Scene2DPackageMetadata.cs`

Sets positive encoded-byte admission limits for opening a package and acquiring a payload.

```csharp
public sealed class Scene2DPackageReadOptions
```

## Examples

```csharp
using Scene2DPackage package = await Scene2DPackage.OpenAsync(
    packageDirectory,
    new Scene2DPackageReadOptions { MaxPayloadBytes = 8 * 1024 * 1024 });
```

## Remarks

Limits are checked before allocating the corresponding encoded byte buffer.
The catalog limit excludes its 32-byte checksum trailer. The payload limit applies
to each requested block, not to the whole package and not to unrequested blocks.
An oversized payload fails explicitly without preventing a smaller independent read.

These are **not** total managed-memory, native-memory, decoded-image or GPU-memory
budgets. Decoding can allocate more than the encoded size, and concurrent reads
have separate buffers. Scene residency and the common image cache own their
respective acquisition policies.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DPackageReadOptions()` | Creates options with the defaults below. |

## Properties

| Name | Default | Description |
| --- | ---: | --- |
| `MaxCatalogBytes` | 16,777,216 | Maximum encoded catalog content bytes. |
| `MaxPayloadBytes` | 67,108,864 | Maximum encoded bytes per requested payload. |

Both properties are init-only. `OpenAsync` rejects nonpositive values with
`ArgumentOutOfRangeException`.

## See also

- [Scene2DPackage](Cerneala.Scene2D.Packages.Scene2DPackage.md)
