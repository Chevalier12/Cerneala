using System.Collections.Immutable;

namespace Cerneala.UI.Prism.Definitions;

public readonly record struct PrismCurvePoint
{
    public PrismCurvePoint(float input, float output)
    {
        Input = PrismDefinitionValidation.UnitInterval(
            input,
            nameof(input));
        Output = PrismDefinitionValidation.UnitInterval(
            output,
            nameof(output));
    }

    public float Input { get; }

    public float Output { get; }
}

public sealed class PrismCurvesResource
{
    private static readonly ImmutableArray<PrismCurvePoint>
        IdentityCurve =
        [
            new(0, 0),
            new(1, 1)
        ];

    public PrismCurvesResource(
        IEnumerable<PrismCurvePoint>? composite = null,
        IEnumerable<PrismCurvePoint>? red = null,
        IEnumerable<PrismCurvePoint>? green = null,
        IEnumerable<PrismCurvePoint>? blue = null)
    {
        Composite = ValidateCurve(
            composite,
            nameof(composite));
        Red = ValidateCurve(red, nameof(red));
        Green = ValidateCurve(green, nameof(green));
        Blue = ValidateCurve(blue, nameof(blue));
    }

    public ImmutableArray<PrismCurvePoint> Composite { get; }

    public ImmutableArray<PrismCurvePoint> Red { get; }

    public ImmutableArray<PrismCurvePoint> Green { get; }

    public ImmutableArray<PrismCurvePoint> Blue { get; }

    private static ImmutableArray<PrismCurvePoint> ValidateCurve(
        IEnumerable<PrismCurvePoint>? points,
        string parameterName)
    {
        ImmutableArray<PrismCurvePoint> curve =
            points?.ToImmutableArray() ?? IdentityCurve;
        if (curve.Length < 2)
        {
            throw new ArgumentException(
                "A Prism curve requires at least two points.",
                parameterName);
        }
        if (curve[0].Input != 0 ||
            curve[^1].Input != 1)
        {
            throw new ArgumentException(
                "A Prism curve must start at input 0 and end at input 1.",
                parameterName);
        }

        for (int index = 1; index < curve.Length; index++)
        {
            if (curve[index].Input <= curve[index - 1].Input)
            {
                throw new ArgumentException(
                    "Prism curve inputs must be strictly increasing.",
                    parameterName);
            }
        }

        return curve;
    }
}
