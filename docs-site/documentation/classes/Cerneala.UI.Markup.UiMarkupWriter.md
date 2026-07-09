# UiMarkupWriter Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupWriter.cs`

Serializes a `UiMarkupDocument` tree into a compact XML markup string.

```csharp
public sealed class UiMarkupWriter
```

Inheritance:
`object` -> `UiMarkupWriter`

## Examples

Serialize a markup document with attributes, child nodes, and text content:

```csharp
using Cerneala.UI.Markup;

UiMarkupDocument document = UiMarkupDocument.FromRoot(new UiMarkupNode(
    "StackPanel",
    [new UiMarkupAttribute("Name", "Root")],
    [
        new UiMarkupNode("TextBlock", [new UiMarkupAttribute("Text", "Hello")]),
        new UiMarkupNode("Button", text: "Click")
    ]));

UiMarkupWriter writer = new();
MarkupResult<string> result = writer.Write(document);

string? markup = result.Value;
// <StackPanel Name="Root"><TextBlock Text="Hello" /><Button>Click</Button></StackPanel>
```

Handle a document without a root node:

```csharp
UiMarkupWriter writer = new();
MarkupResult<string> result = writer.Write(new UiMarkupDocument(null));

bool failed = result.HasErrors;       // true
string? markup = result.Value;        // null
string code = result.Diagnostics[0].Code; // MARKUP010
```

## Remarks

`UiMarkupWriter` walks the `UiMarkupDocument.Root` node recursively and builds an XML element for each `UiMarkupNode`. Node attributes become XML attributes, `UiMarkupTextContent` becomes XML text, and `UiMarkupChildContent` becomes nested XML elements.

The writer uses XML APIs for serialization, so attribute values and text are escaped according to XML rules. Output is written with formatting disabled, producing a compact single-line string instead of indented markup.

If `document` is `null`, `Write` throws `ArgumentNullException`. If `document.Root` is `null`, `Write` returns a `MarkupResult<string>` with `Value` set to `null` and a `MARKUP010` error diagnostic. Existing diagnostics already stored on the input `UiMarkupDocument` are not copied into the returned result.

Content order is preserved from `UiMarkupNode.Content`, including mixed child and text content.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupWriter()` | Initializes a new `UiMarkupWriter` instance. |

## Methods

| Name | Description |
| --- | --- |
| `Write(UiMarkupDocument document)` | Serializes `document.Root` to a compact XML string and returns it in a `MarkupResult<string>`. Returns a `MARKUP010` error result when the document has no root node. |

## Method Details

### Write(UiMarkupDocument)

```csharp
public MarkupResult<string> Write(UiMarkupDocument document)
```

#### Parameters

| Name | Type | Description |
| --- | --- | --- |
| `document` | `UiMarkupDocument` | The markup document to serialize. |

#### Returns

`MarkupResult<string>`

A result whose `Value` contains the serialized XML markup when the document has a root node. If the document has no root node, `Value` is `null` and `Diagnostics` contains an error diagnostic with code `MARKUP010`.

#### Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `document` is `null`. |

## Applies to

`Cerneala` project, markup document serialization APIs.

## See also

- `UiMarkupDocument`
- `UiMarkupNode`
- `MarkupResult<T>`
- `UiMarkupReader`
