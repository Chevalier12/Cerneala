using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Motion.Properties;

namespace Cerneala.Tests.UI.Layout;

public sealed class CanvasTests
{
    [Fact]
    public void CanvasMeasuresChildrenWithoutChildOffsetForcingDesiredSize()
    {
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        Canvas.SetLeft(child, 50);
        Canvas.SetTop(child, 40);
        canvas.VisualChildren.Add(child);

        LayoutSize desired = canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(LayoutSize.Zero, desired);
        Assert.Equal(new LayoutSize(20, 10), child.DesiredSize);
    }

    [Fact]
    public void CanvasArrangesChildAtCoordinates()
    {
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        Canvas.SetLeft(child, 5);
        Canvas.SetTop(child, 7);
        canvas.VisualChildren.Add(child);
        canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));

        canvas.Arrange(new ArrangeContext(new LayoutRect(10, 20, 100, 100)));

        Assert.Equal(new LayoutRect(15, 27, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void RootFrameKeepsCanvasChildCoordinates()
    {
        UIRoot root = new(100, 100);
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        Canvas.SetLeft(child, 5);
        Canvas.SetTop(child, 7);
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(child);
        root.Invalidate(
            InvalidationFlags.Subtree | InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest,
            "initial canvas frame");

        root.ProcessFrame();

        Assert.Equal(new LayoutRect(5, 7, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void ChangingAttachedChildCoordinatesQueuesCanvasArrange()
    {
        UIRoot root = new(100, 100);
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(child);
        canvas.Measure(new MeasureContext(new LayoutSize(100, 100)));
        canvas.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Canvas.SetLeft(child, 12);
        Canvas.SetTop(child, 8);

        Assert.Contains(canvas, root.LayoutQueue.SnapshotArrange());

        root.ProcessFrame(new FramePhaseProcessors
        {
            Arrange = element =>
            {
                if (ReferenceEquals(element, canvas))
                {
                    canvas.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
                }
            }
        });

        Assert.Equal(new LayoutRect(12, 8, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void CanvasLeftPropertyMovesAnAlreadyArrangedChildOnTheNextFrame()
    {
        (UIRoot root, FixedElement child) = CreateArrangedCanvasChild();

        child.SetValue(Canvas.LeftProperty, 30f);
        root.ProcessFrame();

        Assert.Equal(30f, Canvas.GetLeft(child));
        Assert.Equal(30f, child.GetValue(Canvas.LeftProperty));
        Assert.Equal(new LayoutRect(30, 0, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void CanvasTopPropertyMovesAnAlreadyArrangedChildOnTheNextFrame()
    {
        (UIRoot root, FixedElement child) = CreateArrangedCanvasChild();

        child.SetValue(Canvas.TopProperty, 40f);
        root.ProcessFrame();

        Assert.Equal(40f, Canvas.GetTop(child));
        Assert.Equal(40f, child.GetValue(Canvas.TopProperty));
        Assert.Equal(new LayoutRect(0, 40, 20, 10), child.ArrangedBounds);
    }

    [Fact]
    public void ChangingCanvasLeftQueuesTheCanvasArrangeAndNotTheChildArrange()
    {
        (UIRoot root, FixedElement child) = CreateArrangedCanvasChild();
        UIElement canvas = child.VisualParent!;
        Assert.Empty(root.LayoutQueue.SnapshotArrange());

        Canvas.SetLeft(child, 40);

        IReadOnlyList<UIElement> queued = root.LayoutQueue.SnapshotArrange();
        Assert.Contains(canvas, queued);
        Assert.DoesNotContain(child, queued);
    }

    [Fact]
    public void ParentArrangeOptionQueuesTheVisualParentArrangeOfAnyPanel()
    {
        UIRoot root = new(100, 100);
        Panel stack = new StackPanel();
        FixedElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(stack);
        stack.VisualChildren.Add(child);
        root.Invalidate(
            InvalidationFlags.Subtree | InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest,
            "initial stack frame");
        root.ProcessFrame();
        Assert.Empty(root.LayoutQueue.SnapshotArrange());

        child.SetValue(ParentArrangeProbe.RankProperty, 3);

        IReadOnlyList<UIElement> queued = root.LayoutQueue.SnapshotArrange();
        Assert.Contains(stack, queued);
        Assert.DoesNotContain(child, queued);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MotionClassifiesCanvasPositionAsALayoutProperty(bool left)
    {
        MotionPropertyInvalidationCategory category = MotionPropertyInvalidationClassifier.Classify(
            left ? Canvas.LeftProperty : Canvas.TopProperty);

        Assert.True(category.HasFlag(MotionPropertyInvalidationCategory.Layout));
    }

    private static class ParentArrangeProbe
    {
        public static readonly UiProperty<int> RankProperty = UiProperty<int>.Register(
            "Rank",
            typeof(ParentArrangeProbe),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsParentArrange));
    }

    private static (UIRoot Root, FixedElement Child) CreateArrangedCanvasChild()
    {
        UIRoot root = new(100, 100);
        Canvas canvas = new();
        FixedElement child = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(canvas);
        canvas.VisualChildren.Add(child);
        root.Invalidate(
            InvalidationFlags.Subtree | InvalidationFlags.Measure | InvalidationFlags.Arrange | InvalidationFlags.Render | InvalidationFlags.HitTest,
            "initial canvas frame");
        root.ProcessFrame();
        Assert.Equal(new LayoutRect(0, 0, 20, 10), child.ArrangedBounds);
        return (root, child);
    }

    [Theory]
    [InlineData(1f, 3f, 13f)]
    [InlineData(2f, 1.5f, 6.5f)]
    public void LayoutRoundingKeepsChildrenContiguousWhenTheCanvasOriginIsOnAHalfDevicePixel(float scale, float childWidth, float slotWidth)
    {
        // A canvas of 12 device pixels centered in a slot one device pixel wider starts half a device pixel in;
        // its own bounds round to 0..12 device pixels. Its four children of 3 device pixels touch each other.
        Canvas canvas = new()
        {
            Width = childWidth * 4,
            Height = 10,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        FixedElement[] cells = new FixedElement[4];
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = new FixedElement(new LayoutSize(childWidth, 10));
            Canvas.SetLeft(cells[i], childWidth * i);
            canvas.VisualChildren.Add(cells[i]);
        }

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        canvas.Measure(new MeasureContext(new LayoutSize(slotWidth, 10), rounding));
        canvas.Arrange(new ArrangeContext(new LayoutRect(0, 0, slotWidth, 10), rounding));

        Assert.Equal("0..12", GetHorizontalDeviceEdges([canvas], scale));
        Assert.Equal("0..3, 3..6, 6..9, 9..12", GetHorizontalDeviceEdges(cells, scale));
    }

    private static string GetHorizontalDeviceEdges(IEnumerable<UIElement> elements, float scale)
    {
        return string.Join(", ", elements.Select(element =>
            $"{element.ArrangedBounds.X * scale}..{(element.ArrangedBounds.X + element.ArrangedBounds.Width) * scale}"));
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}
