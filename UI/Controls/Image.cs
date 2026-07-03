using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public class Image : Control
{
    private IResourceProvider? resourceProvider;
    private ResourceDependencyTracker? resourceDependencyTracker;
    private ResourceStore? subscribedStore;
    private ResourceId<ImageResource>? sourceResourceId;
    private bool useIntrinsicSize = true;

    public static readonly UiProperty<IDrawImage?> SourceProperty = UiProperty<IDrawImage?>.Register(
        nameof(Source),
        typeof(Image),
        new UiPropertyMetadata<IDrawImage?>(
            null,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender,
            ReferenceImageComparer.Instance));

    public IDrawImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public ResourceId<ImageResource>? SourceResourceId
    {
        get => sourceResourceId;
        set
        {
            if (sourceResourceId == value)
            {
                return;
            }

            sourceResourceId = value;
            InvalidateResolvedSource("Image resource id changed");
        }
    }

    public bool UseIntrinsicSize
    {
        get => useIntrinsicSize;
        set
        {
            if (useIntrinsicSize == value)
            {
                return;
            }

            useIntrinsicSize = value;
            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Image intrinsic size mode changed");
        }
    }

    public IResourceProvider? ResourceProvider
    {
        get => resourceProvider;
        set
        {
            if (ReferenceEquals(resourceProvider, value))
            {
                return;
            }

            if (subscribedStore is not null)
            {
                subscribedStore.ResourceChanged -= OnResourceChanged;
                subscribedStore = null;
            }

            resourceProvider = value;
            if (value is ResourceStore store)
            {
                subscribedStore = store;
                subscribedStore.ResourceChanged += OnResourceChanged;
            }

            IncrementLayoutVersion();
            IncrementRenderVersion();
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Image resource provider changed");
        }
    }

    public ResourceDependencyTracker? ResourceDependencyTracker
    {
        get => resourceDependencyTracker;
        set => resourceDependencyTracker = value;
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        IDrawImage? source = ResolveSource();
        return source is null || !UseIntrinsicSize ? LayoutSize.Zero : new LayoutSize(source.Width, source.Height);
    }

    protected override void OnRender(RenderContext context)
    {
        IDrawImage? source = ResolveSource();
        if (source is null || context.Bounds.Width <= 0 || context.Bounds.Height <= 0)
        {
            return;
        }

        context.DrawingContext.DrawImage(source, Border.ToDrawRect(context.Bounds), Foreground);
    }

    private IDrawImage? ResolveSource()
    {
        if (SourceResourceId is ResourceId<ImageResource> id)
        {
            ResourceDependencyTracker?.RecordDependency(this, id);
            long version = ResourceDependencyTracker?.GetDependencyVersion(this) ?? GetProviderVersion(id);
            SetRenderDependencies(RenderDependencies
                .WithResourceIdentity(id.ToString())
                .WithResourceVersion(version));

            if (ResourceProvider is null || !ResourceProvider.TryGetResource(id, out ImageResource? resource))
            {
                return null;
            }

            return resource.Resolve();
        }

        return Source;
    }

    private long GetProviderVersion(ResourceId<ImageResource> id)
    {
        return ResourceProvider is ResourceStore store ? store.GetVersion(id) : 0;
    }

    private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        if (SourceResourceId is not ResourceId<ImageResource> id || !args.Matches(id))
        {
            return;
        }

        ResourceDependencyTracker?.NotifyResourceChanged(args);
        InvalidationFlags flags = UseIntrinsicSize
            ? InvalidationFlags.Measure | InvalidationFlags.Render
            : InvalidationFlags.Render;
        IncrementRenderVersion();
        if (flags.HasFlag(InvalidationFlags.Measure))
        {
            IncrementLayoutVersion();
        }

        Invalidate(flags, "Image resource changed");
    }

    private void InvalidateResolvedSource(string reason)
    {
        InvalidationFlags flags = UseIntrinsicSize
            ? InvalidationFlags.Measure | InvalidationFlags.Render
            : InvalidationFlags.Render;
        IncrementRenderVersion();
        if (flags.HasFlag(InvalidationFlags.Measure))
        {
            IncrementLayoutVersion();
        }

        Invalidate(flags, reason);
    }

    private sealed class ReferenceImageComparer : IEqualityComparer<IDrawImage?>
    {
        public static readonly ReferenceImageComparer Instance = new();

        public bool Equals(IDrawImage? x, IDrawImage? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(IDrawImage? obj)
        {
            return obj is null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
