# UiMarkupDocument Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiMarkupDocument.cs`

Represents a parsed UI markup document with an optional root node and diagnostics collected while reading or creating markup.

```csharp
public sealed class UiMarkupDocument
```

Inheritance:
`Object` -> `UiMarkupDocument`

## Examples

Create a document from a root markup node and serialize it through `UiMarkupWriter`.

```csharp
using Cerneala.UI.Markup;

UiMarkupDocument document = UiMarkupDocument.FromRoot(new UiMarkupNode(
    "StackPanel",
    [new UiMarkupAttribute("Name", "Root")],
    [
        new UiMarkupNode("TextBlock", [new UiMarkupAttribute("Text", "Hello")]),
        new UiMarkupNode("Button", text: "Click")
    ]));

MarkupResult<string> written = new UiMarkupWriter().Write(document);
```

Inspect diagnostics on a document returned by `UiMarkupReader`.

```csharp
using Cerneala.UI.Markup;

MarkupResult<UiMarkupDocument> result = new UiMarkupReader().Read(" ");

if (result.Value?.HasErrors == true)
{
    IReadOnlyList<MarkupDiagnostic> diagnostics = result.Value.Diagnostics;
}
```

## Remarks

`UiMarkupDocument` is the container passed between markup parsing, writing, and factory creation APIs. `UiMarkupReader.Read` returns it inside `MarkupResult<UiMarkupDocument>`, `UiMarkupWriter.Write` serializes it, and `UiFactory.Create` uses it to create a `UIElement`.

`Root` can be `null`. The reader uses a null root for missing or malformed markup cases where a document object is still returned, and writer/factory APIs report diagnostics when asked to process a document without a root.

The constructor copies the supplied diagnostics into a read-only list. Passing `null` for `diagnostics` creates an empty diagnostics list. `HasErrors` returns `true` when any diagnostic has `MarkupDiagnosticSeverity.Error`.

Use `FromRoot` when constructing a valid document from an existing `UiMarkupNode`; it validates that the root argument is not null.

## Constructors

| Name | Description |
| --- | --- |
| `UiMarkupDocument(UiMarkupNode? root, IEnumerable<MarkupDiagnostic>? diagnostics = null)` | Initializes a document with an optional root node and a read-only snapshot of diagnostics. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Diagnostics` | `IReadOnlyList<MarkupDiagnostic>` | Gets the diagnostics associated with the document. |
| `HasErrors` | `bool` | Gets whether `Diagnostics` contains at least one error diagnostic. |
| `Root` | `UiMarkupNode?` | Gets the root markup node, or `null` when the document has no parsed root. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `FromRoot(UiMarkupNode root)` | `UiMarkupDocument` | Creates a document from a non-null root node. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `FromRoot(UiMarkupNode root)` | `ArgumentNullException` | `root` is `null`. |

## Applies to

- `Cerneala.UI.Markup.UiMarkupDocument`

## See also

- `Cerneala.UI.Markup.UiMarkupNode`
- `Cerneala.UI.Markup.UiMarkupReader`
- `Cerneala.UI.Markup.UiMarkupWriter`
- `Cerneala.UI.Markup.UiFactory`
- `Cerneala.UI.Markup.MarkupDiagnostic`
