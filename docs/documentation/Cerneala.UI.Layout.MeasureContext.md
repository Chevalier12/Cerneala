# MeasureContext Struct

## Definition
Namespace: `Cerneala.UI.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Layout/MeasureContext.cs`

Represents the available size and layout rounding policy used during the measure pass.

```csharp
public readonly record struct MeasureContext(LayoutSize AvailableSize, LayoutRounding Rounding)
```

Inheritance:
`Object` -> `ValueType` -> `MeasureContext`

Implements:
`IEquatable<MeasureContext>`

## Examples

Measure an element with a 200 by 100 available layout size and the default disabled rounding policy.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

UIElement element = new UIElement();

LayoutSize desired = element.Measure(
    new MeasureContext(new LayoutSize(200, 100)));
```

Propagate an existing rounding policy when measuring a child from a `MeasureCore` override.

```csharp
protected override LayoutSize MeasureCore(MeasureContext context)
{
    LayoutSize childAvailable = new LayoutSize(
        context.AvailableSize.Width,
        context.AvailableSize.Height);

    return child.Measure(new MeasureContext(childAvailable, context.Rounding));
}
```

## Remarks

`MeasureContext` is passed to `UIElement.Measure` and `UIElement.MeasureCore` to describe the constraints for desired-size calculation. `AvailableSize` contains the size available to the element during measurement. `Rounding` carries the `LayoutRounding` policy used by the layout pipeline to round measured layout values.

`UIElement.Measure` skips recomputation when the last measured available size and layout version still match. When measurement runs, `UIElement` deflates `AvailableSize` by margin before calling `MeasureCore`, clamps the returned content size to non-negative values, adds margin back, and rounds the final desired size with `context.Rounding`.

The `MeasureContext(LayoutSize availableSize)` constructor uses `LayoutRounding.Disabled`. Use the primary constructor when the current rounding policy must be preserved across nested measure calls.

`MeasureContext` is a `readonly record struct`, so its data is value-based and immutable after construction. The compiler provides record-struct equality and deconstruction members based on `AvailableSize` and `Rounding`; this type does not add custom equality behavior.

## Constructors

| Name | Description |
| --- | --- |
| `MeasureContext(LayoutSize availableSize)` | Initializes a measure context with `AvailableSize` set to `availableSize` and `Rounding` set to `LayoutRounding.Disabled`. |
| `MeasureContext(LayoutSize AvailableSize, LayoutRounding Rounding)` | Initializes a measure context with an explicit available size and rounding policy. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `AvailableSize` | `LayoutSize` | Gets the size available for desired-size measurement. |
| `Rounding` | `LayoutRounding` | Gets the layout rounding policy for the measure pass. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out LayoutSize AvailableSize, out LayoutRounding Rounding)` | Deconstructs the context into its `AvailableSize` and `Rounding` components. |
| `Equals(MeasureContext other)` | Determines whether another `MeasureContext` has the same component values. |
| `GetHashCode()` | Returns a hash code based on `AvailableSize` and `Rounding`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala UI layout measure pipeline.

## See also

- `Cerneala.UI.Layout.LayoutSize`
- `Cerneala.UI.Layout.LayoutRounding`
- `Cerneala.UI.Elements.UIElement.Measure(MeasureContext)`
- `Cerneala.UI.Elements.UIElement.MeasureCore(MeasureContext)`
