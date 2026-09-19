using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism;

internal readonly record struct PrismDrawImageResource(
    PrismResourceId Id,
    IDrawImage Image,
    long Version = 0,
    long Identity = 0);

internal readonly record struct PrismDrawCurvesResource(
    PrismResourceId Id,
    PrismCurvesResource Resource,
    long Version = 0,
    long Identity = 0);

internal readonly record struct PrismDrawGradientMapResource(
    PrismResourceId Id,
    PrismGradientMapResource Resource,
    long Version = 0,
    long Identity = 0);

internal readonly record struct PrismDrawLensProfileResource(
    PrismResourceId Id,
    PrismLensProfileResource Resource,
    long Version = 0,
    long Identity = 0);

internal readonly record struct PrismDrawLightingResource(
    PrismResourceId Id,
    PrismLightingResource Resource,
    long Version = 0,
    long Identity = 0);

internal readonly record struct PrismDrawColorMatrixResource(
    PrismResourceId Id,
    PrismColorMatrixResource Resource,
    long Version = 0,
    long Identity = 0);

internal sealed class PrismDrawResources
{
    private readonly Dictionary<PrismResourceId, ResolvedResource<IDrawImage>> images;
    private readonly Dictionary<PrismResourceId, ResolvedResource<PrismCurvesResource>> curves;
    private readonly Dictionary<PrismResourceId, ResolvedResource<PrismGradientMapResource>> gradients;
    private readonly Dictionary<PrismResourceId, ResolvedResource<PrismLensProfileResource>>
        lensProfiles;
    private readonly Dictionary<PrismResourceId, ResolvedResource<PrismLightingResource>>
        lighting;
    private readonly Dictionary<PrismResourceId, ResolvedResource<PrismColorMatrixResource>>
        colorMatrices;

    private PrismDrawResources(
        Dictionary<PrismResourceId, ResolvedResource<IDrawImage>> images,
        Dictionary<PrismResourceId, ResolvedResource<PrismCurvesResource>> curves,
        Dictionary<PrismResourceId, ResolvedResource<PrismGradientMapResource>> gradients,
        Dictionary<PrismResourceId, ResolvedResource<PrismLensProfileResource>> lensProfiles,
        Dictionary<PrismResourceId, ResolvedResource<PrismLightingResource>> lighting,
        Dictionary<PrismResourceId, ResolvedResource<PrismColorMatrixResource>> colorMatrices,
        bool hasStableVersions)
    {
        this.images = images;
        this.curves = curves;
        this.gradients = gradients;
        this.lensProfiles = lensProfiles;
        this.lighting = lighting;
        this.colorMatrices = colorMatrices;
        HasStableVersions = hasStableVersions;
    }

    public static PrismDrawResources Empty { get; } =
        new([], [], [], [], [], [], hasStableVersions: true);

    public bool HasStableVersions { get; }

    internal IEnumerable<IDrawImage> Images =>
        images.Values.Select(static resource => resource.Resource);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources) =>
        Create(resources, []);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources,
        IEnumerable<PrismDrawCurvesResource> curveResources) =>
        Create(resources, curveResources, []);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources,
        IEnumerable<PrismDrawCurvesResource> curveResources,
        IEnumerable<PrismDrawGradientMapResource> gradientResources)
        => Create(resources, curveResources, gradientResources, []);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources,
        IEnumerable<PrismDrawCurvesResource> curveResources,
        IEnumerable<PrismDrawGradientMapResource> gradientResources,
        IEnumerable<PrismDrawLensProfileResource> lensProfileResources)
        => Create(
            resources,
            curveResources,
            gradientResources,
            lensProfileResources,
            []);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources,
        IEnumerable<PrismDrawCurvesResource> curveResources,
        IEnumerable<PrismDrawGradientMapResource> gradientResources,
        IEnumerable<PrismDrawLensProfileResource> lensProfileResources,
        IEnumerable<PrismDrawLightingResource> lightingResources) =>
        Create(
            resources,
            curveResources,
            gradientResources,
            lensProfileResources,
            lightingResources,
            []);

    public static PrismDrawResources Create(
        IEnumerable<PrismDrawImageResource> resources,
        IEnumerable<PrismDrawCurvesResource> curveResources,
        IEnumerable<PrismDrawGradientMapResource> gradientResources,
        IEnumerable<PrismDrawLensProfileResource> lensProfileResources,
        IEnumerable<PrismDrawLightingResource> lightingResources,
        IEnumerable<PrismDrawColorMatrixResource> colorMatrixResources)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(curveResources);
        ArgumentNullException.ThrowIfNull(gradientResources);
        ArgumentNullException.ThrowIfNull(lensProfileResources);
        ArgumentNullException.ThrowIfNull(lightingResources);
        ArgumentNullException.ThrowIfNull(colorMatrixResources);
        Dictionary<PrismResourceId, ResolvedResource<IDrawImage>> images = [];
        foreach (PrismDrawImageResource resource in resources)
        {
            ArgumentNullException.ThrowIfNull(resource.Image);
            if (resource.Version < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resources),
                    resource.Version,
                    "Prism resource versions cannot be negative.");
            }
            if (resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resources),
                    resource.Identity,
                    "Prism resource identities cannot be negative.");
            }

            images[resource.Id] =
                new ResolvedResource<IDrawImage>(
                    resource.Image,
                    resource.Identity,
                    resource.Version);
        }

        Dictionary<PrismResourceId, ResolvedResource<PrismCurvesResource>> curves = [];
        foreach (PrismDrawCurvesResource resource in curveResources)
        {
            ArgumentNullException.ThrowIfNull(resource.Resource);
            if (resource.Version < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(curveResources),
                    resource.Version,
                    "Prism resource versions cannot be negative.");
            }
            if (resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(curveResources),
                    resource.Identity,
                    "Prism resource identities cannot be negative.");
            }

            curves[resource.Id] =
                new ResolvedResource<PrismCurvesResource>(
                    resource.Resource,
                    resource.Identity,
                    resource.Version);
        }

        Dictionary<PrismResourceId, ResolvedResource<PrismGradientMapResource>> gradients = [];
        foreach (PrismDrawGradientMapResource resource in gradientResources)
        {
            ArgumentNullException.ThrowIfNull(resource.Resource);
            if (resource.Version < 0 || resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gradientResources),
                    "Prism gradient identities and versions cannot be negative.");
            }
            gradients[resource.Id] = new ResolvedResource<PrismGradientMapResource>(
                resource.Resource,
                resource.Identity,
                resource.Version);
        }

        Dictionary<PrismResourceId, ResolvedResource<PrismLensProfileResource>> lensProfiles = [];
        foreach (PrismDrawLensProfileResource resource in lensProfileResources)
        {
            ArgumentNullException.ThrowIfNull(resource.Resource);
            if (resource.Version < 0 || resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lensProfileResources),
                    "Prism lens-profile identities and versions cannot be negative.");
            }
            lensProfiles[resource.Id] = new ResolvedResource<PrismLensProfileResource>(
                resource.Resource,
                resource.Identity,
                resource.Version);
        }

        Dictionary<PrismResourceId, ResolvedResource<PrismLightingResource>> lighting = [];
        foreach (PrismDrawLightingResource resource in lightingResources)
        {
            ArgumentNullException.ThrowIfNull(resource.Resource);
            if (resource.Version < 0 || resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lightingResources),
                    "Prism lighting identities and versions cannot be negative.");
            }
            lighting[resource.Id] = new ResolvedResource<PrismLightingResource>(
                resource.Resource,
                resource.Identity,
                resource.Version);
        }

        Dictionary<PrismResourceId, ResolvedResource<PrismColorMatrixResource>> colorMatrices = [];
        foreach (PrismDrawColorMatrixResource resource in colorMatrixResources)
        {
            ArgumentNullException.ThrowIfNull(resource.Resource);
            if (resource.Version < 0 || resource.Identity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(colorMatrixResources),
                    "Prism color-matrix identities and versions cannot be negative.");
            }
            colorMatrices[resource.Id] = new ResolvedResource<PrismColorMatrixResource>(
                resource.Resource,
                resource.Identity,
                resource.Version);
        }

        if (images.Count == 0 &&
            curves.Count == 0 &&
            gradients.Count == 0 &&
            lensProfiles.Count == 0 &&
            lighting.Count == 0 &&
            colorMatrices.Count == 0)
        {
            return Empty;
        }

        bool hasStableVersions =
            images.Values.All(image => image.Version > 0) &&
            curves.Values.All(curve => curve.Version > 0) &&
            gradients.Values.All(gradient => gradient.Version > 0) &&
            lensProfiles.Values.All(profile => profile.Version > 0) &&
            lighting.Values.All(resource => resource.Version > 0) &&
            colorMatrices.Values.All(resource => resource.Version > 0);

        return new PrismDrawResources(
            images,
            curves,
            gradients,
            lensProfiles,
            lighting,
            colorMatrices,
            hasStableVersions);
    }

    public bool TryGetImage(
        PrismResourceId id,
        out IDrawImage image) =>
        TryGetResource(images, id, out image, out _, out _);

    public bool TryGetVersion(
        PrismResourceId id,
        out long version) =>
        TryGetDependency(id, out _, out version);

    public bool TryGetDependency(
        PrismResourceId id,
        out long identity,
        out long version)
    {
        if (images.TryGetValue(id, out ResolvedResource<IDrawImage> resource))
        {
            identity = resource.Identity;
            version = resource.Version;
            return true;
        }
        if (curves.TryGetValue(id, out ResolvedResource<PrismCurvesResource> curve))
        {
            identity = curve.Identity;
            version = curve.Version;
            return true;
        }
        if (gradients.TryGetValue(id, out ResolvedResource<PrismGradientMapResource> gradient))
        {
            identity = gradient.Identity;
            version = gradient.Version;
            return true;
        }
        if (lensProfiles.TryGetValue(id, out ResolvedResource<PrismLensProfileResource> lensProfile))
        {
            identity = lensProfile.Identity;
            version = lensProfile.Version;
            return true;
        }
        if (lighting.TryGetValue(id, out ResolvedResource<PrismLightingResource> lightingResource))
        {
            identity = lightingResource.Identity;
            version = lightingResource.Version;
            return true;
        }
        if (colorMatrices.TryGetValue(id, out ResolvedResource<PrismColorMatrixResource> colorMatrix))
        {
            identity = colorMatrix.Identity;
            version = colorMatrix.Version;
            return true;
        }

        identity = 0;
        version = 0;
        return false;
    }

    public bool TryGetLensProfile(
        PrismResourceId id,
        out PrismLensProfileResource resource,
        out long identity,
        out long version) =>
        TryGetResource(lensProfiles, id, out resource, out identity, out version);

    public bool TryGetLighting(
        PrismResourceId id,
        out PrismLightingResource resource,
        out long identity,
        out long version) =>
        TryGetResource(lighting, id, out resource, out identity, out version);

    public bool TryGetColorMatrix(
        PrismResourceId id,
        out PrismColorMatrixResource resource,
        out long identity,
        out long version) =>
        TryGetResource(colorMatrices, id, out resource, out identity, out version);

    public bool TryGetCurves(
        PrismResourceId id,
        out PrismCurvesResource resource,
        out long identity,
        out long version) =>
        TryGetResource(curves, id, out resource, out identity, out version);

    public bool TryGetGradientMap(
        PrismResourceId id,
        out PrismGradientMapResource resource,
        out long identity,
        out long version) =>
        TryGetResource(gradients, id, out resource, out identity, out version);

    private static bool TryGetResource<TResource>(
        Dictionary<PrismResourceId, ResolvedResource<TResource>> resources,
        PrismResourceId id,
        out TResource resource,
        out long identity,
        out long version)
        where TResource : class
    {
        if (resources.TryGetValue(id, out ResolvedResource<TResource> resolved))
        {
            resource = resolved.Resource;
            identity = resolved.Identity;
            version = resolved.Version;
            return true;
        }
        resource = null!;
        identity = 0;
        version = 0;
        return false;
    }

    private readonly record struct ResolvedResource<TResource>(
        TResource Resource,
        long Identity,
        long Version);
}
