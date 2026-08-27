using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.SdlGpu;

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

    private static DrawCommandList CreateCommands(
        PrismCatalogOperationInfo operation,
        long ownerToken = 1,
        Color? color = null,
        PrismBlendMode blendMode = PrismBlendMode.Normal)
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
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(ownerToken),
            new DrawRect(0, 0, 48, 32),
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 48, 32),
            color ?? new Color(80, 140, 220, 255)));
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
        DrawCommandList commands)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        session.BeginFrame(Color.Transparent);
        IBackdropFrameLease? backdrop = analysis.RequiresBackdrop
            ? session.AcquireFrame(new BackdropFrameRequest(
                48,
                32,
                1,
                analysis.BackdropRequirement))
            : null;
        try
        {
            DrawingFrameContext frame = new(
                analysis,
                backdrop,
                backdrop is null ? default : PrismBackdropSourceToken.CreateUnique());
            session.DrawingBackend.Render(commands, in frame);
        }
        finally
        {
            backdrop?.Dispose();
        }
        session.CompleteFrame(present: false);
    }

    private static PrismExecutionDiagnostics Diagnostics(
        SdlGpuWindowGraphicsSession session) =>
        Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend).PrismDiagnostics;
}
