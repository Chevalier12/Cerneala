using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

internal static class PrismTestData
{
    public static PrismLayerDefinition Layer(
        int id,
        string name,
        bool visible = true,
        bool clipToBelow = false,
        PrismMaskDefinition? mask = null,
        float opacity = 1,
        float fill = 1)
    {
        return new PrismLayerDefinition(
            new PrismNodeId(id),
            name,
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            mask: mask,
            visible: visible,
            opacity: opacity,
            fill: fill,
            clipToBelow: clipToBelow);
    }

    public static PrismBackdropDefinition Backdrop(int id, string name)
    {
        return new PrismBackdropDefinition(
            new PrismNodeId(id),
            name,
            filters: [new PrismFilterDefinition(PrismFilterId.GaussianBlur)]);
    }

    public static PrismDrawScope Scope(
        PrismCompositionDefinition definition,
        long ownerToken = 1,
        DrawRect bounds = default,
        Matrix3x2 transform = default,
        float pixelScale = 1,
        long visualContentVersion = 1,
        PrismDrawResources? resources = null)
    {
        if (bounds == default)
        {
            bounds = new DrawRect(0, 0, 20, 10);
        }

        if (transform == default)
        {
            transform = Matrix3x2.Identity;
        }

        return new PrismDrawScope(
            new PrismInstance(definition),
            new PrismCacheOwnerToken(ownerToken),
            bounds,
            transform,
            pixelScale,
            visualContentVersion,
            resources ?? PrismDrawResources.Empty);
    }

    public static PrismCompositionDefinition Composition(
        string name,
        params PrismNodeDefinition[] nodes)
    {
        return new PrismCompositionDefinition(name, nodes);
    }

    public static DrawCommandList Commands(params DrawCommand[] commands)
    {
        DrawCommandList result = new();
        foreach (DrawCommand command in commands)
        {
            result.Add(command);
        }

        return result;
    }
}
