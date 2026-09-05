using System.ComponentModel;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void GeneratedAnimationStateBindingRecordsIdleWalkAndAttackFrames()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.Tests.SourceGen.SpriteAnimationMarkupState">
              <RenderSurface2D.Resources>
                <SpriteAnimationSet Name="Animations">
                  <SpriteAnimationClip Name="Idle">
                    <SpriteAnimationFrame SourceX="0" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="100ms" />
                  </SpriteAnimationClip>
                  <SpriteAnimationClip Name="Walk">
                    <SpriteAnimationFrame SourceX="16" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="100ms" />
                  </SpriteAnimationClip>
                  <SpriteAnimationClip Name="Attack" IsLooping="false">
                    <SpriteAnimationFrame SourceX="32" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="100ms" />
                  </SpriteAnimationClip>
                </SpriteAnimationSet>
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D><Sprite2D Animations="$Animations" AnimationState="$DataContext.AnimationState:OneWay" /></Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        GeneratorRunResult result = RunGenerator("AnimatedBinding.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emitted = compilation.Emit(stream);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        SpriteAnimationMarkupState state = new();
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(InvokeCreate(stream, "Cerneala.GeneratedUi.AnimatedBindingFactory", state));
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(surface.Scene!.Children));
        sprite.Source = new AnimationMarkupImage();
        sprite.Destination = new DrawRect(0, 0, 16, 16);
        // Exercise the real recorder without widening the core assembly's friend API.
        MethodInfo record = typeof(RenderSurface2D).GetInterface("IRenderSurface2DFrameSource")!.GetMethod("RecordFrame")!;
        foreach ((string name, float sourceX) in new[] { ("Idle", 0f), ("Walk", 16f), ("Attack", 32f) })
        {
            state.AnimationState = name;
            state.Notify(nameof(state.AnimationState));
            Assert.Equal(name, sprite.AnimationState);
            DrawCommandList commands = new();
            record.Invoke(surface, [commands, new DrawRect(0, 0, 64, 64)]);
            Assert.Equal(new DrawRect(sourceX, 0, 16, 16), Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawImage)).ImageSource);
        }
    }

    private sealed class AnimationMarkupImage : IDrawImage
    {
        public int Width => 64;
        public int Height => 64;
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void SpriteAndPromotedTileMarkupCompileWithIdleWalkAttackAspectMotionAndPrism()
    {
        const string markup = """
            <RenderSurface2D DataType="Cerneala.Tests.SourceGen.SpriteAnimationMarkupState"
                             RedrawMode="OnDemand"
                             xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <resources:ImageResource Name="HeroAtlas" Source="Assets/hero.png" />
                <SpriteAnimationSet Name="HeroAnimations">
                  <SpriteAnimationClip Name="Idle" IsLooping="true">
                    <SpriteAnimationFrame SourceX="0" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="240ms" />
                    <SpriteAnimationFrame SourceX="16" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="240ms" />
                  </SpriteAnimationClip>
                  <SpriteAnimationClip Name="Walk" IsLooping="true">
                    <SpriteAnimationFrame SourceX="0" SourceY="16" SourceWidth="16" SourceHeight="16" Duration="90ms" />
                    <SpriteAnimationFrame SourceX="16" SourceY="16" SourceWidth="16" SourceHeight="16" Duration="110ms" Flip="Horizontal" />
                  </SpriteAnimationClip>
                  <SpriteAnimationClip Name="Attack" IsLooping="false">
                    <SpriteAnimationFrame SourceX="0" SourceY="32" SourceWidth="16" SourceHeight="16" Duration="60ms" />
                    <SpriteAnimationFrame SourceX="16" SourceY="32" SourceWidth="16" SourceHeight="16" Duration="140ms" />
                  </SpriteAnimationClip>
                </SpriteAnimationSet>
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Sprite2D SourceResourceId="$HeroAtlas"
                            Animations="$HeroAnimations"
                            AnimationState="$DataContext.AnimationState:OneWay"
                            AnimationPlaybackRate="$DataContext.PlaybackRate:OneWay"
                            IsAnimationPaused="$DataContext.IsPaused:OneWay"
                            AnimationStateChangeMode="Resume">
                    <Sprite2D.Aspect>
                      @when $DataContext.IsAttacking
                      {
                        @set { AnimationState = "Attack"; Tint = #FFFF8080; }
                      }
                      @on Loaded
                      {
                        @animate with Tween(100ms)
                        {
                          @to { TranslateX = 8; Opacity = 0.75; AnimationPlaybackRate = 1.25; }
                        }
                      }
                    </Sprite2D.Aspect>
                    @prism
                    {
                      @layer HeroContent { Opacity = 1; @filter Blur { Radius = 1; } }
                    }
                  </Sprite2D>
                  <TileMap2D>
                    <TileLayer2D LayerId="Actors">
                      <TileInstance2D X="2"
                                      Y="3"
                                      TileId="1"
                                      Animations="$HeroAnimations"
                                      AnimationState="Idle">
                        <TileInstance2D.Aspect>
                          @on Loaded
                          {
                            @animate with Tween(100ms)
                            {
                              @to { Opacity = 0.8; TranslateY = 2; }
                            }
                          }
                        </TileInstance2D.Aspect>
                        @prism
                        {
                          @layer TileContent { Opacity = 1; @filter Blur { Radius = 1; } }
                        }
                      </TileInstance2D>
                    </TileLayer2D>
                  </TileMap2D>
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("AnimatedWorld.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [Trait("SpriteAnimationStage", "0")]
    [InlineData(
        "<SpriteAnimationSet Name=\"Animations\"><SpriteAnimationClip Name=\"Idle\"><SpriteAnimationFrame SourceX=\"0\" SourceY=\"0\" SourceWidth=\"16\" SourceHeight=\"16\" Duration=\"1ms\" /></SpriteAnimationClip><SpriteAnimationClip Name=\"Idle\"><SpriteAnimationFrame SourceX=\"16\" SourceY=\"0\" SourceWidth=\"16\" SourceHeight=\"16\" Duration=\"1ms\" /></SpriteAnimationClip></SpriteAnimationSet>",
        "duplicate",
        "Idle")]
    [InlineData(
        "<SpriteAnimationSet Name=\"Animations\"><SpriteAnimationClip Name=\"Idle\"><SpriteAnimationFrame SourceX=\"0\" SourceY=\"0\" SourceWidth=\"0\" SourceHeight=\"16\" Duration=\"1ms\" /></SpriteAnimationClip></SpriteAnimationSet>",
        "source",
        "SourceWidth")]
    [InlineData(
        "<SpriteAnimationSet Name=\"Animations\"><SpriteAnimationClip Name=\"Idle\"><SpriteAnimationFrame SourceX=\"0\" SourceY=\"0\" SourceWidth=\"16\" SourceHeight=\"16\" Duration=\"0ms\" /></SpriteAnimationClip></SpriteAnimationSet>",
        "duration",
        "Duration")]
    public void InvalidAnimationDefinitionsProduceLocatedActionableDiagnostics(
        string resource,
        string expectedMessagePart,
        string sourceToken)
    {
        string markup = $$"""
            <RenderSurface2D>
              <RenderSurface2D.Resources>
                {{resource}}
              </RenderSurface2D.Resources>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("InvalidAnimation.crn", markup, out Compilation compilation);
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics.Concat(compilation.GetDiagnostics()),
            candidate => candidate.Id == "CERNEALAUI016" &&
                         candidate.Severity == DiagnosticSeverity.Error &&
                         candidate.GetMessage().Contains(expectedMessagePart, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(LocationKind.ExternalFile, diagnostic.Location.Kind);
        Assert.Equal("InvalidAnimation.crn", diagnostic.Location.GetLineSpan().Path);
        string sourceText = markup.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
        Assert.Contains(sourceToken, sourceText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("SpriteAnimationStage", "0")]
    public void StaticMissingAnimationStateProducesLocatedDiagnostic()
    {
        const string markup = """
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
                  <Sprite2D Animations="$HeroAnimations" AnimationState="Missing" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("MissingAnimationState.crn", markup, out Compilation compilation);
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics.Concat(compilation.GetDiagnostics()),
            candidate => candidate.Id == "CERNEALAUI016" &&
                         candidate.Severity == DiagnosticSeverity.Error &&
                         candidate.GetMessage().Contains("Missing", StringComparison.Ordinal));

        Assert.Equal(LocationKind.ExternalFile, diagnostic.Location.Kind);
        Assert.Equal("MissingAnimationState.crn", diagnostic.Location.GetLineSpan().Path);
        Assert.Contains(
            "Missing",
            markup.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length),
            StringComparison.Ordinal);
    }
}

public sealed class SpriteAnimationMarkupState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string AnimationState { get; set; } = "Idle";

    public double PlaybackRate { get; set; } = 1;

    public bool IsPaused { get; set; }

    public bool IsAttacking { get; set; }

    public void Notify(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
