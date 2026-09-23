using System;
using System.Collections;
using System.IO;
using System.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void RenderSurface3DFactoryInstantiatesOverlayAndScalarProperties()
    {
        const string markup = """
            <RenderSurface3D ClearColor="#00000000" RedrawMode="Continuous" Width="320" Height="240">
              <Button Content="Overlay" />
            </RenderSurface3D>
            """;

        GeneratorRunResult result = RunGenerator("RenderSurface3DHost.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        RenderSurface3D surface = Assert.IsType<RenderSurface3D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.RenderSurface3DHostFactory"));
        Assert.Equal(Color.Transparent, surface.ClearColor);
        Assert.Equal(RenderSurface3DRedrawMode.Continuous, surface.RedrawMode);
        Assert.Equal(320, surface.Width);
        Assert.Equal(240, surface.Height);
        Assert.Equal("Overlay", Assert.IsType<Button>(surface.Content).Content);
    }

    [Fact]
    public void RenderSurface3DDrawHandlerUsesExistingPairedMarkupEventMechanism()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class SurfaceHost : UserControl
            {
                public int DrawCount { get; private set; }
                private void OnDraw(RenderSurface3D sender, RenderSurface3DFrame frame)
                {
                    DrawCount++;
                    frame.DrawMarker(System.Numerics.Vector3.Zero, Cerneala.Drawing.Color.White);
                }
            }
            """;
        const string markup = """
            <UserControl>
              <RenderSurface3D Name="Viewport" Draw="OnDraw" RedrawMode="OnDemand">
                <Button Content="Overlay" />
              </RenderSurface3D>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/SurfaceHost.crn", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("Viewport.Draw += this.OnDraw;", SingleGeneratedSource(result), StringComparison.Ordinal);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(stream.ToArray());
        Type hostType = assembly.GetType("TestInput.Views.SurfaceHost", throwOnError: true)!;
        UserControl host = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(hostType));
        RenderSurface3D surface = Assert.IsType<RenderSurface3D>(host.ComponentTemplateInstance!.Root);
        Assert.IsType<Button>(surface.Content);
        Type sourceType = typeof(RenderSurface3D).Assembly.GetType("Cerneala.Drawing.IRenderSurface3DSource", throwOnError: true)!;
        object recording = sourceType.GetMethod("RecordFrame")!.Invoke(surface, [new DrawRect(0, 0, 320, 240), 1f])!;
        Assert.Equal(1, hostType.GetProperty("DrawCount")!.GetValue(host));
        IEnumerable primitives = Assert.IsAssignableFrom<IEnumerable>(recording.GetType().GetProperty("Primitives")!.GetValue(recording));
        Assert.Single(primitives.Cast<object>());
    }

    [Fact]
    public void RenderSurface3DUsesExistingAspectMotionAndPrismSyntaxOnItsUiImage()
    {
        const string markup = """
            <RenderSurface3D Opacity="1">
              <RenderSurface3D.Aspect>
                @on Loaded
                {
                  @animate with Tween(100ms)
                  {
                    @to { Opacity = 0.75; }
                  }
                }
              </RenderSurface3D.Aspect>
              @prism
              {
                @layer SurfaceImage { Opacity = 1; @filter Blur { Radius = 1; } }
              }
              <Button Content="Overlay" />
            </RenderSurface3D>
            """;
        GeneratorRunResult result = RunGenerator("RenderSurface3DEffects.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        RenderSurface3D surface = Assert.IsType<RenderSurface3D>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.RenderSurface3DEffectsFactory"));
        Assert.NotNull(surface.Aspect);
        Assert.IsType<Button>(surface.Content);
    }
}
