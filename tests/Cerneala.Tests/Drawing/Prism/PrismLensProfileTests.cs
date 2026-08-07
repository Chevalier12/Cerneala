using System.Collections.Immutable;
using System.Numerics;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismLensProfileTests
{
    [Fact]
    public void SparsePolynomialEvaluatesItsNormalizedInputs()
    {
        PrismSparsePolynomial polynomial = new(
        [
            Term(0.25f),
            Term(2, x: 1),
            Term(-0.5f, wavelength: 2)
        ]);

        float value = polynomial.Evaluate(
            new PrismLensFlarePolynomialInput(
                new Vector2(0.4f, -0.2f),
                0.45f,
                0.55f,
                0.3f,
                -0.5f));

        Assert.Equal(0.925f, value, 5);
    }

    [Fact]
    public void TiledGhostRasterizationSeparatesChromaticCentroids()
    {
        PrismLensProfileResource profile = ChromaticProfile();

        Vector4[] flare = PrismLensFlareRenderer.Render(
            profile,
            96,
            64,
            new Vector2(0.75f, 0.5f),
            1);

        Assert.Contains(flare, pixel => pixel.X > 0);
        Assert.Contains(flare, pixel => pixel.Y > 0);
        Assert.Contains(flare, pixel => pixel.Z > 0);
        Assert.True(
            ChannelCentroidX(flare, 96, channel: 0) >
            ChannelCentroidX(flare, 96, channel: 2));
    }

    [Fact]
    public void ApertureAndHousingPredicatesBlockGhostTriangles()
    {
        PrismLensProfileResource profile = Profile(
            apertureX: Constant(2),
            apertureY: Constant(0),
            sensorX: PupilX(0.2f),
            sensorY: PupilY(0.2f),
            transmission: Constant(1),
            relativeRadius: Constant(0.5f));

        Vector4[] flare = PrismLensFlareRenderer.Render(
            profile,
            32,
            32,
            new Vector2(0.5f),
            1);

        Assert.All(
            flare,
            pixel => Assert.Equal(Vector4.Zero, pixel));
    }

    [Fact]
    public void FitterRecoversSharedSparseRayTransferModel()
    {
        List<PrismLensFlareRaySample> samples = [];
        foreach (float angle in new[] { 0f, 15f, 30f })
        foreach (float wavelength in new[] { 450f, 550f, 650f })
        for (int y = -3; y <= 3; y++)
        for (int x = -3; x <= 3; x++)
        {
            Vector2 pupil = new(x / 3f, y / 3f);
            if (pupil.LengthSquared() > 1)
            {
                continue;
            }
            float normalizedAngle = angle / 60;
            float normalizedWavelength = (wavelength - 550) / 200;
            samples.Add(new PrismLensFlareRaySample(
                0,
                pupil,
                angle,
                wavelength,
                pupil * 0.5f,
                new Vector2(
                    (pupil.X * 0.3f) +
                        (normalizedAngle * 0.1f) +
                        (normalizedWavelength * 0.05f),
                    pupil.Y * 0.3f),
                0.4f + (normalizedWavelength * 0.1f),
                0.5f));
        }

        PrismLensProfileResource fitted = PrismLensProfileFitter.Fit(
            samples,
            new PrismLensProfileFitOptions
            {
                RegionCount = 1,
                MaximumTermCount = 10,
                MinimumSamplesPerRegion = 12,
                MinimumCorrelation = 1e-9
            });
        PrismLensFlarePolynomialRegion region =
            Assert.Single(Assert.Single(fitted.Ghosts).Regions);
        PrismLensFlarePolynomialInput query =
            PrismLensProfileFitter.Normalize(
                new Vector2(0.35f, -0.2f),
                22.5f,
                600);

        Assert.Equal(0.155f, region.SensorX.Evaluate(query), 3);
        Assert.Equal(-0.06f, region.SensorY.Evaluate(query), 3);
        Assert.Equal(0.425f, region.Transmission.Evaluate(query), 3);
        Assert.Equal(0.5f, region.RelativeRadius.Evaluate(query), 3);
    }

    [Fact]
    public void CpuLensFlareRequiresProfileAndPreservesAlpha()
    {
        PrismCatalogFilterPlan plan = new(
            PrismFilterId.LensFlare,
            PrismCatalogFilterPrimitive.Procedural,
            PrismBlendMode.Normal,
            ImmutableArray.Create(
                new PrismCatalogFilterPass(
                    PrismCatalogFilterPassKind.Direct,
                    0,
                    0,
                    0,
                    0,
                    0,
                    IsNoOp: false)))
        {
            Options0 = new Vector4(0.5f, 0.5f, 0, 0),
            Options1 = new Vector4(1, 0, 0, 0),
            PrimaryResource = new PrismResourceId("lens"),
            PrimaryResourceRequired = true
        };
        PrismPremultipliedColor sourcePixel =
            PrismPremultipliedColor.FromStraight(
                0.1,
                0.1,
                0.1,
                0.4);
        PrismPremultipliedColor[] source =
            Enumerable.Repeat(sourcePixel, 32 * 32).ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                32,
                32,
                PrismColorProfile.LinearSrgb));
        PrismPremultipliedColor[] result =
            PrismCatalogFilterMath.Apply(
                plan,
                source,
                32,
                32,
                PrismColorProfile.LinearSrgb,
                lensProfile: ChromaticProfile());

        Assert.All(
            result,
            color => Assert.Equal(0.4, color.Alpha, 6));
        Assert.Contains(
            result,
            color => color.Red > sourcePixel.Red);
    }

    [Fact]
    public void LensProfileResourceParticipatesInDrawDependencies()
    {
        PrismResourceId id = new("lens-profile");
        PrismLensProfileResource profile = ChromaticProfile();
        PrismDrawResources resources = PrismDrawResources.Create(
            [],
            [],
            [],
            [
                new PrismDrawLensProfileResource(
                    id,
                    profile,
                    Version: 7,
                    Identity: 11)
            ]);

        Assert.True(resources.HasStableVersions);
        Assert.True(resources.TryGetLensProfile(
            id,
            out PrismLensProfileResource resolved,
            out long identity,
            out long version));
        Assert.Same(profile, resolved);
        Assert.Equal(11, identity);
        Assert.Equal(7, version);
        Assert.True(resources.TryGetDependency(
            id,
            out identity,
            out version));
        Assert.Equal(11, identity);
        Assert.Equal(7, version);
    }

    [Fact]
    public void JsonLoaderRoundTripsValidatedLensProfile()
    {
        PrismLensProfileResource profile = ChromaticProfile();

        string json = PrismLensProfileJson.Serialize(
            profile,
            indented: false);
        PrismLensProfileResource parsed =
            PrismLensProfileJson.Parse(json);
        using MemoryStream stream = new();
        PrismLensProfileJson.Save(stream, profile);
        stream.Position = 0;
        PrismLensProfileResource loaded =
            PrismLensProfileJson.Load(stream);

        Assert.Equal(profile.PupilGridSize, parsed.PupilGridSize);
        Assert.Equal(profile.Ghosts.Length, parsed.Ghosts.Length);
        PrismLensFlarePolynomialInput input =
            PrismLensProfileFitter.Normalize(
                new Vector2(0.25f, -0.15f),
                12,
                625);
        float expected = profile.Ghosts[0].Regions[0]
            .SensorX.Evaluate(input);
        Assert.Equal(
            expected,
            parsed.Ghosts[0].Regions[0].SensorX.Evaluate(input),
            6);
        Assert.Equal(
            expected,
            loaded.Ghosts[0].Regions[0].SensorX.Evaluate(input),
            6);
    }

    private static PrismLensProfileResource ChromaticProfile()
    {
        PrismSparsePolynomial sensorX = new(
        [
            Term(0.28f, x: 1),
            Term(0.12f, wavelength: 1)
        ]);
        return Profile(
            PupilX(0.7f),
            PupilY(0.7f),
            sensorX,
            PupilY(0.28f),
            Constant(0.35f),
            Constant(0.5f));
    }

    private static PrismLensProfileResource Profile(
        PrismSparsePolynomial apertureX,
        PrismSparsePolynomial apertureY,
        PrismSparsePolynomial sensorX,
        PrismSparsePolynomial sensorY,
        PrismSparsePolynomial transmission,
        PrismSparsePolynomial relativeRadius)
    {
        PrismLensFlarePolynomialRegion region = new(
            0,
            61,
            apertureX,
            apertureY,
            sensorX,
            sensorY,
            transmission,
            relativeRadius);
        return new PrismLensProfileResource(
        [
            new PrismLensFlareGhost([region])
        ],
        pupilGridSize: 9);
    }

    private static PrismSparsePolynomial Constant(float value) =>
        new([Term(value)]);

    private static PrismSparsePolynomial PupilX(float scale) =>
        new([Term(scale, x: 1)]);

    private static PrismSparsePolynomial PupilY(float scale) =>
        new([Term(scale, y: 1)]);

    private static PrismSparsePolynomialTerm Term(
        float coefficient,
        byte x = 0,
        byte y = 0,
        byte radius = 0,
        byte inverseRadius = 0,
        byte angle = 0,
        byte wavelength = 0) =>
        new(
            coefficient,
            x,
            y,
            radius,
            inverseRadius,
            angle,
            wavelength);

    private static double ChannelCentroidX(
        IReadOnlyList<Vector4> pixels,
        int width,
        int channel)
    {
        double weightedX = 0;
        double weight = 0;
        for (int index = 0; index < pixels.Count; index++)
        {
            float value = channel switch
            {
                0 => pixels[index].X,
                1 => pixels[index].Y,
                _ => pixels[index].Z
            };
            weightedX += (index % width) * value;
            weight += value;
        }
        return weightedX / weight;
    }
}
