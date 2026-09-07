using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.SdlGpu;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismSpatterPointTextureCacheTests
{
    [SdlNativeFact]
    public void SpatterGpuTracksTheCpuRecursiveWangEvaluation()
    {
        const int size = 32;
        const int seed = 1_234_567_890;
        const float tone = 0.2f;
        using SdlPrismKernelFixture fixture = new();
        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.Spatter,
            [
                new PrismGraphParameter(0, PrismGraphParameterValueKind.Integer, integerValue: seed),
                new PrismGraphParameter(1, PrismGraphParameterValueKind.Number, numberValue: 5),
                new PrismGraphParameter(2, PrismGraphParameterValueKind.Number, numberValue: 8)
            ],
            PrismBlendMode.Normal, 1, Matrix3x2.Identity, new DrawRect(0, 0, size, size));
        Vector4[] actual = fixture.RunCatalog(plan,
            Enumerable.Repeat(new Vector4(tone, tone, tone, 1), size * size).ToArray(), size, size,
            configure: uniforms => uniforms[27] = new Vector4(
                unchecked((uint)seed) & 0xffffu, unchecked((uint)seed) >> 16, 0, 0));
        PrismPremultipliedColor[] cpuSource = Enumerable.Repeat(
            PrismPremultipliedColor.FromStraight(tone, tone, tone, 1), size * size).ToArray();
        PrismPremultipliedColor[] expected = PrismCatalogFilterMath.Apply(
            plan, cpuSource, size, size, PrismColorProfile.LinearSrgb);

        double totalDifference = 0;
        double maximumDifference = 0;
        for (int index = 0; index < actual.Length; index++)
        {
            Vector4 pixel = actual[index];
            double difference = Math.Abs(pixel.X - expected[index].Red);
            totalDifference += difference;
            maximumDifference = Math.Max(maximumDifference, difference);
            Assert.InRange(pixel.W, 0.999f, 1);
        }

        double meanDifference = totalDifference / actual.Length;
        Assert.True(meanDifference < 0.06,
            $"Mean GPU/CPU difference was {meanDifference}; maximum was {maximumDifference}.");
    }
}
