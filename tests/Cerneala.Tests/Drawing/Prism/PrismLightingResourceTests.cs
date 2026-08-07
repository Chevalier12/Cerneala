using System.Numerics;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismLightingResourceTests
{
    [Fact]
    public void ResourcePreservesValidatedDirectionalAndPointLights()
    {
        PrismLight directional = PrismLight.Directional(
            new Vector3(0, 0, 2),
            new Vector3(1, 0.5f, 0.25f),
            3);
        PrismLight point = PrismLight.Point(
            new Vector3(0.25f, 0.75f, 0.5f),
            new Vector3(0.25f, 0.5f, 1),
            2);

        PrismLightingResource resource = new(
        [
            directional,
            point
        ]);

        Assert.Equal(2, resource.Lights.Length);
        Assert.Equal(PrismLightKind.Directional, directional.Kind);
        Assert.Equal(Vector3.UnitZ, directional.Direction);
        Assert.Equal(PrismLightKind.Point, point.Kind);
        Assert.Equal(new Vector3(0.25f, 0.75f, 0.5f), point.Position);
    }

    [Fact]
    public void ResourceRejectsInvalidOrExcessiveLights()
    {
        Assert.Throws<ArgumentException>(
            () => new PrismLightingResource([]));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PrismLight.Directional(
                Vector3.Zero,
                Vector3.One));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PrismLight.Point(
                Vector3.Zero,
                new Vector3(-1, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PrismLight.Point(
                Vector3.Zero,
                Vector3.One,
                float.PositiveInfinity));
        Assert.Throws<ArgumentException>(
            () => new PrismLightingResource(
                Enumerable.Repeat(
                    PrismLight.Directional(
                        Vector3.UnitZ,
                        Vector3.One),
                    PrismLightingResource.MaximumLightCount + 1)));
        Assert.Throws<ArgumentException>(
            () => new PrismLightingResource([default]));
    }

    [Fact]
    public void DrawResourcesPreserveLightingIdentityVersionAndDependency()
    {
        PrismResourceId resourceId = new("lighting");
        PrismLightingResource lighting = new(
        [
            PrismLight.Directional(
                Vector3.UnitZ,
                Vector3.One)
        ]);
        PrismDrawResources resources = PrismDrawResources.Create(
            [],
            [],
            [],
            [],
            [new PrismDrawLightingResource(resourceId, lighting, 17, 29)]);

        Assert.True(
            resources.TryGetLighting(
                resourceId,
                out PrismLightingResource? resolved,
                out long identity,
                out long version));
        Assert.Same(lighting, resolved);
        Assert.Equal(17, version);
        Assert.Equal(29, identity);
        Assert.True(resources.TryGetVersion(resourceId, out long resolvedVersion));
        Assert.Equal(17, resolvedVersion);
    }
}
