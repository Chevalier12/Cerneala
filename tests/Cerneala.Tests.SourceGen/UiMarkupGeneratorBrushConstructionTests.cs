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
}
