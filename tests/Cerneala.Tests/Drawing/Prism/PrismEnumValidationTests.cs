using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Controls;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismEnumValidationTests
{
    [Theory]
    [InlineData("UI/Controls/Scene2D.cs")]
    [InlineData("UI/Prism/Definitions/PrismLightingResource.cs")]
    [InlineData("UI/Prism/Runtime/PrismParameterStore.cs")]
    [InlineData("Drawing/Prism/BackdropFrameMetadata.cs")]
    [InlineData("Drawing/Prism/PrismRendererOptions.cs")]
    [InlineData("Drawing/Prism/PrismEnumValidation.cs")]
    [InlineData("Drawing/Prism/Blend/PrismBlendMath.cs")]
    [InlineData("Drawing/Prism/Graph/PrismBackdropFramePolicy.cs")]
    [InlineData("Drawing/Prism/Graph/PrismGraph.cs")]
    [InlineData("Drawing/Prism/Graph/PrismGraphBuilder.cs")]
    [InlineData("Drawing/Prism/Graph/PrismGraphOptimizer.cs")]
    [InlineData("Drawing/Prism/Graph/PrismRetainedCacheKey.cs")]
    [InlineData("Drawing/Prism/Masking/PrismMaskStyle.cs")]
    [InlineData("Drawing/Prism/Styles/PrismStylePlanner.cs")]
    [InlineData("Cerneala.Backends.SdlGpu/Prism/SdlGpuPrismDeviceResources.cs")]
    [InlineData("Cerneala.Backends.SdlGpu/Prism/SdlGpuPrismExecutor.cs")]
    public void ScenePrismStartupPlanningAndExecutionDoNotDiscoverEnumMetadata(string path)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        string source = File.ReadAllText(Path.Combine(directory.FullName, path));
        Assert.DoesNotMatch(@"\bEnum\s*\.\s*(IsDefined|GetValues|GetNames|TryParse|Parse)\b", source);
    }

    [Theory]
    [InlineData(SceneOrderMode.Source)]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void DeclaredSceneOrderModesRemainAccepted(SceneOrderMode mode)
    {
        var scene = new global::Cerneala.UI.Controls.Scene2D { OrderMode = mode };
        Assert.Equal(mode, scene.OrderMode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void InvalidSceneOrderDoesNotChangeTheExistingValue(int value)
    {
        var scene = new global::Cerneala.UI.Controls.Scene2D { OrderMode = SceneOrderMode.Layer };
        Assert.ThrowsAny<ArgumentException>(() => scene.OrderMode = (SceneOrderMode)value);
        Assert.Equal(SceneOrderMode.Layer, scene.OrderMode);
    }

    [Fact]
    public void CanonicalCatalogSymbolsAndStyleHashAliasesKeepTheirValues()
    {
        // Enum metadata is an independent test oracle, not part of the runtime
        // implementation or the performance measurement path.
        foreach (PrismBlendMode mode in Enum.GetValues<PrismBlendMode>())
        {
            Assert.Equal((int)mode, PrismCatalogRuntime.ResolveSymbol("BlendMode", mode.ToString()));
            Assert.Equal(mode, PrismStylePlanner.ResolveBlendMode((int)mode, "BlendMode"));
            int hash = PrismCatalogRuntime.ResolveSymbol("HighlightMode", mode.ToString());
            Assert.Equal(hash, PrismCatalogRuntime.ResolveSymbol("ShadowMode", mode.ToString()));
            Assert.Equal(mode, PrismStylePlanner.ResolveBlendMode(hash, "HighlightMode"));
        }
        AssertSymbols<PrismColorProfile>("WorkingColorProfile");
        AssertSymbols<PrismMaskChannel>("Channel");
        AssertSymbols<PrismKnockout>("Knockout");
        AssertSymbols<PrismBlendIfChannel>("BlendIfChannel");
        Assert.Equal((int)PrismBlendChannels.Rgba, PrismCatalogRuntime.ResolveSymbol("BlendChannels", "RGBA"));
    }

    [Theory]
    [InlineData("normal")]
    [InlineData(" Normal ")]
    [InlineData("145")]
    [InlineData("Normal,Multiply")]
    public void PublicCatalogParametersRejectNoncanonicalEnumSyntax(string symbol)
    {
        PrismCatalogParameterInfo mode = PrismCatalog.GetStyle(PrismStyleId.BevelEmboss).Parameters.Single(
            parameter => parameter.Name == "HighlightMode");
        PrismInstance instance = BevelInstance();
        PrismStyleState style = Assert.Single(instance.GetLayerState(new(1)).Styles);
        string original = style.GetValue<string>(mode);
        PrismValueVersion version = instance.ValueVersion;
        Assert.Throws<ArgumentOutOfRangeException>(() => style.SetValue(mode, symbol));
        Assert.Equal(original, style.GetValue<string>(mode));
        Assert.Equal(version, instance.ValueVersion);
    }

    [Fact]
    public void PublicStyleSymbolsRoundTripAllDeclaredHighlightAndShadowModes()
    {
        PrismInstance instance = BevelInstance();
        PrismStyleState style = Assert.Single(instance.GetLayerState(new(1)).Styles);
        foreach (PrismCatalogParameterInfo parameter in PrismCatalog.GetStyle(PrismStyleId.BevelEmboss)
            .Parameters.Where(parameter => parameter.Name is "HighlightMode" or "ShadowMode"))
        {
            foreach (string symbol in parameter.SymbolOptions)
            {
                style.SetValue(parameter, symbol);
                Assert.Equal(symbol, style.GetValue<string>(parameter));
                PrismValueVersion version = instance.ValueVersion;
                style.SetValue(parameter, symbol);
                Assert.Equal(version, instance.ValueVersion);
            }
        }
    }

    private static PrismInstance BevelInstance() => new(PrismTestData.Composition("Symbol admission",
        new PrismLayerDefinition(new(1), "Styled", styles: [new PrismStyleDefinition(PrismStyleId.BevelEmboss)])));

    [Fact]
    public void ExplicitValidationMatchesEveryDeclaredValueAndRejectsUndefinedIds()
    {
        AssertAdmission<PrismBlendMode>(PrismEnumValidation.IsDefined);
        AssertAdmission<PrismColorProfile>(PrismEnumValidation.IsDefined);
        AssertAdmission<PrismSampling>(PrismEnumValidation.IsDefined);
        AssertAdmission<BackdropPixelFormat>(PrismEnumValidation.IsDefined);
        AssertAdmission<BackdropAlphaMode>(PrismEnumValidation.IsDefined);
        AssertAdmission<PrismKnockout>(PrismEnumValidation.IsDefined);
        AssertAdmission<PrismBlendIfChannel>(PrismEnumValidation.IsDefined);
        AssertAdmission<PrismMaskChannel>(PrismEnumValidation.IsDefined);
    }

    [Fact]
    public void GraphNodeKindsPreserveAdmissionAndValidationOrder()
    {
        PrismCacheOwnerToken owner = new(1);
        AssertAdmission<PrismGraphNodeKind>(kind =>
        {
            Exception? error = Record.Exception(() => new PrismGraphNodeId(owner, 0, kind, 0));
            if (error is null) return true;
            Assert.Equal("kind", Assert.IsType<ArgumentOutOfRangeException>(error).ParamName);
            return false;
        });
        PrismGraphNodeKind unknown = (PrismGraphNodeKind)int.MaxValue;
        Assert.Equal("scopeOwnerToken", Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismGraphNodeId(default, -1, unknown, -1)).ParamName);
        Assert.Equal("definitionNodeId", Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismGraphNodeId(owner, -1, unknown, -1)).ParamName);
        Assert.Equal("ordinal", Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismGraphNodeId(owner, 0, unknown, -1)).ParamName);
        Assert.Equal("analysisScopeIndex", Assert.Throws<ArgumentOutOfRangeException>(
            () => new PrismGraphNodeId(owner, 0, unknown, 0, -1)).ParamName);
    }

    [Fact]
    public void GraphBoundsStatusesPreserveAdmission()
    {
        PrismGraphNodeId node = new(new(1), 0, PrismGraphNodeKind.ControlCapture, 0);
        AssertAdmission<PrismGraphBoundsStatus>(status =>
        {
            Exception? error = Record.Exception(() => new PrismGraphNodePlan(node,
                default, status, default, PrismGraphUncacheableReason.None,
                PrismRetainedCacheCandidateKind.None, default, default, default));
            if (error is null) return true;
            Assert.Equal("boundsStatus", Assert.IsType<ArgumentOutOfRangeException>(error).ParamName);
            return false;
        });
    }

    private static void AssertAdmission<T>(Func<T, bool> isDefined) where T : struct, Enum
    {
        IEnumerable<int> ids = Enumerable.Range(-1, 202)
            .Concat(Enum.GetValues<T>().Select(value => Convert.ToInt32(value)))
            .Append(int.MinValue).Append(int.MaxValue).Distinct();
        foreach (int id in ids)
        {
            T value = (T)Enum.ToObject(typeof(T), id);
            Assert.Equal(Enum.IsDefined(value), isDefined(value));
        }
    }

    private static void AssertSymbols<T>(string property) where T : struct, Enum
    {
        foreach (T value in Enum.GetValues<T>())
        {
            Assert.Equal(Convert.ToInt32(value), PrismCatalogRuntime.ResolveSymbol(property, value.ToString()));
        }
    }
}
