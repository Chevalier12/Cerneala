using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGraphFilterResources : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly PrismGraphFallbackTracker fallbackTracker;
    private readonly PrismCurveTextureCache curveTextures;
    private readonly PrismGradientMapTextureCache gradientMapTextures;
    private readonly PrismGradientOverlayTextureCache gradientOverlayTextures;
    private readonly PrismGradientDitherTexture gradientDitherTexture;
    private readonly PrismLensProfileTextureCache lensProfileTextures;
    private readonly PrismWaveNoiseTextureCache waveNoiseTextures;
    private readonly PrismSpatterPointTextureCache spatterPointTextures;
    private bool disposed;

    public PrismGraphFilterResources(
        GraphicsDevice graphicsDevice,
        PrismGraphFallbackTracker fallbackTracker)
    {
        this.graphicsDevice = graphicsDevice;
        this.fallbackTracker = fallbackTracker;

        PrismCurveTextureCache? curves = null;
        PrismGradientMapTextureCache? gradientMaps = null;
        PrismGradientOverlayTextureCache? gradientOverlays = null;
        PrismGradientDitherTexture? gradientDither = null;
        PrismLensProfileTextureCache? lensProfiles = null;
        PrismWaveNoiseTextureCache? waveNoise = null;
        PrismSpatterPointTextureCache? spatterPoints = null;
        try
        {
            curves = new PrismCurveTextureCache(graphicsDevice);
            gradientMaps = new PrismGradientMapTextureCache(graphicsDevice);
            gradientOverlays =
                new PrismGradientOverlayTextureCache(graphicsDevice);
            gradientDither = new PrismGradientDitherTexture(graphicsDevice);
            lensProfiles =
                new PrismLensProfileTextureCache(graphicsDevice);
            waveNoise = new PrismWaveNoiseTextureCache(graphicsDevice);
            spatterPoints =
                new PrismSpatterPointTextureCache(graphicsDevice);

            curveTextures = curves;
            gradientMapTextures = gradientMaps;
            gradientOverlayTextures = gradientOverlays;
            gradientDitherTexture = gradientDither;
            lensProfileTextures = lensProfiles;
            waveNoiseTextures = waveNoise;
            spatterPointTextures = spatterPoints;
        }
        catch
        {
            spatterPoints?.Dispose();
            waveNoise?.Dispose();
            lensProfiles?.Dispose();
            gradientDither?.Dispose();
            gradientOverlays?.Dispose();
            gradientMaps?.Dispose();
            curves?.Dispose();
            throw;
        }
    }

    public Texture2D GradientDitherTexture =>
        gradientDitherTexture.Texture;

    public Texture2D GetGradientOverlay(
        PrismResourceId resource,
        PrismGradientMapResource gradient,
        long identity,
        long version,
        PrismGradientInterpolation interpolation,
        PrismColorProfile workingProfile) =>
        gradientOverlayTextures.GetOrCreate(
            resource,
            gradient,
            identity,
            version,
            interpolation,
            workingProfile);

    public Texture2D GetWaveNoise(PrismWaveNoiseTable table) =>
        waveNoiseTextures.GetOrCreate(table);

    public Texture2D GetSpatterPoints() =>
        spatterPointTextures.GetOrCreate();

    public bool TryResolveCurves(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismResourceId resource,
        out Texture2D texture,
        out bool available)
    {
        texture = fallback;
        available = false;
        if (resource.Value <= 0 ||
            !scope.Resources.TryGetCurves(
                resource,
                out PrismCurvesResource curves,
                out long identity,
                out long version))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Curves resource '{resource}' is not available.");
            return false;
        }

        texture = curveTextures.GetOrCreate(
            resource,
            curves,
            identity,
            version);
        available = true;
        return true;
    }

    public bool TryResolveGradientMap(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismResourceId resource,
        out Texture2D texture,
        out bool available)
    {
        texture = fallback;
        available = false;
        if (resource.Value <= 0 ||
            !scope.Resources.TryGetGradientMap(
                resource,
                out PrismGradientMapResource gradient,
                out long identity,
                out long version))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Gradient map resource '{resource}' is not available.");
            return false;
        }

        texture = gradientMapTextures.GetOrCreate(
            resource,
            gradient,
            identity,
            version);
        available = true;
        return true;
    }

    public bool TryResolveHaldLookup(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismResourceId resource,
        out Texture2D texture,
        out bool available,
        out float cubeSize)
    {
        cubeSize = 0;
        if (!TryResolveImage(
                scope,
                node,
                fallback,
                resource,
                required: true,
                out texture,
                out available))
        {
            return false;
        }

        if (TryGetHaldCubeSize(texture, out int resolvedCubeSize))
        {
            cubeSize = resolvedCubeSize;
            return true;
        }

        fallbackTracker.Record(
            node,
            PrismFallbackReason.UnsupportedCapability,
            "ColorLookup requires a square Hald LUT whose side is level cubed (level >= 2).");
        texture = fallback;
        available = false;
        return false;
    }

    public bool TryResolveImage(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismResourceId resource,
        bool required,
        out Texture2D texture,
        out bool available)
    {
        texture = fallback;
        available = false;
        if (resource.Value <= 0)
        {
            if (!required)
            {
                return true;
            }

            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Filter resource '{resource}' is not available.");
            return false;
        }

        if (!scope.Resources.TryGetImage(resource, out IDrawImage image))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Filter resource '{resource}' is not available.");
            return false;
        }
        if (image is not MonoGameImage monoGameImage ||
            monoGameImage.Texture.IsDisposed ||
            !ReferenceEquals(
                monoGameImage.Texture.GraphicsDevice,
                graphicsDevice))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The filter resource is not a live MonoGame texture owned by this graphics device.");
            return false;
        }

        texture = monoGameImage.Texture;
        available = true;
        return true;
    }

    public bool TryResolveLensProfile(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismCatalogFilterPlan plan,
        out Texture2D texture,
        out bool available)
    {
        texture = fallback;
        available = false;
        PrismResourceId resource = plan.PrimaryResource;
        if (resource.Value <= 0 ||
            !scope.Resources.TryGetLensProfile(
                resource,
                out PrismLensProfileResource profile,
                out long identity,
                out long version))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Lens profile '{resource}' is not available.");
            return false;
        }

        System.Numerics.Vector4 center = plan.GetOption("Center");
        float brightness = MathF.Max(
            0,
            plan.GetOption("Brightness").X);
        texture = lensProfileTextures.GetOrCreate(
            resource,
            profile,
            identity,
            version,
            fallback.Width,
            fallback.Height,
            new System.Numerics.Vector2(center.X, center.Y),
            brightness);
        available = true;
        return true;
    }

    public bool TryResolveColorMatrix(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismCatalogFilterPlan plan,
        out Texture2D texture,
        out bool available,
        out PrismColorMatrixResource? colorMatrix)
    {
        texture = fallback;
        available = false;
        colorMatrix = null;
        PrismResourceId resource = plan.PrimaryResource;
        if (resource.Value <= 0)
        {
            return true;
        }
        if (!scope.Resources.TryGetColorMatrix(
                resource,
                out PrismColorMatrixResource resolved,
                out _,
                out _))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Color matrix '{resource}' is not available.");
            return false;
        }

        colorMatrix = resolved;
        available = true;
        return true;
    }

    public bool TryResolveLighting(
        PrismGraphScope scope,
        PrismGraphNode node,
        Texture2D fallback,
        PrismCatalogFilterPlan plan,
        out Texture2D texture,
        out bool available,
        out PrismLightingResource? lighting)
    {
        texture = fallback;
        available = false;
        lighting = null;
        PrismResourceId resource = plan.PrimaryResource;
        if (resource.Value <= 0 ||
            !scope.Resources.TryGetLighting(
                resource,
                out PrismLightingResource resolved,
                out _,
                out _))
        {
            fallbackTracker.Record(
                node,
                PrismFallbackReason.MissingResource,
                $"Lighting resource '{resource}' is not available.");
            return false;
        }

        lighting = resolved;
        available = true;
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        spatterPointTextures.Dispose();
        waveNoiseTextures.Dispose();
        lensProfileTextures.Dispose();
        gradientDitherTexture.Dispose();
        gradientOverlayTextures.Dispose();
        gradientMapTextures.Dispose();
        curveTextures.Dispose();
        disposed = true;
    }

    private static bool TryGetHaldCubeSize(
        Texture2D texture,
        out int cubeSize)
    {
        cubeSize = 0;
        if (texture.Width != texture.Height || texture.Width < 8)
        {
            return false;
        }

        int level = (int)Math.Round(Math.Pow(texture.Width, 1d / 3d));
        if (level < 2 || (long)level * level * level != texture.Width)
        {
            return false;
        }

        cubeSize = checked(level * level);
        return true;
    }
}
