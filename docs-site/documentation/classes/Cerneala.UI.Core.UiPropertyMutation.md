# UiPropertyMutation Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyMutation.cs`

Captures a low-level UI property source mutation and the effective value state before and after the mutation.

```csharp
public sealed record UiPropertyMutation(
    UiObject Target,
    UiProperty Property,
    UiPropertyValueSource MutatingSource,
    object? OldEffectiveValue,
    UiPropertyValueSource OldEffectiveSource,
    object? NewEffectiveValue,
    UiPropertyValueSource NewEffectiveSource,
    object? OldSourceValue,
    object? NewSourceValue,
    bool WasCoerced);
```

Inheritance:
`Object` -> `UiPropertyMutation`

Implements:
`IEquatable<UiPropertyMutation>`

## Examples

`UiPropertyMutation` records are normally created by `UiObject` and delivered to an internal `UiPropertyMutationObserver`. The following example shows the shape of a mutation record for a local property value change:

```csharp
using Cerneala.UI.Core;

UiPropertyMutation mutation = new(
    element,
    ExampleElement.TitleProperty,
    UiPropertyValueSource.Local,
    "Old title",
    UiPropertyValueSource.Default,
    "New title",
    UiPropertyValueSource.Local,
    null,
    "New title",
    wasCoerced: false);
```

## Remarks

`UiPropertyMutation` is the payload used by `UiPropertyMutationObserver.OnPropertyMutated`. `UiObject` creates one after `SetValue`, `SetValueUntyped`, `ClearValue`, or `ClearValueUntyped` has updated the property store.

Unlike `UiPropertyChangedEventArgs`, a mutation can be reported even when the resolved effective value remains equal. This lets internal systems observe source-level changes such as setting or clearing a lower-priority value that does not currently win property precedence.

`MutatingSource` identifies the source being set or cleared. `OldEffectiveValue` and `NewEffectiveValue` describe the resolved property value before and after the operation, while `OldEffectiveSource` and `NewEffectiveSource` describe which source supplied those effective values. `OldSourceValue` and `NewSourceValue` describe the value at the mutating source itself.

`WasCoerced` is `true` when a set operation supplied a value that was changed by the property's coercion callback before storage. Clear operations report `false`.

Motion transactions consume these records through `MotionTransactionContext`. They ignore animation-sourced mutations, unchanged effective values, detached elements, and properties that are not registered as animatable.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyMutation(UiObject target, UiProperty property, UiPropertyValueSource mutatingSource, object? oldEffectiveValue, UiPropertyValueSource oldEffectiveSource, object? newEffectiveValue, UiPropertyValueSource newEffectiveSource, object? oldSourceValue, object? newSourceValue, bool wasCoerced)` | Initializes an immutable mutation record with the target, property, mutating source, effective values, source values, and coercion flag. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Target` | `UiObject` | Gets the object whose property source was mutated. |
| `Property` | `UiProperty` | Gets the UI property affected by the mutation. |
| `MutatingSource` | `UiPropertyValueSource` | Gets the property value source that was set or cleared. |
| `OldEffectiveValue` | `object?` | Gets the resolved effective value before the mutation. |
| `OldEffectiveSource` | `UiPropertyValueSource` | Gets the source that supplied the effective value before the mutation. |
| `NewEffectiveValue` | `object?` | Gets the resolved effective value after the mutation. |
| `NewEffectiveSource` | `UiPropertyValueSource` | Gets the source that supplies the effective value after the mutation. |
| `OldSourceValue` | `object?` | Gets the previous value stored at `MutatingSource`, or `null` when no value was stored there. |
| `NewSourceValue` | `object?` | Gets the new value stored at `MutatingSource`, or `null` when the source value was cleared. |
| `WasCoerced` | `bool` | Gets whether the supplied value was coerced before storage. |

## Applies To

Cerneala retained UI property system and internal motion transaction observation.

## See Also

- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty`
- `Cerneala.UI.Core.UiPropertyMutationObserver`
- `Cerneala.UI.Core.UiPropertyValueSource`
- `Cerneala.UI.Motion.Transactions.MotionTransactionContext`
