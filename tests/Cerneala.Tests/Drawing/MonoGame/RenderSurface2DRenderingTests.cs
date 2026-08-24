using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class RenderSurface2DRenderingTests
{
    [Fact]
    public void ClearColorFillsTheManagedSurface()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        RenderSurface2D surface = new()
        {
            ClearColor = CernealaColor.CornflowerBlue
        };
        surface.Draw += (_, _) => { };

        XnaColor actual = RenderCenterPixel(fixture, surface);

        Assert.InRange(actual.R, 98, 102);
        Assert.InRange(actual.G, 147, 151);
        Assert.InRange(actual.B, 235, 239);
    }

    [Fact]
    public void DrawEventRendersASpriteThroughTheStrict2DFrame()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage image = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.DrawSprite(image, frame.Bounds, CernealaColor.White);
        };

        XnaColor actual = RenderCenterPixel(fixture, surface);

        Assert.InRange(actual.R, 48, 52);
        Assert.InRange(actual.G, 203, 207);
        Assert.InRange(actual.B, 48, 52);
    }

    [Fact]
    public void DrawEventRendersPrismImageThroughTheNativePrismPipeline()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage source = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            new InvertFilter());
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.DrawSprite(image, frame.Bounds, CernealaColor.White);
        };

        XnaColor actual = RenderCenterPixel(fixture, surface);

        Assert.InRange(actual.R, 249, 253);
        Assert.InRange(actual.G, 166, 170);
        Assert.InRange(actual.B, 249, 253);
    }

    [Fact]
    public void OnDemandSurfaceRedrawsWhenItsPrismImagePipelineChanges()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage source = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        BlurFilter blur = new() { Radius = 1 };
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            blur);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.DrawSprite(image, frame.Bounds, CernealaColor.White);
        };

        _ = RenderCenterPixel(fixture, surface);
        blur.Radius = 4;
        _ = RenderCenterPixel(fixture, surface);
        _ = RenderCenterPixel(fixture, surface);

        Assert.Equal(2, drawCount);
    }

    [Fact]
    public void OnDemandSurfaceStopsObservingPrismImagesRemovedFromItsFrame()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage source = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        BlurFilter firstBlur = new() { Radius = 1 };
        BlurFilter secondBlur = new() { Radius = 1 };
        PrismImage first = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            firstBlur);
        PrismImage second = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            secondBlur);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        bool drawSecond = false;
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.DrawSprite(
                drawSecond ? second : first,
                frame.Bounds,
                CernealaColor.White);
        };

        _ = RenderCenterPixel(fixture, surface);
        drawSecond = true;
        surface.InvalidateFrame();
        _ = RenderCenterPixel(fixture, surface);

        firstBlur.Radius = 4;
        _ = RenderCenterPixel(fixture, surface);
        secondBlur.Radius = 4;
        _ = RenderCenterPixel(fixture, surface);

        Assert.Equal(3, drawCount);
    }

    [Fact]
    public void MultipleDrawSubscribersComposeInRegistrationOrder()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.FillRectangle(frame.Bounds, CernealaColor.CornflowerBlue);
        };
        surface.Draw += (_, frame) =>
            frame.FillRectangle(
                frame.Bounds,
                CernealaColor.HotPink);

        XnaColor center = RenderCenterPixel(fixture, surface);

        Assert.InRange(center.R, 253, 255);
        Assert.InRange(center.G, 103, 107);
        Assert.InRange(center.B, 178, 182);
    }

    [Fact]
    public void DrawEventRendersGeneralDrawingPrimitives()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.FillEllipse(frame.Bounds, CernealaColor.LimeGreen);
        };

        XnaColor center = RenderCenterPixel(fixture, surface);

        Assert.InRange(center.R, 48, 52);
        Assert.InRange(center.G, 203, 207);
        Assert.InRange(center.B, 48, 52);
    }

    [Fact]
    public void PrismManagedSurfacePreservesUiRenderedBeforeItAcrossFrames()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage image = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.CornflowerBlue));
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.DrawSprite(
                image,
                new DrawRect(24, 16, 16, 16),
                CernealaColor.White);
        };

        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Glow",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        PrismInstance instance = new(
            new PrismCompositionDefinition("SurfaceGlow", [layer]));

        (XnaColor firstBackground, XnaColor firstTransparentInterior, XnaColor firstContent) = RenderPrismPixels(
            fixture,
            surface,
            instance,
            visualContentVersion: 1);
        surface.InvalidateFrame();
        (XnaColor secondBackground, XnaColor secondTransparentInterior, XnaColor secondContent) = RenderPrismPixels(
            fixture,
            surface,
            instance,
            visualContentVersion: 2);

        Assert.Equal(XnaColor.HotPink, firstBackground);
        Assert.Equal(XnaColor.HotPink, secondBackground);
        Assert.Equal(XnaColor.HotPink, firstTransparentInterior);
        Assert.Equal(XnaColor.HotPink, secondTransparentInterior);
        Assert.Equal(XnaColor.CornflowerBlue, firstContent);
        Assert.Equal(XnaColor.CornflowerBlue, secondContent);
    }

    [Fact]
    public void RetainedSessionSkipsRasterizationForAnIdenticalCommandStream()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        int drawCount = 0;

        void Draw(RenderSurface2DFrame frame)
        {
            drawCount++;
            frame.FillRectangle(
                new DrawRect(2, 2, 8, 6),
                CernealaColor.CornflowerBlue);
        }

        session.Render(Draw, CernealaColor.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        session.Render(
            Draw,
            CernealaColor.Black,
            TimeSpan.FromMilliseconds(16));

        Assert.Equal(2, drawCount);
        Assert.Equal(1, session.RasterizedFrameCount);
        Assert.Null(session.LastDamageBounds);
        Assert.Equal(1, session.RetainedCommandCount);
    }

    [Fact]
    public void RetainedSessionRedrawsOnlyCommandsIntersectingChangedDamage()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        int movingX = 2;

        void Draw(RenderSurface2DFrame frame)
        {
            frame.FillRectangle(
                new DrawRect(24, 2, 4, 4),
                CernealaColor.CornflowerBlue);
            frame.FillRectangle(
                new DrawRect(movingX, 2, 4, 4),
                CernealaColor.LimeGreen);
        }

        session.Render(Draw, CernealaColor.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        movingX = 10;
        session.Render(
            Draw,
            CernealaColor.Black,
            TimeSpan.FromMilliseconds(16));
        fixture.Session.GraphicsDevice.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[32 * 16];
        session.Surface.GetData(pixels);

        Assert.Equal(2, session.RasterizedFrameCount);
        Assert.Equal(new Rectangle(2, 2, 12, 4), session.LastDamageBounds);
        Assert.Equal(1, session.LastReplayedCommandCount);
        Assert.Equal(XnaColor.Black, pixels[(3 * 32) + 3]);
        Assert.Equal(XnaColor.LimeGreen, pixels[(3 * 32) + 11]);
        Assert.Equal(XnaColor.CornflowerBlue, pixels[(3 * 32) + 25]);
    }

    [Fact]
    public void RetainedSessionRecomposesOverlappingCommandsWithinDamage()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        int movingX = 2;

        void Draw(RenderSurface2DFrame frame)
        {
            frame.FillRectangle(frame.Bounds, CernealaColor.CornflowerBlue);
            frame.FillRectangle(
                new DrawRect(movingX, 2, 4, 4),
                CernealaColor.HotPink);
        }

        session.Render(Draw, CernealaColor.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        movingX = 10;
        session.Render(
            Draw,
            CernealaColor.Black,
            TimeSpan.FromMilliseconds(16));
        fixture.Session.GraphicsDevice.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[32 * 16];
        session.Surface.GetData(pixels);

        Assert.Equal(new Rectangle(2, 2, 12, 4), session.LastDamageBounds);
        Assert.Equal(2, session.LastReplayedCommandCount);
        Assert.Equal(XnaColor.CornflowerBlue, pixels[(3 * 32) + 3]);
        Assert.Equal(XnaColor.HotPink, pixels[(3 * 32) + 11]);
    }

    [Fact]
    public void RetainedSessionReusesUnchangedPrismImageResultsAcrossFrames()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage source = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            new InvertFilter());
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        int markerX = 20;

        void Draw(RenderSurface2DFrame frame)
        {
            frame.DrawSprite(
                image,
                new DrawRect(0, 0, 16, 16),
                CernealaColor.White);
            frame.FillRectangle(
                new DrawRect(markerX, 2, 2, 2),
                CernealaColor.CornflowerBlue);
        }

        session.Render(Draw, CernealaColor.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        markerX = 24;
        session.Render(
            Draw,
            CernealaColor.Black,
            TimeSpan.FromMilliseconds(16));

        PrismRendererDiagnostics diagnostics = session.PrismDiagnostics;

        Assert.True(diagnostics.RetainedCacheEnabled);
        Assert.True(
            diagnostics.FinalHitCount > 0 ||
            diagnostics.IntermediateHitCount > 0);
        Assert.True(diagnostics.SavedPassCount > 0);
    }

    [Fact]
    public void RetainedSessionEvictsDisposedPrismImageResults()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameImage source = new(CreateSolidTexture(
            fixture.Session.GraphicsDevice,
            XnaColor.LimeGreen));
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            new InvertFilter());
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            32,
            16);
        bool drawImage = true;

        void Draw(RenderSurface2DFrame frame)
        {
            if (drawImage)
            {
                frame.DrawSprite(
                    image,
                    new DrawRect(0, 0, 16, 16),
                    CernealaColor.White);
            }
        }

        session.Render(Draw, CernealaColor.Black, TimeSpan.Zero);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        Assert.True(session.PrismDiagnostics.RetainedEntryCount > 0);

        drawImage = false;
        image.Dispose();
        session.Render(
            Draw,
            CernealaColor.Black,
            TimeSpan.FromMilliseconds(16));

        Assert.Equal(0, session.PrismDiagnostics.RetainedEntryCount);
    }

    private static Texture2D CreateSolidTexture(
        GraphicsDevice graphicsDevice,
        XnaColor color)
    {
        Texture2D texture = new(graphicsDevice, 1, 1);
        texture.SetData([color]);
        return texture;
    }

    private static XnaColor RenderCenterPixel(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(0, 0, 96, 64),
            CernealaColor.White));
        fixture.Session.BeginFrame(CernealaColor.Black);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);
        fixture.Session.DrawingBackend.Render(commands, in frameContext);
        fixture.Session.Present();

        PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels =
            new XnaColor[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        return pixels[
            ((parameters.BackBufferHeight / 2) * parameters.BackBufferWidth) +
            (parameters.BackBufferWidth / 2)];
    }

    private static (
        XnaColor Background,
        XnaColor TransparentInterior,
        XnaColor Content) RenderPrismPixels(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        RenderSurface2D surface,
        PrismInstance instance,
        long visualContentVersion)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 96, 64),
            CernealaColor.FromArgb(255, 255, 105, 180)));
        commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
            instance,
            new PrismCacheOwnerToken(1),
            new DrawRect(16, 0, 64, 64),
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion)));
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(16, 0, 64, 64),
            CernealaColor.White));
        commands.Add(DrawCommand.EndPrism());

        fixture.Session.BeginFrame(CernealaColor.Black);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);
        fixture.Session.DrawingBackend.Render(commands, in frameContext);
        fixture.Session.Present();

        PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels =
            new XnaColor[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        return (
            pixels[(8 * parameters.BackBufferWidth) + 8],
            pixels[(8 * parameters.BackBufferWidth) + 20],
            pixels[(24 * parameters.BackBufferWidth) + 48]);
    }

}
