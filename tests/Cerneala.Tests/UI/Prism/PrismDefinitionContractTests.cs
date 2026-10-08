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
            typeof(PrismClipDefinition),
            typeof(PrismLayerDefinition),
            typeof(PrismGroupDefinition),
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
        PrismClipDefinition composition = new("Card", [front, back]);

        Assert.Equal(["Front", "Back"], composition.Nodes.Select(node => node.Name));
        Assert.Equal(["Back", "Front"], composition.EnumerateContentBottomUp().Select(node => node.Name));
        Assert.Same(front, composition.Nodes[0]);
        Assert.Same(back, composition.Nodes[1]);
    }

    [Fact]
    public void NamesMustBeUniqueWithinAnAddressScope()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new PrismClipDefinition(
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
    public void IndependentDefinitionsHaveStructuralEquality()
    {
        PrismClipDefinition first = CompositionSnapshot();
        PrismClipDefinition second = CompositionSnapshot();

        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void DefinitionCollectionsSnapshotCallerOwnedLists()
    {
        PrismFilterDefinition filter = new(PrismFilterId.Blur);
        PrismStyleDefinition style = new(PrismStyleId.DropShadow);
        List<PrismFilterDefinition> layerFilters = [filter];
        List<PrismStyleDefinition> layerStyles = [style];
        PrismLayerDefinition layer = new(new(1), "Content", layerFilters, layerStyles);
        List<PrismNodeDefinition> children = [layer];
        List<PrismFilterDefinition> groupFilters = [filter];
        List<PrismStyleDefinition> groupStyles = [style];
        PrismGroupDefinition group = new(new(2), "Effects", children, groupFilters, groupStyles);
        List<PrismNodeDefinition> nodes = [group];
        PrismClipDefinition composition = new("Snapshot", nodes);

        layerFilters.Clear();
        layerStyles.Clear();
        children.Clear();
        groupFilters.Clear();
        groupStyles.Clear();
        nodes.Clear();

        Assert.Same(filter, Assert.Single(layer.Filters));
        Assert.Same(style, Assert.Single(layer.Styles));
        Assert.Same(layer, Assert.Single(group.Children));
        Assert.Same(filter, Assert.Single(group.Filters));
        Assert.Same(style, Assert.Single(group.Styles));
        Assert.Same(group, Assert.Single(composition.Nodes));
        Assert.True(composition.TryGetNamedNode("Effects.Content", out PrismNodeId content));
        Assert.Equal(layer.Id, content);
    }

    [Fact]
    public void SourceMetadataDoesNotAffectSemanticEqualityOrHashing()
    {
        PrismSourceSpan firstSpan = new(1, 2, "First.crn");
        PrismSourceSpan secondSpan = new(30, 40, "Second.crn");
        PrismClipDefinition first = WithSource(firstSpan);
        PrismClipDefinition second = WithSource(secondSpan);
        PrismGroupDefinition firstGroup = Assert.IsType<PrismGroupDefinition>(Assert.Single(first.Nodes));
        PrismGroupDefinition secondGroup = Assert.IsType<PrismGroupDefinition>(Assert.Single(second.Nodes));
        PrismNodeDefinition firstLayer = Assert.Single(firstGroup.Children);
        PrismNodeDefinition secondLayer = Assert.Single(secondGroup.Children);

        Assert.Equal(firstSpan, first.SourceSpan);
        Assert.Equal(firstSpan, firstGroup.SourceSpan);
        Assert.Equal(firstSpan, firstLayer.SourceSpan);
        Assert.Equal(secondSpan, second.SourceSpan);
        Assert.Equal(secondSpan, secondGroup.SourceSpan);
        Assert.Equal(secondSpan, secondLayer.SourceSpan);
        Assert.Equal(first, second);
        Assert.Equal(firstGroup, secondGroup);
        Assert.Equal(firstLayer, secondLayer);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.Equal(firstGroup.GetHashCode(), secondGroup.GetHashCode());
        Assert.Equal(firstLayer.GetHashCode(), secondLayer.GetHashCode());

        static PrismClipDefinition WithSource(PrismSourceSpan sourceSpan)
        {
            PrismLayerDefinition layer = new(new(1), "Content",
                filters: [new(PrismFilterId.Blur)], sourceSpan: sourceSpan);
            PrismGroupDefinition group = new(new(2), "Effects", [layer], sourceSpan: sourceSpan);
            return new("Metadata", [group], sourceSpan: sourceSpan);
        }
    }

    [Fact]
    public void NamesAreScopedByGroupWhileNodeIdsAreGlobal()
    {
        PrismGroupDefinition left = new(new(10), "Left", [Layer(1, "Content")]);
        PrismGroupDefinition right = new(new(20), "Right", [Layer(2, "Content")]);
        PrismClipDefinition composition = new("Addresses", [left, right]);

        Assert.True(composition.TryGetNamedNode("Left.Content", out PrismNodeId leftId));
        Assert.True(composition.TryGetNamedNode("Right.Content", out PrismNodeId rightId));
        Assert.Equal(new(1), leftId);
        Assert.Equal(new(2), rightId);
        Assert.False(composition.TryGetNamedNode("left.Content", out _));
        Assert.Throws<ArgumentException>(() => new PrismClipDefinition("DuplicateIds",
            [left, new PrismGroupDefinition(new(20), "Right", [Layer(1, "Other")])]));
        Assert.Throws<ArgumentException>(() => new PrismClipDefinition("DuplicateLocalNames",
            [new PrismGroupDefinition(new(10), "Left", [Layer(1, "Content"), Layer(2, "Content")])]));
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
            """.ReplaceLineEndings("\n"),
            first);
    }

    [Fact]
    public void NamedNodesResolveToTypedIdsButCannotBecomeSources()
    {
        PrismClipDefinition composition = CompositionSnapshot();

        Assert.True(composition.TryGetNamedNode("Effects.Front", out PrismNodeId front));
        Assert.Equal(new PrismNodeId(1), front);
        Assert.True(composition.TryGetNamedNode("Effects.Back", out PrismNodeId back));
        Assert.Equal(new PrismNodeId(2), back);
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

    private static PrismClipDefinition CompositionSnapshot()
    {
        PrismGroupDefinition group = new(
            new PrismNodeId(3),
            "Effects",
            [Layer(1, "Front"), Layer(2, "Back")]);
        return new PrismClipDefinition("Snapshot", [group]);
    }
}
