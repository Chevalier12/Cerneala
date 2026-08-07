using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismGradientOverlayPipelineTests
{
    [Fact]
    public void GradientResourceSupportsAlphaAndHardStops()
    {
        PrismGradientMapResource resource = new(
        [
            new PrismGradientMapPoint(0, Vector3.Zero),
            new PrismGradientMapPoint(0.5f, Vector3.Zero),
            new PrismGradientMapPoint(0.5f, Vector3.One, 0.25f),
            new PrismGradientMapPoint(1, Vector3.One)
        ]);

        Assert.Equal(4, resource.Points.Length);
        Assert.Equal(0.25f, resource.Points[2].Alpha);
        Assert.Throws<ArgumentException>(() =>
            new PrismGradientMapResource(
            [
                new PrismGradientMapPoint(0, Vector3.Zero),
                new PrismGradientMapPoint(0.75f, Vector3.One),
                new PrismGradientMapPoint(0.5f, Vector3.Zero),
                new PrismGradientMapPoint(1, Vector3.One)
            ]));
    }

    [Fact]
    public void PerceptualMethodUsesOklabInsteadOfClassicSrgb()
    {
        PrismGradientMapResource resource = RedToBlue();
        Vector4 perceptual = PrismCssGradientLut.Create(
            resource,
            PrismGradientInterpolation.PerceptualOklab,
            PrismColorProfile.LinearSrgb).Sample(0.5f);
        Vector4 classic = PrismCssGradientLut.Create(
            resource,
            PrismGradientInterpolation.ClassicSrgb,
            PrismColorProfile.LinearSrgb).Sample(0.5f);

        Assert.True(Vector3.Distance(
            new Vector3(perceptual.X, perceptual.Y, perceptual.Z),
            new Vector3(classic.X, classic.Y, classic.Z)) > 0.05f);
        Assert.True(perceptual.Y > classic.Y);
    }

    [Fact]
    public void PremultipliedInterpolationAvoidsTransparentColorHalos()
    {
        PrismGradientMapResource resource = new(
        [
            new PrismGradientMapPoint(0, Vector3.UnitX, 1),
            new PrismGradientMapPoint(1, Vector3.UnitZ, 0)
        ]);
        Vector4 midpoint = PrismCssGradientLut.Create(
            resource,
            PrismGradientInterpolation.PerceptualOklab,
            PrismColorProfile.LinearSrgb).Sample(0.5f);
        Vector3 straight = new(
            midpoint.X / midpoint.W,
            midpoint.Y / midpoint.W,
            midpoint.Z / midpoint.W);

        Assert.Equal(0.5f, midpoint.W, 2);
        Assert.True(straight.X > 0.99f);
        Assert.True(straight.Y < 0.01f);
        Assert.True(straight.Z < 0.01f);
    }

    [Fact]
    public void DuplicateOffsetsProduceAStableHardStop()
    {
        PrismGradientMapResource resource = new(
        [
            new PrismGradientMapPoint(0, Vector3.Zero),
            new PrismGradientMapPoint(0.5f, Vector3.Zero),
            new PrismGradientMapPoint(0.5f, Vector3.One),
            new PrismGradientMapPoint(1, Vector3.One)
        ]);
        PrismCssGradientLut lut = PrismCssGradientLut.Create(
            resource,
            PrismGradientInterpolation.ClassicSrgb,
            PrismColorProfile.LinearSrgb);

        Assert.True(lut.Sample(0.49f).X < 0.01f);
        Assert.True(lut.Sample(0.51f).X > 0.99f);
    }

    [Fact]
    public void CatalogExposesGradientAsTypedResource()
    {
        PrismCatalogPropertyDescriptor gradient =
            PrismCatalogRuntime
                .GetEntry((int)PrismStyleId.GradientOverlay)
                .Properties
                .Single(property => property.Name == "Gradient");

        Assert.Equal(PrismCatalogValueType.Resource, gradient.ValueType);
    }

    private static PrismGradientMapResource RedToBlue() =>
        new(
        [
            new PrismGradientMapPoint(0, Vector3.UnitX),
            new PrismGradientMapPoint(1, Vector3.UnitZ)
        ]);
}
