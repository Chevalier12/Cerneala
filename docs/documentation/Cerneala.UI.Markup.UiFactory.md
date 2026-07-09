# UiFactory Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/UiFactory.cs`

Creates a retained `UIElement` tree from a parsed `UiMarkupDocument` by using registered markup element and property factories.

```csharp
public sealed class UiFactory
```

Inheritance:
`object` -> `UiFactory`

## Examples

Create a UI tree from markup by parsing the markup, creating the default registry, and passing the parsed document to `UiFactory`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

UiMarkupReader reader = new();
MarkupResult<UiMarkupDocument> readResult = reader.Read("""
<StackPanel>
  <TextBlock>Hello from markup</TextBlock>
</StackPanel>
""");

if (!readResult.HasErrors && readResult.Value is not null)
{
    UiFactory factory = new(UiMarkupSchema.CreateDefault());
    MarkupResult<UIElement> createResult = factory.Create(readResult.Value);

    if (!createResult.HasErrors)
    {
        UIElement root = createResult.Value!;
    }
}
```

Use recovery mode when the caller wants diagnostics and any partial tree that can still be created.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

UiMarkupDocument document = UiMarkupDocument.FromRoot(
    new UiMarkupNode(
        "StackPanel",
        children:
        [
            new UiMarkupNode("TextBlock", text: "Created"),
            new UiMarkupNode("MissingElement")
        ]));

UiFactory factory = new(UiMarkupSchema.CreateDefault());
MarkupResult<UIElement> result = factory.Create(document, MarkupLoadOptions.Recover);

IReadOnlyList<MarkupDiagnostic> diagnostics = result.Diagnostics;
UIElement? root = result.Value;
```

## Remarks

`UiFactory` is the runtime object materializer for parsed markup. It does not parse XML text itself; use `UiMarkupReader` to create a `UiMarkupDocument`, then pass that document to `Create(UiMarkupDocument, MarkupLoadOptions?)`.

The factory depends on a `UiMarkupTypeRegistry`. For each markup node, it looks up the element registration by node name, invokes the registration factory, applies attributes through registered property setters, applies text content through the registered content property, and adds child elements through the registered child action.

`Create` starts with diagnostics already attached to the document and appends creation diagnostics. When `options` is `null`, `MarkupLoadOptions.Strict` is used. In strict mode, any error diagnostic causes the returned `MarkupResult<UIElement>` to have a `null` `Value`. In recovery mode, the method continues collecting diagnostics and may return the created root, including a partial tree when some child nodes could not be created or attached.

The method reports markup creation failures as diagnostics rather than throwing for unknown elements, unknown properties, unsupported text content, unsupported children, failed element construction, failed child attachment, or invalid property values. It still throws `ArgumentNullException` when the `document` argument is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `UiFactory(UiMarkupTypeRegistry registry)` | Initializes a markup factory with the element and property registry used during creation. Throws `ArgumentNullException` when `registry` is `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create(UiMarkupDocument document, MarkupLoadOptions? options = null)` | `MarkupResult<UIElement>` | Creates a root `UIElement` from the document root, preserving document diagnostics and adding creation diagnostics. Uses `MarkupLoadOptions.Strict` when `options` is `null`. |

## Diagnostics

| Code | Condition |
| --- | --- |
| `MARKUP020` | The document has no root node. |
| `MARKUP021` | A markup node name is not registered as an element. |
| `MARKUP022` | A registered element factory throws while creating the element. |
| `MARKUP023` | A node attribute does not match a registered property. |
| `MARKUP024` | A node has text content, but the element has no registered content property. |
| `MARKUP025` | A node has child elements, but the element registration has no child add action. |
| `MARKUP026` | The registered child add action throws. |
| `MARKUP027` | A registered property setter throws `FormatException`, `ArgumentException`, or `InvalidOperationException`. |

## Applies to

`Cerneala` projects targeting `net8.0`.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Markup.UiMarkupReader`
- `Cerneala.UI.Markup.UiMarkupDocument`
- `Cerneala.UI.Markup.UiMarkupTypeRegistry`
- `Cerneala.UI.Markup.MarkupLoadOptions`
- `Cerneala.UI.Markup.MarkupResult<T>`
