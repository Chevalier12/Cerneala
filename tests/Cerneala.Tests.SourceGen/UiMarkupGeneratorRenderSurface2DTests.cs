using System;
using System.IO;
using System.Linq;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void RenderSurface2DHostsRetainedContentFromMarkup()
    {
        const string markup = """
            <RenderSurface2D>
              <Button Content="Overlay" />
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator(
            "RenderSurface2DHost.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.RenderSurface2DHostFactory"));
        Button overlay = Assert.IsType<Button>(surface.Content);

        Assert.Equal("Overlay", overlay.Content);
    }
}
