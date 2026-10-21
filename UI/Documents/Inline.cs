using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Text;

namespace Cerneala.UI.Documents;

public abstract class Inline : TextElement
{
    public static readonly UiProperty<TextDecorations> TextDecorationsProperty = UiProperty<TextDecorations>.Register(
        nameof(TextDecorations),
        typeof(Inline),
        new UiPropertyMetadata<TextDecorations>(
            TextDecorations.None,
            UiPropertyOptions.AffectsRender,
            validateValue: TextDecorationsValidation.IsDefined));

    public TextDecorations TextDecorations
    {
        get => GetValue(TextDecorationsProperty);
        set => SetValue(TextDecorationsProperty, value);
    }

    // The Span or TextBlock whose Inlines collection holds this inline; at most one of them is set.
    internal Span? ParentSpan { get; set; }

    internal TextBlock? ParentTextBlock { get; set; }

    internal bool HasParent => ParentSpan is not null || ParentTextBlock is not null;

    internal override TextBlock? OwnerTextBlock
    {
        get
        {
            Inline current = this;
            while (current.ParentSpan is { } span)
            {
                current = span;
            }

            return current.ParentTextBlock;
        }
    }

    // As in WPF, a decoration of a parent draws over all of its content, so the decorations of an inline add to the ones
    // that it inherits; an explicit None does not remove the decorations of the parent.
    internal override InlineStyle Resolve(InlineStyle inherited)
    {
        InlineStyle style = base.Resolve(inherited);
        return style with { Decorations = inherited.Decorations | TextDecorations };
    }

    // Appends the text of this inline and of the inlines under it to the builder: the text of a Run, "\n" for a LineBreak.
    internal abstract void AppendText(System.Text.StringBuilder builder);

    // Adds the text pieces of this inline and of the inlines under it, in order, each with its effective style.
    internal abstract void CollectPieces(InlineStyle inherited, List<(string Text, InlineStyle Style)> pieces);
}
