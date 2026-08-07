using System.Collections.Immutable;
using System.Numerics;

namespace Cerneala.UI.Prism.Definitions;


public enum PrismLightKind
{

    Directional,


    Point
}


public readonly record struct PrismLight
{
    private PrismLight(
        PrismLightKind kind,
        Vector3 directionOrPosition,
        Vector3 linearSrgb,
        float intensity)
    {
        ValidateColor(linearSrgb);
        if (!float.IsFinite(intensity) || intensity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intensity),
                "Light intensity must be finite and non-negative.");
        }

        Kind = kind;
        DirectionOrPosition = directionOrPosition;
        LinearSrgb = linearSrgb;
        Intensity = intensity;
    }


    public PrismLightKind Kind { get; }


    public Vector3 LinearSrgb { get; }


    public float Intensity { get; }







    public Vector3 Direction =>
        Kind == PrismLightKind.Directional
            ? DirectionOrPosition
            : throw new InvalidOperationException(
                "A point light does not have a constant direction.");







    public Vector3 Position =>
        Kind == PrismLightKind.Point
            ? DirectionOrPosition
            : throw new InvalidOperationException(
                "A directional light does not have a position.");

    internal Vector3 PackedDirectionOrPosition =>
        DirectionOrPosition;

    internal bool IsValid =>
        Enum.IsDefined(Kind) &&
        IsFinite(DirectionOrPosition) &&
        (Kind != PrismLightKind.Directional ||
            DirectionOrPosition.LengthSquared() > 0) &&
        IsFinite(LinearSrgb) &&
        LinearSrgb.X >= 0 &&
        LinearSrgb.Y >= 0 &&
        LinearSrgb.Z >= 0 &&
        float.IsFinite(Intensity) &&
        Intensity >= 0;

    private Vector3 DirectionOrPosition { get; }







    public static PrismLight Directional(
        Vector3 surfaceToLightDirection,
        Vector3 linearSrgb,
        float intensity = 1)
    {
        if (!IsFinite(surfaceToLightDirection) ||
            surfaceToLightDirection.LengthSquared() <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(surfaceToLightDirection),
                "A directional light requires a finite non-zero direction.");
        }

        return new PrismLight(
            PrismLightKind.Directional,
            Vector3.Normalize(surfaceToLightDirection),
            linearSrgb,
            intensity);
    }











    public static PrismLight Point(
        Vector3 position,
        Vector3 linearSrgb,
        float intensity = 1)
    {
        if (!IsFinite(position))
        {
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "A point-light position must be finite.");
        }

        return new PrismLight(
            PrismLightKind.Point,
            position,
            linearSrgb,
            intensity);
    }

    private static void ValidateColor(Vector3 color)
    {
        if (!IsFinite(color) ||
            color.X < 0 ||
            color.Y < 0 ||
            color.Z < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(color),
                "Light colors must contain finite non-negative linear-sRGB values.");
        }
    }

    private static bool IsFinite(Vector3 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z);
}




public sealed class PrismLightingResource
{




    public const int MaximumLightCount = 8;


    public PrismLightingResource(IEnumerable<PrismLight> lights)
    {
        ArgumentNullException.ThrowIfNull(lights);
        Lights = lights.ToImmutableArray();
        if (Lights.IsEmpty ||
            Lights.Length > MaximumLightCount ||
            Lights.Any(light => !light.IsValid))
        {
            throw new ArgumentException(
                $"A lighting resource must contain between 1 and " +
                $"{MaximumLightCount} valid lights.",
                nameof(lights));
        }
    }


    public ImmutableArray<PrismLight> Lights { get; }
}
