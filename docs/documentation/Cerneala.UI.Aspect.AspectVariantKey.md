# AspectVariantKey Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectVariantKey.cs`

Represents the non-generic base identity for a typed aspect variant key.

```csharp
public abstract class AspectVariantKey : IEquatable<AspectVariantKey>
```

Inheritance:
`object` -> `AspectVariantKey`

Derived:
`AspectVariantKey<TOwner, TValue>`

Implements:
`IEquatable<AspectVariantKey>`

## Examples

Create a typed key, store a variant value, and read it back:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectVariantKey<Button, ButtonKind> key =
    AspectVariantKey.For<Button, ButtonKind>("kind");

AspectVariantSet variants = AspectVariantSet.Empty.Set(key, ButtonKind.Primary);

bool hasKind = variants.TryGet(key, out ButtonKind kind);
```

Use the same key in a variant condition:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectVariantKey<Button, ButtonKind> key =
    AspectVariantKey.For<Button, ButtonKind>("kind");

AspectCondition condition = AspectCondition.Variant(key, ButtonKind.Primary);
AspectMatchContext context = new(
    new Button(),
    variants: AspectVariantSet.Empty.Set(key, ButtonKind.Primary));

bool matches = condition.Evaluate(context).Matches;
```

## Remarks

`AspectVariantKey` identifies a variant by three values: `Name`, `OwnerType`, and `ValueType`. `For<TOwner, TValue>(string)` creates the public typed key form, `AspectVariantKey<TOwner, TValue>`, which records `typeof(TOwner)` and `typeof(TValue)`.

The key is immutable after construction. Equality and hash codes compare the name with ordinal string comparison and require the owner and value types to be the same `Type` instances. Two keys with the same name but different owner or value types are distinct.

Variant keys are used by `AspectVariantSet` to store values and by `AspectCondition.Variant<TControl, TValue>(AspectVariantKey<TControl, TValue>, TValue)` to match rules against an `AspectMatchContext`. The untyped `AspectVariantSet.Set(AspectVariantKey, object?)` overload checks non-null values against `ValueType`; the typed overload relies on the generic `TValue` argument.

`ToString()` returns the owner type name followed by the key name, for example `Button.kind`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectVariantKey(string name, Type ownerType, Type valueType)` | Initializes the base key with a non-empty name, owner type, and value type. This constructor is `private protected`; use `For<TOwner, TValue>(string)` from public code. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the variant name used for identity, diagnostics, and string conversion. |
| `OwnerType` | `Type` | Gets the control or owner type associated with the variant key. |
| `ValueType` | `Type` | Gets the type expected for values stored with this key. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `For<TOwner, TValue>(string name)` | `AspectVariantKey<TOwner, TValue>` | Creates a typed variant key for the supplied owner type, value type, and non-empty name. |
| `Equals(AspectVariantKey? other)` | `bool` | Returns `true` when `other` has the same name, owner type, and value type. |
| `Equals(object? obj)` | `bool` | Returns `true` when `obj` is an `AspectVariantKey` equal to this instance. |
| `GetHashCode()` | `int` | Returns a hash code based on the ordinal name hash, owner type, and value type. |
| `ToString()` | `string` | Returns `OwnerType.Name` and `Name` separated by a period. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `For<TOwner, TValue>(string name)` | `ArgumentException` | `name` is null, empty, or whitespace. |

## Applies to

Cerneala UI aspect variants, aspect conditions, and aspect rule matching.

## See also

- `Cerneala.UI.Aspect.AspectVariantKey<TOwner, TValue>`
- `Cerneala.UI.Aspect.AspectVariantSet`
- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectMatchContext`
