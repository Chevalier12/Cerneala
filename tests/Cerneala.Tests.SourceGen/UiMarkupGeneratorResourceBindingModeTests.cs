using System;
using System.IO;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void OneWaySuffixOnResourceReferenceIsAccepted()
    {
        const string markup = """
            <TextBlock Foreground="$PaperBrush:OneWay">
              <TextBlock.Resources>
                <SolidColorBrush Name="PaperBrush" Color="#FF123456" />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator(
            "ResourceBindingMode.crn",
            markup,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock textBlock = Assert.IsType<TextBlock>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.ResourceBindingModeFactory"));
        Assert.Equal(new Color(18, 52, 86, 255), Assert.IsType<SolidColorBrush>(textBlock.Foreground).Color);
    }
}
