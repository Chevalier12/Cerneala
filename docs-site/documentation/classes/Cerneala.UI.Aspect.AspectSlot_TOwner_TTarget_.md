# AspectSlot<TOwner, TTarget> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectSlot{TOwner,TTarget}.cs`

Provides the strongly typed aspect slot identifier returned by `AspectSlot.For<TOwner, TTarget>(string)` and `AspectSlot.Root<TOwner>()`.

```csharp
public sealed class AspectSlot<TOwner, TTarget> : AspectSlot
```

Inheritance:
`Object` -> `AspectSlot` -> `AspectSlot<TOwner, TTarget>`

## Examples

Define slots for the parts of a `Button` template:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectSlot<Button, Border> rootSlot = AspectSlot.For<Button, Border>("Root");
AspectSlot<Button, ContentPresenter> contentSlot =
    AspectSlot.For<Button, ContentPresenter>("Content");

Console.WriteLine(rootSlot.Name);        // Root
Console.WriteLine(rootSlot.OwnerType);   // Cerneala.UI.Controls.Button
Console.WriteLine(rootSlot.TargetType);  // Cerneala.UI.Controls.Border
Console.WriteLine(contentSlot);          // Button.Content
```

Create the conventional root slot for a type:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectSlot<Button, Button> buttonRoot = AspectSlot.Root<Button>();
```

## Remarks

`AspectSlot<TOwner, TTarget>` adds compile-time owner and target type information to the non-generic `AspectSlot` identifier. The base constructor stores `typeof(TOwner)` in `OwnerType` and `typeof(TTarget)` in `TargetType`.

The class is sealed and has no public constructor. Create instances through `AspectSlot.For<TOwner, TTarget>(string)` for named slots, or `AspectSlot.Root<TOwner>()` for the `"Root"` slot where owner and target are the same type.

Equality and hash code behavior are inherited from `AspectSlot`: two slots match when their `Name`, `OwnerType`, and `TargetType` are equal. This is the comparison used by aspect targets and slot paths.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the slot name inherited from `AspectSlot`. |
| `OwnerType` | `Type` | Gets `typeof(TOwner)`. |
| `TargetType` | `Type` | Gets `typeof(TTarget)`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(AspectSlot? other)` | `bool` | Inherited. Returns `true` when `other` has the same name, owner type, and target type. |
| `Equals(object? obj)` | `bool` | Inherited. Returns `true` when `obj` is an equal `AspectSlot`. |
| `GetHashCode()` | `int` | Inherited. Returns a hash code based on `Name`, `OwnerType`, and `TargetType`. |
| `ToString()` | `string` | Inherited. Returns a diagnostic label in the form `OwnerType.Name.Name`, such as `Button.Content`. |

## Applies to

Cerneala UI aspect slot declarations, component template slots, and aspect target matching.

## See also

- `Cerneala.UI.Aspect.AspectSlot`
- `Cerneala.UI.Aspect.AspectSlotPath`
- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Controls.Buttons.ButtonSlots`
