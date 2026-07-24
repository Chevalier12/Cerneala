using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Presentation;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Presentation;

public sealed class PrismStudioModelTests
{
    [Fact]
    public void ResetStartsWithAnEmptyStack()
    {
        PrismStudioModel model = new();

        Assert.Empty(model.Layers);
        Assert.Equal(0, model.OperationCount);
        Assert.Equal(PrismStudioTarget.Mascot, model.Target);
        Assert.Equal(0, model.SelectedLayerId);
        Assert.Null(model.SelectedOperationId);
    }

    [Fact]
    public void AddLayerStartsWithoutFiltersOrStyles()
    {
        PrismStudioModel model = new();

        PrismStudioLayer layer = model.AddLayer("EMPTY");

        Assert.Empty(layer.Filters);
        Assert.Empty(layer.Styles);
        Assert.Empty(layer.Operations);
        Assert.Equal(layer.Id, model.SelectedLayerId);
        Assert.Null(model.SelectedOperationId);
    }

    [Fact]
    public void StructuralEditsPreserveFilterAndStyleOrdering()
    {
        PrismStudioModel model = new();
        PrismStudioLayer layer = model.AddLayer("EDIT");

        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetFilter(PrismFilterId.Blur)));
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetStyle(PrismStyleId.Stroke)));
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetStyle(PrismStyleId.DropShadow)));
        int dropShadowId = layer.Styles[1].Id;
        Assert.True(model.MoveOperation(dropShadowId, -1));

        PrismLayerDefinition definition = Assert.IsType<PrismLayerDefinition>(model.BuildDefinition().Nodes[0]);
        Assert.Equal([PrismFilterId.Color, PrismFilterId.Blur], definition.Filters.Select(filter => filter.Filter));
        Assert.Equal([PrismStyleId.DropShadow, PrismStyleId.Stroke], definition.Styles.Select(style => style.Style));

        model.AddLayer("TOP");
        Assert.True(model.MoveLayer(layer.Id, -1));
        Assert.Equal(layer.Id, model.Layers[0].Id);
    }

    [Fact]
    public void SelectionVisibilityAndRemovalStayConsistent()
    {
        PrismStudioModel model = new();
        PrismStudioLayer layer = model.AddLayer("EDIT");
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetStyle(PrismStyleId.Stroke)));
        PrismStudioOperation style = layer.Styles.Single();

        model.SelectOperation(style.Id);
        layer.IsVisible = false;
        layer.Filters[0].IsVisible = false;

        PrismLayerDefinition definition = Assert.IsType<PrismLayerDefinition>(model.BuildDefinition().Nodes[0]);
        Assert.Equal(layer.Id, model.SelectedLayerId);
        Assert.Equal(style.Id, model.SelectedOperationId);
        Assert.False(definition.Visible);
        Assert.False(definition.Filters[0].Visible);

        Assert.True(model.RemoveOperation(style.Id));
        Assert.Equal(layer.Filters[0].Id, model.SelectedOperationId);
        Assert.True(model.RemoveLayer(layer.Id));
        Assert.Empty(model.Layers);
        Assert.Equal(0, model.SelectedLayerId);
        Assert.Null(model.SelectedOperationId);
    }

    [Fact]
    public void RequiredResourceOperationsAreVisibleButCannotBeAdded()
    {
        PrismStudioModel model = new();
        PrismStudioLayer layer = model.AddLayer();
        PrismCatalogOperationInfo colorLookup = PrismCatalog.GetFilter(PrismFilterId.ColorLookup);
        PrismCatalogOperationInfo pattern = PrismCatalog.GetStyle(PrismStyleId.PatternOverlay);

        Assert.True(colorLookup.RequiresResource);
        Assert.True(pattern.RequiresResource);
        Assert.False(model.AddOperation(layer.Id, colorLookup));
        Assert.False(model.AddOperation(layer.Id, pattern));
    }

    [Fact]
    public void StackHasNoArtificialLayerLimitAndOperationsCanReturnToEmpty()
    {
        PrismStudioModel model = new();
        for (int index = 0; index < 256; index++)
        {
            model.AddLayer();
        }

        Assert.Equal(256, model.Layers.Count);
        Assert.All(model.Layers, layer => Assert.Empty(layer.Operations));

        PrismStudioLayer first = model.Layers[0];
        Assert.True(model.AddOperation(first.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        Assert.Single(model.BuildDefinition().Nodes);
        Assert.True(model.RemoveOperation(first.Operations.Single().Id));
        Assert.Empty(first.Operations);
        Assert.Null(model.SelectedOperationId);
    }

    [Fact]
    public void TargetChangesKeepTheSameStackAndValues()
    {
        PrismStudioModel model = new();
        PrismStudioLayer layer = model.AddLayer();
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        PrismStudioOperation color = layer.Filters.Single();
        PrismCatalogParameterInfo exposure = color.Catalog.Parameters.Single(parameter => parameter.Name == "Exposure");
        color.SetValue(exposure, 0.3f);
        int[] layerIds = model.Layers.Select(layer => layer.Id).ToArray();

        model.SelectTarget(PrismStudioTarget.Card);

        Assert.Equal(PrismStudioTarget.Card, model.Target);
        Assert.Equal(layerIds, model.Layers.Select(layer => layer.Id));
        Assert.Equal(0.3f, color.GetValue(exposure));
    }

    [Fact]
    public void ApplyReplacesTopologyAndReappliesTypedValues()
    {
        PrismStudioModel model = new();
        PrismStudioLayer baseLayer = model.AddLayer("BASE");
        Assert.True(model.AddOperation(baseLayer.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        PrismInstance instance = new(model.BuildDefinition());
        model.ApplyTo(instance);
        PrismStudioLayer newLayer = model.AddLayer("NEW");
        Assert.True(model.AddOperation(newLayer.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        PrismStudioOperation filter = newLayer.Filters.Single();
        PrismCatalogParameterInfo exposure = filter.Catalog.Parameters.Single(parameter => parameter.Name == "Exposure");
        filter.SetValue(exposure, 0.4f);

        model.ApplyTo(instance);

        PrismFilterState state = instance.GetLayerState(new PrismNodeId(newLayer.Id)).Filters.Single();
        Assert.Equal(0.4f, state.GetValue<float>(exposure));
        Assert.Equal(2, instance.StructuralVersion.Value);
    }

    [Fact]
    public void InteractiveValuesUpdateTypedStateWithoutReplacingTopology()
    {
        PrismStudioModel model = new();
        PrismStudioLayer layer = model.AddLayer();
        Assert.True(model.AddOperation(layer.Id, PrismCatalog.GetFilter(PrismFilterId.Color)));
        PrismInstance instance = new(model.BuildDefinition());
        model.ApplyTo(instance);
        PrismStudioOperation filter = layer.Filters.Single();
        PrismCatalogParameterInfo exposure = filter.Catalog.Parameters.Single(parameter => parameter.Name == "Exposure");

        model.SetLayerOpacity(instance, layer.Id, 0.72f);
        model.SetLayerFill(instance, layer.Id, 0.64f);
        model.SetLayerBlendMode(instance, layer.Id, PrismBlendMode.Multiply);
        model.SetOperationVisibility(instance, filter.Id, false);
        model.SetFilterOpacity(instance, filter.Id, 0.81f);
        model.SetFilterBlendMode(instance, filter.Id, PrismBlendMode.Screen);
        model.SetOperationValue(instance, filter.Id, exposure, 0.25f);

        PrismLayerState state = instance.GetLayerState(new PrismNodeId(layer.Id));
        Assert.Equal(1, instance.StructuralVersion.Value);
        Assert.True(instance.ValueVersion.Value > 0);
        Assert.Equal(0.72f, state.Opacity);
        Assert.Equal(0.64f, state.Fill);
        Assert.Equal(PrismBlendMode.Multiply, state.BlendMode);
        Assert.False(state.Filters[0].Visible);
        Assert.Equal(0.81f, state.Filters[0].Opacity);
        Assert.Equal(PrismBlendMode.Screen, state.Filters[0].BlendMode);
        Assert.Equal(0.25f, state.Filters[0].GetValue<float>(exposure));
    }

    [Fact]
    public void ResetRestoresTheEmptyInitialState()
    {
        PrismStudioModel model = new();
        model.AddLayer("TEMP");
        model.SelectTarget(PrismStudioTarget.Badge);

        model.Reset();

        Assert.Empty(model.Layers);
        Assert.Equal(PrismStudioTarget.Mascot, model.Target);
        Assert.Equal(0, model.SelectedLayerId);
        Assert.Null(model.SelectedOperationId);
    }
}
