using System.Collections.Immutable;
using System.Numerics;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Drawing.MonoGame.Prism.Cache;

public sealed class PrismRetainedCacheKeyTests
{
    private static readonly PrismRetainedRasterContext RasterContext =
        new(
            surfaceWidth: 20,
            surfaceHeight: 10,
            PrismColorProfile.LinearSrgb,
            BackdropPixelFormat.Rgba8Unorm,
            PrismSampling.Linear,
            PrismGraphCapabilities.ControlCapture |
            PrismGraphCapabilities.FilterProcessing |
            PrismGraphCapabilities.StyleProcessing |
            PrismGraphCapabilities.MaskProcessing |
            PrismGraphCapabilities.GroupProcessing |
            PrismGraphCapabilities.GroupIsolation |
            PrismGraphCapabilities.ColorConversion,
            shaderPackageVersion: 1);

    [Fact]
    public void FingerprintsAreDeterministicAndIgnoreDiagnosticNames()
    {
        PrismCompositionDefinition first = PrismTestData.Composition(
            "First composition name",
            PrismTestData.Layer(1, "First layer name"));
        PrismCompositionDefinition second = PrismTestData.Composition(
            "Completely different composition name",
            PrismTestData.Layer(1, "Completely different layer name"));

        PrismRetainedCacheKey firstKey = FinalKey(
            BuildPlan(first, ownerToken: 71));
        PrismRetainedCacheKey secondKey = FinalKey(
            BuildPlan(second, ownerToken: 71));

        Assert.Equal(firstKey, secondKey);
        Assert.Equal(
            firstKey.StructuralFingerprint,
            secondKey.StructuralFingerprint);
        Assert.Equal(
            firstKey.ValueFingerprint,
            secondKey.ValueFingerprint);
        Assert.Equal(
            firstKey.DependencyFingerprint,
            secondKey.DependencyFingerprint);
        Assert.NotEqual(
            first.Nodes[0].Name,
            second.Nodes[0].Name);
    }

    [Fact]
    public void EveryExplicitKeyDimensionParticipatesInEquality()
    {
        PrismRetainedCacheKey key = FinalKey(
            BuildPlan(
                PrismTestData.Composition(
                    "NearKeys",
                    PrismTestData.Layer(1, "Layer")),
                ownerToken: 81));
        PrismDependencyStamp stamp = key.DependencyStamp;
        PrismGraphNodeId nodeId = key.StableNodeId;
        PrismVerifiedFingerprint differentFingerprint = new(
            ImmutableArray.Create(901L, 902L));

        PrismRetainedCacheKey[] nearKeys =
        [
            key with
            {
                CandidateKind =
                    PrismRetainedCacheCandidateKind.Intermediate
            },
            key with
            {
                StableNodeId = new PrismGraphNodeId(
                    new PrismCacheOwnerToken(
                        nodeId.ScopeOwnerToken.Value + 1),
                    nodeId.DefinitionNodeId,
                    nodeId.Kind,
                    nodeId.Ordinal)
            },
            key with
            {
                DependencyStamp = stamp with
                {
                    CacheOwnerToken = new PrismCacheOwnerToken(
                        stamp.CacheOwnerToken.Value + 1)
                }
            },
            key with
            {
                DependencyStamp = stamp with
                {
                    StructuralVersion = new PrismStructuralVersion(
                        stamp.StructuralVersion.Value + 1)
                }
            },
            key with
            {
                DependencyStamp = stamp with
                {
                    ValueVersion = new PrismValueVersion(
                        stamp.ValueVersion.Value + 1)
                }
            },
            key with
            {
                DependencyStamp = stamp with
                {
                    VisualContentVersion =
                        stamp.VisualContentVersion + 1
                }
            },
            key with
            {
                DependencyStamp = stamp with
                {
                    DescendantVersion =
                        stamp.DescendantVersion + 1
                }
            },
            key with
            {
                StructuralFingerprint = differentFingerprint
            },
            key with
            {
                ValueFingerprint = differentFingerprint
            },
            key with
            {
                DependencyFingerprint = differentFingerprint
            },
            key with
            {
                RasterBounds = key.RasterBounds with
                {
                    WidthBits = key.RasterBounds.WidthBits + 1
                }
            },
            key with { SurfaceWidth = key.SurfaceWidth + 1 },
            key with { SurfaceHeight = key.SurfaceHeight + 1 },
            key with { LowerUiVersion = key.LowerUiVersion + 1 },
            key with { PixelScaleBits = key.PixelScaleBits + 1 },
            key with
            {
                EffectiveTransform = key.EffectiveTransform with
                {
                    M31Bits =
                        key.EffectiveTransform.M31Bits + 1
                }
            },
            key with
            {
                WorkingColorProfile =
                    (PrismColorProfile)998
            },
            key with
            {
                OutputColorProfile =
                    (PrismColorProfile)997
            },
            key with
            {
                SurfaceFormat =
                    BackdropPixelFormat.Rgba16Float
            },
            key with { Sampling = (PrismSampling)996 },
            key with
            {
                CapabilitySet =
                    key.CapabilitySet |
                    PrismGraphCapabilities.BackdropInput
            },
            key with
            {
                ShaderPackageVersion =
                    key.ShaderPackageVersion + 1
            }
        ];

        Assert.All(
            nearKeys,
            nearKey => Assert.NotEqual(key, nearKey));
    }

    [Fact]
    public void MatchingFastHashNeverOverridesVerifiedComponents()
    {
        PrismRetainedCacheKey key = FinalKey(
            BuildPlan(
                PrismTestData.Composition(
                    "Collision",
                    PrismTestData.Layer(1, "Layer")),
                ownerToken: 91));
        PrismVerifiedFingerprint first = new(
            ImmutableArray.Create(1L, 2L, 3L),
            fastHash: 42);
        PrismVerifiedFingerprint collision = new(
            ImmutableArray.Create(1L, 2L, 4L),
            fastHash: 42);

        Assert.Equal(first.FastHash, collision.FastHash);
        Assert.Equal(
            first.Components.Length,
            collision.Components.Length);
        Assert.NotEqual(first, collision);
        Assert.NotEqual(
            key with { StructuralFingerprint = first },
            key with { StructuralFingerprint = collision });
    }

    [Fact]
    public void KeyTypeGraphContainsNoStringsOrRuntimeOwners()
    {
        Type[] forbidden =
        [
            typeof(string),
            typeof(UIElement),
            typeof(PrismInstance),
            typeof(Delegate),
            typeof(IBackdropFrameLease),
            typeof(IDrawImage),
            typeof(ImageResource)
        ];

        AssertTypeGraphIsValueOnly(
            typeof(PrismRetainedCacheKey),
            forbidden,
            []);
    }

    [Fact]
    public void ResourceIdentityAndVersionAreBothRequiredAndFingerprinted()
    {
        PrismResourceId resourceId = new(41);
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "ResourceIdentity",
                PrismTestData.Layer(
                    1,
                    "Masked",
                    mask: new PrismMaskDefinition(resourceId)));

        PrismGraphExecutionPlan missingIdentity = BuildPlan(
            definition,
            ownerToken: 101,
            resources: Resources(
                resourceId,
                identity: 0,
                version: 7));
        PrismGraphExecutionPlan missingVersion = BuildPlan(
            definition,
            ownerToken: 101,
            resources: Resources(
                resourceId,
                identity: 501,
                version: 0));
        PrismGraphExecutionPlan complete = BuildPlan(
            definition,
            ownerToken: 101,
            resources: Resources(
                resourceId,
                identity: 501,
                version: 7));
        PrismGraphExecutionPlan changedIdentity = BuildPlan(
            definition,
            ownerToken: 101,
            resources: Resources(
                resourceId,
                identity: 502,
                version: 7));
        PrismGraphExecutionPlan changedVersion = BuildPlan(
            definition,
            ownerToken: 101,
            resources: Resources(
                resourceId,
                identity: 501,
                version: 8));

        Assert.False(TryFinalKey(missingIdentity, out _));
        Assert.False(TryFinalKey(missingVersion, out _));
        Assert.True(
            TryFinalKey(
                complete,
                out PrismRetainedCacheKey completeKey));
        Assert.True(
            TryFinalKey(
                changedIdentity,
                out PrismRetainedCacheKey identityKey));
        Assert.True(
            TryFinalKey(
                changedVersion,
                out PrismRetainedCacheKey versionKey));
        Assert.NotEqual(completeKey, identityKey);
        Assert.NotEqual(completeKey, versionKey);

        PrismGraphNode mask = Assert.Single(
            complete.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Mask));
        PrismGraphNodePlan maskPlan =
            complete.GetNodePlan(mask.Id);
        Assert.True(maskPlan.IsCacheable);
        Assert.Equal(
            PrismRetainedCacheCandidateKind.Intermediate,
            maskPlan.CacheCandidateKind);
        Assert.Contains(
            maskPlan.CacheDependencies,
            dependency =>
                dependency.Kind ==
                    PrismGraphDependencyKind.Resource &&
                dependency.Key == 501 &&
                dependency.Version == 7);
    }

    [Fact]
    public void BackdropRequiresSourceIdentityAndTracksSourceVersionAndLowerUi()
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "BackdropIdentity",
                PrismTestData.Layer(1, "Content"),
                PrismTestData.Backdrop(2, "Backdrop"));
        BackdropFrameMetadata metadata = BackdropMetadata(
            contentVersion: 11);
        PrismFrameAnalysis unknownAnalysis = Analyze(
            Scope(
                definition,
                ownerToken: 111,
                lowerUiVersion: 17));
        PrismGraphExecutionPlan unknownSource =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(
                    unknownAnalysis,
                    metadata));

        Assert.False(TryFinalKey(unknownSource, out _));
        PrismGraphNode unknownInput = Assert.Single(
            unknownSource.OptimizedGraph.Nodes.Where(
                node =>
                    node.Kind ==
                    PrismGraphNodeKind.BackdropInput));
        Assert.True(
            unknownSource.GetNodePlan(unknownInput.Id)
                .UncacheableReasons.HasFlag(
                    PrismGraphUncacheableReason.FrameBackdrop));

        PrismBackdropSourceToken firstSource =
            PrismBackdropSourceToken.CreateUnique();
        PrismBackdropSourceToken secondSource =
            PrismBackdropSourceToken.CreateUnique();
        PrismRetainedCacheKey first = BackdropKey(
            definition,
            firstSource,
            metadata,
            lowerUiVersion: 17);
        PrismRetainedCacheKey otherSource = BackdropKey(
            definition,
            secondSource,
            metadata,
            lowerUiVersion: 17);
        PrismRetainedCacheKey otherContent = BackdropKey(
            definition,
            firstSource,
            BackdropMetadata(contentVersion: 12),
            lowerUiVersion: 17);
        PrismRetainedCacheKey otherLowerUi = BackdropKey(
            definition,
            firstSource,
            metadata,
            lowerUiVersion: 18);

        Assert.Equal(17, first.LowerUiVersion);
        Assert.NotEqual(first, otherSource);
        Assert.NotEqual(first, otherContent);
        Assert.NotEqual(first, otherLowerUi);
    }

    [Fact]
    public void OptimizerAloneSelectsCaptureExpensiveAndFinalCandidates()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Blurred",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Blur)
            ]);
        PrismGraphExecutionPlan plan = BuildPlan(
            PrismTestData.Composition(
                "Candidates",
                layer),
            ownerToken: 121,
            configure: instance =>
                GeneratedMarkup.SetPrismFilterNumber(
                    instance.GetLayerState(layer.Id).Filters[0],
                    (int)PrismFilterId.Blur,
                    slot: 0,
                    value: 3));
        PrismGraphScope scope =
            Assert.Single(plan.OptimizedGraph.Scopes);

        PrismGraphNode capture = Assert.Single(
            plan.OptimizedGraph.Nodes.Where(
                node =>
                    node.Kind ==
                    PrismGraphNodeKind.ControlCapture));
        Assert.Equal(
            PrismRetainedCacheCandidateKind.Capture,
            plan.GetNodePlan(capture.Id).CacheCandidateKind);
        Assert.All(
            plan.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Filter),
            filter => Assert.Equal(
                PrismRetainedCacheCandidateKind.Intermediate,
                plan.GetNodePlan(filter.Id).CacheCandidateKind));
        PrismGraphNode layerNode = Assert.Single(
            plan.OptimizedGraph.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Layer));
        Assert.Equal(
            PrismRetainedCacheCandidateKind.None,
            plan.GetNodePlan(layerNode.Id).CacheCandidateKind);
        Assert.Equal(
            PrismRetainedCacheCandidateKind.Final,
            plan.GetNodePlan(scope.Output!.Value)
                .CacheCandidateKind);

        Assert.True(
            PrismRetainedCacheKey.TryCreate(
                plan,
                capture.Id,
                RasterContext,
                out _));
        Assert.False(
            PrismRetainedCacheKey.TryCreate(
                plan,
                layerNode.Id,
                RasterContext,
                out _));
        Assert.True(
            PrismRetainedCacheKey.TryCreate(
                plan,
                scope.Output.Value,
                RasterContext,
                out _));
    }

    [Fact]
    public void RuntimeCatalogCachePolicyMatrixCoversEveryOperation()
    {
        Assert.Equal(
            PrismCatalogGenerated.Entries.Length,
            PrismCatalogGenerated.CachePolicyMatrix.Length);

        for (int index = 0;
            index < PrismCatalogGenerated.Entries.Length;
            index++)
        {
            PrismCatalogEntryDescriptor entry =
                PrismCatalogGenerated.Entries[index];
            Assert.Equal(
                entry,
                PrismCatalogGenerated.CachePolicyMatrix[index]);
            Assert.True(entry.Deterministic);
            Assert.True(entry.Cacheable);
            Assert.True(
                entry.CacheDependencies.HasFlag(
                    PrismCatalogCacheDependency.InputPixels));
            Assert.Equal(
                entry.Properties.Length > 0,
                entry.CacheDependencies.HasFlag(
                    PrismCatalogCacheDependency.ParameterValues));
            Assert.Equal(
                entry.Properties.Any(
                    property => string.Equals(
                        property.Name,
                        "Seed",
                        StringComparison.Ordinal)),
                entry.CacheDependencies.HasFlag(
                    PrismCatalogCacheDependency.ExplicitSeed));
            Assert.Equal(
                entry.Properties.Any(
                    property =>
                        property.ValueType ==
                        PrismCatalogValueType.Resource),
                entry.CacheDependencies.HasFlag(
                    PrismCatalogCacheDependency.VersionedResources));
        }
    }

    [Fact]
    public void ProductionIdentityAllocatorsNeverReuseTokens()
    {
        PrismCompositionDefinition definition =
            PrismTestData.Composition(
                "OwnerTokens",
                PrismTestData.Layer(1, "Layer"));
        UIElement firstOwner = new();
        UIElement secondOwner = new();
        using IDisposable firstLease = PrismAttachment.Set(
            firstOwner,
            () => new PrismInstance(definition),
            []);
        using IDisposable secondLease = PrismAttachment.Set(
            secondOwner,
            () => new PrismInstance(definition),
            []);
        Assert.IsType<PrismAttachment>(firstLease).Attach();
        Assert.IsType<PrismAttachment>(secondLease).Attach();

        Assert.True(
            PrismAttachment.TryGetRenderState(
                firstOwner,
                out _,
                out PrismCacheOwnerToken firstOwnerToken));
        Assert.True(
            PrismAttachment.TryGetRenderState(
                secondOwner,
                out _,
                out PrismCacheOwnerToken secondOwnerToken));
        Assert.NotEqual(firstOwnerToken, secondOwnerToken);

        PrismBackdropSourceToken firstSource =
            PrismBackdropSourceToken.CreateUnique();
        PrismBackdropSourceToken secondSource =
            PrismBackdropSourceToken.CreateUnique();
        Assert.NotEqual(firstSource, secondSource);
        Assert.True(secondSource.Value > firstSource.Value);

        ImageResource firstResource =
            new(new TestImage());
        ImageResource secondResource =
            new(new TestImage());
        Assert.NotEqual(
            firstResource.RetainedIdentity,
            secondResource.RetainedIdentity);
        Assert.True(
            secondResource.RetainedIdentity >
            firstResource.RetainedIdentity);
    }

    private static PrismRetainedCacheKey BackdropKey(
        PrismCompositionDefinition definition,
        PrismBackdropSourceToken sourceToken,
        BackdropFrameMetadata metadata,
        long lowerUiVersion)
    {
        PrismFrameAnalysis analysis = Analyze(
            Scope(
                definition,
                ownerToken: 111,
                lowerUiVersion: lowerUiVersion));
        PrismGraph graph = new PrismGraphBuilder().Build(
            analysis,
            metadata,
            sourceToken);
        return FinalKey(
            new PrismGraphOptimizer().Optimize(graph));
    }

    private static BackdropFrameMetadata BackdropMetadata(
        long contentVersion) =>
        new(
            128,
            64,
            1,
            PrismColorProfile.LinearSrgb,
            BackdropPixelFormat.Rgba8Unorm,
            BackdropAlphaMode.Premultiplied,
            Matrix3x2.Identity,
            contentVersion);

    private static PrismGraphExecutionPlan BuildPlan(
        PrismCompositionDefinition definition,
        long ownerToken,
        PrismDrawResources? resources = null,
        Action<PrismInstance>? configure = null)
    {
        PrismDrawScope scope = Scope(
            definition,
            ownerToken,
            resources: resources);
        configure?.Invoke(scope.Instance);
        PrismGraph graph = new PrismGraphBuilder().Build(
            Analyze(scope));
        return new PrismGraphOptimizer().Optimize(graph);
    }

    private static PrismDrawScope Scope(
        PrismCompositionDefinition definition,
        long ownerToken,
        PrismDrawResources? resources = null,
        long lowerUiVersion = 0)
    {
        return new PrismDrawScope(
            new PrismInstance(definition),
            new PrismCacheOwnerToken(ownerToken),
            new DrawRect(0, 0, 20, 10),
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 7,
            resources ?? PrismDrawResources.Empty,
            lowerUiVersion);
    }

    private static PrismFrameAnalysis Analyze(
        PrismDrawScope scope)
    {
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, 5, 5),
                Color.White),
            DrawCommand.EndPrism());
        return new PrismFrameAnalyzer().Analyze(commands);
    }

    private static PrismDrawResources Resources(
        PrismResourceId id,
        long identity,
        long version) =>
        PrismDrawResources.Create(
        [
            new PrismDrawImageResource(
                id,
                new TestImage(),
                version,
                identity)
        ]);

    private static PrismRetainedCacheKey FinalKey(
        PrismGraphExecutionPlan plan)
    {
        Assert.True(
            TryFinalKey(
                plan,
                out PrismRetainedCacheKey key));
        return key;
    }

    private static bool TryFinalKey(
        PrismGraphExecutionPlan plan,
        out PrismRetainedCacheKey key)
    {
        PrismGraphScope scope =
            Assert.Single(plan.OptimizedGraph.Scopes);
        if (scope.Output is not PrismGraphNodeId output)
        {
            key = default;
            return false;
        }

        return PrismRetainedCacheKey.TryCreate(
            plan,
            output,
            RasterContext,
            out key);
    }

    private static void AssertTypeGraphIsValueOnly(
        Type type,
        IReadOnlyList<Type> forbidden,
        HashSet<Type> visited)
    {
        Assert.DoesNotContain(
            forbidden,
            candidate =>
                candidate == type ||
                candidate.IsAssignableFrom(type));
        if (!visited.Add(type) ||
            type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(decimal) ||
            type == typeof(IntPtr) ||
            type == typeof(UIntPtr))
        {
            return;
        }

        if (type.IsArray)
        {
            AssertTypeGraphIsValueOnly(
                type.GetElementType()!,
                forbidden,
                visited);
            return;
        }
        if (type.IsGenericType)
        {
            foreach (Type argument in
                type.GetGenericArguments())
            {
                AssertTypeGraphIsValueOnly(
                    argument,
                    forbidden,
                    visited);
            }
        }

        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic))
        {
            AssertTypeGraphIsValueOnly(
                field.FieldType,
                forbidden,
                visited);
        }
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 1;

        public int Height => 1;
    }
}
