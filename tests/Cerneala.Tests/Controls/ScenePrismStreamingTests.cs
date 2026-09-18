using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using SceneGraph2D = Cerneala.UI.Controls.Scene2D;

namespace Cerneala.Tests.Controls;

public sealed class ScenePrismStreamingTests
{
    public static TheoryData<PrismStyleId, bool> RequiredStyleDomains
    {
        get
        {
            TheoryData<PrismStyleId, bool> cases = new();
            PrismStyleId[] styles = [PrismStyleId.DropShadow, PrismStyleId.InnerShadow, PrismStyleId.OuterGlow,
                PrismStyleId.InnerGlow, PrismStyleId.BevelEmboss, PrismStyleId.Satin, PrismStyleId.Stroke];
            foreach (PrismStyleId style in styles) { cases.Add(style, false); cases.Add(style, true); }
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(RequiredStyleDomains))]
    public void UncoveredStylesRequireAFiniteDomainBeforeAcquiringPresentation(PrismStyleId style, bool tileMap)
    {
        using Fixture fixture = new(filter: null, tileMap, declareDomain: false,
            composition: new("Declared style input", [new PrismLayerDefinition(new(1), "Style", styles: [new(style)])]));
        AssertDomainError(fixture);
        Assert.Empty(fixture.Loads);

        DrawRect domain = new(1000, 1000, 10, 10);
        fixture.Scene.PrismInputDomain = domain;
        fixture.Tick();
        var scope = Assert.Single(new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes);
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
        Assert.Equal(domain, scope.Scope.ControlBounds);
        Assert.Equal(domain, scope.Scope.InputBounds);
        Assert.Equal(["far"], fixture.Loads);

        fixture.Scene.PrismInputDomain = null;
        fixture.Tick();
        AssertDomainError(fixture);
        Assert.Equal(["far"], fixture.Loads);
        Assert.Equal(["far"], fixture.Releases);

        PrismStyleState state = fixture.Prism.GetLayerState(new(1)).Styles[0];
        state.Visible = false;
        fixture.Tick();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["far", "near"], fixture.Loads);
        state.Visible = true;
        fixture.Tick();
        AssertDomainError(fixture);
        Assert.Equal(["far", "near"], fixture.Loads);
        Assert.Equal(["far", "near"], fixture.Releases);
    }

    [Theory]
    [InlineData(PrismFilterId.AddNoise, false)]
    [InlineData(PrismFilterId.AddNoise, true)]
    [InlineData(PrismFilterId.LensBlur, false)]
    [InlineData(PrismFilterId.LensBlur, true)]
    [InlineData(PrismFilterId.RadialBlur, false)]
    [InlineData(PrismFilterId.RadialBlur, true)]
    [InlineData(PrismFilterId.Twirl, false)]
    [InlineData(PrismFilterId.Twirl, true)]
    public void UncoveredFiltersRequireAFiniteDomainBeforeAcquiringPresentation(PrismFilterId filter, bool tileMap)
    {
        using Fixture fixture = new(filter, tileMap, declareDomain: false);
        AssertDomainError(fixture);
        Assert.Empty(fixture.Loads);

        fixture.Scene.PrismInputDomain = new(0, 0, 10, 10);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["near"], fixture.Loads);
    }

    [Theory]
    [InlineData("Wrap", false)]
    [InlineData("Wrap", true)]
    [InlineData("Mirror", false)]
    [InlineData("Mirror", true)]
    public void LiveWrappedEdgesRequireADomainAndClampRestoresAutomaticSelection(string edge, bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.GaussianBlur, tileMap, declareDomain: false);
        PrismFilterState filter = fixture.Prism.GetLayerState(new(1)).Filters[0];
        Assert.Equal(["near"], fixture.Loads);
        filter.SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.EdgeModeKey,
            PrismCatalogRuntime.ResolveSymbol("EdgeMode", edge));
        fixture.Tick();
        AssertDomainError(fixture);
        Assert.Equal(["near"], fixture.Loads);
        Assert.Equal(["near"], fixture.Releases);

        fixture.Scene.PrismInputDomain = new(1000, 1000, 10, 10);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["near", "far"], fixture.Loads);
        fixture.Scene.PrismInputDomain = null;
        fixture.Tick();
        AssertDomainError(fixture);
        Assert.Equal(["near", "far"], fixture.Releases);

        filter.SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.EdgeModeKey,
            PrismCatalogRuntime.ResolveSymbol("EdgeMode", "Clamp"));
        fixture.Tick();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
        Assert.Equal(["near", "far", "near"], fixture.Loads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UncoveredDefinitionReplacementAndInactiveOperationsReevaluateTheDomainRequirement(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Invert, tileMap, declareDomain: false);
        fixture.Prism.ReplaceDefinition(new("Replacement", [new PrismGroupDefinition(new(2), "Group",
            [new PrismLayerDefinition(new(1), "Uncovered", filters: [new(PrismFilterId.AddNoise)])])]));
        fixture.Tick();
        AssertDomainError(fixture);
        Assert.Equal(["near"], fixture.Loads);
        PrismGroupState group = fixture.Prism.GetGroupState(new(2));
        PrismLayerState layer = fixture.Prism.GetLayerState(new(1));
        Action[] hide = [() => group.Visible = false,
            () => { group.Visible = true; group.Opacity = 0; },
            () => { group.Opacity = 1; layer.Visible = false; },
            () => { layer.Visible = true; layer.Opacity = 0; },
            () => { layer.Opacity = 1; layer.Filters[0].Opacity = 0; },
            () => { layer.Filters[0].Opacity = 1; layer.Filters[0].Visible = false; }];
        foreach (Action deactivate in hide)
        {
            deactivate();
            fixture.Tick();
            fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
            Assert.Null(fixture.Surface.PresentationError);
        }
        layer.Filters[0].Visible = true;
        fixture.Tick();
        AssertDomainError(fixture);
        fixture.Prism.ReplaceDefinition(new("Automatic again", [new PrismLayerDefinition(new(1), "Color",
            filters: [new(PrismFilterId.Invert)])]));
        fixture.Tick();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.All(fixture.Loads, id => Assert.Equal("near", id));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedUncoveredStyleNeedsItsOwnDomainDespiteAnAncestorDeclaration(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Threshold, tileMap);
        SceneNode2D child;
        if (tileMap) { child = fixture.Map!; }
        else
        {
            Assert.True(fixture.Items!.TryGetRealizedNode("far", out SceneNode2D? sprite));
            child = sprite!;
        }
        using IDisposable effect = GeneratedMarkup.AttachPrism(child, () => new PrismInstance(
            new("Nested style", [new PrismLayerDefinition(new(1), "Glow", styles: [new(PrismStyleId.OuterGlow)])])));
        fixture.Tick();
        AssertDomainError(fixture);

        DrawRect domain = tileMap ? new(1000, 1000, 10, 10) : new(0, 0, 10, 10);
        child.PrismInputDomain = domain;
        fixture.Tick();
        var scopes = new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes;
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(2, scopes.Length);
        Assert.Equal(domain, scopes[1].Scope.ControlBounds);
    }

    private static void AssertDomainError(Fixture fixture)
    {
        Assert.Empty(fixture.Record());
        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);
        Assert.Contains("PrismInputDomain", Assert.IsType<InvalidOperationException>(fixture.Surface.PresentationError).Message);
    }

    [Theory]
    [InlineData(PrismStyleId.ColorOverlay, false)]
    [InlineData(PrismStyleId.ColorOverlay, true)]
    [InlineData(PrismStyleId.GradientOverlay, false)]
    [InlineData(PrismStyleId.GradientOverlay, true)]
    [InlineData(PrismStyleId.PatternOverlay, false)]
    [InlineData(PrismStyleId.PatternOverlay, true)]
    public void PointwisePaintStylesKeepViewportInterestAndReleaseDistantPayloads(PrismStyleId style, bool tileMap)
    {
        using Fixture fixture = new(filter: null, tileMap, declareDomain: false);
        using IDisposable effect = GeneratedMarkup.AttachPrism(fixture.Scene, () => new PrismInstance(
            new("Pointwise paint", [new PrismLayerDefinition(new(1), "Paint", styles: [new(style)])])));
        fixture.Tick();
        fixture.Record();

        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["near"], fixture.Loads);
        Assert.False(PrismInputDependency.RequiresWholeInput(fixture.Prism));
        Assert.True(PrismInputDependency.TryGetLocalInputOutset(fixture.Prism, 1,
            System.Numerics.Matrix3x2.Identity, out var support));
        Assert.Equal(System.Numerics.Vector2.Zero, support);

        fixture.Surface.ViewBox = new(1000, 1000, 100, 100);
        fixture.Tick();
        var scope = Assert.Single(new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes);
        Assert.Equal(Fixture.WorldBounds, scope.Scope.ControlBounds);
        Assert.Equal(["near", "far"], fixture.Loads);
        Assert.Equal(["near"], fixture.Releases);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(false, 12)]
    [InlineData(true, 12)]
    public void ImageMaskReadsItsOwnResourceWithoutExpandingSceneInput(bool tileMap, float feather)
    {
        using Fixture fixture = new(filter: null, tileMap, declareDomain: false);
        fixture.Root.SetImageLoader(new ImageLoader());
        fixture.Surface.Resources.SetResource(new ResourceId<ImageResource>("mask"), new ImageResource("mask.png"));
        using IDisposable effect = GeneratedMarkup.AttachPrism(fixture.Scene, () => new PrismInstance(
            new("Resource mask", [new PrismLayerDefinition(new(1), "Masked",
                filters: [new(PrismFilterId.Invert)], mask: new(new("mask"), feather: feather))])));
        fixture.Tick();
        fixture.Record();

        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["near"], fixture.Loads);
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);
        Assert.False(PrismInputDependency.RequiresWholeInput(fixture.Prism));
        Assert.True(PrismInputDependency.TryGetLocalInputOutset(fixture.Prism, 1,
            System.Numerics.Matrix3x2.Identity, out var support));
        Assert.Equal(System.Numerics.Vector2.Zero, support);

        PrismMaskState mask = fixture.Prism.GetLayerState(new(1)).Mask!;
        mask.Invert = true;
        mask.Channel = PrismMaskChannel.Luminance;
        fixture.Surface.ViewBox = new(1000, 1000, 100, 100);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(["near", "far"], fixture.Loads);
        Assert.Equal(["near"], fixture.Releases);
        Assert.Equal(1, fixture.Root.ImageResourceCache.ResidentCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void NestedCaptureUsesItsActualContentInsideTheRequiredInputRegion(bool tileMap, bool globalParent)
    {
        using Fixture fixture = new(globalParent ? PrismFilterId.Threshold : PrismFilterId.Average,
            tileMap, declareDomain: globalParent, entries: [new("near", new(10, 20, 10, 10))]);
        SceneNode2D child;
        if (tileMap) { child = fixture.Map!; }
        else
        {
            Assert.True(fixture.Items!.TryGetRealizedNode("near", out SceneNode2D? sprite));
            child = sprite!;
        }
        using IDisposable nested = GeneratedMarkup.AttachPrism(child, () => new PrismInstance(
            new("Bounded child", [new PrismLayerDefinition(new(1), "Blur", filters: [new(PrismFilterId.Blur)])])));
        fixture.Tick();

        var scopes = new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes;

        Assert.Equal(2, scopes.Length);
        Assert.Equal(globalParent ? Fixture.WorldBounds : new DrawRect(10, 20, 10, 10), scopes[0].Scope.InputBounds);
        Assert.Equal(tileMap ? new DrawRect(10, 20, 10, 10) : new DrawRect(0, 0, 10, 10), scopes[1].Scope.InputBounds);
        Assert.Equal(new DrawRect(10, 20, 10, 10), scopes[1].Bounds);
        Assert.Equal(["near"], fixture.Loads);
    }

    [Fact]
    public void LocalFilterPassesComposeAndCachedParametersRemainLive()
    {
        PrismInstance instance = new(new("Local chain", [new PrismLayerDefinition(new(1), "Local",
            filters: [new(PrismFilterId.Average), new(PrismFilterId.GaussianBlur)])]));
        PrismFilterState blur = instance.GetLayerState(new(1)).Filters[1];
        blur.SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.RadiusKey, 2.5f);
        PrismInputDependency.LocalInputCache cache = new();
        Assert.True(cache.TryGet(instance, 1, System.Numerics.Matrix3x2.Identity, out var support));
        Assert.Equal(new System.Numerics.Vector2(4), support);
        Assert.True(cache.TryGet(instance, 2, System.Numerics.Matrix3x2.Identity, out support));
        Assert.Equal(new System.Numerics.Vector2(6), support);
        blur.SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.RadiusKey, 0);
        Assert.True(cache.TryGet(instance, 2, System.Numerics.Matrix3x2.Identity, out support));
        Assert.Equal(System.Numerics.Vector2.One, support);
        instance.ReplaceDefinition(new("Replacement", [new PrismLayerDefinition(new(1), "Global", filters: [new(PrismFilterId.Threshold)])]));
        Assert.False(cache.TryGet(instance, 2, System.Numerics.Matrix3x2.Identity, out _));
    }

    [Fact]
    public void WrappedKernelDoesNotRedefineTheWorldBoundaryAsTheCameraEdge()
    {
        PrismInstance instance = new(new("Wrapped kernel", [new PrismLayerDefinition(new(1), "Blur", filters: [new(PrismFilterId.GaussianBlur)])]));
        PrismInputDependency.LocalInputCache cache = new();
        Assert.True(cache.TryGet(instance, 1, System.Numerics.Matrix3x2.Identity, out _));
        instance.GetLayerState(new(1)).Filters[0].SetValue(
            PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.EdgeModeKey,
            PrismCatalogRuntime.ResolveSymbol("EdgeMode", "Wrap"));
        Assert.False(cache.TryGet(instance, 1, System.Numerics.Matrix3x2.Identity, out _));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SceneScaleProjectsRasterSamplingSupportBackToLocalCoordinates(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Average, tileMap, declareDomain: false);
        fixture.Scene.Scale = 2;
        fixture.Tick();
        var scope = Assert.Single(new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes);
        Assert.Equal(SceneBounds2D.Known(new(-0.5f, -0.5f, 51, 51)),
            SceneSpatialInterest2D.ResolveInputBounds(fixture.Scene, SceneBounds2D.Known(new(0, 0, 50, 50))));
        Assert.Equal(new DrawRect(0, 0, 50.5f, 50.5f), scope.Scope.InputBounds);
        Assert.Equal(new DrawRect(0, 0, 101, 101), scope.Bounds);
        Assert.Equal(["near"], fixture.Loads);
    }

    [Fact]
    public void NestedSceneOwnersComposeInputSupportBeforeMaterialization()
    {
        using Fixture fixture = new(PrismFilterId.Average, tileMap: true, declareDomain: false, entries:
        [new("near", new(0, 0, 10, 10)), new("neighbor", new(101.5f, 0, 10, 10)), new("far", new(1000, 1000, 10, 10))]);
        Assert.Equal(["near"], fixture.Loads);
        using IDisposable nested = GeneratedMarkup.AttachPrism(fixture.Map!, () => new PrismInstance(
            new("Map filter", [new PrismLayerDefinition(new(1), "Average", filters: [new(PrismFilterId.Average)])])));
        fixture.Tick();
        var scopes = new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes;
        Assert.Equal(["near", "neighbor"], fixture.Loads);
        Assert.Equal(2, scopes.Length);
        Assert.Equal(new DrawRect(0, 0, 101, 101), scopes[0].Scope.InputBounds);
        Assert.Equal(new DrawRect(0, 0, 102, 102), scopes[1].Scope.InputBounds);
        Assert.Equal(2, fixture.Map!.GetDiagnosticsSnapshot().DrawnTiles);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovingLocalSupportRetiresUnneededPresentation(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.GaussianBlur, tileMap, declareDomain: false, entries:
        [new("near", new(0, 0, 10, 10)), new("neighbor", new(100.25f, 0, 10, 10)), new("far", new(1000, 1000, 10, 10))]);
        Assert.Equal(["near", "neighbor"], fixture.Loads);
        fixture.Prism.GetLayerState(new(1)).Filters[0].SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.RadiusKey, 0);
        fixture.Tick();
        var commands = fixture.Record();
        Assert.Equal(new DrawRect(0, 0, 100, 100), Assert.Single(new PrismFrameAnalyzer().Analyze(commands).Scopes).Scope.InputBounds);
        if (tileMap) { Assert.Equal(1, fixture.Map!.GetDiagnosticsSnapshot().DrawnTiles); } // Optional warm data is a separate bounded interest.
        else { Assert.Equal(["neighbor"], fixture.Releases); Assert.Equal(1, fixture.Items!.RealizedItemCount); }
        Assert.DoesNotContain("far", fixture.Loads);
    }

    [Theory]
    [InlineData(PrismBlendMode.PassThrough, 3)]
    [InlineData(PrismBlendMode.Normal, 2)]
    public void LocalGroupFootprintIncludesItsActualBackgroundDependency(PrismBlendMode blend, float expected)
    {
        PrismInstance instance = new(new("Groups",
        [
            new PrismGroupDefinition(new(1), "Group", [new PrismLayerDefinition(new(2), "Child", filters: [new(PrismFilterId.Average)])],
                blendMode: blend, filters: [new(PrismFilterId.Average)]),
            new PrismLayerDefinition(new(3), "Below", filters: [new(PrismFilterId.Average), new(PrismFilterId.Average)])
        ]));
        Assert.True(PrismInputDependency.TryGetLocalInputOutset(instance, 1, System.Numerics.Matrix3x2.Identity, out var support));
        Assert.Equal(new System.Numerics.Vector2(expected), support);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LocalInputProjectionUsesIncomingArrangeSize(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Average, tileMap, declareDomain: false, entries:
        [new("near", new(0, 0, 10, 10)), new("neighbor", new(101.5f, 0, 10, 10)), new("far", new(1000, 1000, 10, 10))]);
        Assert.Equal(["near"], fixture.Loads);
        fixture.Surface.Width = 50;
        fixture.Root.ProcessFrame(); // No later UpdateRenderTime to repair a wrong arrange-time region.
        var commands = fixture.Record();
        Assert.Equal(["near", "neighbor"], fixture.Loads);
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(SceneBounds2D.Known(new(-2, -1, 104, 102)),
            SceneSpatialInterest2D.ResolveInputBounds(fixture.Scene, SceneBounds2D.Known(new(0, 0, 100, 100))));
        Assert.Equal(new DrawRect(0, 0, 102, 101), Assert.Single(new PrismFrameAnalyzer().Analyze(commands).Scopes).Scope.InputBounds);
    }

    [Theory]
    [InlineData(PrismFilterId.Average, false)]
    [InlineData(PrismFilterId.Average, true)]
    [InlineData(PrismFilterId.GaussianBlur, false)]
    [InlineData(PrismFilterId.GaussianBlur, true)]
    [InlineData(PrismFilterId.HighPass, false)]
    [InlineData(PrismFilterId.HighPass, true)]
    [InlineData(PrismFilterId.Sharpen, false)]
    [InlineData(PrismFilterId.Sharpen, true)]
    public void LocalNeighborhoodPreparesNearbyInputWithoutLoadingTheWholeCatalog(PrismFilterId filter, bool tileMap)
    {
        using Fixture fixture = new(filter, tileMap, declareDomain: false, entries:
        [
            new("near", new(0, 0, 10, 10)),
            new("neighbor", new(100.25f, 0, 10, 10)),
            new("far", new(1000, 1000, 10, 10))
        ]);

        DrawCommandList commands = fixture.Record();

        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["near", "neighbor"], fixture.Loads);
        var scope = Assert.Single(new PrismFrameAnalyzer().Analyze(commands).Scopes);
        DrawRect required = Assert.IsType<DrawRect>(scope.Scope.InputBounds);
        SceneBounds2D interest = SceneSpatialInterest2D.ResolveInputBounds(fixture.Scene,
            SceneBounds2D.Known(new(0, 0, 100, 100)));
        Assert.Equal(SceneBoundsKind.Known, interest.Kind);
        Assert.True(interest.Bounds.X < 0 && interest.Bounds.Right > 100 && interest.Bounds.Width < 400, interest.ToString());
        Assert.Equal(0, required.X);
        Assert.Equal(interest.Bounds.Right, required.Right);
        if (tileMap) { Assert.Equal(2, fixture.Map!.GetDiagnosticsSnapshot().DrawnTiles); }
        else { Assert.Equal(2, commands.Count(command => command.Kind == DrawCommandKind.DrawImage)); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GlobalSceneFilterWithoutDeclaredDomainFailsWithoutLoadingTheWorld(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Threshold, tileMap, declareDomain: false);

        fixture.Record();

        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);
        Assert.Contains("PrismInputDomain", Assert.IsType<InvalidOperationException>(fixture.Surface.PresentationError).Message);
        Assert.Empty(fixture.Loads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EnablingAutomaticLevelsRequiresADomainAndRecoversWhenOneIsDeclared(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Levels, tileMap, declareDomain: false);
        Assert.Equal(["near"], fixture.Loads);
        fixture.Prism.GetLayerState(new(1)).Filters[0].SetValue(
            PrismCatalogGenerated.PrismFilterParameterKeys.Levels.AutoKey, true);
        fixture.Tick();
        Assert.Empty(fixture.Record());
        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);
        Assert.Equal(["near"], fixture.Loads);
        Assert.Equal(["near"], fixture.Releases);

        fixture.Scene.PrismInputDomain = new(1000, 1000, 10, 10);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Null(fixture.Surface.PresentationError);
        Assert.Equal(["near", "far"], fixture.Loads);
    }

    [Fact]
    public void MapOwnedDomainUsesLocalCoordinatesThroughMapAndSceneTransforms()
    {
        using Fixture fixture = new(filter: null, tileMap: true);
        TileMap2D map = fixture.Map!;
        DrawRect domain = new(1000, 1000, 10, 10);
        map.PrismInputDomain = domain;
        map.Offset = new(7, 9);
        fixture.Scene.Scale = 2;
        fixture.Scene.TranslateX = 23;
        fixture.Scene.TranslateY = 31;
        using IDisposable effect = GeneratedMarkup.AttachPrism(map, () => new PrismInstance(
            new("Map domain", [new PrismLayerDefinition(new(1), "Global", filters: [new(PrismFilterId.Threshold)])])));
        fixture.Tick();

        var scope = Assert.Single(new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes);

        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(domain, scope.Scope.ControlBounds);
        Assert.Equal(domain, scope.Scope.InputBounds);
        Assert.Equal(new DrawRect(2037, 2049, 20, 20), scope.Bounds);
        Assert.Equal(1, map.GetDiagnosticsSnapshot().DrawnTiles);
        Assert.Equal(["near", "far"], fixture.Loads);
        Assert.Equal(["near"], fixture.Releases);
    }

    [Theory]
    [InlineData(PrismFilterId.BrightnessContrast)]
    [InlineData(PrismFilterId.Levels)]
    [InlineData(PrismFilterId.Exposure)]
    [InlineData(PrismFilterId.Vibrance)]
    [InlineData(PrismFilterId.HueSaturation)]
    [InlineData(PrismFilterId.ColorBalance)]
    [InlineData(PrismFilterId.BlackWhite)]
    [InlineData(PrismFilterId.PhotoFilter)]
    [InlineData(PrismFilterId.ChannelMixer)]
    [InlineData(PrismFilterId.Invert)]
    [InlineData(PrismFilterId.Posterize)]
    [InlineData(PrismFilterId.SelectiveColor)]
    public void PointwiseSceneFilterLoadsOnlyTheVisiblePayload(PrismFilterId filter)
    {
        using Fixture fixture = new(filter);

        DrawCommandList commands = fixture.Record();

        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(["near"], fixture.Loads);
        Assert.Equal(1, fixture.Items!.RealizedItemCount);
        Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(Fixture.WorldBounds,
            Assert.Single(new PrismFrameAnalyzer().Analyze(commands).Scopes).Scope.ControlBounds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GlobalDomainChangesReconcileDataAndRecordOffscreenInput(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Threshold, tileMap, declareDomain: false);
        Assert.Empty(fixture.Loads);
        for (int index = 0; index < 16; index++)
        {
            float origin = index % 2 == 0 ? 1000 : 0;
            DrawRect domain = new(origin, origin, 10, 10);
            fixture.Scene.PrismInputDomain = domain;
            fixture.Tick();
            DrawCommandList commands = fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
            Assert.Equal(index + 1, fixture.Loads.Count);
            Assert.Equal(index, fixture.Releases.Count);
            var scope = Assert.Single(new PrismFrameAnalyzer().Analyze(commands).Scopes);
            Assert.Equal(domain, scope.Scope.ControlBounds);
            Assert.Equal(domain, scope.Scope.InputBounds);
            Assert.Equal(domain, scope.Bounds);
            if (tileMap) { Assert.Equal(1, fixture.Map!.GetDiagnosticsSnapshot().DrawnTiles); }
            else { Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawImage)); }
        }
        fixture.Scene.PrismInputDomain = null;
        fixture.Tick();
        Assert.Empty(fixture.Record());
        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);
        Assert.Equal(16, fixture.Releases.Count);
    }

    [Fact]
    public void DomainMustBeFiniteWithPositiveArea()
    {
        SceneGraph2D scene = new();
        (float X, float Y, float Width, float Height)[] invalid = [(0, 0, 0, 1), (0, 0, -1, 1), (float.NaN, 0, 1, 1),
            (0, 0, float.PositiveInfinity, 1), (float.MaxValue, 0, float.MaxValue, 1)];
        foreach (var domain in invalid)
        {
            Assert.ThrowsAny<ArgumentException>(() => scene.PrismInputDomain = new(domain.X, domain.Y, domain.Width, domain.Height));
            Assert.Null(scene.PrismInputDomain);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyPresentationInterestDoesNotPrepareAGlobalDomain(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Threshold, tileMap);
        ISceneSpatialParticipant2D source = tileMap ? fixture.Map! : fixture.Items!;
        source.UpdateSpatialInterest(SceneBounds2D.Empty, []);
        Assert.Equal(2, fixture.Releases.Count);
    }

    [Fact]
    public void NestedSpriteCaptureKeepsItsOwnBoundsInsideTheAncestorDomain()
    {
        using Fixture fixture = new(PrismFilterId.Threshold);
        Assert.True(fixture.Items!.TryGetRealizedNode("far", out SceneNode2D? sprite));
        using IDisposable effect = GeneratedMarkup.AttachPrism(sprite!, () => new PrismInstance(
            new("Nested pointwise", [new PrismLayerDefinition(new(1), "Color", filters: [new(PrismFilterId.Invert)])])));
        fixture.Tick();
        var scopes = new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes;
        Assert.Equal(2, scopes.Length);
        Assert.Equal(Fixture.WorldBounds, scopes[0].Bounds);
        Assert.Equal(new DrawRect(0, 0, 10, 10), scopes[1].Scope.InputBounds);
        Assert.Equal(new DrawRect(1000, 1000, 10, 10), scopes[1].Bounds);
    }

    [Fact]
    public void NestedGlobalCompositionRequiresItsOwnDomain()
    {
        using Fixture fixture = new(PrismFilterId.Threshold);
        Assert.True(fixture.Items!.TryGetRealizedNode("far", out SceneNode2D? sprite));
        using IDisposable effect = GeneratedMarkup.AttachPrism(sprite!, () => new PrismInstance(
            new("Nested global", [new PrismLayerDefinition(new(1), "Analysis", filters: [new(PrismFilterId.Threshold)])])));
        fixture.Tick();
        Assert.Empty(fixture.Record());
        Assert.Equal(RenderSurface2DPresentationState.Error, fixture.Surface.PresentationState);

        sprite!.PrismInputDomain = new(0, 0, 10, 10);
        fixture.Tick();
        var scopes = new PrismFrameAnalyzer().Analyze(fixture.Record()).Scopes;
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(2, scopes.Length);
        Assert.Equal(new DrawRect(0, 0, 10, 10), scopes[1].Scope.ControlBounds);
        Assert.Equal(new DrawRect(1000, 1000, 10, 10), scopes[1].Bounds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PointwisePrismRetiresDataWhenTheCameraMoves(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Invert, tileMap);
        fixture.Record();
        Assert.Equal(["near"], fixture.Loads);

        for (int cycle = 0; cycle < 16; cycle++)
        {
            fixture.Surface.ViewBox = new(1000, 1000, 100, 100);
            fixture.Tick();
            DrawCommandList far = fixture.Record();
            Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
            Assert.Equal(Fixture.WorldBounds,
                Assert.Single(new PrismFrameAnalyzer().Analyze(far).Scopes).Scope.ControlBounds);
            Assert.Equal(fixture.Loads.Count - 1, fixture.Releases.Count);

            fixture.Surface.ViewBox = new(0, 0, 100, 100);
            fixture.Tick();
            fixture.Record();
            Assert.Equal(fixture.Loads.Count - 1, fixture.Releases.Count);
        }
        Assert.Equal(33, fixture.Loads.Count);
        Assert.Equal(32, fixture.Releases.Count);
    }

    [Fact]
    public void LogicalSourceBoundsAndLayerOrderingDoNotDependOnRealization()
    {
        using Fixture fixture = new(filter: null);
        Assert.Equal(1, fixture.Items!.RealizedItemCount);
        Sprite2D sibling = new() { Image = new(new TestImage()), Width = 20, Height = 20 };
        fixture.Scene.Children.Add(sibling);
        fixture.Scene.OrderMode = SceneOrderMode.LayerThenY;

        fixture.Record();

        Assert.Equal(SceneBounds2D.Known(Fixture.WorldBounds), fixture.Items.GetLocalBounds());
        Assert.Equal([sibling, fixture.Items], fixture.Scene.RecordedOrder.Select(entry => entry.Node));
    }

    [Theory]
    [InlineData(PrismFilterId.Threshold)]
    [InlineData(PrismFilterId.GaussianBlur)]
    public void DeclaredNonPointwiseDomainPreparesItsCompleteInput(PrismFilterId filter)
    {
        using Fixture fixture = new(filter);
        fixture.Record();
        Assert.Equal(["near", "far"], fixture.Loads);
        Assert.Equal(2, fixture.Items!.RealizedItemCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveLevelsAutoAndDefinitionReplacementReevaluateInput(bool tileMap)
    {
        using Fixture fixture = new(PrismFilterId.Levels, tileMap);
        PrismInstance prism = fixture.Prism;
        PrismFilterState filter = prism.GetLayerState(new(1)).Filters[0];
        Assert.Equal(["near"], fixture.Loads);

        filter.SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.Levels.AutoKey, true);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(["near", "far"], fixture.Loads);
        Assert.Empty(fixture.Releases);

        filter.SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.Levels.AutoKey, false);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(["far"], fixture.Releases);
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);

        prism.ReplaceDefinition(new("Replacement", [new PrismGroupDefinition(new(2), "Group",
            [new PrismLayerDefinition(new(3), "Global", filters: [new(PrismFilterId.Threshold)])])]));
        fixture.Tick();
        fixture.Record();
        Assert.Equal(["near", "far", "far"], fixture.Loads);
        prism.GetGroupState(new(2)).Visible = false;
        fixture.Tick();
        fixture.Record();
        Assert.Equal(["far", "far"], fixture.Releases);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void InactiveNonPointwiseOperationsDoNotHoldOffscreenPayloads(int channel)
    {
        using Fixture fixture = new(PrismFilterId.Threshold);
        PrismLayerState layer = fixture.Prism.GetLayerState(new(1));
        switch (channel)
        {
            case 0: layer.Filters[0].Visible = false; break;
            case 1: layer.Filters[0].Opacity = 0; break;
            case 2: layer.Visible = false; break;
            case 3: layer.Opacity = 0; break;
        }
        fixture.Tick();
        fixture.Record();
        Assert.Equal(["far"], fixture.Releases);
        Assert.Equal(1, fixture.Items!.RealizedItemCount);
    }

    [Theory]
    [InlineData(PrismFilterId.Curves)]
    [InlineData(PrismFilterId.ColorLookup)]
    [InlineData(PrismFilterId.GradientMap)]
    public void ColorIndexedResourcesDoNotReadNeighboringSourcePixels(PrismFilterId filter)
    {
        PrismInstance prism = new(new("Lookup", [new PrismLayerDefinition(new(1), "Color", filters: [new(filter)])]));
        Assert.False(PrismInputDependency.RequiresWholeInput(prism));
    }

    [Fact]
    public void MaskResourceDoesNotExpandTheSceneDependencyOfAnInactiveNeighborhoodStyle()
    {
        PrismInstance prism = new(new("Mask and style", [new PrismLayerDefinition(new(1), "Color",
            filters: [new(PrismFilterId.Invert)], styles: [new(PrismStyleId.DropShadow)],
            mask: new(new PrismResourceId("mask")))]));
        PrismLayerState layer = prism.GetLayerState(new(1));
        Assert.True(PrismInputDependency.RequiresWholeInput(prism));
        layer.Styles[0].Visible = false;
        Assert.False(PrismInputDependency.RequiresWholeInput(prism));
        layer.Mask!.Density = 0;
        Assert.False(PrismInputDependency.RequiresWholeInput(prism));
        layer.Styles[0].Visible = true;
        Assert.True(PrismInputDependency.RequiresWholeInput(prism));
    }

    [Fact]
    public void CatalogPublicationUpdatesLogicalBoundsWithoutLoadingOffscreenData()
    {
        using Fixture fixture = new(PrismFilterId.Invert);
        SceneItems2D items = fixture.Items!;
        Assert.Equal(SceneBounds2D.Known(Fixture.WorldBounds), items.GetLocalBounds());
        var source = Assert.IsType<SceneSpatialSource2D<object>>(items.ItemsSource);
        source.SetEntries([new("near", new(0, 0, 10, 10), null), new("far", new(-100, 0, 10, 10), null)]);
        Assert.Equal(SceneBounds2D.Known(new(-100, 0, 110, 10)), items.GetLocalBounds());
        fixture.Record();
        Assert.Equal(["near"], fixture.Loads);
        source.SetEntries([]);
        Assert.Equal(SceneBounds2D.Empty, items.GetLocalBounds());
        items.ItemsSource = null;
        Assert.Equal(SceneBounds2D.Empty, items.GetLocalBounds());
    }

    [Fact]
    public void NestedSceneTransformsUseTheLocalViewportAndPreserveCaptureBounds()
    {
        using Fixture fixture = new(PrismFilterId.Invert);
        fixture.Scene.TranslateX = 200;
        fixture.Scene.TranslateY = 300;
        fixture.Scene.Scale = 2;
        fixture.Surface.ViewBox = new(200, 300, 200, 200);
        fixture.Tick();
        DrawCommandList commands = fixture.Record();
        Assert.Equal(RenderSurface2DPresentationState.Ready, fixture.Surface.PresentationState);
        Assert.Equal(1, fixture.Items!.RealizedItemCount);
        Assert.Equal(Fixture.WorldBounds, Assert.Single(new PrismFrameAnalyzer().Analyze(commands).Scopes).Scope.ControlBounds);
    }

    [Theory]
    [InlineData(PrismFilterId.Invert)]
    [InlineData(PrismFilterId.Average)]
    public void OffscreenNpcKeepsIdentityAndCollisionButReleasesItsImageUnderLocalPrism(PrismFilterId filter)
    {
        using Fixture fixture = new(filter, declareDomain: false);
        ImageLoader loader = new();
        fixture.Root.SetImageLoader(loader);
        ResourceId<ImageResource> image = new("npc");
        fixture.Surface.Resources.SetResource(image, new ImageResource("npc.png"));
        Sprite2D npc = new() { X = 16, Y = 16, Width = 10, Height = 10, Image = new(image),
            Collider = new BoxCollider2D { Width = 10, Height = 10 } };
        int loads = 0, releases = 0;
        SceneItems2D actors = new() { ItemsSource = new SceneSpatialSource2D<object>(
            [new("npc", new(0, 0, 64, 64), isSimulated: true)], (_, _) =>
            {
                loads++;
                return ValueTask.FromResult(new SceneSpatialLease2D<object>(npc, _ => releases++));
            }) };
        Sprite2D wall = new() { X = 40, Y = 16, Collider = new BoxCollider2D { Width = 4, Height = 10 } };
        fixture.Scene.Children.Add(actors);
        fixture.Scene.Children.Add(wall);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(1, fixture.Root.ImageResourceCache!.ResidentCount);
        fixture.Surface.ViewBox = new(1000, 1000, 100, 100);
        fixture.Tick();
        fixture.Record();
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
        Assert.Equal(1, loader.Disposed);
        for (int index = 0; index < 16; index++)
        {
            var blocked = fixture.Scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(100, 0));
            Assert.Same(wall.Collider, blocked.Collision!.Collider);
            var movement = fixture.Scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(index % 2 == 0 ? 2 : -2, 0));
            Assert.Null(movement.Collision);
            npc.X += movement.Travel.X;
            fixture.Tick();
            Assert.Single(fixture.Record().Where(command => command.Kind == DrawCommandKind.DrawImage));
            Assert.True(actors.TryGetRealizedNode("npc", out SceneNode2D? current));
            Assert.Same(npc, current);
            Assert.True(npc.IsAttached && npc.IsVisible && npc.Collider!.Enabled);
        }
        Assert.Equal(16, npc.X);
        Assert.Equal(1, loads);
        Assert.Equal(0, releases);
        Assert.Equal(0, fixture.Root.ImageResourceCache.ResidentCount);
    }

    private sealed class Fixture : IDisposable
    {
        internal static readonly DrawRect WorldBounds = new(0, 0, 1010, 1010);
        private readonly IDisposable? effect;
        private int frames;
        internal UIRoot Root { get; } = new(100, 100);

        internal Fixture(PrismFilterId? filter, bool tileMap = false, bool declareDomain = true,
            SceneSpatialEntry2D[]? entries = null, PrismCompositionDefinition? composition = null)
        {
            if (declareDomain) { Scene.PrismInputDomain = WorldBounds; }
            entries ??=
            [
                new("near", new(0, 0, 10, 10), null),
                new("far", new(1000, 1000, 10, 10), null)
            ];
            if (tileMap)
            {
                ImageReference picture = new(new TestImage());
                TileMapCatalog2D catalog = new("pointwise-map",
                    entries.Select(entry => new TileMapChunkInfo2D(entry, 1, [picture])));
                Map = new() { Source = new(catalog, (_, info, _) =>
                {
                    SceneSpatialEntry2D entry = info.Spatial;
                    Loads.Add(entry.Id);
                    TileMapChunkData2D data = new([new Tile(picture, x: entry.Bounds.X,
                        y: entry.Bounds.Y, width: 10, height: 10)]);
                    return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(data,
                        _ => Releases.Add(entry.Id)));
                }) };
                Scene.Children.Add(Map);
            }
            else
            {
                Items = new() { ItemsSource = new SceneSpatialSource2D<object>(entries, (entry, _) =>
                {
                    Loads.Add(entry.Id);
                    Sprite2D sprite = new() { Image = new(new TestImage()),
                        X = entry.Bounds.X, Y = entry.Bounds.Y, Width = 10, Height = 10 };
                    return ValueTask.FromResult(new SceneSpatialLease2D<object>(sprite,
                        _ => Releases.Add(entry.Id)));
                }) };
                Scene.Children.Add(Items);
            }
            if (composition is not null || filter is not null)
            {
                effect = GeneratedMarkup.AttachPrism(Scene, () => new PrismInstance(
                    composition ?? new PrismCompositionDefinition("Streaming input", [new PrismLayerDefinition(
                        new(1), "Adjustment", filters: [new(filter!.Value)])])));
            }
            Surface.Scene = Scene;
            Root.VisualChildren.Add(Surface);
            Tick();
        }

        internal SceneGraph2D Scene { get; } = new();
        internal SceneItems2D? Items { get; }
        internal TileMap2D? Map { get; }
        internal RenderSurface2D Surface { get; } = new() { ViewBox = new(0, 0, 100, 100) };
        internal List<string> Loads { get; } = [];
        internal List<string> Releases { get; } = [];
        internal PrismInstance Prism
        {
            get
            {
                Assert.True(PrismAttachment.TryGetInstance(Scene, out PrismInstance? instance));
                return instance!;
            }
        }
        internal void Tick()
        {
            Root.ProcessFrame();
            ((ITimeSensitiveRenderElement)Surface).UpdateRenderTime(TimeSpan.FromMilliseconds(++frames * 16));
        }
        internal DrawCommandList Record()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)Surface).RecordFrame(commands, Surface.GetPresentationBounds());
            return commands;
        }
        public void Dispose()
        {
            Root.VisualChildren.Remove(Surface);
            effect?.Dispose();
        }
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 10;
        public int Height => 10;
    }

    private sealed class ImageLoader : IAsyncImageLoader
    {
        internal int Disposed;
        public IDrawImage Load(string path) => new Image(this);
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
        private sealed class Image(ImageLoader owner) : IDrawImage, IDisposable
        {
            public int Width => 10;
            public int Height => 10;
            public void Dispose() => owner.Disposed++;
        }
    }
}
