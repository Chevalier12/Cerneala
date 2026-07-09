# UiMarkupChildContent Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Represents child-node content inside a markup node's ordered content list.

```csharp
public sealed class UiMarkupChildContent : UiMarkupContent
```

Inheritance:
`Object` -> `UiMarkupContent` -> `UiMarkupChildContent`

## Examples

Wrap a child node as ordered markup content.

```csharp
using Cerneala.UI.Markup;

UiMarkupNode child = new("TextBlock", text: "Hello");
UiMarkupChildContent content = new(child);

UiMarkupNode parent = new(
    "StackPanel",
    attributes: null,
    content: new UiMarkupContent[] { content });
```

## Remarks

`UiMarkupChildContent` is one of the concrete `UiMarkupContent` types used by `UiMarkupNode.Content`.

`UiMarkupNode.Children` is built from `UiMarkupChildContent` entries in the ordered content list. The constructor throws when `node` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupChildContent(UiMarkupNode)` | Initializes child content for a markup node. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Node` | `UiMarkupNode` | Gets the child markup node represented by this content item. |

## Applies to

- `Cerneala.UI.Markup.UiMarkupChildContent`

## See also

- `Cerneala.UI.Markup.UiMarkupContent`
- `Cerneala.UI.Markup.UiMarkupTextContent`
- `Cerneala.UI.Markup.UiMarkupNode`
