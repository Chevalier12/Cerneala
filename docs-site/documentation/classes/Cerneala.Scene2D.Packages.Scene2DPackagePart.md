# Scene2DPackagePart Enum

## Definition

Namespace: `Cerneala.Scene2D.Packages`

Assembly/Project: `Cerneala.Scene2D.Packages`

Source: `Cerneala.Scene2D.Packages/IScene2DPackageRangeReader.cs`

Identifies one of the two byte streams in a prepared CPV2 scene package.

```csharp
public enum Scene2DPackagePart
```

## Examples

```csharp
long catalogLength = await reader.GetLengthAsync(Scene2DPackagePart.Catalog, cancellationToken);
```

Here `reader` is an application implementation of [IScene2DPackageRangeReader](Cerneala.Scene2D.Packages.IScene2DPackageRangeReader.md).

## Remarks

The catalog contains the package index; payload blocks are addressed in the separate payload stream. A range reader must serve both parts from one immutable prepared-package revision. These values select byte streams, not maps, entities, or spatial regions. The interface does not decode an arbitrary application format.

## Fields

| Name | Description |
| --- | --- |
| `Catalog` | Prepared package catalog and its checksum trailer. |
| `Payloads` | Individually indexed package payload blocks. |

## See also

- [IScene2DPackageRangeReader](Cerneala.Scene2D.Packages.IScene2DPackageRangeReader.md)
- [Scene2DPackage](Cerneala.Scene2D.Packages.Scene2DPackage.md)
