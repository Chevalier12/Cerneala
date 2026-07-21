using System.Collections.Immutable;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;
using ExecutorTests = Cerneala.Tests.Drawing.MonoGame.PrismGraphExecutorTests;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame.Prism.Cache;

public sealed class PrismRetainedCacheContractTests
{
    [Theory]
    [InlineData(PrismCacheScenario.Simple)]
    [InlineData(PrismCacheScenario.Mask)]
    [InlineData(PrismCacheScenario.Group)]
    [InlineData(PrismCacheScenario.Nested)]
    [InlineData(PrismCacheScenario.Backdrop)]
    public void CacheOnOutputMatchesFreshCacheOff(
        PrismCacheScenario scenario)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismRetainedCacheContractHarness harness = new();

        PrismCacheComparison result =
            harness.CompareWithFreshCacheOff(scenario);

        AssertPixelsWithin(
            result.CacheOnPixels,
            result.CacheOffPixels);
    }

    [Fact]
    public void IdenticalStaticSecondFrameHitsFinalAndSkipsCoveredWork()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismRetainedCacheContractHarness harness = new();

        PrismStaticHitObservation result =
            harness.RenderIdenticalStaticFrames();

        AssertPixelsWithin(
            result.SecondFramePixels,
            result.FreshPixels);
        Assert.Equal(1, result.FinalHitCount);
        Assert.Equal(0, result.SecondFrameCaptureCount);
        Assert.Equal(0, result.SecondFrameCoveredPassCount);
        Assert.Equal(1, result.CachedPresentationCount);
    }

    [Theory]
    [InlineData(PrismCacheMutation.Content)]
    [InlineData(PrismCacheMutation.Structure)]
    [InlineData(PrismCacheMutation.Parameter)]
    [InlineData(PrismCacheMutation.Motion)]
    [InlineData(PrismCacheMutation.Resource)]
    [InlineData(PrismCacheMutation.LowerUi)]
    [InlineData(PrismCacheMutation.BackdropContentVersion)]
    [InlineData(PrismCacheMutation.Bounds)]
    [InlineData(PrismCacheMutation.PixelScale)]
    [InlineData(PrismCacheMutation.ColorProfile)]
    [InlineData(PrismCacheMutation.SurfaceFormat)]
    [InlineData(PrismCacheMutation.CapabilitySet)]
    [InlineData(PrismCacheMutation.ShaderPackage)]
    public void PixelAffectingMutationMissesAndMatchesFreshOutput(
        PrismCacheMutation mutation)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismRetainedCacheContractHarness harness = new();

        PrismMutationObservation result =
            harness.RenderAfterMutation(mutation);

        AssertPixelsWithin(
            result.CacheOnPixels,
            result.FreshPixels);
        Assert.Equal(0, result.FinalHitCount);
        Assert.True(result.MissCount > 0);
        Assert.Equal(mutation, result.Mutation);
    }

    [Fact]
    public void IntermediateHitPrunesOnlyTheCoveredSubgraph()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismRetainedCacheContractHarness harness = new();

        PrismIntermediateHitObservation result =
            harness.RenderWithIntermediateHit();

        AssertPixelsWithin(
            result.CacheOnPixels,
            result.FreshPixels);
        Assert.Equal(0, result.FinalHitCount);
        Assert.True(result.IntermediateHitCount > 0);
        Assert.True(result.SavedPassCount > 0);
        Assert.True(result.ExecutedPassCount > 0);
    }

    [Fact]
    public void IdentityCollisionBudgetAndPinningStaySafe()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismRetainedCacheContractHarness harness = new();

        PrismOwnershipObservation result =
            harness.ExerciseIdentityBudgetAndPinning();

        Assert.False(result.SharedAcrossControlOwners);
        Assert.False(result.AcceptedHashCollision);
        Assert.False(result.EvictedPinnedEntry);
        Assert.True(result.BudgetWasRespected);
        Assert.True(result.FallbackMatchedFreshOutput);
    }

    [Theory]
    [InlineData(PrismCacheLifecycleCase.ExecutionException)]
    [InlineData(PrismCacheLifecycleCase.Detach)]
    [InlineData(PrismCacheLifecycleCase.Hidden)]
    [InlineData(PrismCacheLifecycleCase.Collapsed)]
    [InlineData(PrismCacheLifecycleCase.Replacement)]
    [InlineData(PrismCacheLifecycleCase.DeviceReset)]
    public void FailureAndLifecyclePathsLeaveNoStalePixelsOrLeases(
        PrismCacheLifecycleCase lifecycleCase)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismRetainedCacheContractHarness harness = new();

        PrismLifecycleObservation result =
            harness.ExerciseLifecycle(lifecycleCase);

        AssertPixelsWithin(
            result.ActualPixels,
            result.FreshPixels);
        Assert.Equal(0, result.PartialEntryCount);
        Assert.Equal(0, result.OrphanedLeaseCount);
        Assert.False(result.ReusedInvalidPixels);
        if (lifecycleCase is PrismCacheLifecycleCase.Hidden or
            PrismCacheLifecycleCase.Collapsed)
        {
            Assert.Equal(0, result.LookupCount);
            Assert.Equal(0, result.PromotionCount);
        }
    }

    private static void AssertPixelsWithin(
        byte[] actual,
        byte[] expected)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int index = 0; index < actual.Length; index++)
        {
            Assert.InRange(
                Math.Abs(actual[index] - expected[index]),
                0,
                1);
        }
    }
}

internal sealed class PrismRetainedCacheContractHarness : IDisposable
{
    private const long OwnerToken = 91_001;

    private readonly ExecutorTests.WindowsDxFixture fixture = new();

    private GraphicsDevice GraphicsDevice =>
        fixture.Session.GraphicsDevice;

    public PrismCacheComparison CompareWithFreshCacheOff(
        PrismCacheScenario scenario)
    {
        using ExecutorTests.PrismRetainedScenario scene =
            CreateScenario(scenario);
        using ExecutorTests.TestPrismRenderer cacheOnRenderer =
            CreateRenderer();
        using ExecutorTests.TestPrismRenderer cacheOffRenderer =
            CreateRenderer();
        using PrismGraphExecutor cacheOn = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: true);
        using PrismGraphExecutor cacheOff = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: false);
        Viewport viewport = CreateViewport();

        Execute(cacheOnRenderer, cacheOn, scene, viewport);
        Execute(cacheOnRenderer, cacheOn, scene, viewport);
        byte[] cacheOnPixels =
            ToBytes(cacheOnRenderer.ReadPixels());

        Execute(cacheOffRenderer, cacheOff, scene, viewport);
        byte[] cacheOffPixels =
            ToBytes(cacheOffRenderer.ReadPixels());

        Assert.True(cacheOn.RendererDiagnostics.FinalHitCount > 0);
        Assert.Equal(0, cacheOff.RendererDiagnostics.LookupCount);
        return new PrismCacheComparison(
            cacheOffPixels,
            cacheOnPixels);
    }

    public PrismStaticHitObservation RenderIdenticalStaticFrames()
    {
        using ExecutorTests.PrismRetainedScenario scene =
            ExecutorTests.CreateAlphaRetainedScenario();
        using ExecutorTests.TestPrismRenderer renderer =
            CreateRenderer();
        PrismExecutionDiagnostics executionDiagnostics = new();
        using PrismGraphExecutor executor = new(
            GraphicsDevice,
            executionDiagnostics);
        Viewport viewport = CreateViewport();

        Execute(renderer, executor, scene, viewport);
        byte[] freshPixels = ToBytes(renderer.ReadPixels());
        PrismRendererDiagnostics before =
            executor.RendererDiagnostics;

        Execute(renderer, executor, scene, viewport);
        byte[] secondFramePixels =
            ToBytes(renderer.ReadPixels());
        PrismRendererDiagnostics after =
            executor.RendererDiagnostics;
        int presentationCount = CountPresentations(scene.Plan);

        return new PrismStaticHitObservation(
            freshPixels,
            secondFramePixels,
            ToInt(after.FinalHitCount - before.FinalHitCount),
            executionDiagnostics.Counters.CaptureCount,
            Math.Max(
                0,
                executionDiagnostics.Counters.PassCount -
                    presentationCount),
            presentationCount);
    }

    public PrismMutationObservation RenderAfterMutation(
        PrismCacheMutation mutation)
    {
        using MutationPair pair = CreateMutationPair(mutation);
        using ExecutorTests.TestPrismRenderer cacheOnRenderer =
            CreateRenderer();
        using ExecutorTests.TestPrismRenderer freshRenderer =
            CreateRenderer();
        using PrismGraphExecutor cacheOn = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: true);
        using PrismGraphExecutor fresh = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: false);
        Viewport viewport = CreateViewport();

        Execute(cacheOnRenderer, cacheOn, pair.Base, viewport);
        pair.PrepareChanged?.Invoke(cacheOn);
        PrismRendererDiagnostics before =
            cacheOn.RendererDiagnostics;
        Execute(cacheOnRenderer, cacheOn, pair.Changed, viewport);
        byte[] cacheOnPixels =
            ToBytes(cacheOnRenderer.ReadPixels());
        PrismRendererDiagnostics after =
            cacheOn.RendererDiagnostics;

        Execute(freshRenderer, fresh, pair.Changed, viewport);
        byte[] freshPixels = ToBytes(freshRenderer.ReadPixels());

        return new PrismMutationObservation(
            mutation,
            freshPixels,
            cacheOnPixels,
            ToInt(after.FinalHitCount - before.FinalHitCount),
            ToInt(after.MissCount - before.MissCount));
    }

    public PrismIntermediateHitObservation RenderWithIntermediateHit()
    {
        using ExecutorTests.PrismRetainedScenario scene =
            ExecutorTests.CreateComplexRetainedScenario(
                GraphicsDevice);
        using ExecutorTests.TestPrismRenderer renderer =
            CreateRenderer();
        PrismExecutionDiagnostics executionDiagnostics = new();
        using PrismGraphExecutor executor = new(
            GraphicsDevice,
            executionDiagnostics);
        Viewport viewport = CreateViewport();
        PrismRetainedRasterContext rasterContext =
            ExecutorTests.CreateRetainedRasterContext(
                scene.Analysis,
                viewport);

        Execute(renderer, executor, scene, viewport);
        byte[] freshPixels = ToBytes(renderer.ReadPixels());
        Assert.True(
            ExecutorTests.RemoveFinalEntries(
                scene.Plan,
                executor.RetainedSurfaceCache,
                rasterContext) > 0);
        PrismRendererDiagnostics before =
            executor.RendererDiagnostics;

        Execute(renderer, executor, scene, viewport);
        byte[] cacheOnPixels = ToBytes(renderer.ReadPixels());
        PrismRendererDiagnostics after =
            executor.RendererDiagnostics;

        return new PrismIntermediateHitObservation(
            freshPixels,
            cacheOnPixels,
            ToInt(after.FinalHitCount - before.FinalHitCount),
            ToInt(
                after.IntermediateHitCount -
                before.IntermediateHitCount),
            ToInt(
                after.SavedPassCount -
                before.SavedPassCount),
            executionDiagnostics.Counters.PassCount);
    }

    public PrismOwnershipObservation
        ExerciseIdentityBudgetAndPinning()
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Owner isolation",
                PrismTestData.Layer(1, "Layer"));
        ContractScene first = CreateScene(
            definition,
            ownerToken: 92_001,
            color: new CernealaColor(220, 40, 70, 255));
        ContractScene second = CreateScene(
            definition,
            ownerToken: 92_002,
            color: new CernealaColor(30, 210, 95, 255));
        PrismRendererOptions options = new()
        {
            SurfaceHardByteLimit = 2L * 1024 * 1024,
            RetainedCacheSoftByteLimit = 1L * 1024 * 1024,
            RetainedCacheEntryLimit = 16
        };
        using ExecutorTests.TestPrismRenderer renderer =
            CreateRenderer();
        using PrismGraphExecutor executor = new(
            GraphicsDevice,
            diagnostics: null,
            options,
            retainedCacheEnabled: true);
        Viewport viewport = CreateViewport();

        Execute(renderer, executor, first, viewport);
        byte[] firstPixels = ToBytes(renderer.ReadPixels());
        PrismRendererDiagnostics beforeSecond =
            executor.RendererDiagnostics;
        Execute(renderer, executor, second, viewport);
        byte[] secondPixels = ToBytes(renderer.ReadPixels());
        PrismRendererDiagnostics afterSecond =
            executor.RendererDiagnostics;
        bool sharedAcrossOwners =
            afterSecond.FinalHitCount >
                beforeSecond.FinalHitCount ||
            PixelsMatch(firstPixels, secondPixels);

        PrismRetainedRasterContext rasterContext =
            ExecutorTests.CreateRetainedRasterContext(
                second.Analysis,
                viewport);
        PrismRetainedSurfaceLease pinnedLease =
            AcquireAny(
                second.Plan,
                executor.RetainedSurfaceCache,
                rasterContext,
                out PrismRetainedCacheKey retainedKey);
        RenderTarget2D pinnedTarget = pinnedLease.Surface;
        executor.RetainedSurfaceCache.Clear();
        bool evictedPinnedEntry = pinnedTarget.IsDisposed;
        Assert.False(
            executor.RetainedSurfaceCache.Contains(retainedKey));
        pinnedLease.Dispose();

        PrismRendererDiagnostics budgetSnapshot =
            afterSecond;
        bool budgetWasRespected =
            budgetSnapshot.RetainedEntryCount <=
                options.RetainedCacheEntryLimit &&
            budgetSnapshot.RetainedByteCount <=
                options.RetainedCacheSoftByteLimit &&
            budgetSnapshot.TotalByteCount <=
                options.SurfaceHardByteLimit;

        PrismVerifiedFingerprint firstFingerprint = new(
            ImmutableArray.Create(1L, 2L, 3L),
            fastHash: 42);
        PrismVerifiedFingerprint collidingFingerprint = new(
            ImmutableArray.Create(1L, 2L, 4L),
            fastHash: 42);
        bool acceptedHashCollision =
            (retainedKey with
            {
                StructuralFingerprint = firstFingerprint
            }).Equals(
                retainedKey with
                {
                    StructuralFingerprint = collidingFingerprint
                });

        bool fallbackMatchedFreshOutput =
            RenderRejectedPromotionMatchesFresh(second, viewport);

        return new PrismOwnershipObservation(
            sharedAcrossOwners,
            acceptedHashCollision,
            evictedPinnedEntry,
            budgetWasRespected,
            fallbackMatchedFreshOutput);
    }

    public PrismLifecycleObservation ExerciseLifecycle(
        PrismCacheLifecycleCase lifecycleCase)
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Lifecycle",
                PrismTestData.Layer(1, "Layer"));
        ContractScene initial = CreateScene(
            definition,
            OwnerToken,
            visualContentVersion: 1,
            configure: instance =>
                instance.GetLayerState(
                    new PrismNodeId(1)).Opacity = 0.25f,
            color: new CernealaColor(220, 35, 60, 255));
        ContractScene changed = CreateScene(
            definition,
            OwnerToken,
            visualContentVersion: 2,
            configure: instance =>
                instance.GetLayerState(
                    new PrismNodeId(1)).Opacity = 0.75f,
            color: new CernealaColor(25, 205, 90, 255));
        using ExecutorTests.TestPrismRenderer renderer =
            CreateRenderer();
        using ExecutorTests.TestPrismRenderer freshRenderer =
            CreateRenderer();
        using PrismGraphExecutor executor = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: true);
        using PrismGraphExecutor fresh = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: false);
        Viewport viewport = CreateViewport();

        Execute(renderer, executor, initial, viewport);
        byte[] stalePixels = ToBytes(renderer.ReadPixels());
        PrismRendererDiagnostics before =
            executor.RendererDiagnostics;

        switch (lifecycleCase)
        {
            case PrismCacheLifecycleCase.ExecutionException:
                renderer.ThrowOnNextRenderCommand = true;
                Assert.Throws<InvalidOperationException>(
                    () => Execute(
                        renderer,
                        executor,
                        changed,
                        viewport));
                break;
            case PrismCacheLifecycleCase.Detach:
            case PrismCacheLifecycleCase.Hidden:
            case PrismCacheLifecycleCase.Collapsed:
            case PrismCacheLifecycleCase.Replacement:
                executor.Invalidate(
                    PrismCacheInvalidation.ForOwner(
                        new PrismCacheOwnerToken(
                            OwnerToken)));
                break;
            case PrismCacheLifecycleCase.DeviceReset:
                executor.Reset();
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(lifecycleCase),
                    lifecycleCase,
                    "Unknown Prism lifecycle case.");
        }

        int partialEntryCount =
            executor.RetainedSurfaceCache.EntryCount;
        byte[] actualPixels;
        byte[] freshPixels;
        if (lifecycleCase is PrismCacheLifecycleCase.Hidden or
            PrismCacheLifecycleCase.Collapsed)
        {
            actualPixels = ReadCleared(renderer);
            freshPixels = ReadCleared(freshRenderer);
        }
        else
        {
            Execute(renderer, executor, changed, viewport);
            actualPixels = ToBytes(renderer.ReadPixels());
            Execute(freshRenderer, fresh, changed, viewport);
            freshPixels = ToBytes(freshRenderer.ReadPixels());
        }

        PrismRendererDiagnostics after =
            executor.RendererDiagnostics;
        bool reusedInvalidPixels =
            !PixelsMatch(actualPixels, freshPixels) ||
            (PixelsMatch(actualPixels, stalePixels) &&
                !PixelsMatch(freshPixels, stalePixels));

        return new PrismLifecycleObservation(
            freshPixels,
            actualPixels,
            partialEntryCount,
            executor.RetainedSurfaceCache.ActiveLeaseCount,
            ToInt(after.LookupCount - before.LookupCount),
            ToInt(after.PromotionCount - before.PromotionCount),
            reusedInvalidPixels);
    }

    public void Dispose()
    {
        fixture.Dispose();
    }

    private ExecutorTests.PrismRetainedScenario CreateScenario(
        PrismCacheScenario scenario) =>
        scenario switch
        {
            PrismCacheScenario.Simple =>
                ExecutorTests.CreateAlphaRetainedScenario(),
            PrismCacheScenario.Mask =>
                ExecutorTests.CreateComplexRetainedScenario(
                    GraphicsDevice),
            PrismCacheScenario.Group =>
                ExecutorTests.CreateComplexRetainedScenario(
                    GraphicsDevice),
            PrismCacheScenario.Nested =>
                ExecutorTests.CreateNestedRetainedScenario(),
            PrismCacheScenario.Backdrop =>
                ExecutorTests.CreateBackdropRetainedScenario(
                    GraphicsDevice),
            _ => throw new ArgumentOutOfRangeException(
                nameof(scenario),
                scenario,
                "Unknown Prism cache scenario.")
        };

    private MutationPair CreateMutationPair(
        PrismCacheMutation mutation)
    {
        if (mutation == PrismCacheMutation.Resource)
        {
            return CreateResourceMutationPair();
        }
        if (mutation is PrismCacheMutation.LowerUi or
            PrismCacheMutation.BackdropContentVersion)
        {
            return CreateBackdropMutationPair(mutation);
        }

        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Mutation",
                PrismTestData.Layer(1, "Layer"));
        ContractScene baseline;
        ContractScene changed;
        Action<PrismGraphExecutor>? prepareChanged = null;

        switch (mutation)
        {
            case PrismCacheMutation.Content:
                baseline = CreateScene(
                    definition,
                    OwnerToken,
                    visualContentVersion: 1,
                    color: new CernealaColor(
                        220,
                        40,
                        70,
                        255));
                changed = CreateScene(
                    definition,
                    OwnerToken,
                    visualContentVersion: 2,
                    color: new CernealaColor(
                        35,
                        205,
                        95,
                        255));
                break;
            case PrismCacheMutation.Structure:
                baseline = CreateScene(
                    definition,
                    OwnerToken);
                changed = CreateScene(
                    PrismTestData.Composition(
                        "Mutation changed",
                        PrismTestData.Layer(1, "Layer"),
                        PrismTestData.Layer(2, "Second")),
                    OwnerToken);
                break;
            case PrismCacheMutation.Parameter:
            case PrismCacheMutation.Motion:
                baseline = CreateScene(
                    definition,
                    OwnerToken,
                    configure: instance =>
                        instance.GetLayerState(
                            new PrismNodeId(1)).Opacity =
                            0.25f);
                changed = CreateScene(
                    definition,
                    OwnerToken,
                    configure: instance =>
                        instance.GetLayerState(
                            new PrismNodeId(1)).Opacity =
                            mutation ==
                                PrismCacheMutation.Motion
                                ? 0.85f
                                : 0.65f);
                break;
            case PrismCacheMutation.Bounds:
                baseline = CreateScene(
                    definition,
                    OwnerToken);
                changed = CreateScene(
                    definition,
                    OwnerToken,
                    width: ExecutorTests.SurfaceWidth - 2,
                    height: ExecutorTests.SurfaceHeight - 1);
                break;
            case PrismCacheMutation.PixelScale:
                baseline = CreateScene(
                    definition,
                    OwnerToken);
                changed = CreateScene(
                    definition,
                    OwnerToken,
                    pixelScale: 1.25f);
                break;
            case PrismCacheMutation.ColorProfile:
            case PrismCacheMutation.SurfaceFormat:
            case PrismCacheMutation.CapabilitySet:
            case PrismCacheMutation.ShaderPackage:
                baseline = CreateScene(
                    definition,
                    OwnerToken);
                changed = baseline;
                prepareChanged = executor =>
                    executor.EnsureRasterContext(
                        CreateChangedRasterContext(
                            mutation,
                            changed.Analysis));
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(mutation),
                    mutation,
                    "Unknown Prism cache mutation.");
        }

        return new MutationPair(
            baseline,
            changed,
            prepareChanged,
            ownedResource: null);
    }

    private MutationPair CreateResourceMutationPair()
    {
        Texture2D texture = new(
            GraphicsDevice,
            ExecutorTests.SurfaceWidth,
            ExecutorTests.SurfaceHeight,
            false,
            SurfaceFormat.Color);
        texture.SetData(
            Enumerable.Repeat(
                    XnaColor.White,
                    ExecutorTests.SurfaceWidth *
                        ExecutorTests.SurfaceHeight)
                .ToArray());
        MonoGameImage image = new(texture);
        PrismResourceId resourceId =
            new("ContractMask");
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Resource mutation",
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Masked",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur)
                    ],
                    mask: new PrismMaskDefinition(
                        resourceId)));
        PrismDrawResources firstResources =
            PrismDrawResources.Create(
            [
                new PrismDrawImageResource(
                    resourceId,
                    image,
                    Version: 1,
                    Identity: 93_001)
            ]);
        PrismDrawResources changedResources =
            PrismDrawResources.Create(
            [
                new PrismDrawImageResource(
                    resourceId,
                    image,
                    Version: 2,
                    Identity: 93_001)
            ]);

        return new MutationPair(
            CreateScene(
                definition,
                OwnerToken,
                resources: firstResources),
            CreateScene(
                definition,
                OwnerToken,
                resources: changedResources),
            prepareChanged: null,
            image);
    }

    private MutationPair CreateBackdropMutationPair(
        PrismCacheMutation mutation)
    {
        Texture2D texture = new(
            GraphicsDevice,
            ExecutorTests.SurfaceWidth,
            ExecutorTests.SurfaceHeight,
            false,
            SurfaceFormat.Color);
        texture.SetData(
            Enumerable.Repeat(
                    new XnaColor(38, 112, 210, 255),
                    ExecutorTests.SurfaceWidth *
                        ExecutorTests.SurfaceHeight)
                .ToArray());
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "Backdrop mutation",
                PrismTestData.Layer(1, "Foreground"),
                PrismTestData.Backdrop(2, "Backdrop"));
        PrismBackdropSourceToken sourceToken =
            PrismBackdropSourceToken.CreateUnique();
        long firstLowerUi = 10;
        long changedLowerUi =
            mutation == PrismCacheMutation.LowerUi
                ? 11
                : firstLowerUi;
        long firstContentVersion = 20;
        long changedContentVersion =
            mutation ==
                PrismCacheMutation.BackdropContentVersion
                ? 21
                : firstContentVersion;

        return new MutationPair(
            CreateBackdropScene(
                definition,
                texture,
                sourceToken,
                firstLowerUi,
                firstContentVersion),
            CreateBackdropScene(
                definition,
                texture,
                sourceToken,
                changedLowerUi,
                changedContentVersion),
            prepareChanged: null,
            texture);
    }

    private ContractScene CreateBackdropScene(
        PrismCompositionDefinition definition,
        Texture2D texture,
        PrismBackdropSourceToken sourceToken,
        long lowerUiVersion,
        long contentVersion)
    {
        PrismInstance instance = new(definition);
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(OwnerToken),
            new DrawRect(
                0,
                0,
                ExecutorTests.SurfaceWidth,
                ExecutorTests.SurfaceHeight),
            System.Numerics.Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1,
            PrismDrawResources.Empty,
            lowerUiVersion);
        DrawCommandList commands =
            PrismTestData.Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(
                        0,
                        0,
                        ExecutorTests.SurfaceWidth,
                        ExecutorTests.SurfaceHeight),
                    new CernealaColor(
                        230,
                        70,
                        86,
                        208)),
                DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        BackdropFrameMetadata metadata = new(
            ExecutorTests.SurfaceWidth,
            ExecutorTests.SurfaceHeight,
            1,
            PrismColorProfile.Srgb,
            BackdropPixelFormat.Rgba8Unorm,
            BackdropAlphaMode.Opaque,
            System.Numerics.Matrix3x2.Identity,
            contentVersion);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(
                    analysis,
                    metadata,
                    sourceToken));
        return new ContractScene(
            commands,
            analysis,
            plan,
            new BorrowedBackdropLease(
                texture,
                metadata));
    }

    private static ContractScene CreateScene(
        PrismCompositionDefinition definition,
        long ownerToken,
        int width = ExecutorTests.SurfaceWidth,
        int height = ExecutorTests.SurfaceHeight,
        float pixelScale = 1,
        long visualContentVersion = 1,
        long lowerUiVersion = 0,
        PrismDrawResources? resources = null,
        Action<PrismInstance>? configure = null,
        CernealaColor? color = null)
    {
        PrismInstance instance = new(definition);
        configure?.Invoke(instance);
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(ownerToken),
            new DrawRect(0, 0, width, height),
            System.Numerics.Matrix3x2.Identity,
            pixelScale,
            visualContentVersion,
            resources ?? PrismDrawResources.Empty,
            lowerUiVersion);
        DrawCommandList commands =
            PrismTestData.Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(0, 0, width, height),
                    color ?? CernealaColor.White),
                DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new ContractScene(
            commands,
            analysis,
            plan,
            BackdropLease: null);
    }

    private static PrismRetainedRasterContext
        CreateChangedRasterContext(
            PrismCacheMutation mutation,
            PrismFrameAnalysis analysis)
    {
        PrismColorProfile outputProfile =
            mutation == PrismCacheMutation.ColorProfile
                ? PrismColorProfile.LinearSrgb
                : PrismColorProfile.Srgb;
        BackdropPixelFormat format =
            mutation == PrismCacheMutation.SurfaceFormat
                ? BackdropPixelFormat.Rgba8Unorm
                : BackdropPixelFormat.Rgba16Float;
        PrismGraphCapabilities capabilities =
            analysis.RequiredCapabilities;
        if (mutation == PrismCacheMutation.CapabilitySet)
        {
            capabilities |=
                PrismGraphCapabilities.BackdropInput;
        }
        long shaderPackageVersion =
            PrismKernelRegistry.ShaderPackageVersion +
            (mutation == PrismCacheMutation.ShaderPackage
                ? 1
                : 0);
        return new PrismRetainedRasterContext(
            ExecutorTests.SurfaceWidth,
            ExecutorTests.SurfaceHeight,
            outputProfile,
            format,
            PrismSampling.Linear,
            capabilities,
            shaderPackageVersion);
    }

    private bool RenderRejectedPromotionMatchesFresh(
        ContractScene scene,
        Viewport viewport)
    {
        PrismRendererOptions noRetained = new()
        {
            RetainedCacheSoftByteLimit = 0,
            RetainedCacheEntryLimit = 0
        };
        using ExecutorTests.TestPrismRenderer rejectedRenderer =
            CreateRenderer();
        using ExecutorTests.TestPrismRenderer freshRenderer =
            CreateRenderer();
        using PrismGraphExecutor rejected = new(
            GraphicsDevice,
            diagnostics: null,
            noRetained,
            retainedCacheEnabled: true);
        using PrismGraphExecutor fresh = new(
            GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: false);

        Execute(rejectedRenderer, rejected, scene, viewport);
        byte[] rejectedPixels =
            ToBytes(rejectedRenderer.ReadPixels());
        Execute(freshRenderer, fresh, scene, viewport);
        byte[] freshPixels = ToBytes(freshRenderer.ReadPixels());

        return PixelsMatch(rejectedPixels, freshPixels) &&
            rejected.RendererDiagnostics.RejectedPromotionCount > 0 &&
            rejected.RetainedSurfaceCache.EntryCount == 0;
    }

    private static PrismRetainedSurfaceLease AcquireAny(
        PrismGraphExecutionPlan plan,
        PrismRetainedSurfaceCache cache,
        PrismRetainedRasterContext rasterContext,
        out PrismRetainedCacheKey acquiredKey)
    {
        for (int index = plan.ExecutionOrder.Length - 1;
            index >= 0;
            index--)
        {
            if (!PrismRetainedCacheKey.TryCreate(
                    plan,
                    plan.ExecutionOrder[index],
                    rasterContext,
                    out PrismRetainedCacheKey key) ||
                !cache.TryAcquire(
                    key,
                    out PrismRetainedSurfaceLease? lease))
            {
                continue;
            }

            acquiredKey = key;
            return lease!;
        }

        throw new InvalidOperationException(
            "The retained-cache contract scenario produced no acquirable entry.");
    }

    private static int CountPresentations(
        PrismGraphExecutionPlan plan) =>
        plan.OptimizedGraph.Scopes.Count(
            scope =>
                scope.Depth == 0 &&
                scope.Output.HasValue);

    private static void Execute(
        ExecutorTests.TestPrismRenderer renderer,
        PrismGraphExecutor executor,
        ExecutorTests.PrismRetainedScenario scene,
        Viewport viewport)
    {
        ExecutorTests.ExecuteFrame(
            renderer,
            executor,
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            viewport,
            scene.BackdropLease);
    }

    private static void Execute(
        ExecutorTests.TestPrismRenderer renderer,
        PrismGraphExecutor executor,
        ContractScene scene,
        Viewport viewport)
    {
        ExecutorTests.ExecuteFrame(
            renderer,
            executor,
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            viewport,
            scene.BackdropLease);
    }

    private ExecutorTests.TestPrismRenderer CreateRenderer() =>
        new(
            GraphicsDevice,
            ExecutorTests.SurfaceWidth,
            ExecutorTests.SurfaceHeight);

    private static Viewport CreateViewport() =>
        new(
            0,
            0,
            ExecutorTests.SurfaceWidth,
            ExecutorTests.SurfaceHeight);

    private static byte[] ReadCleared(
        ExecutorTests.TestPrismRenderer renderer)
    {
        renderer.BeginFrame();
        renderer.EndBatch();
        return ToBytes(renderer.ReadPixels());
    }

    private static byte[] ToBytes(XnaColor[] pixels)
    {
        byte[] bytes = new byte[pixels.Length * 4];
        for (int index = 0; index < pixels.Length; index++)
        {
            int offset = index * 4;
            bytes[offset] = pixels[index].R;
            bytes[offset + 1] = pixels[index].G;
            bytes[offset + 2] = pixels[index].B;
            bytes[offset + 3] = pixels[index].A;
        }
        return bytes;
    }

    private static bool PixelsMatch(
        byte[] first,
        byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }
        for (int index = 0; index < first.Length; index++)
        {
            if (Math.Abs(first[index] - second[index]) > 1)
            {
                return false;
            }
        }
        return true;
    }

    private static int ToInt(long value) =>
        checked((int)value);

    private sealed record ContractScene(
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan,
        IBackdropFrameLease? BackdropLease);

    private sealed class MutationPair : IDisposable
    {
        private readonly IDisposable? ownedResource;

        public MutationPair(
            ContractScene baseline,
            ContractScene changed,
            Action<PrismGraphExecutor>? prepareChanged,
            IDisposable? ownedResource)
        {
            Base = baseline;
            Changed = changed;
            PrepareChanged = prepareChanged;
            this.ownedResource = ownedResource;
        }

        public ContractScene Base { get; }

        public ContractScene Changed { get; }

        public Action<PrismGraphExecutor>? PrepareChanged { get; }

        public void Dispose()
        {
            ownedResource?.Dispose();
        }
    }

    private sealed class BorrowedBackdropLease :
        IMonoGameBackdropFrameLease
    {
        public BorrowedBackdropLease(
            Texture2D texture,
            BackdropFrameMetadata metadata)
        {
            Texture = texture;
            Metadata = metadata;
        }

        public Texture2D Texture { get; }

        public BackdropFrameMetadata Metadata { get; }

        public void Dispose()
        {
        }
    }
}

public enum PrismCacheScenario
{
    Simple,
    Mask,
    Group,
    Nested,
    Backdrop
}

public enum PrismCacheMutation
{
    Content,
    Structure,
    Parameter,
    Motion,
    Resource,
    LowerUi,
    BackdropContentVersion,
    Bounds,
    PixelScale,
    ColorProfile,
    SurfaceFormat,
    CapabilitySet,
    ShaderPackage
}

public enum PrismCacheLifecycleCase
{
    ExecutionException,
    Detach,
    Hidden,
    Collapsed,
    Replacement,
    DeviceReset
}

internal readonly record struct PrismCacheComparison(
    byte[] CacheOffPixels,
    byte[] CacheOnPixels);

internal readonly record struct PrismStaticHitObservation(
    byte[] FreshPixels,
    byte[] SecondFramePixels,
    int FinalHitCount,
    int SecondFrameCaptureCount,
    int SecondFrameCoveredPassCount,
    int CachedPresentationCount);

internal readonly record struct PrismMutationObservation(
    PrismCacheMutation Mutation,
    byte[] FreshPixels,
    byte[] CacheOnPixels,
    int FinalHitCount,
    int MissCount);

internal readonly record struct PrismIntermediateHitObservation(
    byte[] FreshPixels,
    byte[] CacheOnPixels,
    int FinalHitCount,
    int IntermediateHitCount,
    int SavedPassCount,
    int ExecutedPassCount);

internal readonly record struct PrismOwnershipObservation(
    bool SharedAcrossControlOwners,
    bool AcceptedHashCollision,
    bool EvictedPinnedEntry,
    bool BudgetWasRespected,
    bool FallbackMatchedFreshOutput);

internal readonly record struct PrismLifecycleObservation(
    byte[] FreshPixels,
    byte[] ActualPixels,
    int PartialEntryCount,
    int OrphanedLeaseCount,
    int LookupCount,
    int PromotionCount,
    bool ReusedInvalidPixels);
