using System;
using System.IO;
using System.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void InlineTemplateOwnerBorderBrushRendersOverFrameworkDefaultBorderAspect()
    {
        const string markup = """
            <Button Width="40" Height="20" BorderBrush="Black" BorderThickness="1">
              @template
              {
                <Border BorderBrush="$owner.BorderBrush" BorderThickness="$owner.BorderThickness" />
              }
            </Button>
            """;
        GeneratorRunResult result = RunGenerator("TemplateOwnerBorderBrush.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateOwnerBorderBrushFactory"));
        UIRoot root = new(40, 20);
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.VisualChildren.Add(button);
        root.ProcessFrame();
        root.ProcessFrame();

        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        Assert.Same(button.BorderBrush, border.BorderBrush);
        Assert.Equal(UiPropertyValueSource.TemplateOwnerBinding, border.GetValueSource(Control.BorderBrushProperty));
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawRectangle
            && ReferenceEquals(command.Brush, button.BorderBrush));
        Assert.DoesNotContain(commands, command => command.Brush is Cerneala.UI.Media.SolidColorBrush brush
            && brush.Color == new Color(148, 163, 184));
    }
}
