using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.UI.Prism;

public sealed class PrismInstanceTests
{
    [Fact]
    public void PublicCatalogExposesCompleteEditorMetadata()
    {
        Assert.Equal(134, PrismCatalog.Filters.Length);
        Assert.Equal(10, PrismCatalog.Styles.Length);
        Assert.Equal(9, PrismCatalog.Filters.Count(operation => operation.RequiresResource));
        Assert.Single(PrismCatalog.Styles, operation => operation.RequiresResource);
        Assert.All(
            PrismCatalog.Filters.Concat(PrismCatalog.Styles)
                .SelectMany(operation => operation.Parameters)
                .Where(parameter => parameter.ValueKind == PrismCatalogValueKind.Symbol),
            parameter => Assert.NotEmpty(parameter.SymbolOptions));

        PrismCatalogParameterInfo brightness = PrismCatalog.GetFilter(PrismFilterId.BrightnessContrast)
            .Parameters.Single(parameter => parameter.Name == "Brightness");
        Assert.Equal(PrismCatalogValueKind.Number, brightness.ValueKind);
        Assert.Equal(-1d, brightness.Minimum);
        Assert.Equal(1d, brightness.Maximum);
        Assert.Equal("0", brightness.DefaultValue);
    }

    [Fact]
    public void PublicCatalogAccessRoundTripsEveryParameterKind()
    {
        PrismInstance instance = new(CreateEditorComposition());
        PrismLayerState layer = instance.GetLayerState(new PrismNodeId(1));

        PrismFilterState color = layer.Filters[0];
        SetAndAssert(color, "Clamp", false);
        SetAndAssert(color, "Exposure", 0.25f);
        SetAndAssert(color, "Matrix", "Identity");

        PrismFilterState noise = layer.Filters[1];
        SetAndAssert(noise, "Seed", 17);

        PrismFilterState scanlines = layer.Filters[2];
        SetAndAssert(scanlines, "Color", new Color(255, 32, 64, 96));

        PrismFilterState chromatic = layer.Filters[3];
        SetAndAssert(chromatic, "Direction", new Vector4(0f, 1f, 0f, 0f));

        PrismFilterState texturizer = layer.Filters[4];
        PrismCatalogParameterInfo textureImage = Parameter(texturizer, "TextureImage");
        Assert.Equal(default, texturizer.GetValue<PrismResourceId>(textureImage));

        PrismStyleState shadow = Assert.Single(layer.Styles);
        PrismCatalogParameterInfo size = PrismCatalog.GetStyle(shadow.Style)
            .Parameters.Single(parameter => parameter.Name == "Size");
        shadow.SetValue(size, 12f);
        Assert.Equal(12f, shadow.GetValue<float>(size));

        Assert.True(instance.ValueVersion.Value >= 5);
    }

    [Fact]
    public void PublicCatalogAccessRejectsWrongTypesOperationsAndStaleState()
    {
        PrismInstance instance = new(CreateEditorComposition());
        PrismFilterState color = instance.GetLayerState(new PrismNodeId(1)).Filters[0];
        PrismCatalogParameterInfo exposure = Parameter(color, "Exposure");
        PrismCatalogParameterInfo seed = PrismCatalog.GetFilter(PrismFilterId.AddNoise)
            .Parameters.Single(parameter => parameter.Name == "Seed");

        Assert.Throws<ArgumentException>(() => color.GetValue<int>(exposure));
        Assert.Throws<ArgumentException>(() => color.GetValue<int>(seed));

        instance.ReplaceDefinition(CreateComposition());

        Assert.Throws<InvalidOperationException>(() => color.GetValue<float>(exposure));
    }

    [Fact]
    public void InstancesShareDefinitionWithoutSharingValues()
    {
        PrismCompositionDefinition definition = CreateComposition();
        PrismInstance first = new(definition);
        PrismInstance second = new(definition);

        first.GetLayerState(new PrismNodeId(1)).Opacity = 0.25f;

        Assert.Same(definition, first.Definition);
        Assert.Same(definition, second.Definition);
        Assert.Equal(0.25f, first.GetLayerState(new PrismNodeId(1)).Opacity);
        Assert.Equal(1f, second.GetLayerState(new PrismNodeId(1)).Opacity);
        Assert.Equal(1, first.ValueVersion.Value);
        Assert.Equal(0, second.ValueVersion.Value);
    }

    [Fact]
    public void CatalogDefaultsPopulateAdvancedLayerState()
    {
        PrismLayerState layer = new PrismInstance(CreateComposition())
            .GetLayerState(new PrismNodeId(1));

        Assert.Equal(PrismBlendChannels.Rgba, layer.BlendChannels);
        Assert.Equal(PrismKnockout.None, layer.Knockout);
        Assert.False(layer.BlendInteriorStylesAsGroup);
        Assert.True(layer.BlendClippedLayersAsGroup);
        Assert.True(layer.TransparencyShapesLayer);
        Assert.True(layer.LayerMaskHidesStyles);
        Assert.False(layer.VectorMaskHidesStyles);
        Assert.Equal(PrismBlendIfChannel.Gray, layer.BlendIfChannel);
        Assert.Equal(new PrismBlendRange(0f, 0f, 1f, 1f), layer.ThisLayerRange);
        Assert.Equal(new PrismBlendRange(0f, 0f, 1f, 1f), layer.UnderlyingRange);
        Assert.Equal(0, layer.DissolveSeed);
    }

    [Fact]
    public void IdenticalWritesAreNoOpAndResetRestoresDefinitionDefaults()
    {
        PrismInstance instance = new(CreateComposition(opacity: 0.8f));
        PrismLayerState layer = instance.GetLayerState(new PrismNodeId(1));

        layer.Opacity = 0.8f;
        Assert.Equal(0, instance.ValueVersion.Value);

        layer.Opacity = 0.4f;
        instance.ResetToDefaults();

        Assert.Equal(0.8f, layer.Opacity);
        Assert.Equal(2, instance.ValueVersion.Value);

        instance.ResetToDefaults();
        Assert.Equal(2, instance.ValueVersion.Value);
    }

    [Fact]
    public void ReplacementVersionsTopologyAndDataIndependently()
    {
        PrismInstance instance = new(CreateComposition());
        PrismLayerState stale = instance.GetLayerState(new PrismNodeId(1));

        instance.ReplaceDefinition(CreateComposition(opacity: 0.6f));

        Assert.Equal(1, instance.StructuralVersion.Value);
        Assert.Equal(1, instance.ValueVersion.Value);
        Assert.Equal(0.6f, instance.GetLayerState(new PrismNodeId(1)).Opacity);
        Assert.Throws<InvalidOperationException>(() => stale.Opacity = 0.2f);

        PrismCompositionDefinition replacement = new(
            "replacement",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(2),
                    "replacement",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
            ]);
        instance.ReplaceDefinition(replacement);

        Assert.Equal(2, instance.StructuralVersion.Value);
        Assert.Equal(1, instance.ValueVersion.Value);
    }

    [Fact]
    public void GeneratedTypedKeysAddressDenseFilterValues()
    {
        PrismInstance instance = new(CreateComposition());
        PrismFilterState blur = instance.GetLayerState(new PrismNodeId(1)).Filters[0];
        PrismParameterKey<float> radius =
            PrismCatalogGenerated.PrismFilterParameterKeys.Blur.RadiusKey;

        Assert.Equal(1f, blur.GetValue(radius));

        blur.SetValue(radius, 24f);
        Assert.Equal(24f, blur.GetValue(radius));
        Assert.Equal(1, instance.ValueVersion.Value);
    }

    [Fact]
    public void TypedUpdatesAllocateNothingAfterWarmup()
    {
        PrismInstance instance = new(CreateComposition());
        PrismLayerState layer = instance.GetLayerState(new PrismNodeId(1));
        for (int index = 0; index < 32; index++)
        {
            layer.Opacity = (index & 1) == 0 ? 0.4f : 0.6f;
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10_000; index++)
        {
            layer.Opacity = (index & 1) == 0 ? 0.4f : 0.6f;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void RuntimeFoundationDoesNotOwnGpuResources()
    {
        Type[] runtimeTypes =
        [
            typeof(PrismInstance),
            typeof(PrismCompositionState),
            typeof(PrismLayerState),
            typeof(PrismGroupState),
            typeof(PrismBackdropState),
            typeof(PrismFilterState),
            typeof(PrismStyleState),
            typeof(PrismMaskState)
        ];

        Assert.All(
            runtimeTypes.SelectMany(type =>
                type.GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic)),
            field => Assert.DoesNotContain(
                "Microsoft.Xna.Framework.Graphics",
                field.FieldType.FullName ?? string.Empty,
                StringComparison.Ordinal));
    }

    private static PrismCompositionDefinition CreateComposition(float opacity = 1f)
    {
        return new PrismCompositionDefinition(
            "shared",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "content",
                    filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
                    opacity: opacity)
            ]);
    }

    private static PrismCompositionDefinition CreateEditorComposition()
    {
        return new PrismCompositionDefinition(
            "editor",
            [
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "content",
                    filters:
                    [
                        new PrismFilterDefinition(PrismFilterId.Color),
                        new PrismFilterDefinition(PrismFilterId.AddNoise),
                        new PrismFilterDefinition(PrismFilterId.Scanlines),
                        new PrismFilterDefinition(PrismFilterId.ChromaticAberration),
                        new PrismFilterDefinition(PrismFilterId.Texturizer)
                    ],
                    styles: [new PrismStyleDefinition(PrismStyleId.DropShadow)])
            ]);
    }

    private static PrismCatalogParameterInfo Parameter(PrismFilterState state, string name)
    {
        return PrismCatalog.GetFilter(state.Filter)
            .Parameters.Single(parameter => parameter.Name == name);
    }

    private static void SetAndAssert<T>(PrismFilterState state, string name, T value)
    {
        PrismCatalogParameterInfo parameter = Parameter(state, name);
        state.SetValue(parameter, value);
        Assert.Equal(value, state.GetValue<T>(parameter));
    }
}
