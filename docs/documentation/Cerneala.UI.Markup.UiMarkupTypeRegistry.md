# UiMarkupTypeRegistry Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupTypeRegistry.cs`

Stores markup element registrations by name so markup loading can resolve element names to factories and property metadata.

```csharp
public sealed class UiMarkupTypeRegistry
```

Inheritance:
`object` -> `UiMarkupTypeRegistry`

## Examples
The following example creates a small registry, registers a `TextBlock` element, and resolves it by its markup name.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

UiMarkupTypeRegistry registry = new UiMarkupTypeRegistry()
    .Register(new UiMarkupElementRegistration(
            "TextBlock",
            () => new TextBlock(),
            contentPropertyName: nameof(TextBlock.Text))
        .RegisterProperty(new UiMarkupPropertyRegistration(
            nameof(TextBlock.Text),
            (element, value) => ((TextBlock)element).Text = value)));

if (registry.TryGetElement("TextBlock", out UiMarkupElementRegistration registration))
{
    UIElement element = registration.Factory();
}
```

## Remarks
`UiMarkupTypeRegistry` is the top-level lookup table used by `UiFactory` when converting a parsed `UiMarkupDocument` into a retained UI element tree. `UiMarkupSchema.CreateDefault` builds a registry with the built-in markup names used by the default schema.

Element names are compared with `StringComparer.Ordinal`, so lookup is exact and case-sensitive. Calling `Register` with an element name that already exists throws `InvalidOperationException`. Calling `Register` with `null` throws `ArgumentNullException`.

The registry stores `UiMarkupElementRegistration` instances. Each element registration supplies the element factory, optional child-adder, optional content property name, and its own property registrations.

## Constructors
| Name | Description |
| --- | --- |
| `UiMarkupTypeRegistry()` | Initializes an empty markup type registry. |

## Methods
| Name | Description |
| --- | --- |
| `Register(UiMarkupElementRegistration registration)` | Adds an element registration and returns the same registry instance for fluent chaining. Throws if `registration` is `null` or its name is already registered. |
| `TryGetElement(string name, out UiMarkupElementRegistration registration)` | Attempts to retrieve the element registration for `name`. Returns `true` when the name is registered; otherwise returns `false`. |

## Applies to
Project: `Cerneala`

## See also
- [UiFactory](Cerneala.UI.Markup.UiFactory.md)
- [UiMarkupPropertyRegistration](Cerneala.UI.Markup.UiMarkupPropertyRegistration.md)
- `UiMarkupSchema`
