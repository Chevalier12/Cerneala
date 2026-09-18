# SceneJsonValue2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneJsonValue2D.cs`

Stores detached JSON metadata with explicit reference identity.

```csharp
public sealed class SceneJsonValue2D
```

## Examples

```csharp
using System.Text.Json;
using Cerneala.UI.Controls;

SceneJsonValue2D metadata;
using (JsonDocument document = JsonDocument.Parse("{\"category\":\"terrain\"}"))
{
    metadata = new SceneJsonValue2D(document.RootElement);
}

// The original document is already disposed.
string? category = metadata.Value.GetProperty("category").GetString();
```

## Remarks

The constructor clones the supplied element into independent storage. Disposing
the input `JsonDocument` does not invalidate `Value`. JSON numbers, strings,
arrays, objects, booleans and null retain their JSON representation; construction
does not interpret JSON as application instructions or convert it into CLR
objects through reflection.

Equality and hashing use ordinary object identity. Reusing one instance in
multiple property bags preserves that identity. Constructing two instances,
even from the same input element or identical text, creates distinct values.
There is no automatic structural comparison or content-based interning. This is
intentional for opaque metadata that participates in collision coalescing.

The Tiled and LDtk importers use this type for JSON fields retained verbatim in
`Properties`, including nested provenance dictionaries. Mapped primitive fields,
`Color`, `DrawPoint`, and the typed IntGrid collection keep their existing types.
Code that previously cast a retained JSON field to `JsonElement` must instead
cast to `SceneJsonValue2D` and read its `Value` property. Do not compare the
exposed `JsonElement` values to implement scene metadata equality.

The value owns managed JSON storage, not a file or graphical resource. It has no
disposal method. Keeping the wrapper or its `Value` alive keeps that storage
alive; a property bag does not make an independently retained value unloadable.
This type does not itself perform streaming or disk I/O.

## Constructors

| Name | Description |
| --- | --- |
| `SceneJsonValue2D(JsonElement value)` | Creates a new identity and detached content. Undefined input throws `ArgumentException`; input from a disposed document throws `ObjectDisposedException`. |

## Properties

| Name | Description |
| --- | --- |
| `JsonElement Value` | Read-only access to the detached JSON content. |

## See also

- [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md)
- [TiledScene2DImporter](Cerneala.Scene2D.Importers.TiledScene2DImporter.md)
- [LdtkScene2DImporter](Cerneala.Scene2D.Importers.LdtkScene2DImporter.md)
