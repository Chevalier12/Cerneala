using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class TextBlockLayoutContractTests
{
    [Fact]
    public void TextBlockWrappingAffectsMeasure()
    {
        const string text = "abcdefghijklmnopqrst";
        TextBlock noWrap = new()
        {
            Text = text,
            FontSize = 10,
            TextWrapping = TextWrapping.NoWrap
        };
        TextBlock wrap = new()
        {
            Text = text,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap
        };

        LayoutSize noWrapSize = noWrap.Measure(new MeasureContext(new LayoutSize(50, 200)));
        LayoutSize wrapSize = wrap.Measure(new MeasureContext(new LayoutSize(50, 200)));

        Assert.True(wrapSize.Height > noWrapSize.Height);
        Assert.True(wrapSize.Width <= 50);
    }

    [Fact]
    public void TextBlockWrappingInvalidatesMeasureAndRender()
    {
        TextBlock textBlock = new()
        {
            Text = "abcdefghijklmnopqrst"
        };
        textBlock.Measure(new MeasureContext(new LayoutSize(50, 200)));
        textBlock.DirtyState.ClearAll();

        textBlock.TextWrapping = TextWrapping.Wrap;

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void TextBlockTrimmingPropertyInvalidatesRenderWithoutClaimingProductionEllipsis()
    {
        TextBlock textBlock = new()
        {
            Text = "abcdefghijklmnopqrst"
        };
        textBlock.Measure(new MeasureContext(new LayoutSize(50, 200)));
        string identity = textBlock.RenderDependencies.TextLayoutIdentity;
        textBlock.DirtyState.ClearAll();

        textBlock.TextTrimming = TextTrimming.None;

        Assert.Equal(identity, textBlock.RenderDependencies.TextLayoutIdentity);
        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void TextBlockMeasureAndRenderUseSameTextLayoutKey()
    {
        UIRoot root = new(60, 200);
        TextBlock textBlock = new()
        {
            Text = "abcdefghijklmnopqrst",
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap
        };
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        string measuredIdentity = textBlock.RenderDependencies.TextLayoutIdentity;

        root.RetainedRenderer.Commit(root);

        Assert.Equal(measuredIdentity, textBlock.RenderDependencies.TextLayoutIdentity);
    }

    [Fact]
    public void TextBlockMeasureUsesFontLineHeightForSystemFonts()
    {
        const float fontSize = 14;
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        IDrawFont font = new SystemFontSource().LoadFont("Arial", fontSize);
        store.SetResource(id, new FontResource(font));
        TextBlock textBlock = new()
        {
            Text = "Ready. No retained work should run on unchanged frames.",
            FontSize = fontSize,
            FontResourceId = id,
            ResourceProvider = store
        };
        Assert.True(TextShaper.Default.TryMeasureLineHeight(new DrawTextRun(font, "Ag", fontSize), out float expected));

        LayoutSize measured = textBlock.Measure(new MeasureContext(new LayoutSize(500, 100)));

        Assert.Equal(expected, measured.Height, precision: 3);
    }

    [Fact]
    public void TextBlockMeasureUsesShapedAdvanceWidthForSystemFonts()
    {
        const string text = "Hello world!";
        const float fontSize = 16;
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        IDrawFont font = new SystemFontSource().LoadFont("Arial", fontSize);
        store.SetResource(id, new FontResource(font));
        TextBlock textBlock = new()
        {
            Text = text,
            FontFamily = "Arial",
            FontSize = fontSize,
            FontResourceId = id,
            ResourceProvider = store
        };
        float shapedAdvance = new SkiaTextShaper().Shape(new DrawTextRun(font, text, fontSize)).AdvanceWidth;

        LayoutSize measured = textBlock.Measure(new MeasureContext(new LayoutSize(500, 100)));

        Assert.Equal(shapedAdvance, measured.Width, precision: 3);
    }

    [Fact]
    public void TextBlockRootFontResourceChangeInvalidatesWrappedMeasurement()
    {
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        store.SetResource(id, new FontResource(new TestFont("Body", 10)));
        UIRoot root = new(80, 200);
        root.SetResourceProvider(store);
        TextBlock textBlock = new()
        {
            Text = "abcdefghijklmnopqrst",
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            FontResourceId = id
        };
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        textBlock.DirtyState.ClearAll();

        store.SetResource(id, new FontResource(new TestFont("Body2", 10)));

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;
}
