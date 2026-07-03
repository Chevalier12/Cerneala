using System.Runtime.CompilerServices;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Rendering;

public readonly record struct ClipNode(LayoutRect Bounds)
{
    private static readonly ConditionalWeakTable<UIElement, ClipBox> clips = new();

    public static void SetClip(UIElement element, LayoutRect bounds)
    {
        ArgumentNullException.ThrowIfNull(element);
        clips.GetOrCreateValue(element).Clip = new ClipNode(bounds);
    }

    public static void ClearClip(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        clips.Remove(element);
    }

    public static bool TryGetClip(UIElement element, out ClipNode clip)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (clips.TryGetValue(element, out ClipBox? box) && box.Clip.HasValue)
        {
            clip = box.Clip.Value;
            return true;
        }

        clip = default;
        return false;
    }

    private sealed class ClipBox
    {
        public ClipNode? Clip { get; set; }
    }
}
