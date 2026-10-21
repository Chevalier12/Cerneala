using System.Collections.ObjectModel;
using Cerneala.UI.Controls;

namespace Cerneala.UI.Documents;

// The inlines of a TextBlock or of a Span. An inline has at most one parent, and a change of the collection tells the
// TextBlock that owns the tree, so it measures and draws again and its Text follows the content.
public sealed class InlineCollection : Collection<Inline>
{
    private readonly TextBlock? ownerTextBlock;
    private readonly Span? ownerSpan;

    internal InlineCollection(TextBlock owner)
    {
        ownerTextBlock = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal InlineCollection(Span owner)
    {
        ownerSpan = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    protected override void InsertItem(int index, Inline item)
    {
        ValidateItem(item);
        base.InsertItem(index, item);
        Attach(item);
        NotifyChanged();
    }

    protected override void SetItem(int index, Inline item)
    {
        ValidateItem(item);
        Inline replaced = this[index];
        base.SetItem(index, item);
        Detach(replaced);
        Attach(item);
        NotifyChanged();
    }

    protected override void RemoveItem(int index)
    {
        Inline removed = this[index];
        base.RemoveItem(index);
        Detach(removed);
        NotifyChanged();
    }

    protected override void ClearItems()
    {
        Inline[] removed = [.. this];
        base.ClearItems();
        foreach (Inline inline in removed)
        {
            Detach(inline);
        }

        if (removed.Length > 0)
        {
            NotifyChanged();
        }
    }

    private void ValidateItem(Inline item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.HasParent)
        {
            throw new InvalidOperationException("An inline that already has a parent cannot be added to an inline collection.");
        }

        // A Span cannot become part of its own subtree.
        for (Span? span = ownerSpan; span is not null; span = span.ParentSpan)
        {
            if (ReferenceEquals(span, item))
            {
                throw new InvalidOperationException("An inline cannot be added to its own inline collection or to one under it.");
            }
        }
    }

    private void Attach(Inline item)
    {
        item.ParentSpan = ownerSpan;
        item.ParentTextBlock = ownerTextBlock;
    }

    private static void Detach(Inline item)
    {
        item.ParentSpan = null;
        item.ParentTextBlock = null;
    }

    private void NotifyChanged()
    {
        TextBlock? block = ownerTextBlock ?? ownerSpan?.OwnerTextBlock;
        block?.OnInlinesChanged();
    }
}
