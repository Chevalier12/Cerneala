using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Tests.Drawing.SdlGpu;
using Xunit.Abstractions;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeDrawingInterpolationBoundaryTests(ITestOutputHelper output)
{
    private const int ImageSize = 20;
    private const int CropSize = 16;
    private const int FrameSize = 64;
    private static readonly DrawRect Crop = new(2, 2, CropSize, CropSize);
    private static readonly DrawRect DiagonalDestination = new(8.5f, 8.5f, 32, 32);
    private static readonly DrawRect BoundaryDestination = new(8.275f, 8.275f, 32, 32);

    [Fact]
    public void OptionsQuadProvenanceSurvivesGeometryCopiesWithoutChangingPublicOptions()
    {
        using SdlGpuImage image = new(1, 1, [255, 255, 255, 255]);
        DrawImageOptions requested = new(source: new DrawRect(0, 0, 1, 1),
            sampling: DrawSamplingMode.Point);
        DrawCommand optionsQuad = DrawCommand.DrawImageQuad(
            image, new DrawPoint(0, 0), new DrawPoint(1, 0),
            new DrawPoint(1, 1), new DrawPoint(0, 1), requested);
        DrawCommand authoredQuad = DrawCommand.DrawImageQuad(
            image,
            new DrawVertex2D(new DrawPoint(0, 0), Color.White),
            new DrawVertex2D(new DrawPoint(1, 0), Color.White),
            new DrawVertex2D(new DrawPoint(1, 1), Color.White),
            new DrawVertex2D(new DrawPoint(0, 1), Color.White),
            sampling: DrawSamplingMode.Point);

        Assert.Null(optionsQuad.ImageOptions!.Source);
        Assert.True(optionsQuad.Mesh!.IsOptionsImageQuad);
        Assert.False(authoredQuad.Mesh!.IsOptionsImageQuad);
        Assert.True(optionsQuad.Mesh.Transform(
            static point => new DrawPoint(point.X + 2, point.Y + 3)).IsOptionsImageQuad);
        Assert.True(optionsQuad.Mesh.WithImage(image).IsOptionsImageQuad);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void EquivalentImageMeshTriangulationsPreservePointSampledInteriorPixels()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: true);
        using SdlGpuImage atlas = new(ImageSize, ImageSize, CreateAtlasPixels());
        output.WriteLine($"Window sample count: {fixture.Session.Diagnostics.SampleCount}");
        List<string> pointViolations = [];

        foreach (DrawSamplingMode sampling in new[] { DrawSamplingMode.Point, DrawSamplingMode.Linear })
        {
            Color[] standard = RenderMesh(fixture, atlas, sampling, MeshSubdivision.Standard);
            Color[] alternate = RenderMesh(fixture, atlas, sampling, MeshSubdivision.Alternate);
            Color[] centerFan = RenderMesh(fixture, atlas, sampling, MeshSubdivision.CenterFan);
            Color[] grid = RenderMesh(fixture, atlas, sampling, MeshSubdivision.Grid);

            Compare("alternate diagonal", alternate);
            Compare("center fan", centerFan);
            Compare("2x2 grid", grid);

            void Compare(string label, Color[] candidate)
            {
                List<PixelDifference> differences = Differences(
                    standard, candidate, static (x, y) => IsQuadInterior(x, y));
                Report($"{sampling} standard versus {label}", differences);
                if (sampling == DrawSamplingMode.Point && differences.Count != 0)
                {
                    pointViolations.Add($"{label}: {differences.Count} interior pixels");
                }
            }
        }

        Assert.True(pointViolations.Count == 0,
            "Equivalent affine-UV mesh triangulations changed Point-sampled interior pixels: " +
            string.Join("; ", pointViolations));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void PointCropDoesNotSampleAtlasNeighborsAtFractionalSurfaceEdge()
    {
        byte[] atlasPixels = CreateAtlasPixels();
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize, ExtractCropPixels(atlasPixels));
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        output.WriteLine($"Parent window sample count: {fixture.Session.Diagnostics.SampleCount}");
        List<PixelDifference>? directPointEdge = null;
        List<PixelDifference>? surfacePointEdge = null;
        List<PixelDifference>? surfacePointInterior = null;

        foreach (DrawSamplingMode sampling in new[] { DrawSamplingMode.Point, DrawSamplingMode.Linear })
        {
            foreach (bool offscreen in new[] { false, true })
            {
                Color[] atlasFrame = RenderImage(fixture, atlas, Crop, sampling, offscreen);
                Color[] extractedFrame = RenderImage(fixture, extracted, null, sampling, offscreen);
                string path = offscreen ? "offscreen+composite" : "direct";
                List<PixelDifference> edge = Differences(
                    extractedFrame, atlasFrame, static (x, y) => IsExternalEdge(x, y));
                List<PixelDifference> interior = Differences(
                    extractedFrame, atlasFrame, static (x, y) => IsQuadInterior(x, y));
                Report($"{sampling} {path} external right/bottom edges", edge);
                Report($"{sampling} {path} safe interior", interior);
                if (sampling == DrawSamplingMode.Point)
                {
                    if (offscreen)
                    {
                        surfacePointEdge = edge;
                        surfacePointInterior = interior;
                    }
                    else
                    {
                        directPointEdge = edge;
                    }
                }
            }
        }

        Assert.NotNull(directPointEdge);
        Assert.NotNull(surfacePointEdge);
        Assert.NotNull(surfacePointInterior);
        Assert.Empty(directPointEdge);
        Assert.Empty(surfacePointInterior);
        Assert.True(surfacePointEdge.Count == 0,
            $"Point sampling read unrelated atlas texels at {surfacePointEdge.Count} external-edge pixels.");
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void PointSpriteBatchKeepsEachAtlasSourceIsolatedWithoutSplittingTheBatch()
    {
        DrawRect firstCrop = new(2, 2, 6, 6);
        DrawRect secondCrop = new(12, 12, 6, 6);
        DrawRect firstDestination = new(3.275f, 3.275f, 16, 16);
        DrawRect secondDestination = new(29.275f, 29.275f, 16, 16);
        byte[] atlasPixels = CreateTwoRegionAtlas(firstCrop, secondCrop);
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage first = new(6, 6, ExtractRegion(atlasPixels, firstCrop));
        using SdlGpuImage second = new(6, 6, ExtractRegion(atlasPixels, secondCrop));
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);

        Color[] actual = RenderInSurface(fixture, drawing => drawing.DrawSpriteBatch(
            new DrawSpriteBatch(atlas,
            [
                new DrawSprite2D(firstDestination,
                    new DrawImageOptions(source: firstCrop, sampling: DrawSamplingMode.Point)),
                new DrawSprite2D(secondDestination,
                    new DrawImageOptions(source: secondCrop, sampling: DrawSamplingMode.Point))
            ])));
        Color[] expected = RenderInSurface(fixture, drawing =>
        {
            drawing.DrawImage(first, firstDestination,
                new DrawImageOptions(sampling: DrawSamplingMode.Point));
            drawing.DrawImage(second, secondDestination,
                new DrawImageOptions(sampling: DrawSamplingMode.Point));
        });

        List<PixelDifference> differences = Differences(
            expected, actual, static (_, _) => true);
        Report("Point sprite batch with two distinct crop domains", differences);
        Assert.Empty(differences);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void FractionalAndSubtexelPointSourcesDoNotReadChangedAtlasGuards()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        DrawRect[] sources =
        [
            new DrawRect(2.25f, 2.25f, 15.5f, 15.5f),
            new DrawRect(10.25f, 10.25f, 0.5f, 0.5f)
        ];
        DrawRect[] destinations = [BoundaryDestination, BoundaryDestination];

        for (int caseIndex = 0; caseIndex < sources.Length; caseIndex++)
        {
            DrawRect source = sources[caseIndex];
            using SdlGpuImage first = new(ImageSize, ImageSize,
                CreateSourceWithGuard(source, Color.Magenta));
            using SdlGpuImage second = new(ImageSize, ImageSize,
                CreateSourceWithGuard(source, Color.Cyan));
            Color[] firstFrame = RenderImage(fixture, first, source,
                DrawSamplingMode.Point, offscreen: true, destinations[caseIndex]);
            Color[] secondFrame = RenderImage(fixture, second, source,
                DrawSamplingMode.Point, offscreen: true, destinations[caseIndex]);
            List<PixelDifference> differences = Differences(
                firstFrame, secondFrame, static (_, _) => true);
            Report($"Point source {source} guard independence", differences);
            Assert.Empty(differences);
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void ExactIntegerSourceExternalBoundariesUseCoveredSamplingButKeepOrdinaryInterior()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        DrawPoint uv0 = new(Crop.X / ImageSize, Crop.Y / ImageSize);
        DrawPoint uv1 = new(Crop.Right / ImageSize, Crop.Bottom / ImageSize);
        DrawRect destination = DiagonalDestination;

        foreach (Color guard in new[] { Color.Magenta, Color.Cyan })
        {
            byte[] atlasPixels = CreateSourceWithGuard(Crop, guard);
            using SdlGpuImage image = new(ImageSize, ImageSize, atlasPixels);
            using SdlGpuImage extracted = new(CropSize, CropSize,
                ExtractCropPixels(atlasPixels));
            Color[] selected = RenderInSurface(fixture, drawing =>
                drawing.DrawImage(image, destination,
                    new DrawImageOptions(source: Crop, sampling: DrawSamplingMode.Point)));
            Color[] ordinary = RenderInSurface(fixture, drawing =>
                drawing.DrawImageQuad(
                    image,
                    new DrawVertex2D(new DrawPoint(destination.X, destination.Y),
                        Color.White, uv0),
                    new DrawVertex2D(new DrawPoint(destination.Right, destination.Y),
                        Color.White, new DrawPoint(uv1.X, uv0.Y)),
                    new DrawVertex2D(new DrawPoint(destination.Right, destination.Bottom),
                        Color.White, uv1),
                    new DrawVertex2D(new DrawPoint(destination.X, destination.Bottom),
                        Color.White, new DrawPoint(uv0.X, uv1.Y)),
                    sampling: DrawSamplingMode.Point));
            Color[] isolated = RenderInSurface(fixture, drawing =>
                drawing.DrawImage(extracted, destination,
                    new DrawImageOptions(sampling: DrawSamplingMode.Point)));

            List<PixelDifference> differences = Differences(
                ordinary, selected, static (x, y) => IsQuadInterior(x, y));
            Report($"Exact integer crop selected versus ordinary interior, guard {guard}",
                differences);
            Assert.Empty(differences);
            foreach ((string name, int x, int y) in new[]
                     {
                         ("left", 8, 24), ("right", 40, 24),
                         ("top", 24, 8), ("bottom", 24, 40)
                     })
            {
                int index = (y * FrameSize) + x;
                output.WriteLine($"Exact-center {name} edge at ({x + 0.5f}, {y + 0.5f}): " +
                    $"guard={guard}, selected={selected[index]}, " +
                    $"ordinary={ordinary[index]}, isolated={isolated[index]}");
                Assert.Equal(isolated[index], selected[index]);
            }
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void ExactFlippedOuterBoundaryUsesCoveredSampling()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        DrawRect destination = DiagonalDestination;
        byte[] atlasPixels = CreateSourceWithGuard(Crop, Color.Magenta);
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize,
            ExtractCropPixels(atlasPixels));
        DrawImageOptions selectedOptions = new(
            source: Crop, flip: DrawImageFlip.Horizontal, sampling: DrawSamplingMode.Point);
        DrawImageOptions isolatedOptions = new(
            flip: DrawImageFlip.Horizontal, sampling: DrawSamplingMode.Point);

        Color[] selected = RenderInSurface(fixture, drawing =>
            drawing.DrawImage(atlas, destination, selectedOptions));
        Color[] isolated = RenderInSurface(fixture, drawing =>
            drawing.DrawImage(extracted, destination, isolatedOptions));
        int rightEdgeIndex = (24 * FrameSize) + 40;
        output.WriteLine($"Exact flipped right edge at x=40.5: " +
            $"selected={selected[rightEdgeIndex]}, isolated={isolated[rightEdgeIndex]}");
        Assert.Equal(isolated[rightEdgeIndex], selected[rightEdgeIndex]);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void ExactUnflippedRightOuterBoundaryUsesCoveredSampling()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        byte[] atlasPixels = CreateSourceWithGuard(Crop, Color.Magenta);
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize,
            ExtractCropPixels(atlasPixels));
        DrawImageOptions selectedOptions = new(
            source: Crop, sampling: DrawSamplingMode.Point);
        DrawImageOptions isolatedOptions = new(sampling: DrawSamplingMode.Point);

        Color[] selected = RenderInSurface(fixture, drawing =>
            drawing.DrawImage(atlas, DiagonalDestination, selectedOptions));
        Color[] isolated = RenderInSurface(fixture, drawing =>
            drawing.DrawImage(extracted, DiagonalDestination, isolatedOptions));
        int rightEdgeIndex = (24 * FrameSize) + 40;
        output.WriteLine($"Exact unflipped right edge at x=40.5: " +
            $"selected={selected[rightEdgeIndex]}, isolated={isolated[rightEdgeIndex]}");
        Assert.Equal(isolated[rightEdgeIndex], selected[rightEdgeIndex]);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void VillageScaleFlippedBoundaryUsesCoveredSampling()
    {
        const int atlasWidth = 192;
        const int atlasHeight = 176;
        const int frameSize = 768;
        DrawRect source = new(112, 48, 16, 16);
        DrawRect groundSource = new(0, 0, 16, 16);
        DrawRect destination = new(267.5f, 510.5f, 80, 80);
        DrawRect rightDestination = new(destination.Right, destination.Y, 80, 80);
        Color grass = new(132, 198, 105);
        Color neighbor = new(234, 165, 108);
        byte[] atlasPixels = new byte[atlasWidth * atlasHeight * 4];
        for (int y = 0; y < atlasHeight; y++)
        for (int x = 0; x < atlasWidth; x++)
        {
            Color color = Inside(source, x, y) || Inside(groundSource, x, y)
                ? grass : neighbor;
            int offset = ((y * atlasWidth) + x) * 4;
            atlasPixels[offset] = color.R;
            atlasPixels[offset + 1] = color.G;
            atlasPixels[offset + 2] = color.B;
            atlasPixels[offset + 3] = color.A;
        }
        byte[] isolatedPixels = new byte[16 * 16 * 4];
        for (int offset = 0; offset < isolatedPixels.Length; offset += 4)
        {
            isolatedPixels[offset] = grass.R;
            isolatedPixels[offset + 1] = grass.G;
            isolatedPixels[offset + 2] = grass.B;
            isolatedPixels[offset + 3] = grass.A;
        }
        using SdlGpuImage atlas = new(atlasWidth, atlasHeight, atlasPixels);
        using SdlGpuImage isolatedImage = new(16, 16, isolatedPixels);
        DrawImageOptions selectedOptions = new(source: source,
            flip: DrawImageFlip.Horizontal, sampling: DrawSamplingMode.Point);
        DrawImageOptions selectedGroundOptions = new(
            source: groundSource, sampling: DrawSamplingMode.Point);
        DrawImageOptions isolatedOptions = new(
            flip: DrawImageFlip.Horizontal, sampling: DrawSamplingMode.Point);
        DrawImageOptions isolatedGroundOptions = new(sampling: DrawSamplingMode.Point);

        using (SdlDrawingFixture fixture = new(frameSize, frameSize, useMultisampling: false))
        {
            Color[] selected = RenderInSurface(fixture, drawing =>
                drawing.DrawImage(atlas, destination, selectedOptions), frameSize);
            Color[] isolated = RenderInSurface(fixture, drawing =>
                drawing.DrawImage(isolatedImage, destination, isolatedOptions), frameSize);
            int edgeIndex = (550 * frameSize) + 347;
            output.WriteLine($"Village-scale exact flipped right edge: " +
                $"selected={selected[edgeIndex]}, isolated={isolated[edgeIndex]}");
            Assert.Equal(isolated[edgeIndex], selected[edgeIndex]);

            Color[] selectedLeftGround = RenderInSurface(fixture, drawing =>
                drawing.DrawImage(atlas, destination, selectedGroundOptions), frameSize);
            Color[] selectedRightGround = RenderInSurface(fixture, drawing =>
                drawing.DrawImage(atlas, rightDestination, selectedGroundOptions), frameSize);
            Color[] selectedJoin = RenderInSurface(fixture, drawing =>
            {
                drawing.DrawImage(atlas, destination, selectedGroundOptions);
                drawing.DrawImage(atlas, rightDestination, selectedGroundOptions);
                drawing.DrawImage(atlas, destination, selectedOptions);
            }, frameSize);
            Color[] isolatedJoin = RenderInSurface(fixture, drawing =>
            {
                drawing.DrawImage(isolatedImage, destination, isolatedGroundOptions);
                drawing.DrawImage(isolatedImage, rightDestination, isolatedGroundOptions);
                drawing.DrawImage(isolatedImage, destination, isolatedOptions);
            }, frameSize);
            output.WriteLine($"Village-scale ground left={selectedLeftGround[edgeIndex]}, " +
                $"ground right={selectedRightGround[edgeIndex]}, " +
                $"join={selectedJoin[edgeIndex]}, isolated join={isolatedJoin[edgeIndex]}");
            Assert.Equal(isolatedJoin[edgeIndex], selectedJoin[edgeIndex]);

            foreach (float left in new[]
                     {
                         MathF.BitDecrement(destination.X),
                         MathF.BitIncrement(destination.X)
                     })
            {
                DrawRect shifted = new(left, destination.Y, destination.Width, destination.Height);
                Color[] shiftedAtlas = RenderInSurface(fixture, drawing =>
                    drawing.DrawImage(atlas, shifted, selectedOptions), frameSize);
                Color[] shiftedIsolated = RenderInSurface(fixture, drawing =>
                    drawing.DrawImage(isolatedImage, shifted, isolatedOptions), frameSize);
                DrawPoint uvTopLeft = new(source.Right / atlasWidth, source.Y / atlasHeight);
                DrawPoint uvTopRight = new(source.X / atlasWidth, source.Y / atlasHeight);
                DrawPoint uvBottomRight = new(source.X / atlasWidth, source.Bottom / atlasHeight);
                DrawPoint uvBottomLeft = new(source.Right / atlasWidth, source.Bottom / atlasHeight);
                Color[] shiftedOrdinary = RenderInSurface(fixture, drawing =>
                    drawing.DrawImageQuad(
                        atlas,
                        new DrawVertex2D(new DrawPoint(shifted.X, shifted.Y),
                            Color.White, uvTopLeft),
                        new DrawVertex2D(new DrawPoint(shifted.Right, shifted.Y),
                            Color.White, uvTopRight),
                        new DrawVertex2D(new DrawPoint(shifted.Right, shifted.Bottom),
                            Color.White, uvBottomRight),
                        new DrawVertex2D(new DrawPoint(shifted.X, shifted.Bottom),
                            Color.White, uvBottomLeft),
                        DrawSamplingMode.Point, DrawAddressMode.Clamp), frameSize);
                double centerFromRight = (double)347.5f - shifted.Right;
                output.WriteLine($"One-ULP x shift left={left:R}, right={shifted.Right:R}: " +
                    $"centerFromRight={centerFromRight:R}, selected={shiftedAtlas[edgeIndex]}, " +
                    $"ordinary={shiftedOrdinary[edgeIndex]}, " +
                    $"isolated={shiftedIsolated[edgeIndex]}");
                Assert.NotEqual(0d, centerFromRight);
                if (centerFromRight > 0)
                {
                    // The center is outside the logical image. Covered
                    // interpolation must keep this atlas crop isolated.
                    Assert.Equal(shiftedIsolated[edgeIndex], shiftedAtlas[edgeIndex]);
                }
                else
                {
                    // The center is strictly inside the logical image. This path
                    // must match the same Point/Clamp authored ordinary quad, even
                    // when the isolated texture chooses a different nearest texel.
                    Assert.Equal(shiftedOrdinary[edgeIndex], shiftedAtlas[edgeIndex]);
                }
            }
        }

        using SdlDrawingFixture transformedFixture = new(1025, 750, useMultisampling: false);
        Matrix3x2 viewTransform =
            Matrix3x2.CreateTranslation(99.8000031f, 68.1999969f) *
            Matrix3x2.CreateScale(2.5f);
        Color[] transformedAtlas = RenderTransformed(atlas, selectedGroundOptions,
            selectedOptions);
        Color[] transformedIsolated = RenderTransformed(isolatedImage,
            isolatedGroundOptions, isolatedOptions);
        Color[] transformedTopAtlas = RenderTransformed(atlas, selectedGroundOptions,
            selectedOptions, includeGround: false);
        Color[] transformedTopIsolated = RenderTransformed(isolatedImage,
            isolatedGroundOptions, isolatedOptions, includeGround: false);
        Color[] physicalAtlas = RenderTransformed(atlas, selectedGroundOptions,
            selectedOptions, preTransform: false);
        Color[] physicalIsolated = RenderTransformed(isolatedImage,
            isolatedGroundOptions, isolatedOptions, preTransform: false);
        int windowEdgeIndex = (550 * 1025) + 347;
        output.WriteLine($"Village-scale scene transform edge: " +
            $"selected={transformedAtlas[windowEdgeIndex]}, " +
            $"isolated={transformedIsolated[windowEdgeIndex]}, " +
            $"viewTransform={viewTransform}");
        output.WriteLine($"Transformed top alone selected={transformedTopAtlas[windowEdgeIndex]}, " +
            $"isolated={transformedTopIsolated[windowEdgeIndex]}; " +
            $"physical quad selected={physicalAtlas[windowEdgeIndex]}, " +
            $"isolated={physicalIsolated[windowEdgeIndex]}");
        Assert.Equal(transformedIsolated[windowEdgeIndex],
            transformedAtlas[windowEdgeIndex]);

        Color[] RenderTransformed(SdlGpuImage image,
            DrawImageOptions groundOptions, DrawImageOptions topOptions,
            bool includeGround = true, bool preTransform = true)
        {
            using RecordedSurface surface = new((commands, _) =>
            {
                DrawingContext drawing = new(commands);
                if (preTransform) { drawing.PushTransform(viewTransform); }
                DrawRect left = preTransform
                    ? new DrawRect(0, 96, 32, 32)
                    : new DrawRect(249.5f, 410.5f, 80, 80);
                DrawRect right = preTransform
                    ? new DrawRect(32, 96, 32, 32)
                    : new DrawRect(329.5f, 410.5f, 80, 80);
                if (includeGround)
                {
                    drawing.DrawImage(image, left, groundOptions);
                    drawing.DrawImage(image, right, groundOptions);
                }
                drawing.DrawImage(image, left, topOptions);
                if (preTransform) { drawing.PopTransform(); }
            }, Color.Black);
            DrawCommandList parent = new();
            parent.Add(DrawCommand.RenderSurface2D(
                surface, new DrawRect(18, 100, 990, 550), Color.White));
            return transformedFixture.Render(parent, Color.Black);
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void FiniteNearDegenerateOptionsQuadDoesNotAcquireBackendException()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        using SdlGpuImage image = new(1, 1, [255, 255, 255, 255]);
        DrawCommand command = DrawCommand.DrawImageQuad(
            image,
            new DrawPoint(0, 0),
            new DrawPoint(0, 1),
            new DrawPoint(1e-40f, 0),
            new DrawPoint(1, 1),
            new DrawImageOptions(sampling: DrawSamplingMode.Point));
        Assert.NotNull(command.Mesh);

        _ = RenderInSurface(fixture, drawing => drawing.DrawImageQuad(
            image,
            new DrawPoint(0, 0),
            new DrawPoint(0, 1),
            new DrawPoint(1e-40f, 0),
            new DrawPoint(1, 1),
            new DrawImageOptions(sampling: DrawSamplingMode.Point)));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void SelectedOptionsQuadsKeepOrdinaryCenterPixelsAcrossShapeVariants()
    {
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        byte[] atlasPixels = CreateAtlasPixels();
        using SdlGpuImage image = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize, ExtractCropPixels(atlasPixels));
        (string Name, DrawPoint[] Corners)[] cases =
        [
            ("skewed shared diagonal",
            [new(8.5f, 8.5f), new(36.5f, 12.5f),
                new(40.5f, 40.5f), new(12.5f, 36.5f)]),
            ("reflected",
            [new(40.5f, 8.5f), new(8.5f, 8.5f),
                new(8.5f, 40.5f), new(40.5f, 40.5f)]),
            ("concave",
            [new(8.5f, 8.5f), new(40.5f, 8.5f),
                new(24.5f, 24.5f), new(8.5f, 40.5f)]),
            ("self-crossing",
            [new(8.5f, 8.5f), new(40.5f, 40.5f),
                new(40.5f, 8.5f), new(8.5f, 40.5f)]),
            ("one collapsed triangle",
            [new(8.5f, 8.5f), new(24.5f, 8.5f),
                new(40.5f, 8.5f), new(8.5f, 40.5f)])
        ];

        DrawPoint topLeftUv = new(Crop.X / ImageSize, Crop.Y / ImageSize);
        DrawPoint topRightUv = new(Crop.Right / ImageSize, Crop.Y / ImageSize);
        DrawPoint bottomRightUv = new(Crop.Right / ImageSize, Crop.Bottom / ImageSize);
        DrawPoint bottomLeftUv = new(Crop.X / ImageSize, Crop.Bottom / ImageSize);
        foreach ((string name, DrawPoint[] corners) in cases)
        {
            Color[] selected = RenderInSurface(fixture, drawing =>
                drawing.DrawImageQuad(image,
                    corners[0], corners[1], corners[2], corners[3],
                    new DrawImageOptions(source: Crop, sampling: DrawSamplingMode.Point)));
            Color[] ordinary = RenderInSurface(fixture, drawing =>
                drawing.DrawImageQuad(image,
                    new DrawVertex2D(corners[0], Color.White, topLeftUv),
                    new DrawVertex2D(corners[1], Color.White, topRightUv),
                    new DrawVertex2D(corners[2], Color.White, bottomRightUv),
                    new DrawVertex2D(corners[3], Color.White, bottomLeftUv),
                    sampling: DrawSamplingMode.Point));

            int compared = 0;
            int diagonalCenters = 0;
            List<PixelDifference> differences = Differences(
                ordinary, selected, (x, y) =>
                {
                    DrawPoint center = new(x + 0.5f, y + 0.5f);
                    bool interior = CenterInLogicalQuadInterior(corners, center);
                    if (!interior || Enumerable.Range(0, 4).Any(index =>
                            DistanceToLine(corners[index], corners[(index + 1) % 4], center) < 2))
                    {
                        return false;
                    }
                    compared++;
                    if (Cross(corners[0], corners[2], center) == 0)
                    {
                        diagonalCenters++;
                    }
                    return true;
                });
            Report($"{name} selected versus ordinary center pixels", differences);
            output.WriteLine($"{name}: compared={compared}, diagonalCenters={diagonalCenters}");
            Assert.True(compared > 20, $"{name} produced too few center samples.");
            if (name == "skewed shared diagonal")
            {
                Assert.True(diagonalCenters >= 8,
                    "The shared diagonal was not exercised at enough exact pixel centers.");
            }
            Assert.Empty(differences);

            if (name == "self-crossing")
            {
                Color[] isolated = RenderInSurface(fixture, drawing =>
                    drawing.DrawImageQuad(extracted,
                        corners[0], corners[1], corners[2], corners[3],
                        new DrawImageOptions(sampling: DrawSamplingMode.Point)));
                // q1 and q3 lie on the same side of q0-q2. Its y=8.5 seam
                // is external to the union despite both inclusive triangle
                // tests calling it inside. These centers use covered UVs.
                Assert.Equal(1024, Cross(corners[0], corners[2], corners[1]));
                Assert.Equal(1024, Cross(corners[0], corners[2], corners[3]));
                for (int x = 12; x <= 36; x += 2)
                {
                    int index = (8 * FrameSize) + x;
                    Assert.False(CenterInLogicalQuadInterior(corners,
                        new DrawPoint(x + 0.5f, 8.5f)));
                    Assert.Equal(isolated[index], selected[index]);
                }
            }
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void CollapsedNineSliceCellsKeepTheWholeOuterImageDomain()
    {
        byte[] atlasPixels = CreateAtlasPixels();
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize, ExtractCropPixels(atlasPixels));
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        DrawRect destination = new(8.275f, 8.275f, 8, 8);

        Color[] selected = RenderInSurface(fixture, drawing =>
            drawing.DrawNineSlice(atlas, destination, new DrawInsets(8),
                new DrawImageOptions(source: Crop, sampling: DrawSamplingMode.Point)));
        Color[] reference = RenderInSurface(fixture, drawing =>
            drawing.DrawNineSlice(extracted, destination, new DrawInsets(8),
                new DrawImageOptions(sampling: DrawSamplingMode.Point)));

        List<PixelDifference> differences = Differences(
            reference, selected, static (_, _) => true);
        Report("collapsed nine-slice cells atlas versus extracted", differences);
        Assert.Empty(differences);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void RotatedFlippedPointImagePreservesOrdinaryInteriorAndCoveredExterior()
    {
        byte[] atlasPixels = CreateAtlasPixels();
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize, ExtractCropPixels(atlasPixels));
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        DrawRect destination = new(18.275f, 18.275f, 32, 32);
        DrawImageFlip flip = DrawImageFlip.Horizontal | DrawImageFlip.Vertical;
        DrawImageOptions atlasOptions = new(
            source: Crop,
            rotation: 0.12f,
            origin: new DrawPoint(8, 8),
            flip: flip,
            sampling: DrawSamplingMode.Point);
        DrawImageOptions extractedOptions = new(
            rotation: 0.12f,
            origin: new DrawPoint(8, 8),
            flip: flip,
            sampling: DrawSamplingMode.Point);

        Color[] selected = RenderInSurface(fixture,
            drawing => drawing.DrawImage(atlas, destination, atlasOptions));
        Color[] reference = RenderInSurface(fixture,
            drawing => drawing.DrawImage(extracted, destination, extractedOptions));
        DrawPoint[] positions = DrawImageGeometry.GetDestinationCorners(
            atlas, destination, atlasOptions);
        DrawPoint[] textureCoordinates = DrawImageGeometry.GetTextureCoordinates(
            atlas, atlasOptions);
        Color tint = DrawImageGeometry.EffectiveTint(atlasOptions);
        Color[] ordinary = RenderInSurface(fixture, drawing =>
            drawing.DrawImageQuad(
                atlas,
                new DrawVertex2D(positions[0], tint, textureCoordinates[0]),
                new DrawVertex2D(positions[1], tint, textureCoordinates[1]),
                new DrawVertex2D(positions[2], tint, textureCoordinates[2]),
                new DrawVertex2D(positions[3], tint, textureCoordinates[3]),
                atlasOptions.Sampling, atlasOptions.AddressMode, atlasOptions.LayerDepth));
        List<PixelDifference> interior = Differences(
            ordinary, selected, (x, y) => CenterInLogicalQuadInterior(
                positions, new DrawPoint(x + 0.5f, y + 0.5f)));
        List<PixelDifference> exterior = Differences(
            reference, selected, (x, y) => !CenterInLogicalQuadInterior(
                positions, new DrawPoint(x + 0.5f, y + 0.5f)));
        Report("rotated and flipped Point true interior versus ordinary", interior);
        Report("rotated and flipped Point covered exterior versus extracted", exterior);
        Assert.Empty(interior);
        Assert.Empty(exterior);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void OptionsImageQuadAndWholeNineSliceUseTheirFullLogicalImageDomain()
    {
        byte[] atlasPixels = CreateAtlasPixels();
        using SdlGpuImage atlas = new(ImageSize, ImageSize, atlasPixels);
        using SdlGpuImage extracted = new(CropSize, CropSize, ExtractCropPixels(atlasPixels));
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        List<string> violations = [];

        Compare("options-based image quad", (drawing, image, source) =>
            drawing.DrawImageQuad(
                image,
                new DrawPoint(8.275f, 8.275f),
                new DrawPoint(40.275f, 8.275f),
                new DrawPoint(40.275f, 40.275f),
                new DrawPoint(8.275f, 40.275f),
                new DrawImageOptions(source: source, sampling: DrawSamplingMode.Point)));
        Compare("nine-slice whole selected image", (drawing, image, source) =>
            drawing.DrawNineSlice(
                image,
                BoundaryDestination,
                new DrawInsets(4),
                new DrawImageOptions(source: source, sampling: DrawSamplingMode.Point)));

        void Compare(string label, Action<DrawingContext, SdlGpuImage, DrawRect?> record)
        {
            Color[] atlasFrame = RenderInSurface(fixture,
                drawing => record(drawing, atlas, Crop));
            Color[] extractedFrame = RenderInSurface(fixture,
                drawing => record(drawing, extracted, null));
            List<PixelDifference> edge = Differences(
                extractedFrame, atlasFrame, static (x, y) => IsExternalEdge(x, y));
            List<PixelDifference> interior = Differences(
                extractedFrame, atlasFrame, static (x, y) => IsQuadInterior(x, y));
            Report($"{label} external edge", edge);
            Report($"{label} internal cells/diagonal", interior);
            if (interior.Count != 0 || edge.Count != 0)
            {
                violations.Add($"{label}: interior={interior.Count}, edge={edge.Count}");
            }
        }

        Assert.True(violations.Count == 0, string.Join("; ", violations));
    }

    private static Color[] RenderMesh(
        SdlDrawingFixture fixture,
        SdlGpuImage image,
        DrawSamplingMode sampling,
        MeshSubdivision subdivision)
    {
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawMesh(CreateMesh(image, subdivision), sampling);
        return fixture.Render(commands, Color.Black);
    }

    private static DrawMesh2D CreateMesh(SdlGpuImage image, MeshSubdivision subdivision)
    {
        if (subdivision == MeshSubdivision.Grid)
        {
            List<DrawVertex2D> vertices = [];
            for (int row = 0; row <= 2; row++)
            for (int column = 0; column <= 2; column++)
            {
                vertices.Add(Vertex(column / 2f, row / 2f));
            }

            List<int> indices = [];
            for (int row = 0; row < 2; row++)
            for (int column = 0; column < 2; column++)
            {
                int topLeft = (row * 3) + column;
                indices.AddRange([topLeft, topLeft + 1, topLeft + 4,
                    topLeft, topLeft + 4, topLeft + 3]);
            }
            return new DrawMesh2D(vertices, indices, image: image);
        }

        DrawVertex2D[] corners = [Vertex(0, 0), Vertex(1, 0), Vertex(1, 1), Vertex(0, 1)];
        return subdivision switch
        {
            MeshSubdivision.Standard => new DrawMesh2D(corners, [0, 1, 2, 0, 2, 3], image: image),
            MeshSubdivision.Alternate => new DrawMesh2D(corners, [0, 1, 3, 1, 2, 3], image: image),
            MeshSubdivision.CenterFan => new DrawMesh2D(
                [.. corners, Vertex(0.5f, 0.5f)],
                [0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4], image: image),
            _ => throw new ArgumentOutOfRangeException(nameof(subdivision))
        };

        static DrawVertex2D Vertex(float x, float y)
        {
            float left = Crop.X / ImageSize;
            float top = Crop.Y / ImageSize;
            float right = Crop.Right / ImageSize;
            float bottom = Crop.Bottom / ImageSize;
            return new DrawVertex2D(
                new DrawPoint(
                    DiagonalDestination.X + (DiagonalDestination.Width * x),
                    DiagonalDestination.Y + (DiagonalDestination.Height * y)),
                Color.White,
                new DrawPoint(left + ((right - left) * x), top + ((bottom - top) * y)));
        }
    }

    private static Color[] RenderImage(
        SdlDrawingFixture fixture,
        SdlGpuImage image,
        DrawRect? source,
        DrawSamplingMode sampling,
        bool offscreen,
        DrawRect? destination = null)
    {
        void Record(DrawCommandList commands)
        {
            new DrawingContext(commands).DrawImage(
                image,
                destination ?? BoundaryDestination,
                new DrawImageOptions(source: source, sampling: sampling));
        }

        if (!offscreen)
        {
            DrawCommandList commands = new();
            Record(commands);
            return fixture.Render(commands, Color.Black);
        }

        using RecordedSurface surface = new((commands, _) => Record(commands), Color.Black);
        DrawCommandList parent = new();
        parent.Add(DrawCommand.RenderSurface2D(
            surface, new DrawRect(0, 0, FrameSize, FrameSize), Color.White));
        return fixture.Render(parent, Color.Black);
    }

    private static Color[] RenderInSurface(
        SdlDrawingFixture fixture,
        Action<DrawingContext> record,
        int frameSize = FrameSize)
    {
        using RecordedSurface surface = new(
            (commands, _) => record(new DrawingContext(commands)), Color.Black);
        DrawCommandList parent = new();
        parent.Add(DrawCommand.RenderSurface2D(
            surface, new DrawRect(0, 0, frameSize, frameSize), Color.White));
        return fixture.Render(parent, Color.Black);
    }

    private static byte[] CreateTwoRegionAtlas(DrawRect first, DrawRect second)
    {
        byte[] pixels = new byte[ImageSize * ImageSize * 4];
        for (int y = 0; y < ImageSize; y++)
        for (int x = 0; x < ImageSize; x++)
        {
            int offset = ((y * ImageSize) + x) * 4;
            Color color = Inside(first, x, y)
                ? Color.Red
                : Inside(second, x, y) ? Color.Green : Color.Magenta;
            pixels[offset] = color.R;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.B;
            pixels[offset + 3] = color.A;
        }
        return pixels;
    }

    private static byte[] CreateSourceWithGuard(DrawRect source, Color guard)
    {
        byte[] pixels = new byte[ImageSize * ImageSize * 4];
        int firstX = (int)MathF.Floor(source.X);
        int firstY = (int)MathF.Floor(source.Y);
        int lastXExclusive = (int)MathF.Ceiling(source.Right);
        int lastYExclusive = (int)MathF.Ceiling(source.Bottom);
        for (int y = 0; y < ImageSize; y++)
        for (int x = 0; x < ImageSize; x++)
        {
            int offset = ((y * ImageSize) + x) * 4;
            bool nearestReachable =
                x >= firstX && x < lastXExclusive &&
                y >= firstY && y < lastYExclusive;
            Color color = nearestReachable ? Color.Green : guard;
            pixels[offset] = color.R;
            pixels[offset + 1] = color.G;
            pixels[offset + 2] = color.B;
            pixels[offset + 3] = color.A;
        }
        return pixels;
    }

    private static byte[] ExtractRegion(byte[] atlas, DrawRect source)
    {
        int width = checked((int)source.Width);
        int height = checked((int)source.Height);
        byte[] region = new byte[width * height * 4];
        for (int row = 0; row < height; row++)
        {
            Buffer.BlockCopy(
                atlas,
                ((((int)source.Y + row) * ImageSize) + (int)source.X) * 4,
                region,
                row * width * 4,
                width * 4);
        }
        return region;
    }

    private static bool Inside(DrawRect source, int x, int y) =>
        x >= source.X && x < source.Right && y >= source.Y && y < source.Bottom;

    private static byte[] CreateAtlasPixels()
    {
        byte[] pixels = new byte[ImageSize * ImageSize * 4];
        for (int y = 0; y < ImageSize; y++)
        for (int x = 0; x < ImageSize; x++)
        {
            int offset = ((y * ImageSize) + x) * 4;
            bool inside = x >= Crop.X && x < Crop.Right && y >= Crop.Y && y < Crop.Bottom;
            pixels[offset] = inside ? (byte)(16 + ((x - 2) * 13)) : (byte)255;
            pixels[offset + 1] = inside ? (byte)(24 + ((y - 2) * 13)) : (byte)0;
            pixels[offset + 2] = inside
                ? (byte)((((x - 2) / 2 + (y - 2) / 2) & 1) == 0 ? 32 : 224)
                : (byte)255;
            pixels[offset + 3] = 255;
        }
        return pixels;
    }

    private static byte[] ExtractCropPixels(byte[] atlas)
    {
        byte[] crop = new byte[CropSize * CropSize * 4];
        for (int row = 0; row < CropSize; row++)
        {
            Buffer.BlockCopy(
                atlas, (((row + 2) * ImageSize) + 2) * 4,
                crop, row * CropSize * 4,
                CropSize * 4);
        }
        return crop;
    }

    private static List<PixelDifference> Differences(
        Color[] expected,
        Color[] actual,
        Func<int, int, bool> include)
    {
        Assert.Equal(expected.Length, actual.Length);
        List<PixelDifference> differences = [];
        for (int y = 0; y < FrameSize; y++)
        for (int x = 0; x < FrameSize; x++)
        {
            if (!include(x, y)) { continue; }
            int index = (y * FrameSize) + x;
            if (expected[index] != actual[index])
            {
                differences.Add(new PixelDifference(x, y, expected[index], actual[index]));
            }
        }
        return differences;
    }

    private void Report(string label, List<PixelDifference> differences)
    {
        output.WriteLine($"{label}: {differences.Count} differing pixels");
        foreach (PixelDifference difference in differences.Take(16))
        {
            output.WriteLine($"  ({difference.X},{difference.Y}) {difference.Expected} -> {difference.Actual}");
        }
    }

    private static bool IsQuadInterior(int x, int y) =>
        x >= 11 && x <= 38 && y >= 11 && y <= 38;

    private static bool IsExternalEdge(int x, int y) =>
        (x == 40 && y >= 11 && y <= 38) ||
        (y == 40 && x >= 11 && x <= 38);

    private static bool CenterInsideTriangle(
        DrawPoint a, DrawPoint b, DrawPoint c, DrawPoint p)
    {
        double area = Cross(a, b, c);
        if (area == 0) { return false; }
        double first = Cross(a, b, p);
        double second = Cross(b, c, p);
        double third = Cross(c, a, p);
        return area > 0
            ? first >= 0 && second >= 0 && third >= 0
            : first <= 0 && second <= 0 && third <= 0;
    }

    private static bool CenterInLogicalQuadInterior(DrawPoint[] q, DrawPoint center)
    {
        double e01 = Cross(q[0], q[1], center);
        double e12 = Cross(q[1], q[2], center);
        double e02 = Cross(q[0], q[2], center);
        double e23 = Cross(q[2], q[3], center);
        double e30 = Cross(q[3], q[0], center);
        bool firstAlive = Cross(q[0], q[1], q[2]) != 0;
        bool secondAlive = Cross(q[0], q[2], q[3]) != 0;
        bool firstPositive = e01 > 0 && e12 > 0;
        bool firstNegative = e01 < 0 && e12 < 0;
        bool secondPositive = e23 > 0 && e30 > 0;
        bool secondNegative = e23 < 0 && e30 < 0;
        bool sharedInternal = firstAlive && secondAlive && e02 == 0 &&
            ((firstPositive && secondPositive) || (firstNegative && secondNegative));
        return (firstAlive && e02 < 0 && firstPositive) ||
            (firstAlive && e02 > 0 && firstNegative) ||
            (secondAlive && e02 > 0 && secondPositive) ||
            (secondAlive && e02 < 0 && secondNegative) ||
            sharedInternal;
    }

    private static double DistanceToLine(DrawPoint a, DrawPoint b, DrawPoint p)
    {
        double length = Math.Sqrt(
            Math.Pow((double)b.X - a.X, 2) + Math.Pow((double)b.Y - a.Y, 2));
        return length == 0 ? double.PositiveInfinity :
            Math.Abs(Cross(a, b, p)) / length;
    }

    private static double Cross(DrawPoint a, DrawPoint b, DrawPoint p) =>
        (((double)b.X - a.X) * ((double)p.Y - a.Y)) -
        (((double)b.Y - a.Y) * ((double)p.X - a.X));

    private enum MeshSubdivision { Standard, Alternate, CenterFan, Grid }

    private readonly record struct PixelDifference(int X, int Y, Color Expected, Color Actual);
}
