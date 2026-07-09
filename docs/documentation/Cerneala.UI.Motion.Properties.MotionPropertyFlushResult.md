# MotionPropertyFlushResult Struct

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyStore.cs`

Represents the counters produced when `MotionPropertyStore` flushes staged animation-source property writes.

```csharp
public readonly record struct MotionPropertyFlushResult(
    int PropertyWrites,
    int RenderInvalidations,
    int LayoutInvalidations)
```

Inheritance:
`Object` -> `ValueType` -> `MotionPropertyFlushResult`

Implements:
`IEquatable<MotionPropertyFlushResult>`

## Examples

Use the result returned by a motion tick after property writes have been flushed into frame counters:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

UIRoot root = new();

MotionFrameResult result = root.Motion.Tick(MotionFrameReason.Manual);

int propertyWrites = result.MotionPropertyWrites;
int renderInvalidations = result.MotionRenderInvalidations;
int layoutInvalidations = result.MotionLayoutInvalidations;
```

Create a result value when aggregating a known set of flush counters:

```csharp
using Cerneala.UI.Motion.Properties;

MotionPropertyFlushResult result = new(
    PropertyWrites: 2,
    RenderInvalidations: 1,
    LayoutInvalidations: 1);
```

## Remarks

`MotionPropertyFlushResult` is returned by the internal `MotionPropertyStore.Flush` operation. The store stages animation property sets and clears, applies them as `UiPropertyValueSource.Animation`, and returns this value with counts for effective property changes and the invalidation categories attached to those changes.

`PropertyWrites` is incremented only after a staged write or clear changes the target property's effective value. If a staged animation value is equal to the current animation value, or if applying or clearing a value leaves the effective value unchanged, the flush does not count it as a property write.

`RenderInvalidations` and `LayoutInvalidations` count changed writes whose `MotionPropertyInvalidationCategory` includes the corresponding flag. A single changed property write can contribute to both counters when the category contains both render and layout invalidation flags.

`MotionSystem.Tick` flushes `MotionPropertyStore` after sampling the motion graph, then adds these counters to the returned `MotionFrameResult`. When there are no staged writes, `MotionPropertyStore.Flush` returns the default value, with all counters set to `0`.

Because `MotionPropertyFlushResult` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as deconstruction, equality, hashing, and string formatting based on the primary-constructor components.

## Constructors

| Name | Description |
| --- | --- |
| `MotionPropertyFlushResult(int PropertyWrites, int RenderInvalidations, int LayoutInvalidations)` | Initializes the flush result with property write, render invalidation, and layout invalidation counts. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PropertyWrites` | `int` | Gets the number of staged animation property writes or clears that changed an effective property value. |
| `RenderInvalidations` | `int` | Gets the number of changed writes whose invalidation category includes render invalidation. |
| `LayoutInvalidations` | `int` | Gets the number of changed writes whose invalidation category includes layout invalidation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out int PropertyWrites, out int RenderInvalidations, out int LayoutInvalidations)` | `void` | Deconstructs the result into its primary-constructor counters. |
| `Equals(MotionPropertyFlushResult other)` | `bool` | Determines whether another result has the same counter values. |
| `GetHashCode()` | `int` | Returns a hash code based on the result counters. |
| `ToString()` | `string` | Returns the compiler-generated record string representation. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationCategory`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionFrameResult`
- `Cerneala.UI.Core.UiPropertyValueSource`
