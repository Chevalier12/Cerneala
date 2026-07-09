# UiMarkupPropertyRegistration Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupTypeRegistry.cs`

Registers how a markup attribute value is applied to a UI element property.

```csharp
public sealed class UiMarkupPropertyRegistration
```

Inheritance:
`Object` -> `UiMarkupPropertyRegistration`

## Examples

Register a property setter on an element registration.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

UiMarkupElementRegistration button = new("Button", () => new UIElement());

button.RegisterProperty(new UiMarkupPropertyRegistration(
    "Name",
    (element, value) => element.Name = value));
```

## Remarks

`UiMarkupPropertyRegistration` stores a property name and a setter delegate. Markup loading code can use the registration to apply string attribute values to created `UIElement` instances.

The constructor rejects null, empty, or whitespace-only property names. It also throws when the setter delegate is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupPropertyRegistration(string, Action<UIElement, string>)` | Initializes a property registration with a markup property name and setter delegate. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the markup property name. |
| `SetValue` | `Action<UIElement, string>` | Gets the delegate that applies a string value to a UI element. |

## Applies to

- `Cerneala.UI.Markup.UiMarkupPropertyRegistration`

## See also

- `Cerneala.UI.Markup.UiMarkupElementRegistration`
- `Cerneala.UI.Markup.UiMarkupTypeRegistry`
