using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public enum ImageLoadingState { Loading, Ready, Error }

public class Image : Control
{
    private IResourceProvider? resourceProvider;
    private ResourceDependencyTracker? resourceDependencyTracker;
    private ResourceId<ImageResource>? sourceResourceId;
    private bool useIntrinsicSize = true;
    private static readonly Action<Elements.UIElement> imagePrepared =
        static owner => ((Image)owner).InvalidateResolvedSource("Image preparation completed");

    private static readonly UiPropertyKey<ImageLoadingState> LoadingStatePropertyKey =
        UiProperty<ImageLoadingState>.RegisterReadOnly(nameof(LoadingState), typeof(Image),
            new UiPropertyMetadata<ImageLoadingState>(ImageLoadingState.Ready));
    public static readonly UiProperty<ImageLoadingState> LoadingStateProperty = LoadingStatePropertyKey.Property;

    private static readonly UiPropertyKey<Exception?> LoadingErrorPropertyKey =
        UiProperty<Exception?>.RegisterReadOnly(nameof(LoadingError), typeof(Image), new UiPropertyMetadata<Exception?>(null));
    public static readonly UiProperty<Exception?> LoadingErrorProperty = LoadingErrorPropertyKey.Property;

    public ImageLoadingState LoadingState => GetValue(LoadingStateProperty);
    public Exception? LoadingError => GetValue(LoadingErrorProperty);

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
            SourceImageLeases.Clear();
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
            SourceImageLeases.Clear();
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

        context.DrawingContext.DrawImage(source, CalculateDestinationRect(source, context.Bounds), Color.White);
    }

    private IDrawImage? ResolveSource()
    {
        var sourceRoot = Root;
        ImageResourceCache? sourceCache = sourceRoot?.ImageResourceCache;
        ResourceId<ImageResource>? sourceId = SourceResourceId;
        IResourceProvider? provider = ResourceProvider;
        IDrawImage? direct = Source;
        ImageResourceResolution resolution;
        using (ImageResourceLeaseSet.Scope usage = SourceImageLeases.Begin())
        {
            if (sourceId is ResourceId<ImageResource> id)
            {
                InvalidationFlags effects = UseIntrinsicSize
                    ? InvalidationFlags.Measure | InvalidationFlags.Render
                    : InvalidationFlags.Render;
                resolution = ImageResourceResolver.Resolve(
                    this, id, provider, ResourceDependencyTracker, effects,
                    affectsIntrinsicSize: UseIntrinsicSize, access: ImageResourceAccess.Prepare,
                    onPrepared: imagePrepared);
                SetRenderDependencies(RenderDependencies
                    .WithResourceIdentity(id.ToString())
                    .WithResourceVersion(resolution.Version));
            }
            else
            {
                SetRenderDependencies(RenderDependency.None);
                resolution = new(direct, 0);
            }
        }

        // End the acquisition scope before notifying application bindings.
        // A loader or a state observer can synchronously replace/detach this
        // source and release its image. Never return that superseded image.
        if (!HasCurrentSource()) { return null; }
        int version = RenderVersion;
        SetValue(LoadingErrorPropertyKey, resolution.Error);
        if (RenderVersion != version || !HasCurrentSource()) { return null; }
        SetValue(LoadingStatePropertyKey, resolution.Error is not null ? ImageLoadingState.Error :
            resolution.IsPending ? ImageLoadingState.Loading : ImageLoadingState.Ready);
        return RenderVersion == version && HasCurrentSource() ? resolution.Image : null;

        bool HasCurrentSource() => ReferenceEquals(Root, sourceRoot) &&
            ReferenceEquals(Root?.ImageResourceCache, sourceCache) && sourceId == SourceResourceId &&
            ReferenceEquals(provider, ResourceProvider) && ReferenceEquals(direct, Source);
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
