using System.Numerics;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism;

public sealed class PrismImage : IDrawImage, IDrawImageInvalidationSource, IDisposable
{
    private const int LayerNodeId = 1;

    private readonly PrismCacheOwnerToken cacheOwnerToken =
        PrismCacheOwnerTokenAllocator.Next();
    private readonly object invalidationGate = new();
    private PrismInstance? instance;
    private long appliedTopologyVersion = -1;
    private long appliedContentSignature = -1;
    private long visualContentVersion = 1;
    private int disposed;
    private bool invalidationSourcesAttached;

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

    event EventHandler? IDrawImageInvalidationSource.ContentChanged
    {
        add
        {
            if (value is null)
            {
                return;
            }

            lock (invalidationGate)
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
                bool attachSources = contentChanged is null;
                contentChanged += value;
                if (attachSources)
                {
                    AttachInvalidationSources();
                }
            }
        }
        remove
        {
            if (value is null)
            {
                return;
            }

            lock (invalidationGate)
            {
                contentChanged -= value;
                if (contentChanged is null)
                {
                    DetachInvalidationSources();
                }
            }
        }
    }

    private event EventHandler? contentChanged;

    internal PrismDrawScope CreateDrawScope(
        DrawRect bounds,
        float pixelScale = 1)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
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
            isLocalDrawingScope: true,
            imageDependency: this);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        EventHandler? invalidated;
        lock (invalidationGate)
        {
            DetachInvalidationSources();
            invalidated = contentChanged;
        }

        instance = null;
        PrismCacheInvalidationHub.EnqueueOwner(cacheOwnerToken);
        invalidated?.Invoke(this, EventArgs.Empty);
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

    private bool IsDisposed => Volatile.Read(ref disposed) != 0;

    private void OnPipelineChanged(object? sender, EventArgs args) =>
        RaiseContentChanged();

    private void OnSourceContentChanged(object? sender, EventArgs args) =>
        RaiseContentChanged();

    private void RaiseContentChanged()
    {
        EventHandler? handlers;
        lock (invalidationGate)
        {
            if (IsDisposed)
            {
                return;
            }
            handlers = contentChanged;
        }
        handlers?.Invoke(this, EventArgs.Empty);
    }

    private void AttachInvalidationSources()
    {
        if (invalidationSourcesAttached)
        {
            return;
        }

        Pipeline.Changed += OnPipelineChanged;
        if (Source is IDrawImageInvalidationSource sourceInvalidation)
        {
            sourceInvalidation.ContentChanged += OnSourceContentChanged;
        }
        invalidationSourcesAttached = true;
    }

    private void DetachInvalidationSources()
    {
        if (!invalidationSourcesAttached)
        {
            return;
        }

        Pipeline.Changed -= OnPipelineChanged;
        if (Source is IDrawImageInvalidationSource sourceInvalidation)
        {
            sourceInvalidation.ContentChanged -= OnSourceContentChanged;
        }
        invalidationSourcesAttached = false;
    }
}
