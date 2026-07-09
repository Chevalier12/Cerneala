# UiProperty Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiProperty.cs`

Represents the untyped descriptor for a registered Cerneala UI property.

```csharp
public abstract class UiProperty
```

Inheritance:
`object` -> `UiProperty`

Derived:
`UiProperty<T>`

## Examples

```csharp
using Cerneala.UI.Core;

UiProperty<double> opacityProperty = UiProperty<double>.Register(
    "Opacity",
    typeof(MyElement),
    new UiPropertyMetadata<double>(1.0, UiPropertyOptions.AffectsRender));

string label = Describe(opacityProperty);

static string Describe(UiProperty property)
{
    string access = property.IsReadOnly ? "read-only" : "writable";
    return $"{property.DiagnosticName} stores {property.ValueType.Name} values and is {access}.";
}

sealed class MyElement : UiObject;
```

## Remarks

`UiProperty` is the non-generic base type used when the property system needs to work with property descriptors without knowing their value type at compile time. A registered property exposes its stable `Id`, simple `Name`, owner type, value type, option flags, and diagnostic name.

Concrete UI properties are created by `UiProperty<T>`. The base constructor is internal, so application code consumes `UiProperty` instances returned by registration APIs instead of deriving from or constructing this type directly.

`DiagnosticName` is built from the owner type full name and property name. `ToString()` returns the same diagnostic name. `IsReadOnly` is true when `Options` contains `UiPropertyOptions.ReadOnly`.

## Constructors

| Name | Description |
| --- | --- |
| None | `UiProperty` has no public constructors. Use `UiProperty<T>.Register` or `UiProperty<T>.RegisterReadOnly` to create concrete property descriptors. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DiagnosticName` | `string` | Gets the fully qualified diagnostic name composed from `OwnerType.FullName` and `Name`. |
| `Id` | `long` | Gets the registry-assigned identifier for the property. |
| `IsReadOnly` | `bool` | Gets whether `Options` includes `UiPropertyOptions.ReadOnly`. |
| `Name` | `string` | Gets the simple registered property name. |
| `Options` | `UiPropertyOptions` | Gets the option flags associated with the property. |
| `OwnerType` | `Type` | Gets the type that owns the property registration. |
| `ValueType` | `Type` | Gets the CLR type accepted by the property. |

## Methods

| Name | Description |
| --- | --- |
| `ToString()` | Returns `DiagnosticName`. |

## Applies to

Cerneala retained UI property system.

## See also

- `Cerneala.UI.Core.UiProperty<T>`
- `Cerneala.UI.Core.UiPropertyMetadata<T>`
- `Cerneala.UI.Core.UiPropertyOptions`
- `Cerneala.UI.Core.UiPropertyRegistry`
- `Cerneala.UI.Core.UiObject`
