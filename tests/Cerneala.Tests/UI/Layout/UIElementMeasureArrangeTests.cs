using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

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

    [Theory]
    [InlineData(false, 0.75f, 0f, "2..20", "17..20")]
    [InlineData(false, 0f, 0.75f, "0..18", "15..18")]
    [InlineData(true, 0.75f, 0f, "2..20", "17..20")]
    [InlineData(true, 0f, 0.75f, "0..18", "15..18")]
    public void LayoutRoundingArrangesAFarEdgeChildFlushWithTheParentsRoundedFarEdge(
        bool vertical,
        float nearMargin,
        float farMargin,
        string expectedPanelEdges,
        string expectedChildEdges)
    {
        // At scale 2 a margin of 0.75 is 1.5 device pixels, so the panel's own bounds round on both edges.
        // A child aligned to the far edge ends where the panel's rounded bounds end, not one device pixel past it.
        const float scale = 2f;
        Panel panel = new()
        {
            Margin = vertical ? new Thickness(0, nearMargin, 0, farMargin) : new Thickness(nearMargin, 0, farMargin, 0)
        };
        FixedElement child = new(vertical ? new LayoutSize(10, 1.5f) : new LayoutSize(1.5f, 10))
        {
            HorizontalAlignment = vertical ? HorizontalAlignment.Stretch : HorizontalAlignment.Right,
            VerticalAlignment = vertical ? VerticalAlignment.Bottom : VerticalAlignment.Stretch
        };
        panel.VisualChildren.Add(child);

        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        panel.Measure(new MeasureContext(new LayoutSize(10, 10), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10), rounding));

        Assert.Equal(expectedPanelEdges, GetDeviceEdges(panel, scale, vertical));
        Assert.Equal(expectedChildEdges, GetDeviceEdges(child, scale, vertical));
    }

    [Theory]
    [InlineData(1f, 0.4f, float.NaN, 100f)]
    [InlineData(1.5f, 0.3f, float.NaN, 300f / 3f)]
    [InlineData(1f, 0f, 99.6f, 100f)]
    [InlineData(1f, 0f, float.NaN, 33.333332f)]
    public void LayoutRoundingMeasuresAStretchedElementAtTheSizeItIsArrangedAt(
        float scale,
        float marginTop,
        float height,
        float slotHeight)
    {
        // As in WPF, MeasureCore and ArrangeCore both receive sizes rounded to the nearest device pixel, so an
        // element that fills its slot is measured at the size it is then arranged at.
        SizeRecordingElement element = new()
        {
            Margin = new Thickness(0, marginTop, 0, 0),
            Height = height
        };
        LayoutRounding rounding = LayoutRounding.ForScale(scale);

        element.Measure(new MeasureContext(new LayoutSize(100, slotHeight), rounding));
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, slotHeight), rounding));

        Assert.Equal(element.ArrangeSize, element.MeasureSize);
    }

    private static string GetDeviceEdges(UIElement element, float scale, bool vertical)
    {
        LayoutRect bounds = element.ArrangedBounds;
        float start = vertical ? bounds.Y : bounds.X;
        float end = start + (vertical ? bounds.Height : bounds.Width);
        return $"{start * scale}..{end * scale}";
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

    private sealed class SizeRecordingElement : UIElement
    {
        public LayoutSize? MeasureSize { get; private set; }

        public LayoutSize? ArrangeSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureSize = context.AvailableSize;
            return context.AvailableSize;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeSize = context.FinalRect.Size;
            return context.FinalRect;
        }
    }
}
