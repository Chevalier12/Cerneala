# UiMarkupElementRegistration Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: [`UI/Markup/UiMarkupTypeRegistry.cs`](../../UI/Markup/UiMarkupTypeRegistry.cs)

Describes how a markup element name maps to a `UIElement` factory, optional child/content behavior, and the set of markup properties that can be applied to created elements.

```csharp
public sealed class UiMarkupElementRegistration
```

Inheritance:
`object` -> `UiMarkupElementRegistration`

## Examples
The following example registers a markup element that creates a `TextBlock` and accepts text content through the `Text` property.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Markup;

UiMarkupElementRegistration registration =
    new UiMarkupElementRegistration(
            "TextBlock",
            () => new TextBlock(),
            contentPropertyName: nameof(TextBlock.Text))
        .RegisterProperty(new UiMarkupPropertyRegistration(
            nameof(TextBlock.Text),
            (element, value) => ((TextBlock)element).Text = value));

if (registration.TryGetProperty(nameof(TextBlock.Text), out UiMarkupPropertyRegistration property))
{
    TextBlock textBlock = (TextBlock)registration.Factory();
    property.SetValue(textBlock, "Hello from markup");
}
```

## Remarks
`UiMarkupElementRegistration` is consumed by `UiMarkupTypeRegistry` and `UiFactory`. The registry stores element registrations by `Name`; `UiFactory` looks up a markup node name, calls `Factory` to create the `UIElement`, applies registered properties, then uses `AddChild` and `ContentPropertyName` to process child elements or text content.

Property registration names are stored with ordinal string comparison. Registering the same property name twice throws an `InvalidOperationException`, which keeps the schema deterministic instead of silently replacing mappings. A `ContentPropertyName` only enables text content when a property with the same name has also been registered.

## Constructors
| Name | Description |
| --- | --- |
| `UiMarkupElementRegistration(string name, Func<UIElement> factory, Action<UIElement, UIElement>? addChild = null, string? contentPropertyName = null)` | Initializes a registration with an element name, element factory, optional child-adder callback, and optional content property name. Throws `ArgumentException` when `name` is null, empty, or whitespace, and `ArgumentNullException` when `factory` is null. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the markup element name used for lookup in `UiMarkupTypeRegistry`. |
| `Factory` | `Func<UIElement>` | Gets the callback used by `UiFactory` to create the registered element instance. |
| `AddChild` | `Action<UIElement, UIElement>?` | Gets the optional callback used by `UiFactory` to attach a child element to a created parent element. |
| `ContentPropertyName` | `string?` | Gets the optional property name used by `UiFactory` when a markup node supplies text content. |
| `Properties` | `IReadOnlyDictionary<string, UiMarkupPropertyRegistration>` | Gets the registered property mappings keyed by property name. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `RegisterProperty(UiMarkupPropertyRegistration property)` | `UiMarkupElementRegistration` | Adds a property registration and returns the same element registration for chaining. Throws `ArgumentNullException` when `property` is null and `InvalidOperationException` when the property name is already registered. |
| `TryGetProperty(string name, out UiMarkupPropertyRegistration property)` | `bool` | Looks up a registered property by name and returns `true` when it exists. |

## Applies To
Markup element schema registration in the `Cerneala` project.

## See Also
- [`UiMarkupTypeRegistry`](../../UI/Markup/UiMarkupTypeRegistry.cs)
- [`UiMarkupPropertyRegistration`](../../UI/Markup/UiMarkupTypeRegistry.cs)
- [`UiFactory`](../../UI/Markup/UiFactory.cs)
- [`UiMarkupSchema`](../../UI/Markup/UiMarkupSchema.cs)
