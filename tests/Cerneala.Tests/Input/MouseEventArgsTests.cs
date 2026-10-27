using System.Numerics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Input;

public sealed class MouseEventArgsTests
{
    [Fact]
    public void GetPositionConvertsThroughAFiniteAncestorTransform()
    {
        (UIElement parent, UIElement child) = CreateArrangedParentAndChild();
        parent.ScaleX = 2;
        MouseEventArgs args = new(InputEvents.MouseDownEvent, child, 10, 10);

        // The parent pivot is its center (100, 80): root x 10 maps to 100 + (10 - 100) / 2 = 55.
        Assert.Equal(new Vector2(55, 10), args.GetPosition(child));
    }

    [Fact]
    public void GetPositionReportsAnOverflowingAncestorTransformAsInvalidOperation()
    {
        // The pivot translation -100 * float.MaxValue overflows float.
        (UIElement parent, UIElement child) = CreateArrangedParentAndChild();
        parent.ScaleX = float.MaxValue;
        MouseEventArgs args = new(InputEvents.MouseDownEvent, child, 10, 10);

        Exception? failure = Record.Exception(() => args.GetPosition(child));

        Assert.True(
            failure is InvalidOperationException,
            "GetPosition " + (failure is null ? "returned" : "threw " + failure.GetType().Name + ": " + failure.Message));
    }

    private static (UIElement Parent, UIElement Child) CreateArrangedParentAndChild()
    {
        UIRoot root = new(200, 160);
        UIElement parent = new();
        UIElement child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        parent.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 160)));
        child.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 80)));
        return (parent, child);
    }
}
