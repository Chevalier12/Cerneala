# MotionPropertyKey Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyKey.cs`

Identifies one animated UI property on one target object for motion property storage.

```csharp
public sealed class MotionPropertyKey : IEquatable<MotionPropertyKey>
```

Inheritance:
`object` -> `MotionPropertyKey`

Implements:
`IEquatable<MotionPropertyKey>`

## Examples

Create keys for a control property and compare their identity:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Motion.Properties;

Control control = new();

MotionPropertyKey first = new(control, Control.BackgroundProperty);
MotionPropertyKey second = new(control, Control.BackgroundProperty);
MotionPropertyKey differentTarget = new(new Control(), Control.BackgroundProperty);

Console.WriteLine(first.Equals(second));          // True
Console.WriteLine(first.Equals(differentTarget)); // False
```

## Remarks

`MotionPropertyKey` is used by `MotionPropertyStore` as the dictionary key for pending animation writes and cached property bindings. It combines the target `UiObject` and the untyped `UiProperty` descriptor so the motion system can keep one entry per target/property pair.

Equality uses reference identity for both `Target` and `Property`. Two keys are equal only when they point to the same target object instance and the same property descriptor instance. `GetHashCode` follows the same identity-based rule by hashing the runtime identity of both components.

The constructor rejects `null` target and property arguments. The class is immutable after construction.

## Constructors

| Name | Description |
| --- | --- |
| `MotionPropertyKey(UiObject target, UiProperty property)` | Initializes a key for `property` on `target`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Target` | `UiObject` | Gets the object whose property is identified by the key. |
| `Property` | `UiProperty` | Gets the untyped UI property identified by the key. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(MotionPropertyKey? other)` | `bool` | Returns `true` when `other` has the same target object reference and property descriptor reference. |
| `Equals(object? obj)` | `bool` | Returns `true` when `obj` is an equivalent `MotionPropertyKey`. |
| `GetHashCode()` | `int` | Returns an identity-based hash code for the target/property pair. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionPropertyKey(...)` | `ArgumentNullException` | `target` or `property` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding`
- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty`
