using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismSpinBlurGpuTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PixelSpaceKernelReadsBaseLevelRegardlessOfMipAvailability(bool mipmapped)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 96;
        const int height = 72;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice device = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(device);
        using SpriteBatch batch = new(device);
        using Texture2D source = new(device, width, height, mipmapped, SurfaceFormat.HalfVector4);
        using RenderTarget2D target = new(device, width, height, false,
            SurfaceFormat.HalfVector4, DepthFormat.None);

        // A constant base level must stay constant after any normalized spin average.
        // Sentinel lower levels expose implicit-derivative LOD selection in the
        // spatially varying sample-count loop, independently of rasterization.
        for (int level = 0; level < source.LevelCount; level++)
        {
            HalfVector4[] pixels = new HalfVector4[
                Math.Max(1, width >> level) * Math.Max(1, height >> level)];
            Array.Fill(pixels, level == 0
                ? new HalfVector4(1, 0, 0, 1)
                : new HalfVector4(0, 0, 1, 1));
            source.SetData(level, null, pixels, 0, pixels.Length);
        }

        PrismLayerDefinition layer = new(new PrismNodeId(1), "spin",
            filters: [new PrismFilterDefinition(PrismFilterId.SpinBlur)]);
        DrawRect bounds = new(0, 0, width, height);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(PrismTestData.Scope(
                PrismTestData.Composition("spin-mip", layer), bounds: bounds)),
            DrawCommand.FillRectangle(bounds, new Cerneala.Drawing.Color(255, 0, 0)),
            DrawCommand.EndPrism());
        PrismGraph graph = new PrismGraphBuilder().Build(new PrismFrameAnalyzer().Analyze(commands));
        PrismNeighborhoodPlan plan = Assert.IsType<PrismNeighborhoodPlan>(
            graph.Nodes.First(node => node.Kind == PrismGraphNodeKind.Filter).NeighborhoodPlan);
        PrismNeighborhoodPass pass = Assert.Single(plan.Passes);
        PrismKernelParameters parameters = new(source, 1,
            new Vector2(1f / width, 1f / height), Vector2.One, Vector2.Zero)
        {
            FilterHeader = new Vector4((int)plan.Operation,
                (int)PrismColorProfile.LinearSrgb, (int)pass.Kind, 0),
            FilterOptions0 = ToXna(plan.Options0),
            FilterOptions1 = ToXna(plan.Options1),
            FilterOptions2 = ToXna(plan.Options2),
            FilterOptions9 = new Vector4(pass.RadiusX, pass.RadiusY,
                pass.SampleCount, (int)plan.BlendMode)
        };
        device.SetRenderTarget(target);
        device.Clear(Microsoft.Xna.Framework.Color.Transparent);
        registry.Bind(registry.NeighborhoodFilter, in parameters);
        batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, registry.Effect);
        batch.Draw(source, new Rectangle(0, 0, width, height), Microsoft.Xna.Framework.Color.White);
        batch.End();
        device.SetRenderTarget(null);

        HalfVector4[] result = new HalfVector4[width * height];
        target.GetData(result);
        float maximumError = result.Max(pixel =>
            Vector4.Distance(pixel.ToVector4(), new Vector4(1, 0, 0, 1)));
        Assert.True(maximumError <= 0.002f,
            $"SpinBlur read a non-base mip level: mipmapped={mipmapped}, max error={maximumError}.");
    }

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
