using System.Numerics;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismDiffuseGlowCharacterizationTests
{
    [Theory]
    [InlineData(0.25f)]
    [InlineData(0.6f)]
    [InlineData(1f)]
    public void GrainUsesStablePixelHashAndPreservesAssociatedCoverage(float grain)
    {
        const int width = 3;
        const int height = 2;
        float[] alphas = [0, 0.25f, 0.5f, 0.75f, 1, 0.125f];
        PrismPremultipliedColor[] source = alphas.Select(alpha =>
            PrismPremultipliedColor.FromStraight(0.125, 0.5, 0.875, alpha)).ToArray();
        PrismResamplingPlan plan = new(
            PrismFilterId.DiffuseGlow,
            PrismResamplingOperation.DiffuseGlow,
            PrismBlendMode.Normal,
            [new PrismResamplingPass(PrismResamplingPassKind.Grain, IsNoOp: false)])
        {
            Options0 = new Vector4(grain, 0, 0, 0)
        };

        PrismPremultipliedColor[] actual = PrismResamplingMath.Apply(
            plan, source, width, height, PrismColorProfile.LinearSrgb);

        for (int index = 0; index < actual.Length; index++)
        {
            float noise = PrismCatalogFilterMath.Hash(index % width, index / width, 9173) - 0.5f;
            Vector3 straight = Vector3.Clamp(
                new Vector3(0.125f, 0.5f, 0.875f) + new Vector3(noise * grain),
                Vector3.Zero,
                Vector3.One);
            Vector3 expected = straight * alphas[index];
            Assert.Equal((double)expected.X, actual[index].Red, precision: 6);
            Assert.Equal((double)expected.Y, actual[index].Green, precision: 6);
            Assert.Equal((double)expected.Z, actual[index].Blue, precision: 6);
            Assert.Equal((double)alphas[index], actual[index].Alpha);
        }
    }
}
