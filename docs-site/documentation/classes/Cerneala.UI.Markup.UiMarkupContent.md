# UiMarkupContent Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Defines the abstract base type for ordered content entries stored by `UiMarkupNode.Content`.

```csharp
public abstract class UiMarkupContent
```

Inheritance:
`Object` -> `UiMarkupContent`

Derived:
`UiMarkupTextContent`, `UiMarkupChildContent`

## Examples

Create a node with mixed ordered content.

```csharp
using Cerneala.UI.Markup;

UiMarkupNode node = new(
    "StackPanel",
    attributes: null,
    content:
    [
        new UiMarkupChildContent(new UiMarkupNode("TextBlock")),
        new UiMarkupTextContent("direct"),
        new UiMarkupChildContent(new UiMarkupNode("Button"))
    ]);

IReadOnlyList<UiMarkupContent> orderedContent = node.Content;
```

## Remarks

`UiMarkupContent` is a marker base class for content items that can appear in a `UiMarkupNode` while preserving source order.

The built-in concrete content types are `UiMarkupTextContent` for non-empty text content and `UiMarkupChildContent` for child element content. `UiMarkupNode.Content` stores both kinds in order, while `UiMarkupNode.Children` is derived from the child-content entries and `UiMarkupNode.Text` is the concatenation of text-content entries.

`UiMarkupReader` creates `UiMarkupContent` instances when it reads XML nodes. It trims non-whitespace text nodes before creating `UiMarkupTextContent` and wraps child elements in `UiMarkupChildContent`. `UiMarkupWriter` serializes the ordered content list by writing text content as `XText` and child content as nested elements.

## Derived Types

| Name | Description |
| --- | --- |
| `UiMarkupTextContent` | Represents a non-empty text item in a markup node's ordered content list. |
| `UiMarkupChildContent` | Represents a child node item in a markup node's ordered content list. |

## Public Members

`UiMarkupContent` declares no public constructors, properties, methods, fields, or events beyond inherited `Object` members.

## Applies to

- `Cerneala.UI.Markup.UiMarkupContent`

## See also

- `Cerneala.UI.Markup.UiMarkupNode`
- `Cerneala.UI.Markup.UiMarkupTextContent`
- `Cerneala.UI.Markup.UiMarkupChildContent`
- `Cerneala.UI.Markup.UiMarkupReader`
- `Cerneala.UI.Markup.UiMarkupWriter`
