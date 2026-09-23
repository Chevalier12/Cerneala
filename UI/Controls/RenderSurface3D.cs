using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public delegate void RenderSurface3DDrawEventHandler(RenderSurface3D sender, RenderSurface3DFrame frame);

public class RenderSurface3D : ContentControl, ITimeSensitiveRenderElement, IRenderSurface3DSource, IRenderSurfaceResourceOwner
{
    private static readonly Matrix4x4 DefaultView = Matrix4x4.CreateLookAt(new Vector3(0, 0, 5), Vector3.Zero, Vector3.UnitY);
    private static readonly RenderProjection3D DefaultProjection = RenderProjection3D.Perspective(MathF.PI / 3, 0.01f, 1000);

    public static readonly UiProperty<Color> ClearColorProperty = UiProperty<Color>.Register(
        nameof(ClearColor), typeof(RenderSurface3D), new UiPropertyMetadata<Color>(Color.Transparent, UiPropertyOptions.AffectsRender));
    public static readonly UiProperty<Matrix4x4> ViewMatrixProperty = UiProperty<Matrix4x4>.Register(
        nameof(ViewMatrix), typeof(RenderSurface3D), new UiPropertyMetadata<Matrix4x4>(DefaultView, UiPropertyOptions.AffectsRender,
            validateValue: static value => IsValidView(value)));
    public static readonly UiProperty<RenderProjection3D> ProjectionProperty = UiProperty<RenderProjection3D>.Register(
        nameof(Projection), typeof(RenderSurface3D), new UiPropertyMetadata<RenderProjection3D>(DefaultProjection, UiPropertyOptions.AffectsRender,
            validateValue: static value => RenderProjection3D.IsValid(value)));
    public static readonly UiProperty<RenderSurface3DRedrawMode> RedrawModeProperty = UiProperty<RenderSurface3DRedrawMode>.Register(
        nameof(RedrawMode), typeof(RenderSurface3D), new UiPropertyMetadata<RenderSurface3DRedrawMode>(RenderSurface3DRedrawMode.OnDemand, UiPropertyOptions.AffectsRender,
            validateValue: static value => Enum.IsDefined(value)));

    private RenderSurface3DDrawEventHandler? draw;
    private readonly Dictionary<object, IRenderSurface3DBackendState> backendStates =
        new(ReferenceEqualityComparer.Instance);
    private long frameVersion = 1;
    private long resourceEpoch;
    private TimeSpan currentFrameTime;

    public Color ClearColor { get => GetValue(ClearColorProperty); set => SetValue(ClearColorProperty, value); }
    public Matrix4x4 ViewMatrix { get => GetValue(ViewMatrixProperty); set => SetValue(ViewMatrixProperty, value); }
    public RenderProjection3D Projection { get => GetValue(ProjectionProperty); set => SetValue(ProjectionProperty, value); }
    public RenderSurface3DRedrawMode RedrawMode { get => GetValue(RedrawModeProperty); set => SetValue(RedrawModeProperty, value); }

    public event RenderSurface3DDrawEventHandler? Draw
    {
        add { draw += value; InvalidateFrame(); }
        remove
        {
            draw -= value;
            if (draw is null) ReleaseDrawingResources();
            InvalidateFrame();
        }
    }

    public void InvalidateFrame()
    {
        AdvanceFrameVersion();
        IncrementRenderVersion();
        Invalidate(InvalidationFlags.Render, "RenderSurface3D frame changed");
    }

    public bool TryWorldToRoot(Vector3 worldPosition, out Vector2 rootPosition)
    {
        rootPosition = default;
        if (!RenderSurface3DValidationVector(worldPosition) || !TryGetGeometry(out ViewportGeometry geometry)) return false;
        Vector4 clip = Vector4.Transform(new Vector4(worldPosition, 1), geometry.WorldToClip);
        if (!float.IsFinite(clip.X) || !float.IsFinite(clip.Y) || !float.IsFinite(clip.Z) || !float.IsFinite(clip.W) || clip.W <= 0 ||
            clip.X < -clip.W || clip.X > clip.W || clip.Y < -clip.W || clip.Y > clip.W || clip.Z < 0 || clip.Z > clip.W) return false;
        Vector2 local = new((clip.X / clip.W + 1) * 0.5f * geometry.LogicalWidth,
            (1 - clip.Y / clip.W) * 0.5f * geometry.LogicalHeight);
        Vector2 layout = local + new Vector2(ArrangedBounds.X, ArrangedBounds.Y);
        Vector2 transformed = Vector2.Transform(layout, geometry.ElementToRoot);
        if (!float.IsFinite(transformed.X) || !float.IsFinite(transformed.Y)) return false;
        rootPosition = transformed;
        return true;
    }

    public bool TryRootToWorldRay(Vector2 rootPosition, out DrawRay3D ray)
    {
        ray = default;
        if (!float.IsFinite(rootPosition.X) || !float.IsFinite(rootPosition.Y) || !TryGetGeometry(out ViewportGeometry geometry)) return false;
        Vector2 layout = Vector2.Transform(rootPosition, geometry.RootToElement);
        Vector2 local = layout - new Vector2(ArrangedBounds.X, ArrangedBounds.Y);
        if (local.X < 0 || local.Y < 0 || local.X > geometry.LogicalWidth || local.Y > geometry.LogicalHeight) return false;
        float x = (local.X / geometry.LogicalWidth * 2) - 1;
        float y = 1 - (local.Y / geometry.LogicalHeight * 2);
        Vector3 near = Unproject(new Vector4(x, y, 0, 1), geometry.ClipToWorld);
        Vector3 far = Unproject(new Vector4(x, y, 1, 1), geometry.ClipToWorld);
        Vector3 direction = far - near;
        if (!RenderSurface3DValidationVector(near) || !RenderSurface3DValidationVector(far) ||
            !TryNormalizeDirection(direction, out Vector3 normalizedDirection)) return false;
        ray = new DrawRay3D(near, normalizedDirection);
        return true;
    }

    bool ITimeSensitiveRenderElement.UpdateRenderTime(TimeSpan frameTime)
    {
        if (frameTime < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(frameTime));
        currentFrameTime = frameTime;
        if (draw is null || RedrawMode != RenderSurface3DRedrawMode.Continuous) return false;
        InvalidateFrame();
        return true;
    }

    protected override void OnRender(RenderContext context)
    {
        Border.RenderBackground(this, context);
        DrawRect bounds = Border.ToDrawRect(context.Bounds);
        if (draw is not null && bounds.Width > 0 && bounds.Height > 0)
        {
            context.DrawingContext.DrawRenderSurface3D(this, bounds, Color.White, frameVersion);
        }
        Border.RenderBorder(this, context);
    }

    protected override void OnDetached()
    {
        ReleaseDrawingResources();
        base.OnDetached();
    }

    void IRenderSurfaceResourceOwner.ReleaseDrawingResources() => ReleaseDrawingResources();

    private void ReleaseDrawingResources()
    {
        resourceEpoch = resourceEpoch == long.MaxValue ? 1 : resourceEpoch + 1;
        foreach (IRenderSurface3DBackendState state in backendStates.Values.ToArray()) state.Dispose();
        backendStates.Clear();
    }

    void IRenderSurface3DSource.SetBackendState(object owner, IRenderSurface3DBackendState? state)
    {
        if (backendStates.Remove(owner, out IRenderSurface3DBackendState? previous) &&
            !ReferenceEquals(previous, state)) previous.Dispose();
        if (state is not null) backendStates[owner] = state;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ClearColorProperty) || ReferenceEquals(args.Property, ViewMatrixProperty) ||
            ReferenceEquals(args.Property, ProjectionProperty) || ReferenceEquals(args.Property, RedrawModeProperty)) AdvanceFrameVersion();
    }

    long IRenderSurface3DSource.FrameVersion => frameVersion;

    bool IRenderSurface3DSource.HasDrawSubscribers => draw is not null;

    long IRenderSurface3DSource.ResourceEpoch => resourceEpoch;

    RenderSurface3DRecording IRenderSurface3DSource.RecordFrame(DrawRect bounds, float rasterScale)
    {
        if (!float.IsFinite(rasterScale) || rasterScale <= 0) throw new ArgumentOutOfRangeException(nameof(rasterScale));
        if (bounds.Width <= 0 || bounds.Height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds));
        long generation = frameVersion;
        Color clearColor = ClearColor;
        TimeSpan frameTime = currentFrameTime;
        Matrix4x4 view = ViewMatrix;
        RenderProjection3D projection = Projection;
        ValidateView(view);
        RenderProjection3D.Validate(projection);
        int pixelWidth = Math.Max(1, checked((int)MathF.Ceiling(bounds.Width * rasterScale)));
        int pixelHeight = Math.Max(1, checked((int)MathF.Ceiling(bounds.Height * rasterScale)));
        Matrix4x4 projectionMatrix = projection.CreateMatrix((float)pixelWidth / pixelHeight);
        Matrix4x4 worldToClip = view * projectionMatrix;
        if (!IsFinite(projectionMatrix) || !IsFinite(worldToClip) ||
            !Matrix4x4.Invert(worldToClip, out Matrix4x4 clipToWorld) || !IsFinite(clipToWorld))
        {
            throw new ArgumentOutOfRangeException(nameof(projection), "The projection cannot produce finite invertible camera matrices for the current raster aspect ratio.");
        }
        RenderSurface3DFrame frame = new(new DrawRect(0, 0, bounds.Width, bounds.Height), pixelWidth, pixelHeight,
            rasterScale, frameTime, view, projectionMatrix);
        try
        {
            draw?.Invoke(this, frame);
            IReadOnlyList<DrawPrimitive3D> primitives = frame.Complete();
            return new(generation, clearColor, frame.Bounds, pixelWidth, pixelHeight, rasterScale,
                frameTime, view, projectionMatrix, primitives);
        }
        catch { frame.Abort(); throw; }
    }

    private bool TryGetGeometry(out ViewportGeometry geometry)
    {
        geometry = default;
        float width = ArrangedBounds.Width;
        float height = ArrangedBounds.Height;
        float scale = Root?.Scale ?? 1;
        Matrix4x4 view = ViewMatrix;
        RenderProjection3D projectionDescriptor = Projection;
        if (!float.IsFinite(width) || !float.IsFinite(height) || width <= 0 || height <= 0 || !float.IsFinite(scale) || scale <= 0 || !IsValidView(view) || !RenderProjection3D.IsValid(projectionDescriptor)) return false;
        int pixelWidth;
        int pixelHeight;
        try { pixelWidth = Math.Max(1, checked((int)MathF.Ceiling(width * scale))); pixelHeight = Math.Max(1, checked((int)MathF.Ceiling(height * scale))); }
        catch (OverflowException) { return false; }
        Matrix3x2 transform = InputCoordinateConverter.GetElementToRootTransform(this);
        if (!IsFinite(transform) || !Matrix3x2.Invert(transform, out Matrix3x2 rootToElement) || !IsFinite(rootToElement)) return false;
        Matrix4x4 projection = projectionDescriptor.CreateMatrix((float)pixelWidth / pixelHeight);
        Matrix4x4 worldToClip = view * projection;
        if (!IsFinite(projection) || !IsFinite(worldToClip) ||
            !Matrix4x4.Invert(worldToClip, out Matrix4x4 clipToWorld) || !IsFinite(clipToWorld)) return false;
        geometry = new(width, height, transform, rootToElement, worldToClip, clipToWorld);
        return true;
    }

    private static Vector3 Unproject(Vector4 point, Matrix4x4 inverse)
    {
        Vector4 value = Vector4.Transform(point, inverse);
        return value.W == 0 ? new(float.NaN) : new(value.X / value.W, value.Y / value.W, value.Z / value.W);
    }

    private void AdvanceFrameVersion() => frameVersion = frameVersion == long.MaxValue ? 1 : frameVersion + 1;
    private static bool IsValidView(Matrix4x4 value) => RenderSurface3DValidation.IsFiniteAffine(value) &&
        Matrix4x4.Invert(value, out Matrix4x4 inverse) && IsFinite(inverse);
    private static void ValidateView(Matrix4x4 value) { if (!IsValidView(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static bool RenderSurface3DValidationVector(Vector3 value) => float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    private static bool IsFinite(Matrix3x2 value) => float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M21) && float.IsFinite(value.M22) && float.IsFinite(value.M31) && float.IsFinite(value.M32);
    private static bool IsFinite(Matrix4x4 value) =>
        float.IsFinite(value.M11) && float.IsFinite(value.M12) && float.IsFinite(value.M13) && float.IsFinite(value.M14) &&
        float.IsFinite(value.M21) && float.IsFinite(value.M22) && float.IsFinite(value.M23) && float.IsFinite(value.M24) &&
        float.IsFinite(value.M31) && float.IsFinite(value.M32) && float.IsFinite(value.M33) && float.IsFinite(value.M34) &&
        float.IsFinite(value.M41) && float.IsFinite(value.M42) && float.IsFinite(value.M43) && float.IsFinite(value.M44);

    private static bool TryNormalizeDirection(Vector3 value, out Vector3 normalized)
    {
        normalized = default;
        if (!RenderSurface3DValidationVector(value)) return false;
        float scale = MathF.Max(MathF.Abs(value.X), MathF.Max(MathF.Abs(value.Y), MathF.Abs(value.Z)));
        if (!float.IsFinite(scale) || scale <= 0) return false;
        Vector3 scaled = value / scale;
        float length = scaled.Length();
        if (!float.IsFinite(length) || length <= 0) return false;
        normalized = scaled / length;
        return RenderSurface3DValidationVector(normalized);
    }

    private readonly record struct ViewportGeometry(
        float LogicalWidth,
        float LogicalHeight,
        Matrix3x2 ElementToRoot,
        Matrix3x2 RootToElement,
        Matrix4x4 WorldToClip,
        Matrix4x4 ClipToWorld);
}
