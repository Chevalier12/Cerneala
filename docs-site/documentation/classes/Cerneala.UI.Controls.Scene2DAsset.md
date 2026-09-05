# Scene2DAsset Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2DDocument.cs`

Declares an atlas resource key, root-relative path, and expected dimensions without loading the image.

```csharp
public sealed class Scene2DAsset
```

## Examples

```csharp
var atlas = new Scene2DAsset(
    new ResourceId<ImageResource>("Atlas"), "images/atlas.png", new DrawSize(256, 128));
```

## Remarks

The constructor requires a nonempty resource key/path and positive dimensions. Backslashes normalize to slashes. Rooted paths, colons, NUL, empty segments, and dot/dot-dot segments are rejected. Importers resolve external relative references inside their permitted root before creating this normalized declaration.

This lexical check does not establish that a file exists or that its filesystem components are safe. Filesystem containment and link checks belong to the importer; decoding/upload belongs to the resource system.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2DAsset(ResourceId<ImageResource> resourceId, string path, DrawSize size)` | Validates the declaration without opening or decoding the asset. |

## Properties

| Name | Description |
| --- | --- |
| `ResourceId` | Atlas image resource identifier. |
| `Path` | Normalized root-relative local path. |
| `Size` | Positive declared atlas dimensions. |
