using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class UIElementMeasureArrangeTests
{
    [Fact]
    public void BaseElementMeasuresAndArrangesDeterministically()
    {
        UIElement element = new();

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 50)));
        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(1, 2, 10, 20)));

        Assert.Equal(LayoutSize.Zero, desired);
        Assert.Equal(LayoutSize.Zero, element.DesiredSize);
        Assert.Equal(new LayoutRect(1, 2, 10, 20), arranged);
        Assert.Equal(arranged, element.ArrangedBounds);
    }

    [Fact]
    public void DerivedElementCanOverrideMeasureAndArrange()
    {
        FixedElement element = new(new LayoutSize(20, 10));

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 40)));

        Assert.Equal(new LayoutSize(20, 10), desired);
        Assert.Equal(new LayoutRect(0, 0, 20, 10), arranged);
    }

    [Fact]
    public void AlignmentPositionsElementWithinExtraArrangeSpace()
    {
        FixedElement element = new(new LayoutSize(20, 10))
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        element.Measure(new MeasureContext(new LayoutSize(100, 100)));

        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(40, 90, 20, 10), arranged);
    }

    [Fact]
    public void ExplicitSizeParticipatesInMeasureAndAlignment()
    {
        UIElement element = new()
        {
            Width = 40,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom
        };

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutSize(40, 20), desired);
        Assert.Equal(new LayoutRect(30, 80, 40, 20), arranged);
    }

    [Fact]
    public void LayoutVersionChangesForLayoutAffectingProperty()
    {
        UIElement element = new();
        int initial = element.LayoutVersion;

        element.Margin = new Thickness(4);

        Assert.True(element.LayoutVersion > initial);
    }

    [Fact]
    public void EqualLayoutPropertySetDoesNotChangeLayoutVersion()
    {
        UIElement element = new();
        element.Margin = new Thickness(4);
        int version = element.LayoutVersion;

        element.Margin = new Thickness(4);

        Assert.Equal(version, element.LayoutVersion);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return new LayoutRect(context.FinalRect.X, context.FinalRect.Y, size.Width, size.Height);
        }
    }
}
