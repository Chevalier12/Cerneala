using System;
using System.IO;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MinAndMaxSizeMarkupSetsTheFourUIElementProperties()
    {
        const string markup = """
            <StackPanel>
              <Border MinWidth="80" MaxWidth="200" MinHeight="10" MaxHeight="40" />
            </StackPanel>
            """;
        GeneratorRunResult result = RunGenerator("MinMaxSize.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.MinMaxSizeFactory"));
        Border border = Assert.IsType<Border>(Assert.Single(panel.Children));
        Assert.Equal((80f, 200f, 10f, 40f), (border.MinWidth, border.MaxWidth, border.MinHeight, border.MaxHeight));
    }

    [Theory]
    [InlineData("<Border MinWidth=\"-1\" />")]
    [InlineData("<Border MaxWidth=\"-1\" />")]
    [InlineData("<Border MinHeight=\"-1\" />")]
    [InlineData("<Border MaxHeight=\"-1\" />")]
    public void NegativeMinOrMaxSizeMarkupReportsPropertyValueDiagnostic(string markup)
    {
        GeneratorRunResult result = RunGenerator("InvalidMinMaxSize.crn", markup, out _);

        AssertDiagnostic(result, "CERNEALAUI004", "InvalidMinMaxSize.crn");
        Assert.Empty(result.GeneratedSources);
    }
}
