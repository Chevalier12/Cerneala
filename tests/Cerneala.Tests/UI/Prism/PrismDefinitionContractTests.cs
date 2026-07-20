using System.Collections.Immutable;
using System.Reflection;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.UI.Prism;

public sealed class PrismDefinitionContractTests
{
    [Fact]
    public void DefinitionsExposeNoPublicMutationSurface()
    {
        Type[] definitionTypes =
        [
            typeof(PrismCompositionDefinition),
            typeof(PrismLayerDefinition),
            typeof(PrismGroupDefinition),
            typeof(PrismBackdropDefinition),
            typeof(PrismFilterDefinition),
            typeof(PrismStyleDefinition),
            typeof(PrismMaskDefinition)
        ];

        foreach (Type definitionType in definitionTypes)
        {
            Assert.Empty(
                definitionType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.SetMethod?.IsPublic == true));
            Assert.Empty(
                definitionType
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(field => !field.IsInitOnly));
        }
    }

    [Fact]
    public void ContentEnumerationIsBottomUpWithoutChangingDeclarationOrder()
    {
        PrismLayerDefinition front = Layer(1, "Front");
        PrismLayerDefinition back = Layer(2, "Back");
        PrismCompositionDefinition composition = new("Card", [front, back]);

        Assert.Equal(["Front", "Back"], composition.Nodes.Select(node => node.Name));
        Assert.Equal(["Back", "Front"], composition.EnumerateContentBottomUp().Select(node => node.Name));
        Assert.Same(front, composition.Nodes[0]);
        Assert.Same(back, composition.Nodes[1]);
    }

    [Fact]
    public void NamesMustBeUniqueWithinAnAddressScope()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new PrismCompositionDefinition(
                "DuplicateNames",
                [Layer(1, "Shared"), Layer(2, "Shared")]));

        Assert.Contains("Shared", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LayerIsALeafAndGroupOwnsNormalChildren()
    {
        Assert.Null(typeof(PrismLayerDefinition).GetProperty("Children"));
        Assert.Equal(
            typeof(ImmutableArray<PrismNodeDefinition>),
            typeof(PrismGroupDefinition).GetProperty(nameof(PrismGroupDefinition.Children))?.PropertyType);

        Assert.Throws<ArgumentException>(
            () => new PrismGroupDefinition(new PrismNodeId(1), "Empty", []));
    }

    [Fact]
    public void BackdropMustBeUniqueAndLastDirectChild()
    {
        PrismBackdropDefinition firstBackdrop = Backdrop(1, "First");
        PrismBackdropDefinition secondBackdrop = Backdrop(2, "Second");
        PrismLayerDefinition layer = Layer(3, "Content");
        PrismCompositionDefinition withoutBackdrop = new("NoBackdrop", [layer]);
        PrismCompositionDefinition withBackdrop = new(
            "OneBackdrop",
            [layer, firstBackdrop]);

        Assert.Null(withoutBackdrop.Backdrop);
        Assert.Same(firstBackdrop, withBackdrop.Backdrop);
        Assert.Throws<ArgumentException>(
            () => new PrismCompositionDefinition("BackdropNotLast", [firstBackdrop, layer]));
        Assert.Throws<ArgumentException>(
            () => new PrismCompositionDefinition("TwoBackdrops", [firstBackdrop, secondBackdrop]));
        Assert.Throws<ArgumentException>(
            () => new PrismGroupDefinition(
                new PrismNodeId(4),
                "NestedBackdrop",
                [firstBackdrop]));
    }

    [Fact]
    public void IndependentDefinitionsHaveStructuralEquality()
    {
        PrismCompositionDefinition first = CompositionSnapshot();
        PrismCompositionDefinition second = CompositionSnapshot();

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DiagnosticSnapshotIsDeterministic()
    {
        string first = CompositionSnapshot().ToDiagnosticString().ReplaceLineEndings("\n");
        string second = CompositionSnapshot().ToDiagnosticString().ReplaceLineEndings("\n");

        Assert.Equal(first, second);
        Assert.Equal(
            """
            Prism Snapshot profile=LinearSrgb light=120/30
              Group #3 name=Effects visible=True opacity=1 blend=PassThrough
                Layer #1 name=Front visible=True opacity=1 fill=1 blend=Normal clipToBelow=False
                  Filter Blur visible=True opacity=1 blend=Normal
                Layer #2 name=Back visible=True opacity=1 fill=1 blend=Normal clipToBelow=False
                  Filter Blur visible=True opacity=1 blend=Normal
              Backdrop #4 name=Glass visible=True opacity=1
                Filter GaussianBlur visible=True opacity=1 blend=Normal
            """.ReplaceLineEndings("\n"),
            first);
    }

    [Fact]
    public void NamedNodesResolveToTypedIdsButCannotBecomeSources()
    {
        PrismCompositionDefinition composition = CompositionSnapshot();

        Assert.True(composition.TryGetNamedNode("Effects.Front", out PrismNodeId front));
        Assert.Equal(new PrismNodeId(1), front);
        Assert.True(composition.TryGetNamedNode("Glass", out PrismNodeId backdrop));
        Assert.Equal(new PrismNodeId(4), backdrop);
        Assert.DoesNotContain(
            typeof(PrismNodeDefinition).Assembly
                .GetTypes()
                .Where(type => type.Namespace == "Cerneala.UI.Prism.Definitions")
                .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)),
            property => property.Name == "Source");
    }

    [Fact]
    public void GeneratedParameterKeysAreTypedAndDense()
    {
        PrismParameterKey<float> radius =
            PrismCatalogGenerated.PrismFilterParameterKeys.Blur.RadiusKey;
        PrismParameterKey<int> edgeMode =
            PrismCatalogGenerated.PrismFilterParameterKeys.Blur.EdgeModeKey;
        PrismParameterKey<int> quality =
            PrismCatalogGenerated.PrismFilterParameterKeys.Blur.QualityKey;
        PrismParameterKey<bool> radial =
            PrismCatalogGenerated.PrismFilterParameterKeys.ChromaticAberration.RadialKey;

        Assert.Equal((int)PrismFilterId.Blur, radius.EntryStableId);
        Assert.Equal(0, radius.Slot);
        Assert.Equal([0, 1], new[] { edgeMode.Slot, quality.Slot }.Order());
        Assert.Equal((int)PrismFilterId.ChromaticAberration, radial.EntryStableId);
        Assert.True(radial.Slot >= 0);
    }

    [Fact]
    public void FoundationSurfaceDoesNotExposeMonoGameTypes()
    {
        Type[] definitionTypes = typeof(PrismCompositionDefinition).Assembly
            .GetTypes()
            .Where(type => type.Namespace == "Cerneala.UI.Prism.Definitions")
            .ToArray();

        Type[] exposedTypes = definitionTypes
            .SelectMany(type =>
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Select(property => property.PropertyType)
                    .Concat(type.GetConstructors().SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType))))
            .ToArray();

        Assert.DoesNotContain(
            exposedTypes,
            type => type.FullName?.StartsWith("Microsoft.Xna.Framework.Graphics.", StringComparison.Ordinal) == true);
    }

    [Theory]
    [InlineData((int)PrismFallbackReason.MissingKernel, (int)PrismFallbackAction.BypassOperation)]
    [InlineData((int)PrismFallbackReason.MissingBackdrop, (int)PrismFallbackAction.OmitBackdrop)]
    [InlineData((int)PrismFallbackReason.InvalidColorProfile, (int)PrismFallbackAction.BypassComposition)]
    public void FallbackPolicyOwnsDegradation(int reason, int expected)
    {
        Assert.Equal(
            (PrismFallbackAction)expected,
            PrismFallbackPolicy.Resolve((PrismFallbackReason)reason));
    }

    private static PrismLayerDefinition Layer(int id, string name)
    {
        return new PrismLayerDefinition(
            new PrismNodeId(id),
            name,
            [new PrismFilterDefinition(PrismFilterId.Blur)]);
    }

    private static PrismBackdropDefinition Backdrop(int id, string name)
    {
        return new PrismBackdropDefinition(
            new PrismNodeId(id),
            name,
            [new PrismFilterDefinition(PrismFilterId.Blur)]);
    }

    private static PrismCompositionDefinition CompositionSnapshot()
    {
        PrismGroupDefinition group = new(
            new PrismNodeId(3),
            "Effects",
            [Layer(1, "Front"), Layer(2, "Back")]);
        PrismBackdropDefinition backdrop = new(
            new PrismNodeId(4),
            "Glass",
            [new PrismFilterDefinition(PrismFilterId.GaussianBlur)]);
        return new PrismCompositionDefinition("Snapshot", [group, backdrop]);
    }
}
