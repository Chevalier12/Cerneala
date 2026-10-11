# TemplatePartAttribute Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplatePartAttribute.cs`

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

Register a part for a control type, then read it back:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

TemplatePartAttribute.Register<CardControl>("PART_Content", typeof(ContentPresenter));

IReadOnlyList<TemplatePartAttribute> parts =
    TemplatePartAttribute.GetParts(typeof(CardControl));

TemplatePartAttribute contentPart = parts[0];
Console.WriteLine($"{contentPart.Name}: {contentPart.Type.Name}"); // PART_Content: ContentPresenter

public sealed class CardControl : Control
{
}
```

## Remarks

`TemplatePartAttribute` records the expected part name and the expected element type for a control template part. It can be applied to a control class as an attribute, and it is also the entry type of a process-wide registry.

`GetParts` reads only that registry. It does not read attributes applied to the class. For example, `[TemplatePart("PART_Content", typeof(ContentPresenter))]` on `CardControl` with no `Register` call makes `GetParts(typeof(CardControl))` return an empty list. Code that wants the applied attributes reads them with reflection, for example `typeof(CardControl).GetCustomAttributes(typeof(TemplatePartAttribute), inherit: true)`.

`Register` adds an entry for a control type. Registering the same part name again for the same control type replaces the earlier entry. `GetParts` walks from the requested type through its base types and returns the entries registered for each type, starting with the requested type, so parts registered for a base control are included when querying a derived control.

The metadata is diagnostic only. The class does not validate an applied `ComponentTemplate`, create template elements, or enforce that a part exists at runtime.

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
| `Register<TControl>(string name, Type partType)` | `void` | Registers a part for `TControl`. Same as `Register(typeof(TControl), name, partType)`. |
| `Register(Type controlType, string name, Type partType)` | `void` | Registers a part for `controlType`, replacing an earlier entry with the same name. |
| `GetParts(Type controlType)` | `IReadOnlyList<TemplatePartAttribute>` | Returns the parts registered with `Register` for `controlType` and its base types. Attributes applied to the class are not read. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `TemplatePartAttribute(string name, Type type)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `TemplatePartAttribute(string name, Type type)` | `ArgumentNullException` | `type` is `null`. |
| `Register(Type controlType, string name, Type partType)` | `ArgumentNullException` | `controlType` or `partType` is `null`. |
| `Register(Type controlType, string name, Type partType)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `GetParts(Type controlType)` | `ArgumentNullException` | `controlType` is `null`. |

## Applies to

`Cerneala` UI controls and control template metadata.

## See also

- `Control`
- `ComponentTemplate`
- `ContentPresenter`
