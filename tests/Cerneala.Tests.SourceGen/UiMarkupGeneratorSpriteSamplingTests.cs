using System.ComponentModel;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void SpriteSamplingMarkupSetsPointAndUpdatesOneWayBinding()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.Tests.SourceGen.SpriteSamplingMarkupState">
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Sprite2D Sampling="Point" />
                  <Sprite2D Sampling="$DataContext.Sampling:OneWay" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("SpriteSampling.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        SpriteSamplingMarkupState state = new();
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.SpriteSamplingFactory", state));
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Sprite2D literal = Assert.IsType<Sprite2D>(surface.Scene!.Children[0]);
        Sprite2D bound = Assert.IsType<Sprite2D>(surface.Scene.Children[1]);
        Assert.Equal(DrawSamplingMode.Point, literal.Sampling);
        Assert.Equal(DrawSamplingMode.Linear, bound.Sampling);

        state.Sampling = DrawSamplingMode.Point;
        state.Notify(nameof(state.Sampling));
        Assert.Equal(DrawSamplingMode.Point, bound.Sampling);
        Assert.Equal(DrawSamplingMode.Point, literal.Sampling);
        root.VisualChildren.Remove(surface);
    }
}

public sealed class SpriteSamplingMarkupState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public DrawSamplingMode Sampling { get; set; } = DrawSamplingMode.Linear;

    public void Notify(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
