using Cerneala.Tests.Drawing.SdlGpu;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using CernealaColor = Cerneala.Drawing.Color;
using PixelColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class RenderSurface2DPresentationTests
{
    [SdlNativeFact]
    public void ClearColorFillsTheManagedSurface()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        RenderSurface2D surface = new()
        {
            ClearColor = CernealaColor.CornflowerBlue
        };
        surface.Draw += (_, _) => { };

        PixelColor actual = RenderCenterPixel(fixture, surface);

        Assert.InRange(actual.R, 98, 102);
        Assert.InRange(actual.G, 147, 151);
        Assert.InRange(actual.B, 235, 239);
    }

    [SdlNativeFact]
    public void DrawEventRendersASpriteThroughTheStrict2DFrame()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(PixelColor.LimeGreen);
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.DrawSprite(image, frame.Bounds, CernealaColor.White);
        };

        PixelColor actual = RenderCenterPixel(fixture, surface);

        Assert.InRange(actual.R, 48, 52);
        Assert.InRange(actual.G, 203, 207);
        Assert.InRange(actual.B, 48, 52);
    }

    [SdlNativeFact]
    public void DrawEventRendersPrismImageThroughTheNativePrismPipeline()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(PixelColor.LimeGreen);
        PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(
            source,
            new InvertFilter());
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.DrawSprite(image, frame.Bounds, CernealaColor.White);
        };

        PixelColor actual = RenderCenterPixel(fixture, surface);

        Assert.InRange(actual.R, 249, 253);
        Assert.InRange(actual.G, 166, 170);
        Assert.InRange(actual.B, 249, 253);
    }

    [SdlNativeFact]
    public void OnDemandSurfaceRedrawsWhenItsPrismImagePipelineChanges()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(PixelColor.LimeGreen);
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

    [SdlNativeFact]
    public void OnDemandSurfaceStopsObservingPrismImagesRemovedFromItsFrame()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(PixelColor.LimeGreen);
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

    [SdlNativeFact]
    public void MultipleDrawSubscribersComposeInRegistrationOrder()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.FillRectangle(frame.Bounds, CernealaColor.CornflowerBlue);
        };
        surface.Draw += (_, frame) =>
            frame.FillRectangle(
                frame.Bounds,
                CernealaColor.HotPink);

        PixelColor center = RenderCenterPixel(fixture, surface);

        Assert.InRange(center.R, 253, 255);
        Assert.InRange(center.G, 103, 107);
        Assert.InRange(center.B, 178, 182);
    }

    [SdlNativeFact]
    public void DrawEventRendersGeneralDrawingPrimitives()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
        {
            frame.FillEllipse(frame.Bounds, CernealaColor.LimeGreen);
        };

        PixelColor center = RenderCenterPixel(fixture, surface);

        Assert.InRange(center.R, 48, 52);
        Assert.InRange(center.G, 203, 207);
        Assert.InRange(center.B, 48, 52);
    }

    [SdlNativeFact]
    public void PrismManagedSurfacePreservesUiRenderedBeforeItAcrossFrames()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(PixelColor.CornflowerBlue);
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

        (PixelColor firstBackground, PixelColor firstTransparentInterior, PixelColor firstContent) = RenderPrismPixels(
            fixture,
            surface,
            instance,
            visualContentVersion: 1);
        surface.InvalidateFrame();
        (PixelColor secondBackground, PixelColor secondTransparentInterior, PixelColor secondContent) = RenderPrismPixels(
            fixture,
            surface,
            instance,
            visualContentVersion: 2);

        Assert.Equal(PixelColor.HotPink, firstBackground);
        Assert.Equal(PixelColor.HotPink, secondBackground);
        Assert.Equal(PixelColor.HotPink, firstTransparentInterior);
        Assert.Equal(PixelColor.HotPink, secondTransparentInterior);
        Assert.Equal(PixelColor.CornflowerBlue, firstContent);
        Assert.Equal(PixelColor.CornflowerBlue, secondContent);
    }

    private static PixelColor RenderCenterPixel(
        SdlDrawingFixture fixture,
        RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(0, 0, 96, 64),
            CernealaColor.White));
        PixelColor[] pixels = fixture.Render(commands);
        return pixels[
            ((fixture.Session.PixelHeight / 2) * fixture.Session.PixelWidth) +
            (fixture.Session.PixelWidth / 2)];
    }

    private static (
        PixelColor Background,
        PixelColor TransparentInterior,
        PixelColor Content) RenderPrismPixels(
        SdlDrawingFixture fixture,
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

        PixelColor[] pixels = fixture.Render(commands);
        return (
            pixels[(8 * fixture.Session.PixelWidth) + 8],
            pixels[(8 * fixture.Session.PixelWidth) + 20],
            pixels[(24 * fixture.Session.PixelWidth) + 48]);
    }

}
