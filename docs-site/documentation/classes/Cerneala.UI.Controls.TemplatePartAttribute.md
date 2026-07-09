# TemplatePartAttribute Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TemplatePartAttribute.cs`

Declares template-part metadata for a control type.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class TemplatePartAttribute : Attribute
```

Inheritance:
`Object` -> `Attribute` -> `TemplatePartAttribute`

Attributes:
`AttributeUsageAttribute` with `AttributeTargets.Class`, `AllowMultiple = true`, and `Inherited = true`.

## Examples

```csharp
using Cerneala.UI.Controls;

[TemplatePart("PART_Content", typeof(ContentPresenter))]
public sealed class CardControl : Control
{
}

IReadOnlyList<TemplatePartAttribute> parts =
    TemplatePartAttribute.GetParts(typeof(CardControl));

TemplatePartAttribute contentPart = parts[0];
Console.WriteLine($"{contentPart.Name}: {contentPart.Type.Name}");
```

## Remarks

`TemplatePartAttribute` is metadata attached to control classes. It records the expected part name and the expected element type for a control template part.

The attribute is diagnostic metadata only. The class stores and retrieves metadata; it does not validate an applied `ControlTemplate`, create template elements, or enforce that a part exists at runtime.

Multiple template parts can be declared on the same control type. Because the attribute is inherited and `GetParts` reads with inheritance enabled, metadata declared on base control classes is available when querying derived control types.

The constructor requires a non-empty, non-whitespace part name and a non-null part type.

## Constructors

| Name | Description |
| --- | --- |
| `TemplatePartAttribute(string name, Type type)` | Initializes a template-part metadata attribute with the part name and expected part type. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the template part name. |
| `Type` | `Type` | Gets the expected type of the template part. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetParts(Type controlType)` | `IReadOnlyList<TemplatePartAttribute>` | Returns the `TemplatePartAttribute` metadata declared on the specified control type, including inherited attributes. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `TemplatePartAttribute(string name, Type type)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `TemplatePartAttribute(string name, Type type)` | `ArgumentNullException` | `type` is `null`. |
| `GetParts(Type controlType)` | `ArgumentNullException` | `controlType` is `null`. |

## Applies to

`Cerneala` UI controls and control template metadata.

## See also

- `Control`
- `ControlTemplate`
- `ContentPresenter`
