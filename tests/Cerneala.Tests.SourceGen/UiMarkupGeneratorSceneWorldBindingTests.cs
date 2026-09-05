using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Theory]
    [InlineData("OneWay")]
    [InlineData("TwoWay")]
    public void InitOnlySourceIsReadableButNotWritableByGeneratedBindings(string mode)
    {
        const string source = """
            namespace TestInput;
            public sealed class WorldState : System.ComponentModel.INotifyPropertyChanged
            {
                public string Name { get; init; } = "Village";
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }
            """;
        string markup = $"<TextBox DataType=\"TestInput.WorldState\" Text=\"$DataContext.Name:{mode}\" />";
        GeneratorRunResult result = RunGeneratorWithInput("InitOnlyBinding.crn", markup, source, out Compilation compilation);
        if (mode == "TwoWay")
        {
            Diagnostic error = Assert.Single(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal("CERNEALAUI007", error.Id);
            Assert.Contains("writable", error.GetMessage());
        }
        else
        {
            Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
            using MemoryStream stream = new();
            var emit = compilation.Emit(stream);
            Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        }
    }

    [Theory]
    [InlineData("System.Collections.ObjectModel.ObservableCollection<string>")]
    [InlineData("System.Collections.Generic.IReadOnlyList<string>")]
    [InlineData("string[]")]
    public void SceneItemsAcceptsOneWayReferenceAssignableCollection(string sourceType)
    {
        string source = $$"""
            namespace TestInput;
            public sealed class WorldState : System.ComponentModel.INotifyPropertyChanged
            {
                public {{sourceType}} Items { get; set; } = null!;
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }
            """;
        const string markup = """
            <RenderSurface2D DataType="TestInput.WorldState">
              <RenderSurface2D.Scene>
                <Scene2D>
                  <SceneItems2D ItemsSource="$DataContext.Items:OneWay">
                    @templates { <ContentTemplate DataType="System.String"><Sprite2D /></ContentTemplate> }
                  </SceneItems2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        GeneratorRunResult result = RunGeneratorWithInput("WorldBindings.crn", markup, source, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void SceneItemsRejectsUnsafeTwoWayReferenceConversion()
    {
        const string source = """
            namespace TestInput;
            public sealed class WorldState : System.ComponentModel.INotifyPropertyChanged
            {
                public System.Collections.ObjectModel.ObservableCollection<string> Items { get; set; } = new();
                public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
            }
            """;
        const string markup = """
            <RenderSurface2D DataType="TestInput.WorldState">
              <RenderSurface2D.Scene><Scene2D><SceneItems2D ItemsSource="$DataContext.Items:TwoWay" /></Scene2D></RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        GeneratorRunResult result = RunGeneratorWithInput("WorldBindings.crn", markup, source, out _);
        Diagnostic error = Assert.Single(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI007", error.Id);
        Assert.Contains("not compatible", error.GetMessage());
    }
}
