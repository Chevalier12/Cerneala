using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuPrismExecutorTests
{
    [Fact]
    public void UniformPackingMatchesTheManifestFloat4LayoutByteForByte()
    {
        SdlGpuPrismUniforms uniforms = new();
        for (int index = 0; index < SdlGpuPrismUniforms.VectorCount; index++)
        {
            uniforms[index] = new Vector4(index + 0.25f, index + 0.5f, index + 0.75f, index + 1f);
        }

        byte[] packed = uniforms.Pack();

        Assert.Equal(944, SdlGpuPrismUniforms.ByteCount);
        Assert.Equal(SdlGpuPrismUniforms.ByteCount, packed.Length);
        for (int index = 0; index < SdlGpuPrismUniforms.VectorCount; index++)
        {
            Vector4 actual = MemoryMarshal.Read<Vector4>(
                packed.AsSpan(SdlGpuPrismUniforms.OffsetOfVector(index), 16));
            Assert.Equal(uniforms[index], actual);
        }

        uniforms.Reset();
        Assert.Equal(new Vector4(1, 1, 0, 0), uniforms[34]);
        Assert.Equal(Vector4.Zero, uniforms[35]);
    }

    [Fact]
    public void StableColorProfileIdsMapToContiguousShaderKernelIds()
    {
        PrismColorProfile[] profiles =
        [
            PrismColorProfile.LinearSrgb,
            PrismColorProfile.Srgb,
            PrismColorProfile.LinearDisplayP3,
            PrismColorProfile.DisplayP3,
            PrismColorProfile.ScRgb
        ];

        Assert.Equal(
            [72, 73, 74, 75, 76],
            profiles.Select(SdlGpuPrismKernelSelector.ForInputColorProfile));
        Assert.Equal(
            [77, 78, 79, 80, 81],
            profiles.Select(SdlGpuPrismKernelSelector.ForPresentation));
        Assert.All(profiles, static profile => Assert.InRange((int)profile, 173, 177));
    }

    [Fact]
    public void StableBlendModeIdsMapToContiguousShaderKernelIds()
    {
        PrismBlendMode[] modes = Enum.GetValues<PrismBlendMode>();

        Assert.Equal(
            Enumerable.Range(0, modes.Length),
            modes.Select(SdlGpuPrismKernelSelector.ResolveBlendMode));
        Assert.Equal(
            Enumerable.Range(44, modes.Length),
            modes.Select(mode =>
                44 + SdlGpuPrismKernelSelector.ResolveBlendMode(mode)));
        Assert.All(
            modes,
            static mode => Assert.InRange((int)mode, 145, 172));
    }

    [Fact]
    public void EveryCatalogEntryIsDiscoveredAutomaticallyAndHasAnSdlGpuKernelRoute()
    {
        PrismCatalogOperationInfo[] operations =
            [.. PrismCatalog.Filters, .. PrismCatalog.Styles];

        Assert.NotEmpty(operations);
        Assert.Equal(operations.Length, operations.Select(static operation => operation.StableId).Distinct().Count());
        foreach (PrismCatalogOperationInfo operation in operations)
        {
            if (operation.Kind == PrismCatalogOperationKind.Filter)
            {
                int kernel = SdlGpuPrismKernelSelector.ResolveCatalogFilter(
                    (PrismFilterId)operation.StableId);
                Assert.InRange(kernel, 9, 97);
            }
            else
            {
                Assert.True(Enum.IsDefined((PrismStyleId)operation.StableId), operation.Symbol);
            }
        }
    }

    [Fact]
    public void SpecializedCatalogAliasesAndCharcoalPassesMatchSharedKernelOwnership()
    {
        Assert.Equal(21, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.DarkStrokes));
        Assert.Equal(21, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.InkOutlines));
        Assert.Equal(26, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.Stamp));
        Assert.Equal(26, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.TornEdges));
        Assert.Equal(20, SdlGpuPrismKernelSelector.ResolveCatalogFilter(PrismFilterId.BasRelief));

        foreach (PrismFilterId filter in new[]
        {
            PrismFilterId.Charcoal,
            PrismFilterId.ConteCrayon,
            PrismFilterId.GraphicPen
        })
        {
            Assert.Equal(89, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(filter, 0));
            Assert.Equal(90, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(filter, 1));
            Assert.Equal(90, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(filter, 3));
            Assert.Equal(91, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(filter, 4));
            Assert.Equal(92, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(filter, 5));
        }
        Assert.Equal(93, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(PrismFilterId.Charcoal, 6));
        Assert.Equal(94, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(PrismFilterId.ConteCrayon, 6));
        Assert.Equal(95, SdlGpuPrismKernelSelector.ResolveCatalogFilterPass(PrismFilterId.GraphicPen, 6));
    }

    [Fact]
    public void GraphicPenFinalPassPacksStableFilterAndWorkingProfileAtManifestOffsets()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-graphic-pen-uniforms", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);

        Render(session, CreateCommands(PrismCatalog.GetFilter(PrismFilterId.GraphicPen)));

        byte[] finalPass = Assert.Single(api.FragmentUniformWrites.Where(bytes =>
            ReadVector(bytes, 34).Z == 95));
        Assert.Equal(
            new Vector4(
                (int)PrismFilterId.GraphicPen,
                (int)PrismColorProfile.LinearSrgb,
                5,
                0),
            ReadVector(finalPass, 23));
        Assert.Equal(
            new Vector4(48, 32, 95, 0),
            ReadVector(finalPass, 34));

        static Vector4 ReadVector(byte[] bytes, int index) =>
            MemoryMarshal.Read<Vector4>(
                bytes.AsSpan(SdlGpuPrismUniforms.OffsetOfVector(index), 16));
    }

    [Fact]
    public void EveryResourceFreeCatalogEntryExecutesThroughTheManifestBindingsWithoutFallback()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-catalog", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);

        foreach (PrismCatalogOperationInfo operation in
            PrismCatalog.Filters.Concat(PrismCatalog.Styles).Where(static operation => !operation.RequiresResource))
        {
            int uniformStart = api.FragmentUniformWrites.Count;
            int samplerStart = api.FragmentSamplerBindings.Count;

            Render(session, CreateCommands(operation));

            Assert.True(api.FragmentUniformWrites.Count > uniformStart, operation.Symbol);
            Assert.All(api.FragmentUniformWrites.Skip(uniformStart), bytes =>
                Assert.Equal(SdlGpuPrismUniforms.ByteCount, bytes.Length));
            uint[] slots = api.FragmentSamplerBindings
                .Skip(samplerStart)
                .Select(static binding => binding.Slot)
                .Distinct()
                .Order()
                .ToArray();
            Assert.Equal(Enumerable.Range(0, 15).Select(static value => (uint)value), slots);
            PrismExecutionDiagnostics diagnostics = Diagnostics(session);
            Assert.True(
                diagnostics.Count == 0,
                $"{operation.Symbol}: {diagnostics.LastFallback}");
        }
    }

    [Fact]
    public void PrismPlanSurfacesGenerateFullMipChainsBeforeTheyAreSampled()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-mipmaps", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);

        Render(session, CreateCommands(PrismCatalog.GetFilter(PrismFilterId.Spherize)));

        Assert.NotEmpty(api.GeneratedMipmaps);
        Assert.All(api.GeneratedMipmaps, texture =>
        {
            SdlGpuTextureCreateInfo createInfo = api.GpuTextures[texture].CreateInfo;
            Assert.Equal(SdlGpuTextureFormat.R16G16B16A16Float, createInfo.Format);
            Assert.Equal(6u, createInfo.MipLevelCount);
            int generateIndex = api.GpuActions.FindIndex(
                action => action.EndsWith($":{texture}", StringComparison.Ordinal) &&
                    action.StartsWith("generate-mipmaps:", StringComparison.Ordinal));
            Assert.True(generateIndex > 0);
            Assert.StartsWith("end-render:", api.GpuActions[generateIndex - 1]);
        });
        Assert.Contains(api.GpuSamplers.Values, sampler =>
            sampler.Filter == SdlGpuFilter.Linear &&
            sampler.MipmapMode == SdlGpuSamplerMipmapMode.Linear &&
            sampler.MinLod == 0 &&
            sampler.MaxLod >= 6);
    }

    [Fact]
    public void MotionBlurPlanSurfacesSkipUnusedMipChains()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-motion-blur-mipmaps",
            48,
            32,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(
            factory,
            api,
            window);

        Render(
            session,
            CreateCommands(PrismCatalog.GetFilter(PrismFilterId.MotionBlur)));

        SdlGpuTextureCreateInfo[] prismTextures = api.GpuTextures.Values
            .Select(static texture => texture.CreateInfo)
            .Where(static texture => texture.Format is
                SdlGpuTextureFormat.R16G16B16A16Float or
                SdlGpuTextureFormat.R32G32B32A32Float)
            .ToArray();
        Assert.NotEmpty(prismTextures);
        Assert.All(
            prismTextures,
            static texture => Assert.Equal(1u, texture.MipLevelCount));
        Assert.Empty(api.GeneratedMipmaps);
    }

    [Fact]
    public void PrismSurfacesAndPipelinesAreDepthlessAndBudgetOnlyColorMipBytes()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-depthless", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuPrismDeviceResources resources = session.DrawingResources.PrismResources;

        using SdlGpuPrismSurfaceLease surface = resources.RentSurface(
            session.WindowIdentity,
            48,
            32,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: true);
        nint pipeline = resources.GetPipeline(SdlGpuTextureFormat.R16G16B16A16Float);

        Assert.Equal(0, surface.Target.DepthStencilTexture);
        Assert.Equal(16_376, resources.TotalBytes);
        Assert.Equal(
            1,
            api.GpuTextures.Values.Count(texture =>
                texture.CreateInfo.Format == SdlGpuTextureFormat.D24UnormS8Uint));
        Assert.Equal(
            SdlGpuTextureFormat.Invalid,
            api.GpuPipelines[pipeline].DepthStencilFormat);
    }

    [Fact]
    public void PrismSurfacesUseKnownGraphBoundsInsteadOfTheEntireHostTarget()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-bounded-surfaces", 256, 256, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                256,
                256,
                coordinateScale: 1));

        Render(
            session,
            CreateCommands(PrismCatalog.GetStyle(PrismStyleId.OuterGlow)),
            256,
            256);

        var prismTextures = api.GpuTextures.Values
            .Where(texture => texture.CreateInfo.Format is
                SdlGpuTextureFormat.R16G16B16A16Float or
                SdlGpuTextureFormat.R32G32B32A32Float)
            .ToArray();
        Assert.NotEmpty(prismTextures);
        Assert.All(prismTextures, texture =>
        {
            Assert.InRange(texture.CreateInfo.Width, 1u, 255u);
            Assert.InRange(texture.CreateInfo.Height, 1u, 255u);
        });
    }

    [Fact]
    public void CoordinateDependentFiltersPreserveTheHostExecutionExtent()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-stable-host-coordinates",
            256,
            256,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session =
            Assert.IsType<SdlGpuWindowGraphicsSession>(
                factory.Create(
                    new SdlWindowSurface(window, api.GetWindowId(window)),
                    256,
                    256,
                    coordinateScale: 1));

        Render(
            session,
            CreateCommands(PrismCatalog.GetFilter(PrismFilterId.Spherize)),
            256,
            256);

        var prismTextures = api.GpuTextures.Values
            .Where(texture => texture.CreateInfo.Format is
                SdlGpuTextureFormat.R16G16B16A16Float or
                SdlGpuTextureFormat.R32G32B32A32Float)
            .ToArray();
        Assert.NotEmpty(prismTextures);
        Assert.All(prismTextures, texture =>
        {
            Assert.Equal(256u, texture.CreateInfo.Width);
            Assert.Equal(256u, texture.CreateInfo.Height);
        });
    }

    [Fact]
    public void PrismSurfacesExcludeEmptySpaceBeforeKnownGraphBounds()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-offset-surfaces", 256, 256, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                256,
                256,
                coordinateScale: 1));

        Render(
            session,
            CreateCommands(
                PrismCatalog.GetStyle(PrismStyleId.OuterGlow),
                origin: new Vector2(100, 120)),
            256,
            256);

        var prismTextures = api.GpuTextures.Values
            .Where(texture => texture.CreateInfo.Format is
                SdlGpuTextureFormat.R16G16B16A16Float or
                SdlGpuTextureFormat.R32G32B32A32Float)
            .ToArray();
        Assert.NotEmpty(prismTextures);
        Assert.All(prismTextures, texture =>
        {
            Assert.InRange(texture.CreateInfo.Width, 1u, 80u);
            Assert.InRange(texture.CreateInfo.Height, 1u, 64u);
        });
    }

    [Fact]
    public void PrismPassesDoNotUploadQuadGeometryPerGraphNode()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-static-quad", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);

        Render(
            session,
            CreateCommands(PrismCatalog.GetStyle(PrismStyleId.OuterGlow)));

        int geometryUploads = api.GpuActions.Count(action =>
            action.StartsWith("upload-buffer:", StringComparison.Ordinal));
        Assert.InRange(geometryUploads, 0, 4);
    }

    [Fact]
    public void StylesSharingAnAlphaSourceComputeOneDistanceField()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-shared-distance-field",
            48,
            32,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(
            factory,
            api,
            window);
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "SharedDistanceField",
            styles:
            [
                new PrismStyleDefinition(PrismStyleId.BevelEmboss),
                new PrismStyleDefinition(PrismStyleId.OuterGlow)
            ]);
        PrismInstance instance = new(
            new PrismCompositionDefinition("SharedDistanceField", [layer]));
        DrawRect bounds = new(0, 0, 48, 32);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
            instance,
            new PrismCacheOwnerToken(704),
            bounds,
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1)));
        commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue));
        commands.Add(DrawCommand.EndPrism());

        Render(session, commands);

        int distanceFieldPasses = api.RenderTargets.Count(target =>
            api.GpuTextures[target.Texture].CreateInfo.Format ==
                SdlGpuTextureFormat.R32G32B32A32Float);
        Assert.Equal(10, distanceFieldPasses);
    }

    [Fact]
    public void AnimatedMotionBlurReusesSurfacesWithinTheSameAllocationTile()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-animated-motion-blur",
            256,
            256,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                256,
                256,
                coordinateScale: 1));
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "MotionBlur",
            filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)]);
        PrismInstance instance = new(
            new PrismCompositionDefinition("AnimatedMotionBlur", [layer]));
        PrismFilterState motionBlur = instance
            .GetLayerState(new PrismNodeId(1))
            .Filters[0];
        PrismCatalogParameterInfo distance = PrismCatalog
            .GetFilter(PrismFilterId.MotionBlur)
            .Parameters
            .Single(parameter => parameter.Name == "Distance");
        PrismCacheOwnerToken owner = new(701);
        PrismCacheInvalidationQueue invalidations = new();
        long warmedCreated = 0;

        for (int value = 1; value <= 15; value++)
        {
            motionBlur.SetValue(distance, (float)value);
            DrawRect bounds = new(96, 100, 48, 32);
            PrismDrawScope scope = new(
                instance,
                owner,
                bounds,
                Matrix3x2.Identity,
                pixelScale: 1,
                visualContentVersion: 1);
            DrawCommandList commands = new();
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue));
            commands.Add(DrawCommand.EndPrism());
            invalidations.EnqueueOwner(owner);

            Render(session, commands, invalidations, 256, 256);
            if (value == 1)
            {
                warmedCreated = session.DrawingResources.PrismResources.CreatedSurfaceCount;
            }
        }

        Assert.Equal(
            warmedCreated,
            session.DrawingResources.PrismResources.CreatedSurfaceCount);
    }

    [Fact]
    public void ChangingMotionBlurValueRetainsTheUnchangedControlCapture()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-motion-blur-retained-capture",
            256,
            256,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session =
            Assert.IsType<SdlGpuWindowGraphicsSession>(
                factory.Create(
                    new SdlWindowSurface(window, api.GetWindowId(window)),
                    256,
                    256,
                    coordinateScale: 1));
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "MotionBlur",
            filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)]);
        PrismInstance instance = new(
            new PrismCompositionDefinition("RetainedMotionBlur", [layer]));
        PrismFilterState motionBlur = instance.GetLayerState(layer.Id).Filters[0];
        PrismCatalogParameterInfo distance = PrismCatalog
            .GetFilter(PrismFilterId.MotionBlur)
            .Parameters
            .Single(parameter => parameter.Name == "Distance");
        PrismCacheOwnerToken owner = new(703);
        PrismCacheInvalidationQueue invalidations = new();
        DrawRect bounds = new(96, 100, 48, 32);

        RenderDistance(5);
        PrismExecutionCounters first = Diagnostics(session).Counters;
        RenderDistance(6);
        PrismExecutionCounters changed = Diagnostics(session).Counters;

        Assert.Equal(1, first.CaptureCount);
        Assert.Equal(0, changed.CaptureCount);
        Assert.True(changed.PassCount < changed.PlannedPassCount);

        void RenderDistance(float value)
        {
            motionBlur.SetValue(distance, value);
            PrismDrawScope scope = new(
                instance,
                owner,
                bounds,
                Matrix3x2.Identity,
                pixelScale: 1,
                visualContentVersion: 1);
            DrawCommandList commands = new();
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue));
            commands.Add(DrawCommand.EndPrism());
            invalidations.EnqueueOwner(owner);
            Render(session, commands, invalidations, 256, 256);
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NestedPrismPresentationRemainsVisibleAwayFromTheHostOrigin()
    {
        NativeSdlApi api = new();
        using SdlPlatformLifetime platform = new(api);
        nint window = api.CreateWindow(
            "prism-nested-offset",
            256,
            256,
            SdlWindowOptions.Hidden);
        try
        {
            using SdlGpuWindowGraphicsSessionFactory factory = new(
                api,
                useMultisampling: false);
            using SdlGpuWindowGraphicsSession session =
                Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                    new SdlWindowSurface(window, api.GetWindowId(window)),
                    256,
                    256,
                    coordinateScale: 1));
            DrawCommandList commands = CreateNestedPrismCommands(
                new Vector2(96, 100));

            Render(session, commands, 256, 256);
            WindowPreviewFrame frame = session.CapturePresentedFrame();
            int visiblePixels = 0;
            for (int y = 100; y < 132; y++)
            {
                for (int x = 96; x < 144; x++)
                {
                    int offset = y * frame.Stride + x * 4;
                    if (frame.Pixels[offset + 3] != 0)
                    {
                        visiblePixels++;
                    }
                }
            }

            Assert.InRange(visiblePixels, 1, 48 * 32);
        }
        finally
        {
            api.DestroyWindow(window);
        }
    }

    [Fact]
    public void ReturnedPrismSurfacesRespectTheConfiguredCacheSoftLimit()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-surface-cache-budget", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuPrismDeviceResources resources = session.DrawingResources.PrismResources;

        for (int size = 2_048; size < 2_051; size++)
        {
            using SdlGpuPrismSurfaceLease surface = resources.RentSurface(
                session.WindowIdentity,
                size,
                size,
                SdlGpuTextureFormat.R16G16B16A16Float,
                mipmapped: false);
        }

        Assert.True(
            resources.TotalBytes <= 64L * 1024 * 1024,
            $"Returned surface cache retained {resources.TotalBytes} bytes.");
    }

    [Fact]
    public void ChangingPrismBoundsRespectTheTransientCacheBudget()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-changing-bounds",
            512,
            512,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session =
            Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                512,
                512,
                coordinateScale: 1));
        SdlGpuPrismDeviceResources resources =
            session.DrawingResources.PrismResources;
        PrismCatalogOperationInfo outerGlow =
            PrismCatalog.GetStyle(PrismStyleId.OuterGlow);

        Render(
            session,
            CreateCommands(outerGlow, extent: 512),
            512,
            512);
        for (int extent = 504; extent >= 64; extent -= 8)
        {
            Render(
                session,
                CreateCommands(outerGlow, extent: extent),
                512,
                512);
        }

        Assert.True(
            resources.FreeBytes <= 32L * 1024 * 1024,
            $"Changing Prism bounds retained {resources.FreeBytes} transient bytes.");
    }

    [Fact]
    public void SoftLimitEvictsTheColdestFreeSurfaceInsteadOfTheJustReturnedHotSurface()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-surface-cache-recency",
            48,
            32,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(
            factory,
            api,
            window);
        SdlGpuPrismDeviceResources resources =
            session.DrawingResources.PrismResources;

        using (resources.RentSurface(
            session.WindowIdentity,
            2_048,
            2_048,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false))
        {
        }
        using (resources.RentSurface(
            session.WindowIdentity,
            2_049,
            2_049,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false))
        {
        }
        using (resources.RentSurface(
            session.WindowIdentity,
            2_048,
            2_048,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false))
        {
        }

        long createdAfterHotReturn = resources.CreatedSurfaceCount;
        using (resources.RentSurface(
            session.WindowIdentity,
            2_048,
            2_048,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false))
        {
        }

        Assert.Equal(createdAfterHotReturn, resources.CreatedSurfaceCount);
    }

    [Fact]
    public void SameKeySurfaceReusePrefersTheMostRecentlyReturnedTarget()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-surface-cache-lifo-reuse",
            48,
            32,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(
            factory,
            api,
            window);
        SdlGpuPrismDeviceResources resources =
            session.DrawingResources.PrismResources;

        SdlGpuPrismSurfaceLease first = resources.RentSurface(
            session.WindowIdentity,
            64,
            64,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false);
        SdlGpuPrismSurfaceLease second = resources.RentSurface(
            session.WindowIdentity,
            64,
            64,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false);
        SdlGpuRenderTarget expected = second.Target;
        first.Dispose();
        second.Dispose();

        using SdlGpuPrismSurfaceLease reused = resources.RentSurface(
            session.WindowIdentity,
            64,
            64,
            SdlGpuTextureFormat.R16G16B16A16Float,
            mipmapped: false);

        Assert.Same(expected, reused.Target);
    }

    [Fact]
    public void BackdropVersionChangesReplaceRetainedOwnerSurfacesInsteadOfAccumulating()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(
            "prism-backdrop-cache-replacement",
            256,
            256,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                256,
                256,
                coordinateScale: 1));
        DrawCommandList commands = CreateNestedPrismCommands(new Vector2(96, 100));
        PrismBackdropSourceToken backdropSource = PrismBackdropSourceToken.CreateUnique();

        Render(session, commands, backdropSource, 256, 256);
        Render(session, commands, backdropSource, 256, 256);
        SdlGpuPrismDeviceResources resources = session.DrawingResources.PrismResources;
        long warmedCreated = resources.CreatedSurfaceCount;
        long warmedBytes = resources.TotalBytes;
        int warmedRetained = resources.RetainedCount;

        for (int frame = 0; frame < 58; frame++)
        {
            Render(session, commands, backdropSource, 256, 256);
        }

        Assert.Equal(warmedCreated, resources.CreatedSurfaceCount);
        Assert.Equal(warmedBytes, resources.TotalBytes);
        Assert.Equal(warmedRetained, resources.RetainedCount);
    }

    [Fact]
    public void TwoWindowsShareDeviceCacheButKeepExecutionAndLifetimeIndependent()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("prism-a", 48, 32, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("prism-b", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        SdlGpuWindowGraphicsSession first = CreateSession(factory, api, firstWindow);
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, secondWindow);
        PrismCatalogOperationInfo invert = PrismCatalog.GetFilter(PrismFilterId.Invert);
        DrawCommandList firstCommands = CreateCommands(invert, ownerToken: 101, Color.Coral);
        DrawCommandList secondCommands = CreateCommands(invert, ownerToken: 202, Color.CornflowerBlue);

        Render(first, firstCommands);
        Render(second, secondCommands);
        Render(first, firstCommands);
        Render(second, secondCommands);
        long warmedCreated = first.DrawingResources.PrismResources.CreatedSurfaceCount;
        Render(first, firstCommands);
        Render(second, secondCommands);

        Assert.Same(first.DrawingResources, second.DrawingResources);
        Assert.True(first.DrawingResources.PrismResources.RetainedCount >= 2);
        Assert.Equal(warmedCreated, first.DrawingResources.PrismResources.CreatedSurfaceCount);
        Assert.True(first.DrawingResources.PrismResources.ReusedSurfaceCount > 0);

        first.Dispose();
        Render(second, secondCommands);

        Assert.Equal(0, Diagnostics(second).Count);
        Assert.Contains(secondWindow, api.ClaimedGpuWindows);
        Assert.DoesNotContain(firstWindow, api.ClaimedGpuWindows);
    }

    [Fact]
    public void TwoActiveWindowsUseDistinctBackdropsAndOneCanCloseDuringTheOtherFrame()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("prism-backdrop-a", 48, 32, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("prism-backdrop-b", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        SdlGpuWindowGraphicsSession first = CreateSession(factory, api, firstWindow);
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, secondWindow);
        PrismCatalogOperationInfo invert = PrismCatalog.GetFilter(PrismFilterId.Invert);
        DrawCommandList firstCommands = CreateCommands(
            invert,
            ownerToken: 301,
            Color.Coral,
            PrismBlendMode.Multiply);
        DrawCommandList secondCommands = CreateCommands(
            invert,
            ownerToken: 302,
            Color.CornflowerBlue,
            PrismBlendMode.Multiply);
        PrismFrameAnalysis firstAnalysis = new PrismFrameAnalyzer().Analyze(firstCommands);
        PrismFrameAnalysis secondAnalysis = new PrismFrameAnalyzer().Analyze(secondCommands);

        Assert.True(firstAnalysis.RequiresBackdrop);
        Assert.True(secondAnalysis.RequiresBackdrop);

        first.BeginFrame(Color.Coral);
        second.BeginFrame(Color.CornflowerBlue);
        IBackdropFrameLease firstBackdrop = first.AcquireFrame(new BackdropFrameRequest(
            48,
            32,
            1,
            firstAnalysis.BackdropRequirement));
        IBackdropFrameLease secondBackdrop = second.AcquireFrame(new BackdropFrameRequest(
            48,
            32,
            1,
            secondAnalysis.BackdropRequirement));

        Assert.NotEqual(
            Assert.IsAssignableFrom<ISdlGpuBackdropFrameLease>(firstBackdrop).Texture,
            Assert.IsAssignableFrom<ISdlGpuBackdropFrameLease>(secondBackdrop).Texture);

        try
        {
            DrawingFrameContext firstFrame = new(
                firstAnalysis,
                firstBackdrop,
                PrismBackdropSourceToken.CreateUnique());
            first.DrawingBackend.Render(firstCommands, in firstFrame);
        }
        finally
        {
            firstBackdrop.Dispose();
        }
        first.CompleteFrame(present: false);

        first.Dispose();

        try
        {
            DrawingFrameContext secondFrame = new(
                secondAnalysis,
                secondBackdrop,
                PrismBackdropSourceToken.CreateUnique());
            second.DrawingBackend.Render(secondCommands, in secondFrame);
        }
        finally
        {
            secondBackdrop.Dispose();
        }
        second.CompleteFrame(present: false);
        Render(second, secondCommands);

        Assert.Equal(0, Diagnostics(second).Count);
        Assert.Contains(secondWindow, api.ClaimedGpuWindows);
        Assert.DoesNotContain(firstWindow, api.ClaimedGpuWindows);
    }

    [Fact]
    public void PrismPresentationUsesPremultipliedAlphaInsteadOfOpaqueReplacement()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-presentation-alpha", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);

        Render(session, CreateCommands(PrismCatalog.GetFilter(PrismFilterId.Invert)));

        string bind = api.GpuActions.Last(action =>
            action.StartsWith("bind-pipeline:", StringComparison.Ordinal));
        nint pipeline = (nint)long.Parse(bind.Split(':')[2]);
        SdlGpuBlendState blend = api.GpuPipelines[pipeline].BlendState;

        Assert.True(blend.Enabled);
        Assert.Equal(SdlGpuBlendFactor.One, blend.SourceColor);
        Assert.Equal(SdlGpuBlendFactor.OneMinusSourceAlpha, blend.DestinationColor);
        Assert.Equal(SdlGpuBlendOperation.Add, blend.ColorOperation);
        Assert.Equal(SdlGpuBlendFactor.One, blend.SourceAlpha);
        Assert.Equal(SdlGpuBlendFactor.OneMinusSourceAlpha, blend.DestinationAlpha);
        Assert.Equal(SdlGpuBlendOperation.Add, blend.AlphaOperation);
    }

    [Fact]
    public void PrismRootInsideHostClipResumesWithoutUnbalancedClipState()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("prism-host-clip", 48, 32, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList prism = CreateCommands(
            PrismCatalog.GetFilter(PrismFilterId.Invert));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.PushClip(new DrawRect(2, 3, 40, 24)));
        foreach (DrawCommand command in prism)
        {
            commands.Add(command);
        }
        commands.Add(DrawCommand.PopClip());
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 48, 32),
            Color.Coral));

        Exception? failure = Record.Exception(() => Render(session, commands));

        Assert.Null(failure);
        Assert.EndsWith(
            ":0,0,48,32",
            api.GpuActions.Last(action =>
                action.StartsWith("scissor:", StringComparison.Ordinal)));
    }

    private static DrawCommandList CreateCommands(
        PrismCatalogOperationInfo operation,
        long ownerToken = 1,
        Color? color = null,
        PrismBlendMode blendMode = PrismBlendMode.Normal,
        Vector2? origin = null,
        float? extent = null)
    {
        PrismLayerDefinition layer = operation.Kind == PrismCatalogOperationKind.Filter
            ? new PrismLayerDefinition(
                new PrismNodeId(1),
                operation.Symbol,
                filters: [new PrismFilterDefinition((PrismFilterId)operation.StableId)],
                blendMode: blendMode)
            : new PrismLayerDefinition(
                new PrismNodeId(1),
                operation.Symbol,
                styles: [new PrismStyleDefinition((PrismStyleId)operation.StableId)],
                blendMode: blendMode);
        PrismInstance instance = new(new PrismCompositionDefinition(operation.Symbol, [layer]));
        Vector2 drawOrigin = origin ?? Vector2.Zero;
        DrawRect bounds = extent is float size
            ? new DrawRect(drawOrigin.X, drawOrigin.Y, size, size)
            : new DrawRect(drawOrigin.X, drawOrigin.Y, 48, 32);
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(ownerToken),
            bounds,
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(DrawCommand.FillRectangle(
            bounds,
            color ?? new Color(80, 140, 220, 255)));
        commands.Add(DrawCommand.EndPrism());
        return commands;
    }

    private static DrawCommandList CreateNestedPrismCommands(Vector2 origin)
    {
        DrawRect bounds = new(origin.X, origin.Y, 48, 32);
        PrismInstance outer = new(new PrismCompositionDefinition(
            "MotionBlur",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "MotionBlur",
                    filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)])
            ]));
        PrismInstance inner = new(new PrismCompositionDefinition(
            "OuterGlow",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "EmbossAndOuterGlow",
                    styles:
                    [
                        new PrismStyleDefinition(PrismStyleId.BevelEmboss),
                        new PrismStyleDefinition(PrismStyleId.OuterGlow)
                    ])
            ]));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
            outer,
            new PrismCacheOwnerToken(702),
            bounds,
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1)));
        commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
            inner,
            new PrismCacheOwnerToken(703),
            bounds,
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1)));
        commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue));
        commands.Add(DrawCommand.EndPrism());
        commands.Add(DrawCommand.EndPrism());
        return commands;
    }

    private static SdlGpuWindowGraphicsSession CreateSession(
        SdlGpuWindowGraphicsSessionFactory factory,
        FakeSdlApi api,
        nint window) =>
        Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)),
            48,
            32,
            coordinateScale: 1));

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands,
        int pixelWidth = 48,
        int pixelHeight = 32)
    {
        Render(
            session,
            commands,
            PrismBackdropSourceToken.CreateUnique(),
            pixelWidth,
            pixelHeight);
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands,
        PrismBackdropSourceToken backdropSource,
        int pixelWidth,
        int pixelHeight)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        session.BeginFrame(Color.Transparent);
        IBackdropFrameLease? backdrop = analysis.RequiresBackdrop
            ? session.AcquireFrame(new BackdropFrameRequest(
                pixelWidth,
                pixelHeight,
                1,
                analysis.BackdropRequirement))
            : null;
        try
        {
            DrawingFrameContext frame = new(
                analysis,
                backdrop,
                backdrop is null ? default : backdropSource);
            session.DrawingBackend.Render(commands, in frame);
        }
        finally
        {
            backdrop?.Dispose();
        }
        session.CompleteFrame(present: false);
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands,
        PrismCacheInvalidationQueue invalidations,
        int pixelWidth,
        int pixelHeight)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        session.BeginFrame(Color.Transparent);
        DrawingFrameContext frame = new(
            analysis,
            backdropLease: null,
            backdropSourceToken: default,
            invalidations);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);
    }

    private static PrismExecutionDiagnostics Diagnostics(
        SdlGpuWindowGraphicsSession session) =>
        Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend).PrismDiagnostics;
}
