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

            resourceProvider = value;
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

        context.DrawingContext.DrawImage(source, CalculateDestinationRect(source, context.Bounds), Foreground);
    }

    private IDrawImage? ResolveSource()
    {
        if (SourceResourceId is ResourceId<ImageResource> id)
        {
            InvalidationFlags effects = UseIntrinsicSize
                ? InvalidationFlags.Measure | InvalidationFlags.Render
                : InvalidationFlags.Render;
            ResourceDependencyTracker? tracker = ResolveResourceDependencyTracker();
            tracker?.RecordDependency(this, id, effects, affectsIntrinsicSize: UseIntrinsicSize);
            long version = tracker?.GetDependencyVersion(this) ?? GetProviderVersion(id);
            SetRenderDependencies(RenderDependencies
                .WithResourceIdentity(id.ToString())
                .WithResourceVersion(version));

            IResourceProvider? provider = ResolveResourceProvider();
            if (provider is null || !provider.TryGetResource(id, out ImageResource? resource))
            {
                return null;
            }

            ImageResourceCache? cache = Root?.ImageResourceCache;
            if (cache is not null)
            {
                return cache.Resolve(resource);
            }

            return resource.Resolve();
        }

        return Source;
    }

    private long GetProviderVersion(ResourceId<ImageResource> id)
    {
        return ResolveResourceProvider() is ResourceStore store ? store.GetVersion(id) : 0;
    }

    private IResourceProvider? ResolveResourceProvider()
    {
        return ResourceProvider ?? Root?.ResourceProvider;
    }

    private static DrawRect CalculateDestinationRect(IDrawImage source, LayoutRect bounds)
    {
        float scale = MathF.Min(bounds.Width / source.Width, bounds.Height / source.Height);
        float width = source.Width * scale;
        float height = source.Height * scale;
        float x = bounds.X + ((bounds.Width - width) / 2);
        float y = bounds.Y + ((bounds.Height - height) / 2);
        return new DrawRect(x, y, width, height);
    }

    private ResourceDependencyTracker? ResolveResourceDependencyTracker()
    {
        return ResourceDependencyTracker ?? Root?.ResourceDependencyTracker;
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
