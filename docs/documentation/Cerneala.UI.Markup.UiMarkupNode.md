# UiMarkupNode Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Represents one parsed or programmatically-created UI markup element, including its name, attributes, ordered content, child nodes, text, and optional source location.

```csharp
public sealed class UiMarkupNode
```

Inheritance:
`object` -> `UiMarkupNode`

## Examples

Create a simple node tree and wrap it in a markup document:

```csharp
using Cerneala.UI.Markup;

UiMarkupNode root = new(
    "StackPanel",
    [new UiMarkupAttribute("Name", "Root")],
    [
        new UiMarkupNode("TextBlock", [new UiMarkupAttribute("Text", "Hello")]),
        new UiMarkupNode("Button", text: "Click")
    ]);

UiMarkupDocument document = UiMarkupDocument.FromRoot(root);
```

Create a node with explicit ordered content when text and child elements must keep their relative order:

```csharp
using Cerneala.UI.Markup;

UiMarkupNode root = new(
    "StackPanel",
    attributes: null,
    content:
    [
        new UiMarkupChildContent(new UiMarkupNode("TextBlock")),
        new UiMarkupTextContent("direct"),
        new UiMarkupChildContent(new UiMarkupNode("Button"))
    ]);
```

## Remarks

`UiMarkupNode` is the element model used by `UiMarkupDocument`. `UiMarkupReader` creates nodes from XML elements, and `UiMarkupWriter` serializes nodes back to XML by walking each node's `Content` list.

`Name` must contain non-whitespace text. Both public constructors throw `ArgumentException` when `name` is `null`, empty, or only whitespace.

The node stores immutable read-only snapshots of the supplied attributes and content. The constructor copies the supplied sequences into `ReadOnlyCollection<T>` instances, so later changes to the original collections are not reflected by the node.

`Content` is the ordered representation of mixed node content. `Children` is a derived view containing only `UiMarkupChildContent.Node` entries in content order. `Text` is a derived value made by concatenating every `UiMarkupTextContent.Text` entry in `Content`; it is `null` when there is no text content.

The constructor that accepts `children` and `text` is a convenience overload. When `text` is not `null` or empty, it creates one leading `UiMarkupTextContent`, then appends each child as `UiMarkupChildContent`. Use the `content` constructor when text and child nodes need to be interleaved in a specific order.

`Line` and `Column` are optional source-location values. `UiMarkupReader` sets them from XML line information when available; programmatic nodes can leave them as `null`.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupNode(string name, IEnumerable<UiMarkupAttribute>? attributes = null, IEnumerable<UiMarkupNode>? children = null, string? text = null, int? line = null, int? column = null)` | Initializes a node from attributes, child nodes, optional text, and optional source location. Non-empty `text` becomes leading text content, followed by each child. |
| `UiMarkupNode(string name, IEnumerable<UiMarkupAttribute>? attributes, IEnumerable<UiMarkupContent> content, int? line = null, int? column = null)` | Initializes a node from attributes, ordered mixed content, and optional source location. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the markup element name. |
| `Attributes` | `IReadOnlyList<UiMarkupAttribute>` | Gets the copied read-only list of markup attributes. |
| `Content` | `IReadOnlyList<UiMarkupContent>` | Gets the copied read-only ordered content list, including text and child-node content items. |
| `Children` | `IReadOnlyList<UiMarkupNode>` | Gets the child nodes extracted from `Content` in content order. |
| `Text` | `string?` | Gets the concatenated text content, or `null` when the node has no text content. |
| `Line` | `int?` | Gets the optional one-based source line for the node. |
| `Column` | `int?` | Gets the optional one-based source column for the node. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `UiMarkupNode(...)` | `ArgumentException` | `name` is `null`, empty, or only whitespace. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Markup/UiMarkupDocument.cs`
- `UI/Markup/UiMarkupReader.cs`
- `UI/Markup/UiMarkupWriter.cs`
