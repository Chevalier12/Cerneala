using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Motion.Layout;

public readonly record struct LayoutSnapshot(
    UIElement Element,
    LayoutRect Bounds,
    UIElement? Parent,
    LayoutMotionId? Id);
