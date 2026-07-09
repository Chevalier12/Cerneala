# ContentPropertyAttribute Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/ContentPropertyAttribute.cs`

Declares the name of the property that represents a type's markup content.

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class ContentPropertyAttribute : Attribute
```

Inheritance:
`Object` -> `Attribute` -> `ContentPropertyAttribute`

Attributes:
`AttributeUsageAttribute` with `AttributeTargets.Class` and `Inherited = true`.

## Examples

```csharp
using System.Reflection;
using Cerneala.UI.Markup;

[ContentProperty(nameof(Body))]
public sealed class MarkdownBlock
{
    public string? Body { get; set; }
}

ContentPropertyAttribute? attribute =
    typeof(MarkdownBlock).GetCustomAttribute<ContentPropertyAttribute>();

Console.WriteLine(attribute?.PropertyName); // Body
```

## Remarks

`ContentPropertyAttribute` is class-level metadata. It records a property name that tooling or markup infrastructure can treat as the target for implicit content.

The attribute stores the supplied property name exactly as provided. It validates only that the constructor argument is not `null`, empty, or whitespace; it does not trim the value, look up the property, verify the property's type, or register the property with the markup factory.

The attribute can be inherited by derived classes. Because `AllowMultiple` is not set on `AttributeUsageAttribute`, a class can have only one direct `ContentPropertyAttribute` declaration.

In the current markup runtime, implicit text content is resolved from `UiMarkupElementRegistration.ContentPropertyName`. This attribute does not by itself change how `UiFactory` applies text content.

## Constructors

| Name | Description |
| --- | --- |
| `ContentPropertyAttribute(string propertyName)` | Initializes the attribute with the name of the property that should receive markup content. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PropertyName` | `string` | Gets the configured content property name. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ContentPropertyAttribute(string propertyName)` | `ArgumentException` | `propertyName` is `null`, empty, or whitespace. |

## Applies to

`Cerneala` markup metadata for class types.

## See also

- `UiMarkupElementRegistration`
- `UiFactory`
- `UiMarkupSchema`
