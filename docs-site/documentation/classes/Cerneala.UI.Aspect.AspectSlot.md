# AspectSlot Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectSlot.cs`

Represents a typed key for naming an aspect slot on an owner element and identifying the element type hosted in that slot.

```csharp
public abstract class AspectSlot : IEquatable<AspectSlot>
```

Inheritance:
`Object` -> `AspectSlot`

Derived:
`AspectSlot<TOwner, TTarget>`

Implements:
`IEquatable<AspectSlot>`

## Examples

Define and register a slot in a component template:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

AspectSlot<Button, Border> rootSlot = AspectSlot.For<Button, Border>("Root");

Button button = new();
Border border = new();

ComponentTemplate<Button> template = new("modern", context =>
{
    context.RegisterSlot(rootSlot, border);
    return border;
});

ComponentTemplateInstance instance = template.CreateInstance(
    button,
    new ComponentTemplateContext(button, new AspectEnvironment("template")));

Border registeredRoot = (Border)instance.Slots[rootSlot];
```

Create a root slot for a control type:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectSlot<Button, Button> buttonRoot = AspectSlot.Root<Button>();

Console.WriteLine(buttonRoot.Name);       // Root
Console.WriteLine(buttonRoot.OwnerType);  // Cerneala.UI.Controls.Button
Console.WriteLine(buttonRoot.TargetType); // Cerneala.UI.Controls.Button
```

## Remarks

`AspectSlot` is the non-generic base type used where the aspect system needs to store or compare slots without knowing their compile-time owner and target types. The generic `AspectSlot<TOwner, TTarget>` carries those types into `OwnerType` and `TargetType`.

Slots are value-like identifiers. Two slots are equal when their `Name`, `OwnerType`, and `TargetType` are equal. Name comparison uses ordinal string comparison. `GetHashCode()` combines the same values, so `AspectSlot` can be used as a dictionary key, as `TemplateSlotMap` does.

Use `For<TOwner, TTarget>(string)` to create a named slot for a part inside an owner component. Use `Root<TOwner>()` for the conventional `"Root"` slot whose owner and target type are both `TOwner`.

The constructor is not public. Custom code creates slots through the static factory methods, while the sealed generic derived type is constructed internally.

`ToString()` returns the owner type name and slot name in the form `OwnerType.Name.Name`, for example `Button.Content`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the slot name. The constructor rejects null, empty, and whitespace-only names. |
| `OwnerType` | `Type` | Gets the element type that owns the slot. |
| `TargetType` | `Type` | Gets the element type expected for the content registered in the slot. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `For<TOwner, TTarget>(string name)` | `AspectSlot<TOwner, TTarget>` | Creates a named slot for owner type `TOwner` and target type `TTarget`. |
| `Root<TOwner>()` | `AspectSlot<TOwner, TOwner>` | Creates the conventional `"Root"` slot for `TOwner`. |
| `Equals(AspectSlot? other)` | `bool` | Returns `true` when `other` has the same name, owner type, and target type. |
| `Equals(object? obj)` | `bool` | Returns `true` when `obj` is an equal `AspectSlot`. |
| `GetHashCode()` | `int` | Returns a hash code based on `Name`, `OwnerType`, and `TargetType`. |
| `ToString()` | `string` | Returns a diagnostic label in the form `OwnerType.Name.Name`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `For<TOwner, TTarget>(string)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |

## Applies to

Cerneala UI aspect slots, component templates, template slot maps, and aspect target matching.

## See also

- `Cerneala.UI.Aspect.AspectSlot<TOwner, TTarget>`
- `Cerneala.UI.Aspect.AspectSlotPath`
- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Controls.Templates.ComponentTemplateContext`
- `Cerneala.UI.Controls.Templates.TemplateSlotMap`
