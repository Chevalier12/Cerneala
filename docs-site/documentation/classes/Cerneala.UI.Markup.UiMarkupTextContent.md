# UiMarkupTextContent Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Represents a non-empty text segment inside a markup node's ordered content.

```csharp
public sealed class UiMarkupTextContent : UiMarkupContent
```

Inheritance:
`object` -> `UiMarkupContent` -> `UiMarkupTextContent`

## Examples

Create text content and attach it to a markup node:

```csharp
using Cerneala.UI.Markup;

UiMarkupTextContent textContent = new("Hello");

UiMarkupNode node = new(
    "TextBlock",
    attributes: null,
    content: new UiMarkupContent[] { textContent });

Console.WriteLine(node.Text); // Hello
```

Read markup that contains text content:

```csharp
using Cerneala.UI.Markup;

UiMarkupReader reader = new();
MarkupResult<UiMarkupDocument> result = reader.Read("<TextBlock>Hello</TextBlock>");

UiMarkupNode? root = result.Value?.Root;
Console.WriteLine(root?.Text); // Hello
```

## Remarks

`UiMarkupTextContent` is one of the concrete `UiMarkupContent` types stored in `UiMarkupNode.Content`. It preserves the string passed to its constructor and exposes it through `Text`.

The constructor rejects `null` and empty strings by throwing `ArgumentException`. It does not reject whitespace-only strings when constructed directly. `UiMarkupReader` is stricter for XML input: it skips whitespace-only text nodes and trims text before creating `UiMarkupTextContent`.

When a `UiMarkupNode` is built from ordered content, its `Text` property is the concatenation of all contained `UiMarkupTextContent.Text` values. `UiMarkupWriter` writes each instance back as XML text. `UiFactory` applies the combined node text to the registered content property; if the element has text but no text content property, it reports markup diagnostic `MARKUP024`.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupTextContent(string text)` | Initializes a text content segment. Throws `ArgumentException` when `text` is `null` or empty. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the text value for this content segment. |

## Applies to

- `Cerneala.UI.Markup.UiMarkupTextContent`

## See also

- `UiMarkupContent`
- `UiMarkupNode`
- `UiMarkupChildContent`
- `UiMarkupReader`
- `UiMarkupWriter`
- `UiFactory`
