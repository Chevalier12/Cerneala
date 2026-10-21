# Inline Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/Inline.cs`

Defines the abstract base of the elements that can be put in the `Inlines` of a `TextBlock` or of a `Span`, and adds text decorations to the text properties of `TextElement`.

```csharp
public abstract class Inline : TextElement
```

Inheritance:
`object` -> `UiObject` -> `TextElement` -> `Inline`

Derived:
`Run`, `Span` (-> `Bold`, `Italic`), `LineBreak`

## Examples

Underline one run and strike through a part of it:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;
using Cerneala.UI.Text;

TextBlock text = new();
Span marked = new() { TextDecorations = TextDecorations.Underline };
marked.Inlines.Add(new Run("kept "));
marked.Inlines.Add(new Run("removed") { TextDecorations = TextDecorations.Strikethrough });
text.Inlines.Add(marked);

// "kept "   : underline
// "removed" : underline and strikethrough
```

## Remarks

An `Inline` has at most one parent: the `Span` or the `TextBlock` whose `Inlines` collection holds it. Adding an inline that already has a parent to another collection throws `InvalidOperationException`; remove it first.

Decorations add up down the tree. The decorations of an inline are combined with the ones it inherits from its `Span` and from `TextBlock.TextDecorations`, and an explicit `TextDecorations.None` does not remove an inherited decoration. A change of `TextDecorations` invalidates render of the owning `TextBlock` and not its measure.

A value with a bit outside the four defined flags throws `ArgumentException`.

`Inline` has no public members to create the content. Use the derived types: `Run` for text, `Span`, `Bold` and `Italic` for groups, and `LineBreak` for a line break.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextDecorationsProperty` | `UiProperty<TextDecorations>` | Identifies `TextDecorations`. Defaults to `TextDecorations.None`; affects render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TextDecorations` | `TextDecorations` | Gets or sets the decorations drawn over the text of this inline and of the inlines under it. Added to the inherited ones. |

## Relevant Inherited Properties

| Name | Declared On | Description |
| --- | --- | --- |
| `FontFamily`, `FontSize`, `FontWeight`, `FontStyle`, `FontStretch`, `Foreground` | `TextElement` | Text style properties. A property that the inline does not set is taken from its parent. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Documents.TextElement`
- `Cerneala.UI.Documents.Run`
- `Cerneala.UI.Documents.Span`
- `Cerneala.UI.Documents.LineBreak`
- `Cerneala.UI.Documents.InlineCollection`
- `Cerneala.UI.Text.TextDecorations`
