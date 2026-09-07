using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismSpinBlurGpuTests
{
    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void PixelSpaceKernelReadsBaseLevelRegardlessOfMipAvailability(bool mipmapped)
    {
        const int width = 96;
        const int height = 72;
        using SdlPrismKernelFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        SdlGpuDrawingResources resources = session.DrawingResources;
        using SdlGpuPrismSurfaceLease source = resources.PrismResources.RentSurface(
            session.WindowIdentity, width, height, SdlGpuTextureFormat.R16G16B16A16Float, mipmapped);
        using SdlGpuPrismSurfaceLease target = resources.PrismResources.RentSurface(
            session.WindowIdentity, width, height, SdlGpuTextureFormat.R16G16B16A16Float, false);
        PrismLayerDefinition layer = new(new PrismNodeId(1), "spin",
            filters: [new PrismFilterDefinition(PrismFilterId.SpinBlur)]);
        DrawRect bounds = new(0, 0, width, height);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(PrismTestData.Scope(
                PrismTestData.Composition("spin-mip", layer), bounds: bounds)),
            DrawCommand.FillRectangle(bounds, new Color(255, 0, 0)),
            DrawCommand.EndPrism());
        PrismGraph graph = new PrismGraphBuilder().Build(new PrismFrameAnalyzer().Analyze(commands));
        PrismNeighborhoodPlan plan = Assert.IsType<PrismNeighborhoodPlan>(
            graph.Nodes.First(node => node.Kind == PrismGraphNodeKind.Filter).NeighborhoodPlan);
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);
        SdlGpuPrismUniforms uniforms = SdlPrismKernelFixture.CreateUniforms(7, width, height);
        uniforms[23] = new Vector4((int)plan.Operation,
            (int)PrismColorProfile.LinearSrgb, (int)pass.Kind, 0);
        uniforms[24] = plan.Options0;
        uniforms[25] = plan.Options1;
        uniforms[26] = plan.Options2;
        uniforms[33] = new Vector4(pass.RadiusX, pass.RadiusY,
            pass.SampleCount, SdlGpuPrismKernelSelector.ResolveBlendMode(plan.BlendMode));

        session.BeginFrame(Color.Transparent);
        try
        {
            nint red = resources.GetOrCreateHalfVector4Texture(session, new object(),
                1, 1, [new Vector4(1, 0, 0, 1)]).Handle;
            nint blue = resources.GetOrCreateHalfVector4Texture(session, new object(),
                1, 1, [new Vector4(0, 0, 1, 1)]).Handle;
            SdlGpuPrismUniforms copy = SdlPrismKernelFixture.CreateUniforms(0, width, height);
            // Populate lower levels with blue, then replace only the base with red.
            // Any implicit-derivative LOD selection in the spin loop reads the sentinel.
            if (mipmapped)
            {
                fixture.Draw(source.Target, blue, blue, blue, copy);
                session.GenerateMipmaps(source.Target);
            }
            fixture.Draw(source.Target, red, red, red, copy);
            fixture.Draw(target.Target, source.Target.SampleTexture,
                source.Target.SampleTexture, source.Target.SampleTexture, uniforms);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }

        float maximumError = fixture.ReadPixels(target.Target).Max(pixel =>
            Vector4.Distance(pixel, new Vector4(1, 0, 0, 1)));
        Assert.True(maximumError <= 0.002f,
            $"SpinBlur read a non-base mip level: mipmapped={mipmapped}, max error={maximumError}.");
    }
}
