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

    // #1153: $Name:OneWay assigns the same SpriteAnimationSet as $Name, so the static state check must see it too.
    [Theory]
    [InlineData("$HeroAnimations")]
    [InlineData("$HeroAnimations:OneWay")]
    public void OneWaySuffixOnSpriteAnimationsKeepsTheStaticAnimationStateCheck(string animations)
    {
        GeneratorRunResult result = RunGenerator(
            "SpriteOneWayState.crn",
            SpriteAnimationsMarkup(animations, "Missing"),
            out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Equal("CERNEALAUI016", diagnostic.Id);
        Assert.Contains("'Missing'", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Contains("'HeroAnimations'", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void OneWaySuffixOnSpriteAnimationsAcceptsAnExistingState()
    {
        GeneratorRunResult result = RunGenerator(
            "SpriteOneWayState.crn",
            SpriteAnimationsMarkup("$HeroAnimations:OneWay", "Idle"),
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.SpriteOneWayStateFactory"));
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(surface.Scene!.Children));
        Assert.Equal("Idle", sprite.AnimationState);
        Assert.True(sprite.Animations!.TryGetClip("Idle", out _));
    }

    private static string SpriteAnimationsMarkup(string animations, string state) => $$"""
        <RenderSurface2D>
          <RenderSurface2D.Resources>
            <SpriteAnimationSet Name="HeroAnimations">
              <SpriteAnimationClip Name="Idle">
                <SpriteAnimationFrame SourceX="0" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="1ms" />
              </SpriteAnimationClip>
            </SpriteAnimationSet>
          </RenderSurface2D.Resources>
          <RenderSurface2D.Scene>
            <Scene2D>
              <Sprite2D Animations="{{animations}}" AnimationState="{{state}}" />
            </Scene2D>
          </RenderSurface2D.Scene>
        </RenderSurface2D>
        """;
}
