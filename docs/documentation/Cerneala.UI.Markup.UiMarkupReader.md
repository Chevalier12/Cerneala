# UiMarkupReader Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupReader.cs`

Reads XML-like UI markup text into a `UiMarkupDocument` and reports markup diagnostics instead of throwing for supported parse failures.

```csharp
public sealed class UiMarkupReader
```

Inheritance:
`object` -> `UiMarkupReader`

## Examples
```csharp
using Cerneala.UI.Markup;

UiMarkupReader reader = new();
MarkupResult<UiMarkupDocument> result = reader.Read(
    "<StackPanel Name=\"Root\"><TextBlock Text=\"A\" />direct<Button>Click</Button></StackPanel>");

if (!result.HasErrors && result.Value?.Root is UiMarkupNode root)
{
    string rootName = root.Name;                 // "StackPanel"
    string firstAttribute = root.Attributes[0].Name; // "Name"
    string? directText = root.Text;              // "direct"
    IReadOnlyList<UiMarkupNode> children = root.Children;
}
```

Use `MarkupLoadOptions.Recover` when malformed XML should still produce an empty `UiMarkupDocument` with diagnostics.

```csharp
MarkupResult<UiMarkupDocument> result = new UiMarkupReader()
    .Read("<StackPanel>", MarkupLoadOptions.Recover);

bool hasParseError = result.HasErrors;
UiMarkupDocument? recoveredDocument = result.Value;
```

## Remarks
`UiMarkupReader` parses the supplied markup with `XDocument.Parse` using line information and preserved whitespace. It converts each element to a `UiMarkupNode`, each non-namespace attribute to a `UiMarkupAttribute`, child elements to `UiMarkupChildContent`, and non-whitespace text nodes to trimmed `UiMarkupTextContent`.

Element and attribute names are stored with `XName.LocalName`, so namespace prefixes are not retained in the produced markup model. Namespace declaration attributes are skipped.

Whitespace-only text nodes are ignored. Non-whitespace text is trimmed before it is added to node content. The resulting `UiMarkupNode.Content` keeps the read order of text and child element content, while `UiMarkupNode.Children` exposes only child element nodes and `UiMarkupNode.Text` combines text content.

When the input is null, empty, or whitespace-only, `Read` returns a `MARKUP001` error diagnostic and a `UiMarkupDocument` whose `Root` is `null`. When XML parsing fails, `Read` returns a `MARKUP002` error diagnostic with the `XmlException` line and column when available. In strict mode the returned `Value` is `null` for malformed XML; with `ContinueOnError` enabled, the returned document has no root and contains the diagnostics.

## Constructors
| Name | Description |
| --- | --- |
| `UiMarkupReader()` | Initializes a new reader instance. |

## Methods
| Name | Description |
| --- | --- |
| `Read(string markup, MarkupLoadOptions? options = null)` | Parses markup text and returns a `MarkupResult<UiMarkupDocument>` containing the document, diagnostics, and error state. |

## Diagnostics
| Code | Condition | Result |
| --- | --- | --- |
| `MARKUP001` | `markup` is null, empty, whitespace-only, or parses without a root element. | Returns an error diagnostic and a document with `Root` set to `null`. |
| `MARKUP002` | `XDocument.Parse` throws an `XmlException`. | Returns an error diagnostic. The result value is `null` unless `options.ContinueOnError` is `true`. |

## Applies to
`Cerneala.UI.Markup` in the `Cerneala` project.

## See also
- `UiMarkupDocument`
- `UiMarkupNode`
- `MarkupLoadOptions`
- `MarkupDiagnostic`
- `UiFactory`
