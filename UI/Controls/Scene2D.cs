using System.Collections.ObjectModel;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

[ContentProperty(nameof(Children))]
public sealed class Scene2D : SceneNode2D
{
    public Scene2D()
    {
        Children = new ChildCollection(this);
    }

    public Collection<SceneNode2D> Children { get; }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        base.AttachSurface(surface);
        foreach (SceneNode2D child in Children)
        {
            child.AttachSurface(surface);
        }
    }

    internal override void Record(RenderSurface2DFrame frame)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this))
        {
            return;
        }

        foreach (SceneNode2D child in Children)
        {
            child.Record(frame);
        }
    }

    private sealed class ChildCollection(Scene2D owner) : Collection<SceneNode2D>
    {
        protected override void InsertItem(int index, SceneNode2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            owner.LogicalChildren.Insert(index, item);
            base.InsertItem(index, item);
            item.AttachSurface(owner.Surface);
            owner.Surface?.InvalidateFrame();
        }

        protected override void SetItem(int index, SceneNode2D item)
        {
            ArgumentNullException.ThrowIfNull(item);
            SceneNode2D previous = this[index];
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            owner.LogicalChildren.Insert(index, item);
            base.SetItem(index, item);
            item.AttachSurface(owner.Surface);
            owner.Surface?.InvalidateFrame();
        }

        protected override void RemoveItem(int index)
        {
            SceneNode2D previous = this[index];
            previous.AttachSurface(null);
            owner.LogicalChildren.Remove(previous);
            base.RemoveItem(index);
            owner.Surface?.InvalidateFrame();
        }

        protected override void ClearItems()
        {
            foreach (SceneNode2D child in this)
            {
                child.AttachSurface(null);
                owner.LogicalChildren.Remove(child);
            }

            base.ClearItems();
            owner.Surface?.InvalidateFrame();
        }
    }
}
