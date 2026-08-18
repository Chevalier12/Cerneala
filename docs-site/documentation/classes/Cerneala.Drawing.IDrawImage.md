# IDrawImage Interface

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/IDrawImage.cs`

Describes the pixel dimensions exposed by a drawing image payload.

```csharp
public interface IDrawImage
```

## Examples

```csharp
using Cerneala.Drawing;

sealed class DemoImage : IDrawImage
{
    public int Width => 128;
    public int Height => 64;
}

IDrawImage image = new DemoImage();
DrawCommand command = DrawCommand.DrawImage(
    image,
    new DrawRect(0, 0, image.Width, image.Height),
    Color.White);
```

## Remarks

`IDrawImage` is the backend-neutral image payload used by `DrawCommand.DrawImage` and image brush descriptors. The interface exposes dimensions so consumers can inspect the source image without depending on a backend-specific image type.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Width` | `int` | Gets the image width in pixels. |
| `Height` | `int` | Gets the image height in pixels. |

## Applies to

Cerneala drawing commands, image brushes, and backend integrations.

## See also

- `Cerneala.Drawing.DrawCommand`
- `Cerneala.Drawing.ImageDrawBrushDescriptor`
