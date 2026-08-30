using System.Numerics;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism;

public readonly record struct PrismCacheOwnerToken
{
    public PrismCacheOwnerToken(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "Prism cache owner tokens must be positive.");
        }

        Value = value;
    }

    public long Value { get; }
}

public readonly record struct PrismDrawScope
{
    public PrismDrawScope(
        PrismInstance instance,
        PrismCacheOwnerToken cacheOwnerToken,
        DrawRect controlBounds,
        Matrix3x2 effectiveTransform,
        float pixelScale,
        long visualContentVersion)
        : this(
            instance,
            cacheOwnerToken,
            controlBounds,
            effectiveTransform,
            pixelScale,
            visualContentVersion,
            PrismDrawResources.Empty)
    {
    }

    internal PrismDrawScope(
        PrismInstance instance,
        PrismCacheOwnerToken cacheOwnerToken,
        DrawRect controlBounds,
        Matrix3x2 effectiveTransform,
        float pixelScale,
        long visualContentVersion,
        PrismDrawResources resources,
        long lowerUiVersion = 0,
        bool isLocalDrawingScope = false,
        IDrawImage? imageDependency = null,
        long drawContentVersion = 0)
    {
        Instance = instance ?? throw new ArgumentNullException(nameof(instance));
        ArgumentNullException.ThrowIfNull(resources);
        if (!float.IsFinite(pixelScale) || pixelScale <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pixelScale),
                pixelScale,
                "Prism pixel scale must be finite and positive.");
        }
        if (visualContentVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(visualContentVersion),
                visualContentVersion,
                "Prism visual content versions cannot be negative.");
        }
        if (lowerUiVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lowerUiVersion),
                lowerUiVersion,
                "Prism lower UI versions cannot be negative.");
        }
        if (drawContentVersion < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(drawContentVersion),
                drawContentVersion,
                "Prism draw content versions cannot be negative.");
        }

        CacheOwnerToken = cacheOwnerToken;
        ControlBounds = controlBounds;
        EffectiveTransform = effectiveTransform;
        PixelScale = pixelScale;
        VisualContentVersion = visualContentVersion;
        LowerUiVersion = lowerUiVersion;
        Resources = resources;
        IsLocalDrawingScope = isLocalDrawingScope;
        ImageDependency = imageDependency;
        DrawContentVersion = drawContentVersion;
    }

    public PrismInstance Instance { get; }

    public PrismCompositionDefinition Definition => Instance.Definition;

    public PrismCacheOwnerToken CacheOwnerToken { get; }

    public DrawRect ControlBounds { get; }

    public Matrix3x2 EffectiveTransform { get; }

    public float PixelScale { get; }

    public PrismStructuralVersion StructuralVersion => Instance.StructuralVersion;

    public PrismValueVersion ValueVersion => Instance.ValueVersion;

    public long VisualContentVersion { get; }

    internal long LowerUiVersion { get; }

    internal PrismDrawResources Resources { get; }

    internal bool IsLocalDrawingScope { get; }

    internal IDrawImage? ImageDependency { get; }

    internal long DrawContentVersion { get; }

    internal PrismDrawScope TranslateLocal(float offsetX, float offsetY)
    {
        if (!IsLocalDrawingScope || offsetX == 0 && offsetY == 0)
        {
            return this;
        }

        return new PrismDrawScope(
            Instance,
            CacheOwnerToken,
            new DrawRect(
                ControlBounds.X + offsetX,
                ControlBounds.Y + offsetY,
                ControlBounds.Width,
                ControlBounds.Height),
            EffectiveTransform,
            PixelScale,
            VisualContentVersion,
            Resources,
            LowerUiVersion,
            isLocalDrawingScope: true,
            ImageDependency,
            DrawContentVersion);
    }

    internal PrismDrawScope ApplyLocalTransform(Matrix3x2 transform)
    {
        if (!IsLocalDrawingScope)
        {
            return this;
        }

        return new PrismDrawScope(
            Instance,
            CacheOwnerToken,
            ControlBounds,
            Matrix3x2.Multiply(EffectiveTransform, transform),
            PixelScale,
            VisualContentVersion,
            Resources,
            LowerUiVersion,
            imageDependency: ImageDependency,
            drawContentVersion: DrawContentVersion);
    }
}
