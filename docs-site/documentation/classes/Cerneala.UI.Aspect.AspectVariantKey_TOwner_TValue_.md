# AspectVariantKey<TOwner, TValue> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectVariantKey{TOwner,TValue}.cs`

Provides a strongly typed aspect variant identifier for values owned by a specific control type.

```csharp
public sealed class AspectVariantKey<TOwner, TValue> : AspectVariantKey
```

Inheritance:
`Object` -> `AspectVariantKey` -> `AspectVariantKey<TOwner, TValue>`

## Examples

Define and apply a button variant key:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectVariantKey<Button, ButtonKind> kind =
    AspectVariantKey.For<Button, ButtonKind>("kind");

Button button = new();
button.SetAspectVariant(kind, ButtonKind.Primary);

AspectVariantSet variants = AspectVariantSet.Empty.Set(kind, ButtonKind.Danger);
if (variants.TryGet(kind, out ButtonKind value))
{
    Console.WriteLine(value);
}
```

Use a variant key in an aspect condition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition primaryButton =
    AspectCondition.Variant(ButtonVariants.Kind, ButtonKind.Primary);
```

## Remarks

`AspectVariantKey<TOwner, TValue>` carries compile-time owner and value type information for an aspect variant. Its internal constructor passes `typeof(TOwner)` to `OwnerType` and `typeof(TValue)` to `ValueType` on the non-generic `AspectVariantKey` base class.

The class is sealed and has no public constructor. Create instances through `AspectVariantKey.For<TOwner, TValue>(string)`. The `name` passed to `For` is stored in `Name` and must not be null, empty, or whitespace.

Typed keys are used by `AspectVariantSet`, `Control.SetAspectVariant<TControl, TValue>(AspectVariantKey<TControl, TValue>, TValue)`, and `AspectCondition.Variant<TControl, TValue>(AspectVariantKey<TControl, TValue>, TValue)`. `AspectVariantSet.Set(AspectVariantKey, object?)` validates non-null values against the key's `ValueType`; the generic `Set` and `TryGet` overloads keep normal use strongly typed.

Equality, hash code, and string formatting are inherited from `AspectVariantKey`. Two keys are equal when their `Name`, `OwnerType`, and `ValueType` are equal. `ToString()` returns a diagnostic label in the form `OwnerType.Name.Name`, such as `Button.kind`.

## Constructors

| Name | Description |
| --- | --- |
| None public | Create instances with `AspectVariantKey.For<TOwner, TValue>(string)`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Inherited. Gets the variant name. |
| `OwnerType` | `Type` | Inherited. Gets `typeof(TOwner)`. |
| `ValueType` | `Type` | Inherited. Gets `typeof(TValue)`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(AspectVariantKey? other)` | `bool` | Inherited. Returns `true` when `other` has the same name, owner type, and value type. |
| `Equals(object? obj)` | `bool` | Inherited. Returns `true` when `obj` is an equal `AspectVariantKey`. |
| `GetHashCode()` | `int` | Inherited. Returns a hash code based on `Name`, `OwnerType`, and `ValueType`. |
| `ToString()` | `string` | Inherited. Returns a diagnostic label in the form `OwnerType.Name.Name`. |

## Applies to

Cerneala UI aspect variant declarations, component template selection, and aspect condition matching.

## See also

- `Cerneala.UI.Aspect.AspectVariantKey`
- `Cerneala.UI.Aspect.AspectVariantSet`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Controls.Control`
- `Cerneala.UI.Controls.ButtonVariants`
