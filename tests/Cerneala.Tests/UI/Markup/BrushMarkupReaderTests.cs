using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Markup;

public sealed class BrushMarkupReaderTests
{
    [Fact]
    public void ReadsGradientAndOrdersStops()
    {
        const string markup = """
            <LinearGradientBrush StartPoint="0,0" EndPoint="10,0" Opacity="0.5">
              <GradientStop Offset="1" Color="Black" />
              <GradientStop Offset="0" Color="White" />
            </LinearGradientBrush>
            """;

        MarkupResult<Brush> result = new BrushMarkupReader().Read(markup);

        LinearGradientBrush brush = Assert.IsType<LinearGradientBrush>(result.Value);
        Assert.Empty(result.Diagnostics);
        Assert.Equal([0, 1], brush.Stops.Select(stop => stop.Offset).ToArray());
        Assert.Equal(0.5f, brush.Opacity);
    }

    [Fact]
    public void ReadsDrawingBrushCommandsAndTileOptions()
    {
        const string markup = """
            <DrawingBrush ContentBounds="0,0,10,10" Viewport="0,0,20,20" TileMode="FlipXY">
              <FillRectangle Rect="0,0,10,10" Color="#FFFF0000" />
            </DrawingBrush>
            """;

        DrawingBrush brush = Assert.IsType<DrawingBrush>(new BrushMarkupReader().Read(markup).Value);

        Assert.Single(brush.Commands);
        Assert.Equal(DrawTileMode.FlipXY, brush.TileMode);
        Assert.Equal(new DrawRect(0, 0, 20, 20), brush.Viewport);
    }

    [Fact]
    public void VisualBrushRequiresResolvedVisual()
    {
        UIElement source = new();
        BrushMarkupReader reader = new(visualResolver: name => name == "Source" ? source : null);

        VisualBrush brush = Assert.IsType<VisualBrush>(reader.Read("<VisualBrush Source=\"$Source\" />").Value);

        Assert.Same(source, brush.Visual);
    }

    [Fact]
    public void ImageBrushUsesRuntimeImageResolver()
    {
        TestImage image = new();
        BrushMarkupReader reader = new(imageResolver: source => source == "accent.png" ? image : null);

        ImageBrush brush = Assert.IsType<ImageBrush>(reader.Read("<ImageBrush Source=\"accent.png\" Stretch=\"Uniform\" />").Value);

        Assert.Same(image, brush.Image);
        Assert.Equal(DrawBrushStretch.Uniform, brush.Stretch);
    }

    [Fact]
    public void ReportsUnknownOrInvalidBrushes()
    {
        MarkupResult<Brush> result = new BrushMarkupReader().Read("<LinearGradientBrush StartPoint=\"0,0\" EndPoint=\"1,0\" />");

        Assert.Null(result.Value);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MARKUP031");
    }

    [Theory]
    [InlineData("Stretch=\"Uniform\"", false)]
    [InlineData("TileMode=\"FlipXY\" AlignmentX=\"Left\" AlignmentY=\"Bottom\"", false)]
    [InlineData("Stretch=\"42\"", true)]
    [InlineData("TileMode=\"42\"", true)]
    [InlineData("Stretch=\"Uniform,Fill\"", true)]
    [InlineData("AlignmentX=\"1\"", true)]
    [InlineData("AlignmentY=\"Top,Bottom\"", true)]
    public void EnumAttributesThatNameNoMemberReportMarkup031(string attribute, bool expectError)
    {
        TestImage image = new();
        BrushMarkupReader reader = new(imageResolver: source => source == "accent.png" ? image : null);

        MarkupResult<Brush> result = reader.Read($"<ImageBrush Source=\"accent.png\" {attribute} />");

        Assert.Equal(expectError, result.HasErrors);
        if (expectError)
        {
            Assert.Null(result.Value);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "MARKUP031");
        }
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;

        public int Height => 16;
    }
}
