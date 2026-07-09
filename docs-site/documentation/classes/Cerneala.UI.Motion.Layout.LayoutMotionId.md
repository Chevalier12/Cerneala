# LayoutMotionId Struct

## Definition
Namespace: `Cerneala.UI.Motion.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Layout/LayoutMotionId.cs`

Identifies an element across layout snapshots so layout motion can preserve visual continuity.

```csharp
public readonly record struct LayoutMotionId(string Value)
```

Inheritance:
`Object` -> `ValueType` -> `LayoutMotionId`

Implements:
`IEquatable<LayoutMotionId>`

## Examples
Assign a stable layout motion id together with layout motion options:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Layout;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

Border card = new()
{
    LayoutMotionId = "card",
    LayoutMotion = LayoutMotionOptions.Spring(
        MotionFactory.Tween<Transform>(TimeSpan.FromMilliseconds(180)))
};
```

## Remarks
`LayoutMotionId` is the value used by `UIElement.LayoutMotionId`. Layout motion participates only when an attached element has both `UIElement.LayoutMotionId` and `UIElement.LayoutMotion` set.

The layout-motion coordinator records first and last layout snapshots for participating elements. The id is stored in those snapshots and is also used to keep previous snapshots by id, allowing the same element instance to retain visual continuity when it moves between visual parents.

`UIElement.LayoutMotionIdProperty` accepts `null`, but rejects non-null ids whose `Value` is `null`, empty, or whitespace. The struct itself does not validate `Value`; validation happens when assigning the id to `UIElement.LayoutMotionId`.

Implicit conversions let callers assign string literals or interpolate ids directly to `UIElement.LayoutMotionId`, and read a `LayoutMotionId` back as its underlying string value.

Because `LayoutMotionId` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as equality, deconstruction, hashing, and string formatting based on `Value`.

## Constructors
| Name | Description |
| --- | --- |
| `LayoutMotionId(string Value)` | Initializes a layout motion id with the supplied string value. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Value` | `string` | Gets the string identifier used for layout snapshot matching. |

## Operators
| Name | Return Type | Description |
| --- | --- | --- |
| `implicit operator LayoutMotionId(string value)` | `LayoutMotionId` | Creates a layout motion id from a string value. |
| `implicit operator string(LayoutMotionId id)` | `string` | Returns the underlying `Value` from a layout motion id. |

## Methods
| Name | Description |
| --- | --- |
| `Deconstruct(out string Value)` | Deconstructs the id into its `Value` component. |
| `Equals(LayoutMotionId other)` | Determines whether another `LayoutMotionId` has the same value. |
| `GetHashCode()` | Returns a hash code based on `Value`. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to
Cerneala retained UI layout motion in the `Cerneala` project.

Target framework: `net8.0`

## See also
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Motion.Layout.LayoutMotionCoordinator`
- `Cerneala.UI.Motion.Layout.LayoutMotionOptions`
- `Cerneala.UI.Motion.Layout.LayoutSnapshot`
