# Image.ReferenceImageComparer Class

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: [`UI/Controls/Image.cs`](../../UI/Controls/Image.cs)

Compares `IDrawImage` values by object identity for `Image.Source` change detection.

```csharp
private sealed class ReferenceImageComparer : IEqualityComparer<IDrawImage?>
```

Inheritance:
`object` -> `Image.ReferenceImageComparer`

Declaring type:
`Image`

Implements:
`IEqualityComparer<IDrawImage?>`

## Examples
`ReferenceImageComparer` is private to `Image`, so callers do not instantiate it directly. It is applied by `Image.SourceProperty` metadata. Replacing `Source` with a different image object is treated as a change even when the image type overrides value equality.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;

Image image = new()
{
    Source = new EqualImage(32, 16)
};

MeasureContext context = new(new LayoutSize(100, 100));
image.Measure(context);

image.Source = new EqualImage(64, 8);

LayoutSize desired = image.Measure(context);
// desired is 64x8 because the replacement source is a different object reference.

private sealed class EqualImage(int width, int height) : IDrawImage
{
    public int Width { get; } = width;

    public int Height { get; } = height;

    public override bool Equals(object? obj) => obj is EqualImage;

    public override int GetHashCode() => 0;
}
```

## Remarks
`ReferenceImageComparer` is an implementation detail nested inside `Image`. The comparer exists so `Image.SourceProperty` uses reference equality instead of `IDrawImage.Equals` when deciding whether a source assignment changed the effective value.

This matters for layout and rendering invalidation. `Image.SourceProperty` is registered with `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender`, and `UiObject` uses the property's metadata comparer before raising a property changed notification. With this comparer, two distinct `IDrawImage` instances are considered different even if their own `Equals` implementations report equality.

`GetHashCode` mirrors the equality semantics: `null` returns `0`; non-null images use `RuntimeHelpers.GetHashCode`, which is based on object identity rather than an overridden `GetHashCode`.

## Fields
| Name | Description |
| --- | --- |
| `Instance` | Singleton comparer instance used by `Image.SourceProperty` metadata. |

## Methods
| Name | Description |
| --- | --- |
| `Equals(IDrawImage? x, IDrawImage? y)` | Returns `true` only when `x` and `y` are the same object reference, including both values being `null`. |
| `GetHashCode(IDrawImage? obj)` | Returns `0` for `null`; otherwise returns the runtime identity hash code for `obj`. |

## Applies to
Cerneala retained UI image control internals.

## See also
- [`Image`](../../UI/Controls/Image.cs)
- [`UiPropertyMetadata<T>`](../../UI/Core/UiPropertyMetadata%7BT%7D.cs)
- [`UiObject`](../../UI/Core/UiObject.cs)
