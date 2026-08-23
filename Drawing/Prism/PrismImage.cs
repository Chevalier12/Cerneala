using System.Numerics;
using System.Threading;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism;

public sealed class PrismImage : IDrawImage
{
    private const int LayerNodeId = 1;
    private static long nextCacheOwnerToken;

    private readonly PrismCacheOwnerToken cacheOwnerToken =
        new(Interlocked.Increment(ref nextCacheOwnerToken));
    private PrismInstance? instance;
    private long appliedTopologyVersion = -1;
    private long appliedContentSignature = -1;
    private long visualContentVersion = 1;

    internal PrismImage(IDrawImage source, PrismPipeline pipeline)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        if (pipeline.Count == 0)
        {
            throw new ArgumentException(
                "A Prism image pipeline must contain at least one operation.",
                nameof(pipeline));
        }
    }

    public IDrawImage Source { get; }

    public PrismPipeline Pipeline { get; }

    public int Width => Source.Width;

    public int Height => Source.Height;

    internal PrismDrawScope CreateDrawScope(
        DrawRect bounds,
        float pixelScale = 1)
    {
        EnsureRuntimeState();
        return new PrismDrawScope(
            instance!,
            cacheOwnerToken,
            bounds,
            Matrix3x2.Identity,
            pixelScale,
            visualContentVersion,
            PrismDrawResources.Empty,
            lowerUiVersion: 0,
            isLocalDrawingScope: true);
    }

    private void EnsureRuntimeState()
    {
        if (Pipeline.Count == 0)
        {
            throw new InvalidOperationException(
                "A Prism image pipeline must contain at least one operation.");
        }

        if (instance is null || appliedTopologyVersion != Pipeline.TopologyVersion)
        {
            PrismFilterDefinition[] filters = Pipeline
                .Where(operation => operation.IsFilter)
                .Select(operation => operation.CreateFilterDefinition()!)
                .ToArray();
            PrismStyleDefinition[] styles = Pipeline
                .Where(operation => !operation.IsFilter)
                .Select(operation => operation.CreateStyleDefinition()!)
                .ToArray();
            PrismCompositionDefinition definition = new(
                "PrismImage",
                [new PrismLayerDefinition(
                    new PrismNodeId(LayerNodeId),
                    "Image",
                    filters,
                    styles)]);
            instance = new PrismInstance(definition);
            appliedTopologyVersion = Pipeline.TopologyVersion;
            appliedContentSignature = -1;
        }

        long contentSignature = Pipeline.ContentSignature;
        if (appliedContentSignature == contentSignature)
        {
            return;
        }

        PrismLayerState layer = instance.GetLayerState(
            new PrismNodeId(LayerNodeId));
        int filterIndex = 0;
        int styleIndex = 0;
        foreach (PrismOperation operation in Pipeline)
        {
            if (operation.IsFilter)
            {
                operation.ApplyTo(layer.Filters[filterIndex++]);
            }
            else
            {
                operation.ApplyTo(layer.Styles[styleIndex++]);
            }
        }

        appliedContentSignature = contentSignature;
        unchecked
        {
            visualContentVersion++;
            if (visualContentVersion < 0)
            {
                visualContentVersion = 1;
            }
        }
    }
}
