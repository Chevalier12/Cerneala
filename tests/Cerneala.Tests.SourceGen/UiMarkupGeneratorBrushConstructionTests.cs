using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Theory]
    [InlineData("SolidColorBrush", "Color=\"Red\" Opacity=\"0.5\"", "")]
    [InlineData("LinearGradientBrush", "StartPoint=\"0,0\" EndPoint=\"1,1\"", "<GradientStop Offset=\"0\" Color=\"Red\" /><GradientStop Offset=\"1\" Color=\"#800000FF\" />")]
    [InlineData("RadialGradientBrush", "Center=\"0.5,0.5\" RadiusX=\"1\" RadiusY=\"2\"", "<GradientStop Offset=\"0\" Color=\"Blue\" />")]
    [InlineData("ImageBrush", "Source=\"sprites.png\" Stretch=\"Uniform\" Viewport=\"0,0,16,16\" Opacity=\"0.4\"", "")]
    public void NamedAndInlineBrushesEmitEquivalentConstructors(string brushType, string attributes, string content)
    {
        string markup = $"""
            <StackPanel>
              <StackPanel.Resources>
                <{brushType} Name="Paint" {attributes}>{content}</{brushType}>
              </StackPanel.Resources>
              <Button Name="Named" Background="$Paint" />
              <Button Name="Inline">
                <Button.Background>
                  <{brushType} {attributes}>{content}</{brushType}>
                </Button.Background>
              </Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("BrushConstruction.crn", markup, out Compilation compilation);
        Assert.Empty(result.Diagnostics);
        SyntaxNode generated = Assert.Single(result.GeneratedSources).SyntaxTree.GetRoot();
        VariableDeclaratorSyntax resource = Assert.Single(generated.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>(), node => node.Identifier.ValueText == "PaintResource0");
        AssignmentExpressionSyntax inline = Assert.Single(generated.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>(), node => node.Left.ToString() == "Inline.Background");
        string typeName = "global::Cerneala.UI.Media." + brushType;
        Assert.StartsWith("new " + typeName + "(", inline.Right.ToString());
        Assert.Equal(inline.Right.ToString().Replace("new " + typeName, "new", StringComparison.Ordinal),
            resource.Initializer!.Value.ToString());

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.BrushConstructionFactory"));
        Button namedButton = Assert.IsType<Button>(panel.VisualChildren[0]);
        Button inlineButton = Assert.IsType<Button>(panel.VisualChildren[1]);
        Assert.Equal("Cerneala.UI.Media." + brushType, namedButton.Background!.GetType().FullName);
        Assert.Equal(namedButton.Background.GetType(), inlineButton.Background!.GetType());
        Assert.NotSame(namedButton.Background, inlineButton.Background);
    }

    [Theory]
    [InlineData("<StackPanel><Button Background=\"255,0,0\" /></StackPanel>")]
    [InlineData("<StackPanel><StackPanel.Resources><SolidColorBrush Name=\"P\" Color=\"#FF0000\" /></StackPanel.Resources></StackPanel>")]
    [InlineData("<StackPanel><StackPanel.Resources><SolidColorBrush Name=\"P\" Color=\"255,0,0\" /></StackPanel.Resources></StackPanel>")]
    [InlineData("<StackPanel><StackPanel.Resources><SolidColorBrush Name=\"P\" Color=\"1, 2, 3, 128\" /></StackPanel.Resources></StackPanel>")]
    [InlineData("<StackPanel><StackPanel.Resources><LinearGradientBrush Name=\"P\" StartPoint=\"0,0\" EndPoint=\"1,1\"><GradientStop Offset=\"0\" Color=\"255,0,0\" /><GradientStop Offset=\"1\" Color=\"0, 0, 255, 128\" /></LinearGradientBrush></StackPanel.Resources></StackPanel>")]
    [InlineData("<StackPanel><StackPanel.Resources><DrawingBrush Name=\"P\" ContentBounds=\"0,0,8,8\"><FillRectangle Rect=\"0,0,8,8\" Color=\"0,128,0\" /></DrawingBrush></StackPanel.Resources></StackPanel>")]
    public void ColorSyntaxIsTheSameInPlainAttributesAndBrushResources(string markup)
    {
        GeneratorRunResult result = RunGenerator("BrushColorSyntax.crn", markup, out _);

        Assert.True(
            result.Diagnostics.Length == 0,
            "Diagnostics: " + string.Join(" | ", result.Diagnostics));
    }

    [Fact]
    public void ByteChannelColorInSolidColorBrushResourceBuildsTheSameColorAsAPlainAttribute()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Rgb" Color="255,0,0" />
                <SolidColorBrush Name="Rgba" Color="1, 2, 3, 128" />
              </StackPanel.Resources>
              <Button Background="$Rgb" />
              <Button Background="$Rgba" />
              <Button Background="1, 2, 3, 128" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("BrushColorValues.crn", markup, out Compilation compilation);
        Assert.Empty(result.Diagnostics);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.BrushColorValuesFactory"));
        Assert.Equal(new Cerneala.Drawing.Color(255, 0, 0), Assert.IsType<Button>(panel.VisualChildren[0]).Background!.SolidColor);
        Assert.Equal(new Cerneala.Drawing.Color(1, 2, 3, 128), Assert.IsType<Button>(panel.VisualChildren[1]).Background!.SolidColor);
        Assert.Equal(new Cerneala.Drawing.Color(1, 2, 3, 128), Assert.IsType<Button>(panel.VisualChildren[2]).Background!.SolidColor);
    }

    [Theory]
    [InlineData("256,0,0")]
    [InlineData("1,2")]
    [InlineData("1,2,3,4,5")]
    [InlineData("-1,0,0")]
    [InlineData("#GG0000")]
    public void InvalidColorInSolidColorBrushResourceReportsInvalidPropertyValue(string color)
    {
        string markup = "<StackPanel><StackPanel.Resources><SolidColorBrush Name=\"P\" Color=\"" + color + "\" /></StackPanel.Resources></StackPanel>";

        GeneratorRunResult result = RunGenerator("InvalidBrushColor.crn", markup, out _);

        AssertDiagnostic(result, "CERNEALAUI004", "InvalidBrushColor.crn");
    }

    [Theory]
    [InlineData("<ImageBrush Name=\"P\" Source=\"a.png\" Viewport=\"0,0,10,10\" />", false)]
    [InlineData("<ImageBrush Name=\"P\" Source=\"a.png\" Viewport=\"0,0,0,0\" />", true)]
    [InlineData("<ImageBrush Name=\"P\" Source=\"a.png\" Viewbox=\"0,0,10,-1\" />", true)]
    [InlineData("<DrawingBrush Name=\"P\" ContentBounds=\"0,0,10,10\"><FillRectangle Rect=\"0,0,10,10\" Color=\"White\" /></DrawingBrush>", false)]
    [InlineData("<DrawingBrush Name=\"P\" ContentBounds=\"0,0,0,10\"><FillRectangle Rect=\"0,0,10,10\" Color=\"White\" /></DrawingBrush>", true)]
    public void NonPositiveTileBrushRectanglesAreReportedAtBuildTime(string brush, bool invalid)
    {
        string markup = "<StackPanel><StackPanel.Resources>" + brush + "</StackPanel.Resources></StackPanel>";

        GeneratorRunResult result = RunGenerator("BrushTileRect.crn", markup, out _);

        bool reported = result.Diagnostics.Any(diagnostic => diagnostic.Id == "CERNEALAUI004" && diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.True(
            reported == invalid,
            "CERNEALAUI004 reported: " + reported + ". Diagnostics: " + string.Join(" | ", result.Diagnostics));
    }

    // #801: RadiusX and RadiusY have no valid default (RadialGradientBrush rejects 0), so a missing radius is a build
    // error that names it, in the resource form and in the property-element form, and no code is generated.
    [Theory]
    [InlineData("Center=\"0.5,0.5\"", "RadiusX")]
    [InlineData("Center=\"0.5,0.5\" RadiusX=\"0.5\"", "RadiusY")]
    [InlineData("Center=\"0.5,0.5\" RadiusY=\"0.5\"", "RadiusX")]
    [InlineData("Center=\"0.5,0.5\" RadiusX=\"0\" RadiusY=\"0.5\"", "RadiusX")]
    [InlineData("Center=\"0.5,0.5\" RadiusX=\"0.5\" RadiusY=\"-1\"", "RadiusY")]
    public void RadialGradientBrushWithoutAPositiveRadiusReportsAnError(string attributes, string missing)
    {
        string markup = $"""
            <StackPanel><StackPanel.Resources><RadialGradientBrush Name="P" {attributes}><GradientStop Offset="0" Color="Blue" /></RadialGradientBrush></StackPanel.Resources><Button Background="$P" /></StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("RadialMissingRadius.crn", markup, out _);

        Assert.True(
            result.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains(missing, StringComparison.Ordinal)),
            "No error mentioning " + missing + ". Diagnostics: " + string.Join(" | ", result.Diagnostics));
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RadialGradientBrushPropertyElementWithoutARadiusReportsAnError()
    {
        const string markup = """
            <Button><Button.Background><RadialGradientBrush Center="0.5,0.5" RadiusX="0.5"><GradientStop Offset="0" Color="Blue" /></RadialGradientBrush></Button.Background></Button>
            """;

        GeneratorRunResult result = RunGenerator("RadialPropertyElementMissingRadius.crn", markup, out _);

        Assert.True(
            result.Diagnostics.Any(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains("RadiusY", StringComparison.Ordinal)),
            "No error mentioning RadiusY. Diagnostics: " + string.Join(" | ", result.Diagnostics));
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void RadialGradientBrushWithBothRadiiReportsNoDiagnostic()
    {
        const string markup = """
            <StackPanel><StackPanel.Resources><RadialGradientBrush Name="P" Center="0.5,0.5" RadiusX="0.5" RadiusY="0.5"><GradientStop Offset="0" Color="Blue" /></RadialGradientBrush></StackPanel.Resources><Button Background="$P" /></StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("RadialBothRadii.crn", markup, out _);

        Assert.Empty(result.Diagnostics);
    }
}
