using System;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    // #731 made every markup value MarkupBase. A control's own starting value must stay below it, so a markup
    // attribute still wins over the value the control's constructor gives.
    [Fact]
    public void MarkupAttributeWinsOverControlConstructorValue()
    {
        const string markup = """
            <StackPanel>
              <Button Name="Action" Focusable="False" IsTabStop="False" />
              <ProgressBar Name="Progress" Background="White" BorderThickness="3" />
              <ColorSwatch Name="Swatch" Width="40" Height="30" />
              <TextBox Name="Input" Focusable="False" />
              <ScrollBar Name="Bar" ViewportSize="5" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("ConstructorDefaults.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using System.IO.MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Cerneala.UI.Controls.StackPanel panel = Assert.IsType<Cerneala.UI.Controls.StackPanel>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.ConstructorDefaultsFactory"));
        Cerneala.UI.Controls.Button action = Assert.IsType<Cerneala.UI.Controls.Button>(panel.Children[0]);
        Cerneala.UI.Controls.ProgressBar progress = Assert.IsType<Cerneala.UI.Controls.ProgressBar>(panel.Children[1]);
        Cerneala.UI.Controls.ColorSwatch swatch = Assert.IsType<Cerneala.UI.Controls.ColorSwatch>(panel.Children[2]);
        Cerneala.UI.Controls.TextBox input = Assert.IsType<Cerneala.UI.Controls.TextBox>(panel.Children[3]);
        Cerneala.UI.Controls.Primitives.ScrollBar bar = Assert.IsType<Cerneala.UI.Controls.Primitives.ScrollBar>(panel.Children[4]);

        Assert.Equal(
            (false, false, new Cerneala.UI.Layout.Thickness(3), 40f, 30f, false, 5f),
            (action.Focusable, action.IsTabStop, progress.BorderThickness, swatch.Width, swatch.Height, input.Focusable, bar.ViewportSize));
        Cerneala.UI.Media.SolidColorBrush background = Assert.IsType<Cerneala.UI.Media.SolidColorBrush>(progress.Background);
        Assert.Equal(Cerneala.Drawing.Color.White, background.Color);
    }
}
