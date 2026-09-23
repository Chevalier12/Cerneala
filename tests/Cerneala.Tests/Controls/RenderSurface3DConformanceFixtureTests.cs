using System.Numerics;
using Cerneala.Drawing;
using Cerneala.SmokeTests;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Aspect;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Prism.Runtime;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface3DConformanceFixtureTests
{
    [Fact]
    public void FixedSkeletonHasTwentyAcyclicJointsAndTwoDeterministicPoses()
    {
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        Assert.Equal(RenderSurface3DConformanceFixture.JointCount, fixture.ParentIndices.Count);
        Assert.Equal(RenderSurface3DConformanceFixture.JointCount, fixture.JointPositions.Count);
        for (int joint = 0; joint < fixture.ParentIndices.Count; joint++)
            Assert.InRange(fixture.ParentIndices[joint], -1, joint - 1);

        Vector3[] rest = fixture.JointPositions.ToArray();
        fixture.SetPose(1);
        Vector3[] raised = fixture.JointPositions.ToArray();
        Assert.False(rest.SequenceEqual(raised));
        fixture.SetPose(0);
        Assert.Equal(rest, fixture.JointPositions);
        fixture.SetPose(1);
        Assert.Equal(raised, fixture.JointPositions);
    }

    [Fact]
    public void GenericSceneAndEightCameraPresetsUseTheSameThinControl()
    {
        RenderSurface3D generic = RenderSurface3DConformanceFixture.CreateGenericScene();
        Assert.IsType<RenderSurface3D>(generic);
        RenderSurface3DRecording genericFrame = ((IRenderSurface3DSource)generic)
            .RecordFrame(new DrawRect(0, 0, 100, 80), 1);
        Assert.Equal(2, genericFrame.Primitives.Count);
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        HashSet<Matrix4x4> views = [];
        for (int direction = 0; direction < 8; direction++)
        {
            fixture.SetPresetDirection(direction);
            views.Add(fixture.Surface.ViewMatrix);
        }
        Assert.Equal(8, views.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => fixture.SetPresetDirection(8));
        RenderSurface3DConformanceFixture dense = RenderSurface3DConformanceFixture.Create(200);
        Assert.Equal(200, dense.RenderedJointCount);
        RenderSurface3DRecording denseFrame = ((IRenderSurface3DSource)dense.Surface)
            .RecordFrame(new DrawRect(0, 0, 800, 600), 1);
        Assert.Equal(411, denseFrame.Primitives.Count);
    }

    [Fact]
    public void DescriptorBindingReplacementAdvancesRequestedRasterGeneration()
    {
        RenderSurface3D surface = new();
        ObservableValue<RenderProjection3D> camera = new(surface.Projection);
        using UiPropertyBinding<RenderProjection3D> binding =
            BindingOperations.BindOneWay(surface, RenderSurface3D.ProjectionProperty, camera);
        long before = ((IRenderSurface3DSource)surface).FrameVersion;
        RenderProjection3D orthographic = RenderProjection3D.Orthographic(3, .01f, 100);
        camera.Value = orthographic;
        Assert.Equal(orthographic, surface.Projection);
        Assert.True(((IRenderSurface3DSource)surface).FrameVersion > before);
    }

    [Fact]
    public void ContentAndTemplateReplacementDetachOldOverlayWithoutDuplicatingDrawHandler()
    {
        RenderSurface3D surface = new();
        Button first = new(), second = new();
        int calls = 0;
        surface.Draw += (_, frame) => { calls++; frame.DrawMarker(Vector3.Zero, Color.White); };
        UIRoot root = new(320, 240);
        root.VisualChildren.Add(surface);
        surface.Content = first;
        surface.Content = second;
        Assert.Null(first.VisualParent);
        Assert.Same(surface, second.VisualParent);

        UIElement templateRoot = new();
        surface.ComponentTemplate = new ComponentTemplate<RenderSurface3D>("fixture-template", _ => templateRoot);
        Assert.Null(second.VisualParent);
        Assert.Same(surface, templateRoot.VisualParent);
        surface.ComponentTemplate = null;
        Assert.Null(templateRoot.VisualParent);
        Assert.Same(surface, second.VisualParent);
        root.VisualChildren.Remove(surface);
        root.VisualChildren.Add(surface);
        ((IRenderSurface3DSource)surface).RecordFrame(new DrawRect(0, 0, 100, 80), 1);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FixtureSelectionUsesPublicWorldRootAndRayConversions()
    {
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(fixture.Surface);
        root.ProcessFrame();
        Assert.True(fixture.Surface.TryWorldToRoot(fixture.JointPositions[0], out Vector2 point));
        Assert.True(fixture.Surface.TryRootToWorldRay(point, out _));
        Assert.True(fixture.TrySelectAt(point));
        Assert.InRange(fixture.SelectedJoint, 0, 19);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WheelInputZoomsProjectedJointsInPerspectiveAndOrthographic(bool orthographic)
    {
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(fixture.Surface);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        if (orthographic)
        {
            PointerSnapshot overlay = PointerSnapshot.Empty.WithPosition(155, 20);
            PointerSnapshot down = overlay.WithButton(InputMouseButton.Left, true);
            bridge.Dispatch(root, new InputFrame(overlay, down, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
            bridge.Dispatch(root, new InputFrame(down, overlay, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
            Assert.Equal(RenderProjection3DKind.Orthographic, fixture.Surface.Projection.Kind);
        }

        float before = JointSeparation(fixture, 6, 10);
        PointerSnapshot atSurface = PointerSnapshot.Empty.WithPosition(320, 240);
        bridge.Dispatch(root, new InputFrame(atSurface, atSurface.WithWheelValue(120),
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        Assert.Equal(1, fixture.ZoomCount);
        Assert.True(JointSeparation(fixture, 6, 10) > before + .1f);
    }

    [Fact]
    public void PickingDoesNotPreferNearJointOutsideItsVisibleMarker()
    {
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        fixture.Surface.ViewMatrix = Matrix4x4.CreateLookAt(new Vector3(0, 0, 17), Vector3.Zero, Vector3.UnitY);
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(fixture.Surface);
        root.ProcessFrame();
        Assert.True(fixture.Surface.TryWorldToRoot(fixture.JointPositions[18], out Vector2 clickedCenter));
        Assert.True(fixture.Surface.TryWorldToRoot(fixture.JointPositions[19], out Vector2 nearCenter));
        Assert.InRange(Vector2.Distance(clickedCenter, nearCenter), 4.6f, 11.9f);
        Assert.True(fixture.TrySelectAt(clickedCenter));
        Assert.Equal(18, fixture.SelectedJoint);
    }

    [Fact]
    public void PickingChoosesFrontmostOverlappingMarkerAndStableLowerIndexOnExactTie()
    {
        RenderSurface3D surface = new();
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.True(surface.TryWorldToRoot(Vector3.Zero, out Vector2 click));
        Assert.Equal(1, RenderSurface3DConformanceFixture.PickVisibleJoint(surface,
            [new Vector3(0, 0, -1), new Vector3(0, 0, .5f)], click));
        Assert.Equal(0, RenderSurface3DConformanceFixture.PickVisibleJoint(surface,
            [Vector3.Zero, Vector3.Zero], click));
    }

    [Fact]
    public void PickingExcludesOffFrustumMarkersAndUsesTransformedMarkerFootprint()
    {
        RenderSurface3D surface = new() { Scale = 2, TranslateX = 20 };
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.True(surface.TryWorldToRoot(Vector3.Zero, out Vector2 center));
        Assert.True(surface.TryRootToWorldRay(center + new Vector2(8, 0), out _));
        Assert.Equal(1, RenderSurface3DConformanceFixture.PickVisibleJoint(surface,
            [new Vector3(100, 0, 0), Vector3.Zero], center + new Vector2(8, 0)));
        Assert.Equal(-1, RenderSurface3DConformanceFixture.PickVisibleJoint(surface,
            [new Vector3(100, 0, 0), Vector3.Zero], center + new Vector2(10, 0)));
        Assert.Equal(-1, RenderSurface3DConformanceFixture.PickVisibleJoint(surface,
            [new Vector3(100, 0, 0)], center));
    }

    private static float JointSeparation(RenderSurface3DConformanceFixture fixture, int first, int second)
    {
        Assert.True(fixture.Surface.TryWorldToRoot(fixture.JointPositions[first], out Vector2 a));
        Assert.True(fixture.Surface.TryWorldToRoot(fixture.JointPositions[second], out Vector2 b));
        return Vector2.Distance(a, b);
    }

    [Fact]
    public void PointerCaptureEndsOnDetachAndExternalCancel()
    {
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(fixture.Surface);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        PointerSnapshot idle = PointerSnapshot.Empty.WithPosition(320, 240);
        PointerSnapshot down = idle.WithButton(InputMouseButton.Left, true);
        InputFrame press = new(idle, down, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
        bridge.Dispatch(root, press);
        Assert.True(bridge.PointerCaptureManager.HasCapture);

        bridge.PointerCaptureManager.Release(root.InputCache.EnsureCurrent(root));
        Assert.False(bridge.PointerCaptureManager.HasCapture);
        int before = fixture.OrbitCount;
        bridge.Dispatch(root, new InputFrame(down, down.WithPosition(360, 260),
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        Assert.Equal(before, fixture.OrbitCount);

        bridge.Dispatch(root, press);
        Assert.True(bridge.PointerCaptureManager.HasCapture);
        root.VisualChildren.Remove(fixture.Surface);
        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    [Fact]
    public void OverlayClickFocusesButtonAndNeverCapturesOrOrbitsTheSurface()
    {
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create();
        UIRoot root = new(640, 480);
        root.VisualChildren.Add(fixture.Surface);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        PointerSnapshot idle = PointerSnapshot.Empty.WithPosition(50, 20);
        PointerSnapshot down = idle.WithButton(InputMouseButton.Left, true);
        bridge.Dispatch(root, new InputFrame(idle, down, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));

        Assert.IsType<Button>(bridge.FocusManager.FocusedElement);
        Assert.False(bridge.PointerCaptureManager.HasCapture);
        Assert.Equal(0, fixture.OrbitCount);
        bridge.Dispatch(root, new InputFrame(down, idle, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []));
        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    [Fact]
    public void AspectMotionAndPrismOperateOnExistingUiPropertiesAndComposedImage()
    {
        RenderSurface3D surface = new()
        {
            Aspect = new ElementAspect([new ElementAspectValue(UIElement.OpacityProperty, .9f)])
        };
        surface.Draw += (_, frame) => frame.DrawMarker(Vector3.Zero, Color.White);
        ManualMotionClock clock = new();
        UIRoot root = new(200, 150, motionClock: clock);
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.Equal(.9f, surface.Opacity);
        using IDisposable prism = GeneratedMarkup.AttachPrism(surface, () =>
            new PrismInstance(PrismTestData.Composition("Surface3D", PrismTestData.Layer(1, "Image"))));
        using MotionHandle motion = surface.Motion().Animate(UIElement.OpacityProperty).To(.5f)
            .With(Cerneala.UI.Motion.Specs.Motion.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.InRange(surface.Opacity, .5f, .9f);
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.RenderSurface3D);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.BeginPrism);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.EndPrism);
    }
}
