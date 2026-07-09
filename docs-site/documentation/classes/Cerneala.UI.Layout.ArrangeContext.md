# ArrangeContext Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/ArrangeContext.cs`

Represents the final rectangle and layout rounding policy used during the arrange pass.

```csharp
public readonly record struct ArrangeContext(LayoutRect FinalRect, LayoutRounding Rounding)
```

Inheritance:
`Object` -> `ValueType` -> `ArrangeContext`

Implements:
`IEquatable<ArrangeContext>`

## Examples

Arrange an element into a 100 by 40 layout rectangle with the default disabled rounding policy.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIElement element = new UIElement();

LayoutRect arranged = element.Arrange(
    new ArrangeContext(new LayoutRect(0, 0, 100, 40)));
```

Propagate an existing rounding policy when arranging a child from an `ArrangeCore` override.

```csharp
protected override LayoutRect ArrangeCore(ArrangeContext context)
{
    LayoutRect childRect = new LayoutRect(
        context.FinalRect.X,
        context.FinalRect.Y,
        context.FinalRect.Width,
        context.FinalRect.Height);

    child.Arrange(new ArrangeContext(childRect, context.Rounding));
    return context.FinalRect;
}
```

## Remarks

`ArrangeContext` is passed to `UIElement.Arrange` and `UIElement.ArrangeCore` to describe the bounds available for final placement. `FinalRect` contains the rectangle for the arrange pass. `Rounding` carries the `LayoutRounding` policy used to round arranged layout values.

The `ArrangeContext(LayoutRect finalRect)` constructor uses `LayoutRounding.Disabled`. Use the primary constructor when the current rounding policy must be preserved across nested arrange calls.

`ArrangeContext` is a `readonly record struct`, so its data is value-based and immutable after construction. The compiler provides record-struct equality and deconstruction members based on `FinalRect` and `Rounding`; this type does not add custom equality behavior.

## Constructors

| Name | Description |
| --- | --- |
| `ArrangeContext(LayoutRect finalRect)` | Initializes an arrange context with `FinalRect` set to `finalRect` and `Rounding` set to `LayoutRounding.Disabled`. |
| `ArrangeContext(LayoutRect FinalRect, LayoutRounding Rounding)` | Initializes an arrange context with an explicit final rectangle and rounding policy. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FinalRect` | `LayoutRect` | Gets the rectangle used for final layout placement. |
| `Rounding` | `LayoutRounding` | Gets the layout rounding policy for the arrange pass. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out LayoutRect FinalRect, out LayoutRounding Rounding)` | Deconstructs the context into its `FinalRect` and `Rounding` components. |
| `Equals(ArrangeContext other)` | Determines whether another `ArrangeContext` has the same component values. |
| `GetHashCode()` | Returns a hash code based on `FinalRect` and `Rounding`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala UI layout arrange pipeline.

## See also

- `Cerneala.UI.Layout.LayoutRect`
- `Cerneala.UI.Layout.LayoutRounding`
- `Cerneala.UI.Elements.UIElement.Arrange(ArrangeContext)`
- `Cerneala.UI.Elements.UIElement.ArrangeCore(ArrangeContext)`
