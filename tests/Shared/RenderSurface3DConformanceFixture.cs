using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;

namespace Cerneala.SmokeTests;

// Linked only into consumer projects. The framework has no skeleton model.
internal sealed class RenderSurface3DConformanceFixture
{
    internal const int JointCount = 20;
    private static readonly int[] Parents = [-1, 0, 1, 2, 3, 4, 2, 6, 7, 8, 2, 10, 11, 12, 0, 14, 15, 0, 17, 18];
    private static readonly Vector3[] Offsets =
    [
        new(0, 0, 0), new(0, .42f, 0), new(0, .43f, 0), new(0, .36f, 0), new(0, .29f, 0), new(0, .19f, 0),
        new(-.32f, .25f, 0), new(-.34f, -.11f, 0), new(-.29f, -.2f, 0), new(-.15f, -.1f, 0),
        new(.32f, .25f, 0), new(.34f, -.11f, 0), new(.29f, -.2f, 0), new(.15f, -.1f, 0),
        new(-.23f, -.24f, 0), new(0, -.51f, 0), new(0, -.46f, .08f),
        new(.23f, -.24f, 0), new(0, -.51f, 0), new(0, -.46f, .08f)
    ];

    private readonly Vector3[] positions = new Vector3[JointCount];
    private readonly FixtureSurface surface;
    private readonly int copies;
    private float yaw = .4f, pitch = .2f, distance = 5.2f, orthographicHeight = 3.5f, armRotation;
    private Vector3 target = Vector3.Zero;
    private int poseIndex;

    private RenderSurface3DConformanceFixture(int jointCount)
    {
        if (jointCount is not (20 or 200)) throw new ArgumentOutOfRangeException(nameof(jointCount));
        copies = jointCount / JointCount;
        surface = new FixtureSurface(this) { ClearColor = new Color(8, 16, 26), Focusable = true };
        Servo.SetId(surface, "3d-surface");
        surface.Draw += Draw;
        surface.MouseWheel += OnWheel;
        StackPanel overlay = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        AddButton(overlay, "3d-pan", "Pan", () => surface.PanMode = !surface.PanMode);
        AddButton(overlay, "3d-projection", "Projection", ToggleProjection);
        AddButton(overlay, "3d-pose", "Pose", () => SetPose(poseIndex == 0 ? 1 : 0));
        surface.Content = overlay;
        RecomputePose();
        UpdateView();
    }

    internal RenderSurface3D Surface => surface;
    internal IReadOnlyList<int> ParentIndices => Parents;
    internal IReadOnlyList<Vector3> JointPositions => positions;
    internal int SelectedJoint { get; private set; } = -1;
    internal int PoseIndex => poseIndex;
    internal int OrbitCount { get; private set; }
    internal int PanCount { get; private set; }
    internal int ZoomCount { get; private set; }
    internal int DrawCount { get; private set; }
    internal int RenderedJointCount => copies * JointCount;

    internal static RenderSurface3DConformanceFixture Create(int jointCount = JointCount) => new(jointCount);

    internal void SetPose(int index)
    {
        if (index is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(index));
        poseIndex = index;
        armRotation = index == 0 ? 0 : -.85f;
        RecomputePose();
        surface.InvalidateFrame();
    }

    internal void SetPresetDirection(int index)
    {
        if ((uint)index >= 8) throw new ArgumentOutOfRangeException(nameof(index));
        target = new Vector3(0, .2f, 0);
        distance = 5.2f;
        orthographicHeight = 3.5f;
        yaw = index * MathF.PI / 4;
        pitch = (index & 1) == 0 ? .08f : .28f;
        if (surface.Projection.Kind == RenderProjection3DKind.Orthographic)
            surface.Projection = RenderProjection3D.Orthographic(orthographicHeight, .01f, 100);
        UpdateView();
    }

    internal bool TrySelectAt(Vector2 rootPoint)
    {
        int best = PickVisibleJoint(surface, positions, rootPoint, SelectedJoint);
        if (best < 0) return false;
        SelectedJoint = best;
        surface.InvalidateFrame();
        return true;
    }

    internal static int PickVisibleJoint(RenderSurface3D surface, IReadOnlyList<Vector3> candidates,
        Vector2 rootPoint, int selectedJoint = -1)
    {
        if (!surface.TryRootToWorldRay(rootPoint, out DrawRay3D ray)) return -1;
        int best = -1;
        float bestDepth = float.PositiveInfinity, bestDistance = float.PositiveInfinity;
        for (int joint = 0; joint < candidates.Count; joint++)
        {
            Vector3 point = candidates[joint];
            if (!surface.TryWorldToRoot(point, out Vector2 screen)) continue;
            float radius = joint == selectedJoint ? 7 : 4.5f;
            if (!TryMarkerDistance(surface, point, screen, rootPoint, radius, out float distance)) continue;
            float depth = Vector3.Dot(point - ray.Origin, ray.Direction);
            if (depth < 0) continue;
            // Of marker footprints containing the click, nearest positive ray depth wins.
            // Depth ties within 1e-4 use normalized marker-center distance;
            // exact distance ties keep the lower joint index.
            if (depth < bestDepth - .0001f ||
                (MathF.Abs(depth - bestDepth) <= .0001f && distance < bestDistance - .0001f))
            {
                best = joint;
                bestDepth = depth;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static bool TryMarkerDistance(RenderSurface3D surface, Vector3 point, Vector2 center,
        Vector2 rootPoint, float radiusDips, out float normalizedDistance)
    {
        normalizedDistance = float.PositiveInfinity;
        Vector3 cameraPoint = Vector3.Transform(point, surface.ViewMatrix);
        float halfHeight = surface.Projection.Kind == RenderProjection3DKind.Perspective
            ? -cameraPoint.Z * MathF.Tan(surface.Projection.VerticalFieldOfViewOrHeight / 2)
            : surface.Projection.VerticalFieldOfViewOrHeight / 2;
        if (!float.IsFinite(halfHeight) || halfHeight <= 0 || surface.ArrangedBounds.Height <= 0 ||
            !Matrix4x4.Invert(surface.ViewMatrix, out Matrix4x4 inverseView)) return false;
        float viewRadius = 2 * halfHeight * radiusDips / surface.ArrangedBounds.Height;
        Vector3 right = Vector3.TransformNormal(Vector3.UnitX * viewRadius, inverseView);
        Vector3 up = Vector3.TransformNormal(Vector3.UnitY * viewRadius, inverseView);
        if (!TryProjectedRadiusAxis(surface, point, center, right, out Vector2 rootRight) ||
            !TryProjectedRadiusAxis(surface, point, center, up, out Vector2 rootUp)) return false;
        float determinant = rootRight.X * rootUp.Y - rootRight.Y * rootUp.X;
        if (!float.IsFinite(determinant) || MathF.Abs(determinant) <= .000001f) return false;
        Vector2 offset = rootPoint - center;
        float x = (offset.X * rootUp.Y - offset.Y * rootUp.X) / determinant;
        float y = (rootRight.X * offset.Y - rootRight.Y * offset.X) / determinant;
        normalizedDistance = MathF.Sqrt(x * x + y * y);
        return float.IsFinite(normalizedDistance) && normalizedDistance <= 1;
    }

    private static bool TryProjectedRadiusAxis(RenderSurface3D surface, Vector3 point, Vector2 center,
        Vector3 worldOffset, out Vector2 rootAxis)
    {
        if (surface.TryWorldToRoot(point + worldOffset, out Vector2 edge))
        {
            rootAxis = edge - center;
            return true;
        }
        if (surface.TryWorldToRoot(point - worldOffset, out edge))
        {
            rootAxis = center - edge;
            return true;
        }
        rootAxis = default;
        return false;
    }

    internal static RenderSurface3D CreateGenericScene()
    {
        RenderSurface3D generic = new() { ClearColor = new Color(8, 16, 26) };
        generic.Draw += static (_, frame) =>
        {
            frame.DrawLine(new Vector3(-1, -1, 0), new Vector3(1, 1, 0), new Color(240, 140, 40), 2);
            frame.DrawMarker(new Vector3(.5f, 0, 0), new Color(30, 220, 180), 12);
        };
        return generic;
    }

    private void RecomputePose()
    {
        Matrix4x4[] transforms = new Matrix4x4[JointCount];
        for (int joint = 0; joint < JointCount; joint++)
        {
            Matrix4x4 local = Matrix4x4.CreateTranslation(Offsets[joint]);
            if (joint == 10) local = Matrix4x4.CreateRotationZ(armRotation) * local;
            transforms[joint] = Parents[joint] < 0 ? local : local * transforms[Parents[joint]];
            positions[joint] = Vector3.Transform(Vector3.Zero, transforms[joint]);
        }
    }

    private void Draw(RenderSurface3D _, RenderSurface3DFrame frame)
    {
        DrawCount++;
        Color grid = new(38, 61, 76);
        for (int step = -4; step <= 4; step++)
        {
            frame.DrawLine(new Vector3(step * .5f, -1.25f, -2), new Vector3(step * .5f, -1.25f, 2), grid);
            frame.DrawLine(new Vector3(-2, -1.25f, step * .5f), new Vector3(2, -1.25f, step * .5f), grid);
        }
        frame.DrawLine(Vector3.Zero, Vector3.UnitX, new Color(240, 68, 76), 2);
        frame.DrawLine(Vector3.Zero, Vector3.UnitY, new Color(80, 226, 104), 2);
        frame.DrawLine(Vector3.Zero, Vector3.UnitZ, new Color(80, 142, 252), 2);
        for (int copy = 0; copy < copies; copy++)
        {
            Matrix4x4 model = Matrix4x4.CreateTranslation((copy % 5 - (copies > 1 ? 2 : 0)) * 1.35f,
                (copy / 5) * 2.4f, 0);
            for (int joint = 0; joint < JointCount; joint++)
            {
                if (Parents[joint] >= 0)
                    frame.DrawLine(positions[Parents[joint]], positions[joint], new Color(224, 226, 212), model, 3);
                frame.DrawMarker(positions[joint], joint == SelectedJoint && copy == 0 ? new Color(255, 196, 50) : new Color(72, 211, 245),
                    model, joint == SelectedJoint && copy == 0 ? 14 : 9);
            }
        }
    }

    private void UpdateView()
    {
        Vector3 offset = new(distance * MathF.Sin(yaw) * MathF.Cos(pitch),
            distance * MathF.Sin(pitch), distance * MathF.Cos(yaw) * MathF.Cos(pitch));
        surface.ViewMatrix = Matrix4x4.CreateLookAt(target + offset, target, Vector3.UnitY);
    }

    private void ToggleProjection() => surface.Projection = surface.Projection.Kind == RenderProjection3DKind.Perspective
        ? RenderProjection3D.Orthographic(orthographicHeight, .01f, 100)
        : RenderProjection3D.Perspective(MathF.PI / 3, .01f, 100);

    private void OnWheel(UiElementId _, RoutedEventArgs args)
    {
        if (args is not MouseWheelEventArgs wheel) return;
        distance = Math.Clamp(distance * MathF.Exp(-wheel.Delta / 1200f), 2, 15);
        orthographicHeight = 3.5f * distance / 5.2f;
        if (surface.Projection.Kind == RenderProjection3DKind.Orthographic)
            surface.Projection = RenderProjection3D.Orthographic(orthographicHeight, .01f, 100);
        ZoomCount++;
        UpdateView();
        args.Handled = true;
    }

    private void Drag(Vector2 delta, bool pan)
    {
        if (pan)
        {
            Matrix4x4.Invert(surface.ViewMatrix, out Matrix4x4 inverse);
            Vector3 right = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, inverse));
            Vector3 up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, inverse));
            target += (-delta.X * right + delta.Y * up) * (distance / 400);
            PanCount++;
        }
        else
        {
            yaw += delta.X * .008f;
            pitch = Math.Clamp(pitch + delta.Y * .008f, -1.35f, 1.35f);
            OrbitCount++;
        }
        UpdateView();
    }

    private static void AddButton(StackPanel panel, string id, string label, Action action)
    {
        Button button = new() { Content = label, Margin = new Thickness(4), Width = 94, Height = 30 };
        Servo.SetId(button, id);
        button.Click += (_, _) => action();
        panel.LogicalChildren.Add(button);
        panel.VisualChildren.Add(button);
    }

    private sealed class FixtureSurface : RenderSurface3D, IPointerDragSource
    {
        private readonly RenderSurface3DConformanceFixture owner;
        private PointerCaptureManager? capture;
        private ElementInputRouteMap? routeMap;
        private Vector2 last;
        private bool moved;
        internal bool PanMode { get; set; }

        internal FixtureSurface(RenderSurface3DConformanceFixture owner)
        {
            this.owner = owner;
            LostMouseCapture += (_, _) => { capture = null; routeMap = null; moved = false; };
        }

        bool IPointerDragSource.BeginPointerDrag(PointerCaptureManager manager, ElementInputRouteMap map, MouseButtonEventArgs args)
        {
            if (args.ChangedButton != InputMouseButton.Left || args.OriginalSource is not UiElementId id ||
                !map.TryGetElement(id, out UIElement? hit) || !ReferenceEquals(hit, this)) return false;
            capture = manager;
            routeMap = map;
            last = new Vector2(args.X, args.Y);
            moved = false;
            manager.Capture(this, map);
            return true;
        }

        bool IPointerDragSource.UpdatePointerDrag(MouseEventArgs args)
        {
            if (capture is null) return false;
            Vector2 current = new(args.X, args.Y);
            Vector2 delta = current - last;
            if (delta.LengthSquared() > 0) { moved = true; owner.Drag(delta, PanMode); }
            last = current;
            return true;
        }

        bool IPointerDragSource.CompletePointerDrag(PointerCaptureManager _, ElementInputRouteMap __, MouseButtonEventArgs args)
        {
            if (capture is null) return false;
            if (!moved) owner.TrySelectAt(new Vector2(args.X, args.Y));
            ReleaseCapture();
            return true;
        }

        protected override void OnDetached() { ReleaseCapture(); base.OnDetached(); }

        private void ReleaseCapture()
        {
            PointerCaptureManager? current = capture;
            ElementInputRouteMap? map = routeMap;
            capture = null;
            routeMap = null;
            if (current is not null && map is not null) current.Release(map);
        }
    }
}
