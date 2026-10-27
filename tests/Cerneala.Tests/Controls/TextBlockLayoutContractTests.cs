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
    public void TextBlockTrimmingPropertyInvalidatesMeasureAndRender()
    {
        TextBlock textBlock = new()
        {
            Text = "abcdefghijklmnopqrst"
        };
        textBlock.Measure(new MeasureContext(new LayoutSize(50, 200)));
        textBlock.DirtyState.ClearAll();

        textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void TextBlockMeasuresAndRendersCharacterEllipsisInsideItsBounds()
    {
        UIRoot root = new(45, 30);
        TextBlock textBlock = new()
        {
            Text = "Alpha beta gamma",
            FontSize = 10,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        root.VisualChildren.Add(textBlock);

        root.ProcessFrame();
        DrawCommand command = Assert.Single(root.RetainedRenderer.Commit(root));

        Assert.EndsWith("…", command.Text, StringComparison.Ordinal);
        Assert.NotEqual(textBlock.Text, command.Text);
        Assert.True(textBlock.DesiredSize.Width <= root.ViewportWidth);
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

    [Theory]
    [InlineData("Button")]
    [InlineData("TextBlock")]
    public void PaddingAddsItsSidesToTheMeasuredSize(string kind)
    {
        static Control Create(string kind, Thickness padding) => kind == "Button"
            ? new Button { Content = "Hi", Padding = padding }
            : new TextBlock { Text = "Hi", Padding = padding };

        Control plain = Create(kind, Thickness.Zero);
        Control padded = Create(kind, new Thickness(10, 4, 6, 2));

        LayoutSize plainSize = plain.Measure(new MeasureContext(new LayoutSize(500, 100)));
        LayoutSize paddedSize = padded.Measure(new MeasureContext(new LayoutSize(500, 100)));

        Assert.True(
            Math.Abs(paddedSize.Width - (plainSize.Width + 16)) < 0.01f && Math.Abs(paddedSize.Height - (plainSize.Height + 6)) < 0.01f,
            $"{kind}: plain {plainSize.Width}x{plainSize.Height}, padded {paddedSize.Width}x{paddedSize.Height}, expected {plainSize.Width + 16}x{plainSize.Height + 6}.");
    }

    [Fact]
    public void PaddingMovesTheDrawnTextOrigin()
    {
        static DrawCommand FirstTextCommand(Thickness padding)
        {
            UIRoot root = new(300, 100);
            TextBlock textBlock = new() { Text = "Hi", Padding = padding };
            root.VisualChildren.Add(textBlock);
            root.ProcessFrame();
            return root.RetainedRenderer.Commit(root).First(command => command.Kind == DrawCommandKind.DrawText);
        }

        DrawCommand plain = FirstTextCommand(Thickness.Zero);
        DrawCommand padded = FirstTextCommand(new Thickness(10, 4, 6, 2));

        Assert.True(
            Math.Abs((padded.Position.X - plain.Position.X) - 10) < 0.01f && Math.Abs((padded.Position.Y - plain.Position.Y) - 4) < 0.01f,
            $"plain origin {plain.Position}, padded origin {padded.Position}; expected an offset of (10, 4).");
    }

    // WPF's TextBlock wraps and trims inside the width that Padding leaves: a padded TextBlock arranged at the same
    // width as an unpadded one has less room for its text.
    [Fact]
    public void PaddingNarrowsTheWidthUsedForWrapping()
    {
        static LayoutSize MeasureWrapped(Thickness padding)
        {
            TextBlock textBlock = new()
            {
                Text = "abcdefghijklmnopqrst",
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Padding = padding
            };
            return textBlock.Measure(new MeasureContext(new LayoutSize(60, 200)));
        }

        LayoutSize plain = MeasureWrapped(Thickness.Zero);
        LayoutSize padded = MeasureWrapped(new Thickness(20, 0, 20, 0));

        Assert.True(padded.Width <= 60.01f, $"padded width {padded.Width} exceeds the available 60.");
        Assert.True(padded.Height > plain.Height, $"plain {plain.Height}, padded {padded.Height}: padding did not narrow the wrap width.");
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;
}
