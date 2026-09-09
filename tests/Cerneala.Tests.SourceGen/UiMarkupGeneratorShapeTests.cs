using Cerneala.Drawing;
using Cerneala.UI.Controls.Shapes;
using StackPanel = Cerneala.UI.Controls.StackPanel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using PathShape = Cerneala.UI.Controls.Shapes.Path;
using RectangleShape = Cerneala.UI.Controls.Shapes.Rectangle;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void ShapePrimitivesCompileAndInstantiateFromCrnLiterals()
    {
        const string markup = """
            <StackPanel>
              <Line StartPoint="1,2" EndPoint="30,40" Stroke="Black" />
              <Polyline Points="-1,2 3.5,4 5e1,6" Stroke="Black" />
              <Polygon Points="0,0 20,0 20,20" Fill="White" FillRule="EvenOdd" />
              <Rectangle Width="100" Height="60" RadiusX="12" RadiusY="6" Fill="White" />
              <Path Data="M0 0 Q10 0 10 10 C10 20 20 20 20 10 A5 3 0 0 1 30 10 Z M2 2L4 2L4 4Z"
                    Fill="White" Stroke="Black" FillRule="EvenOdd" />
            </StackPanel>
            """;
        StackPanel panel = Assert.IsType<StackPanel>(CreateShapeMarkup("ShapePrimitives", markup));
        Line line = Assert.IsType<Line>(panel.VisualChildren[0]);
        Assert.Equal(new DrawPoint(1, 2), line.StartPoint);
        Assert.Equal(new DrawPoint(30, 40), line.EndPoint);
        Polyline polyline = Assert.IsType<Polyline>(panel.VisualChildren[1]);
        Assert.Equal([new DrawPoint(-1, 2), new DrawPoint(3.5f, 4), new DrawPoint(50, 6)], polyline.Points);
        Polygon polygon = Assert.IsType<Polygon>(panel.VisualChildren[2]);
        Assert.Equal(3, polygon.Points.Count);
        Assert.Equal(DrawFillRule.EvenOdd, polygon.FillRule);
        RectangleShape rectangle = Assert.IsType<RectangleShape>(panel.VisualChildren[3]);
        Assert.Equal(12, rectangle.RadiusX);
        Assert.Equal(6, rectangle.RadiusY);
        PathShape path = Assert.IsType<PathShape>(panel.VisualChildren[4]);
        Assert.Equal(2, path.Data!.Path!.Contours.Count);
        Assert.Equal(DrawFillRule.EvenOdd, path.FillRule);
    }

    [Theory]
    [InlineData("1,2,3")]
    [InlineData("0,0 NaN,1")]
    [InlineData("0,0 Infinity,1")]
    public void InvalidShapePointListsProduceValueDiagnostics(string points)
    {
        GeneratorRunResult result = RunGenerator("InvalidShapePoints.crn", $"<Polyline Points=\"{points}\" />", out _);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI004");
    }

    [Fact]
    public void EmptyShapePointListsAreSupported()
    {
        Polyline shape = Assert.IsType<Polyline>(CreateShapeMarkup("EmptyShapePoints", "<Polyline Points=\"\" />"));
        Assert.Empty(shape.Points);
    }

    [Fact]
    public void NegativeRectangleRadiiProduceValueDiagnostics()
    {
        GeneratorRunResult result = RunGenerator("InvalidShapeRadius.crn", "<Rectangle RadiusX=\"-1\" />", out _);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI004");
    }

    [Fact]
    public void ShapePointListBindingsRemainTypedAndUpdateThroughUiProperties()
    {
        const string markup = """
            <StackPanel>
              <Polyline Name="Source" Points="0,0 20,0 20,20" />
              <Polygon Points="$Source.Points:OneWay" />
            </StackPanel>
            """;
        StackPanel panel = Assert.IsType<StackPanel>(CreateShapeMarkup("ShapePointBinding", markup));
        Polyline source = Assert.IsType<Polyline>(panel.VisualChildren[0]);
        Polygon target = Assert.IsType<Polygon>(panel.VisualChildren[1]);
        Assert.Equal(source.Points, target.Points);
        source.Points = [new(0, 0), new(40, 0), new(40, 40), new(0, 40)];
        Assert.Equal(source.Points, target.Points);
    }

    private static Cerneala.UI.Elements.UIElement CreateShapeMarkup(string name, string markup)
    {
        GeneratorRunResult result = RunGenerator(name + ".crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return (Cerneala.UI.Elements.UIElement)InvokeCreate(stream, "Cerneala.GeneratedUi." + name + "Factory");
    }
}
