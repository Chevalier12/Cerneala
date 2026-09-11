using System;
using System.IO;
using System.Linq;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void SpriteAuthoringUsesOneImageAndIndependentGeometryFromMarkup()
    {
        const string markup = """
            <RenderSurface2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <resources:ImageResource Name="Atlas" Source="Assets/atlas.png" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Sprite2D Image="$Atlas" X="100" Y="80" Width="48" Height="72"
                            SourceX="32" SourceY="0" SourceWidth="16" SourceHeight="24" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGenerator("SpriteAuthoring.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.SpriteAuthoringFactory"));
        Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(surface.Scene!.Children));
        Assert.Equal(48, sprite.Width);
        Assert.Equal(72, sprite.Height);
        Assert.Equal(100, sprite.X);
        Assert.Equal(80, sprite.Y);
        Assert.Equal(32, sprite.SourceX);
        Assert.Equal(0, sprite.SourceY);
        Assert.Equal(16, sprite.SourceWidth);
        Assert.Equal(24, sprite.SourceHeight);
        Assert.Equal(new ResourceId<ImageResource>("Atlas"), sprite.Image!.ResourceId);
    }

    [Theory]
    [InlineData("Source")]
    [InlineData("SourceResourceId")]
    [InlineData("Destination")]
    [InlineData("SourceRect")]
    public void SpriteAuthoringRemovesLegacyPublicProperties(string propertyName)
    {
        Assert.Null(typeof(Sprite2D).GetProperty(propertyName));
        Assert.Null(typeof(Sprite2D).GetField(propertyName + "Property"));
    }

    [Theory]
    [InlineData("Source")]
    [InlineData("SourceResourceId")]
    [InlineData("Destination")]
    [InlineData("SourceRect")]
    public void SpriteAuthoringRejectsLegacyMarkupAttributes(string propertyName)
    {
        GeneratorRunResult result = RunGenerator("LegacySprite.crn", $"<Sprite2D {propertyName}=\"0\" />", out _);
        Assert.Contains(result.Diagnostics, d => d.Id == "CERNEALAUI003" && d.GetMessage().Contains(propertyName));
    }

    [Fact]
    public void SpriteAuthoringImageAndGeometryUseAspectAndNamedOneWayBindings()
    {
        const string markup = """
            <RenderSurface2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <resources:ImageResource Name="Atlas" Source="atlas.png" />
                <Aspect Name="SpriteDefaults" TargetType="Sprite2D">
                  @default { Image = $Atlas; X = 10; Y = 20; SourceX = 2; SourceY = 3; SourceWidth = 16; SourceHeight = 12; }
                </Aspect>
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D>
                  <Sprite2D Name="First" Aspect="$SpriteDefaults" />
                  <Sprite2D Image="$First.Image:OneWay" X="$First.X:OneWay" Y="$First.Y:OneWay"
                            SourceX="$First.SourceX:OneWay" SourceY="$First.SourceY:OneWay"
                            SourceWidth="$First.SourceWidth:OneWay" SourceHeight="$First.SourceHeight:OneWay"
                            Width="$First.Width:OneWay" Height="$First.Height:OneWay" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        GeneratorRunResult result = RunGenerator("BoundSprites.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(InvokeCreate(stream, "Cerneala.GeneratedUi.BoundSpritesFactory"));
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Sprite2D first = Assert.IsType<Sprite2D>(surface.Scene!.Children[0]);
        Sprite2D second = Assert.IsType<Sprite2D>(surface.Scene.Children[1]);
        Assert.Equal(new ResourceId<ImageResource>("Atlas"), first.Image!.ResourceId);
        Assert.Equal(first.Image, second.Image);
        Assert.Equal(10, second.X);
        Assert.Equal(20, second.Y);
        Assert.Equal(16, second.SourceWidth);
        first.X = 31; first.Y = 41; first.Width = 48; first.Height = 36;
        first.SourceX = 4; first.SourceY = 5; first.SourceWidth = 8; first.SourceHeight = 6;
        first.Image = new(new SpriteAuthoringImage());
        Assert.Equal(first.Image, second.Image);
        Assert.Equal(31, second.X); Assert.Equal(41, second.Y);
        Assert.Equal(48, second.Width); Assert.Equal(36, second.Height);
        Assert.Equal(4, second.SourceX); Assert.Equal(5, second.SourceY);
        Assert.Equal(8, second.SourceWidth); Assert.Equal(6, second.SourceHeight);
        first.Image = null;
        Assert.Null(second.Image);
        root.VisualChildren.Remove(surface);
    }

    private sealed class SpriteAuthoringImage : Cerneala.Drawing.IDrawImage
    {
        public int Width => 64;
        public int Height => 32;
    }
}
