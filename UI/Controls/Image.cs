using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Controls;

public enum ImageLoadingState { Loading, Ready, Error }

public enum StretchDirection { UpOnly, DownOnly, Both }

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

    public static readonly UiProperty<DrawBrushStretch> StretchProperty =
        UiProperty<DrawBrushStretch>.Register(
            nameof(Stretch),
            typeof(Image),
            new UiPropertyMetadata<DrawBrushStretch>(
                DrawBrushStretch.Uniform,
                UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<StretchDirection> StretchDirectionProperty =
        UiProperty<StretchDirection>.Register(
            nameof(StretchDirection),
            typeof(Image),
            new UiPropertyMetadata<StretchDirection>(
                StretchDirection.Both,
                UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender));

    public DrawBrushStretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public StretchDirection StretchDirection
    {
        get => GetValue(StretchDirectionProperty);
        set => SetValue(StretchDirectionProperty, value);
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
        if (source is null || !UseIntrinsicSize)
        {
            return LayoutSize.Zero;
        }

        LayoutSize natural = new(source.Width, source.Height);
        return ScaleSize(natural, ComputeScaleFactor(context.AvailableSize, natural, Stretch, StretchDirection));
    }

    protected override void OnRender(RenderContext context)
    {
        IDrawImage? source = ResolveSource();
        LayoutRect bounds = context.Bounds;
        if (source is null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        LayoutSize natural = new(source.Width, source.Height);
        LayoutSize content = ScaleSize(natural, ComputeScaleFactor(bounds.Size, natural, Stretch, StretchDirection));
        float x = HorizontalAlignment switch
        {
            HorizontalAlignment.Right => bounds.X + bounds.Width - content.Width,
            HorizontalAlignment.Center => bounds.X + ((bounds.Width - content.Width) / 2),
            HorizontalAlignment.Stretch when content.Width <= bounds.Width => bounds.X + ((bounds.Width - content.Width) / 2),
            _ => bounds.X
        };
        float y = VerticalAlignment switch
        {
            VerticalAlignment.Bottom => bounds.Y + bounds.Height - content.Height,
            VerticalAlignment.Center => bounds.Y + ((bounds.Height - content.Height) / 2),
            VerticalAlignment.Stretch when content.Height <= bounds.Height => bounds.Y + ((bounds.Height - content.Height) / 2),
            _ => bounds.Y
        };

        DrawRect destination = new(x, y, content.Width, content.Height);
        if (content.Width <= bounds.Width && content.Height <= bounds.Height)
        {
            context.DrawingContext.DrawImage(source, destination, Color.White);
            return;
        }

        context.DrawingContext.PushClip(new DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height));
        try
        {
            context.DrawingContext.DrawImage(source, destination, Color.White);
        }
        finally
        {
            context.DrawingContext.PopClip();
        }
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

    // A scale factor kept as the fraction Target / Source of an available and a natural axis size (1 / 1 for no
    // scaling). The scaled size multiplies before it divides, in double: 200 * 60 / 200 is exactly 60, while
    // 200 * (60f / 200) is 60.000004, which is larger than a 60 slot and rounds up to 61.
    private readonly record struct ScaleFraction(float Target, float Source)
    {
        public static readonly ScaleFraction One = new(1, 1);

        public double Value => Source == 0 ? 0 : (double)Target / Source;

        public float Apply(float size) => Source == 0 ? 0 : (float)((double)size * Target / Source);
    }

    private static LayoutSize ScaleSize(LayoutSize natural, (ScaleFraction X, ScaleFraction Y) scale)
    {
        return new LayoutSize(scale.X.Apply(natural.Width), scale.Y.Apply(natural.Height));
    }

    // The scale factor of WPF Viewbox.ComputeScaleFactor: the x and y scale that stretch the natural size into the
    // available size. An unconstrained (+Infinity) axis takes the scale of the other axis.
    private static (ScaleFraction X, ScaleFraction Y) ComputeScaleFactor(
        LayoutSize available, LayoutSize natural, DrawBrushStretch stretch, StretchDirection direction)
    {
        ScaleFraction scaleX = ScaleFraction.One;
        ScaleFraction scaleY = ScaleFraction.One;
        bool widthUnconstrained = available.IsWidthUnconstrained;
        bool heightUnconstrained = available.IsHeightUnconstrained;
        if (stretch != DrawBrushStretch.None && (!widthUnconstrained || !heightUnconstrained))
        {
            scaleX = new ScaleFraction(available.Width, natural.Width);
            scaleY = new ScaleFraction(available.Height, natural.Height);
            if (widthUnconstrained)
            {
                scaleX = scaleY;
            }
            else if (heightUnconstrained)
            {
                scaleY = scaleX;
            }
            else
            {
                switch (stretch)
                {
                    case DrawBrushStretch.Uniform:
                        scaleX = scaleY = scaleX.Value <= scaleY.Value ? scaleX : scaleY;
                        break;
                    case DrawBrushStretch.UniformToFill:
                        scaleX = scaleY = scaleX.Value >= scaleY.Value ? scaleX : scaleY;
                        break;
                }
            }
        }

        switch (direction)
        {
            case StretchDirection.UpOnly:
                scaleX = scaleX.Value < 1 ? ScaleFraction.One : scaleX;
                scaleY = scaleY.Value < 1 ? ScaleFraction.One : scaleY;
                break;
            case StretchDirection.DownOnly:
                scaleX = scaleX.Value > 1 ? ScaleFraction.One : scaleX;
                scaleY = scaleY.Value > 1 ? ScaleFraction.One : scaleY;
                break;
        }

        return (scaleX, scaleY);
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
