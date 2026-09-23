using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface3DStageOnePresenceTests
{
    [Fact]
    public void AcceptedPublicSurfaceAndFrameContractsArePresent()
    {
        Assembly assembly = typeof(RenderSurface2D).Assembly;
        Type surface = Require(assembly, "Cerneala.UI.Controls.RenderSurface3D");
        Type frame = Require(assembly, "Cerneala.UI.Controls.RenderSurface3DFrame");

        Assert.True(typeof(ContentControl).IsAssignableFrom(surface));
        Assert.False(surface.IsSealed);
        Assert.NotNull(surface.GetProperty("ClearColor"));
        Assert.NotNull(surface.GetProperty("ViewMatrix"));
        Assert.NotNull(surface.GetProperty("Projection"));
        Assert.NotNull(surface.GetProperty("RedrawMode"));
        Assert.NotNull(surface.GetMethod("InvalidateFrame", Type.EmptyTypes));
        Assert.NotNull(surface.GetMethod("TryWorldToRoot"));
        Assert.NotNull(surface.GetMethod("TryRootToWorldRay"));
        Assert.Contains(frame.GetMethods(), method => method.Name == "DrawLineBatch");
        Assert.Contains(frame.GetMethods(), method => method.Name == "DrawMarkerBatch");
    }

    [Fact]
    public void AcceptedDrawingTypesAndAppendedCommandKindArePresent()
    {
        Assembly assembly = typeof(DrawCommand).Assembly;
        Assert.NotNull(assembly.GetType("Cerneala.Drawing.RenderProjection3D", false));
        Assert.NotNull(assembly.GetType("Cerneala.Drawing.RenderProjection3DKind", false));
        Assert.NotNull(assembly.GetType("Cerneala.Drawing.DrawRay3D", false));
        Assert.NotNull(assembly.GetType("Cerneala.Drawing.DrawLineSegment3D", false));
        Assert.NotNull(assembly.GetType("Cerneala.Drawing.DrawMarker3D", false));
        Assert.Equal(31, (int)DrawCommandKind.PopLayer);
        Assert.True(Enum.TryParse("RenderSurface3D", out DrawCommandKind kind));
        Assert.Equal(32, (int)kind);
    }

    private static Type Require(Assembly assembly, string name) =>
        assembly.GetType(name, throwOnError: false) ??
        throw new Xunit.Sdk.XunitException($"Required public type {name} is absent.");
}
