using System.Collections.Immutable;
using System.Numerics;

namespace Cerneala.UI.Prism.Definitions;




public sealed class PrismLensProfileResource
{



    public PrismLensProfileResource(
        IEnumerable<PrismLensFlareGhost> ghosts,
        int pupilGridSize = 8)
    {
        ArgumentNullException.ThrowIfNull(ghosts);
        Ghosts = ghosts.ToImmutableArray();
        if (Ghosts.IsEmpty)
        {
            throw new ArgumentException(
                "A lens profile must contain at least one flare ghost.",
                nameof(ghosts));
        }
        if (pupilGridSize is < 2 or > 32)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pupilGridSize),
                "The pupil grid size must be in [2, 32].");
        }

        PupilGridSize = pupilGridSize;
    }


    public ImmutableArray<PrismLensFlareGhost> Ghosts { get; }


    public int PupilGridSize { get; }
}


public sealed class PrismLensFlareGhost
{

    public PrismLensFlareGhost(
        IEnumerable<PrismLensFlarePolynomialRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        Regions = regions
            .OrderBy(region => region.MinimumIncidenceAngleDegrees)
            .ToImmutableArray();
        if (Regions.IsEmpty)
        {
            throw new ArgumentException(
                "A lens flare ghost must contain at least one fitted region.",
                nameof(regions));
        }
        for (int index = 1; index < Regions.Length; index++)
        {
            if (Regions[index].MinimumIncidenceAngleDegrees <
                Regions[index - 1].MaximumIncidenceAngleDegrees)
            {
                throw new ArgumentException(
                    "Lens flare fitting regions cannot overlap.",
                    nameof(regions));
            }
        }
    }


    public ImmutableArray<PrismLensFlarePolynomialRegion> Regions { get; }
}




public sealed class PrismLensFlarePolynomialRegion
{

    public PrismLensFlarePolynomialRegion(
        float minimumIncidenceAngleDegrees,
        float maximumIncidenceAngleDegrees,
        PrismSparsePolynomial apertureX,
        PrismSparsePolynomial apertureY,
        PrismSparsePolynomial sensorX,
        PrismSparsePolynomial sensorY,
        PrismSparsePolynomial transmission,
        PrismSparsePolynomial relativeRadius)
    {
        if (!float.IsFinite(minimumIncidenceAngleDegrees) ||
            !float.IsFinite(maximumIncidenceAngleDegrees) ||
            minimumIncidenceAngleDegrees < 0 ||
            maximumIncidenceAngleDegrees <= minimumIncidenceAngleDegrees)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumIncidenceAngleDegrees),
                "The incidence-angle interval must be finite, non-negative, and increasing.");
        }

        MinimumIncidenceAngleDegrees = minimumIncidenceAngleDegrees;
        MaximumIncidenceAngleDegrees = maximumIncidenceAngleDegrees;
        ApertureX = apertureX ??
            throw new ArgumentNullException(nameof(apertureX));
        ApertureY = apertureY ??
            throw new ArgumentNullException(nameof(apertureY));
        SensorX = sensorX ??
            throw new ArgumentNullException(nameof(sensorX));
        SensorY = sensorY ??
            throw new ArgumentNullException(nameof(sensorY));
        Transmission = transmission ??
            throw new ArgumentNullException(nameof(transmission));
        RelativeRadius = relativeRadius ??
            throw new ArgumentNullException(nameof(relativeRadius));
    }


    public float MinimumIncidenceAngleDegrees { get; }


    public float MaximumIncidenceAngleDegrees { get; }


    public PrismSparsePolynomial ApertureX { get; }


    public PrismSparsePolynomial ApertureY { get; }


    public PrismSparsePolynomial SensorX { get; }


    public PrismSparsePolynomial SensorY { get; }


    public PrismSparsePolynomial Transmission { get; }


    public PrismSparsePolynomial RelativeRadius { get; }
}


public sealed class PrismSparsePolynomial
{

    public PrismSparsePolynomial(
        IEnumerable<PrismSparsePolynomialTerm> terms)
    {
        ArgumentNullException.ThrowIfNull(terms);
        Terms = terms.ToImmutableArray();
        if (Terms.IsEmpty)
        {
            throw new ArgumentException(
                "A sparse polynomial must contain at least one term.",
                nameof(terms));
        }
    }


    public ImmutableArray<PrismSparsePolynomialTerm> Terms { get; }


    public float Evaluate(PrismLensFlarePolynomialInput input)
    {
        Span<float> powers = stackalloc float[18];
        FillPowers(input, powers);
        double result = 0;
        foreach (PrismSparsePolynomialTerm term in Terms)
        {
            double value = term.Coefficient;
            value *= powers[term.PupilXExponent];
            value *= powers[3 + term.PupilYExponent];
            value *= powers[6 + term.RadiusExponent];
            value *= powers[9 + term.InverseRadiusExponent];
            value *= powers[12 + term.IncidenceAngleExponent];
            value *= powers[15 + term.WavelengthExponent];
            result += value;
        }
        return (float)result;
    }

    private static void FillPowers(
        PrismLensFlarePolynomialInput input,
        Span<float> powers)
    {
        Fill(input.PupilPosition.X, powers[0..3]);
        Fill(input.PupilPosition.Y, powers[3..6]);
        Fill(input.Radius, powers[6..9]);
        Fill(input.InverseRadius, powers[9..12]);
        Fill(input.NormalizedIncidenceAngle, powers[12..15]);
        Fill(input.NormalizedWavelength, powers[15..18]);

        static void Fill(float value, Span<float> target)
        {
            target[0] = 1;
            target[1] = value;
            target[2] = value * value;
        }
    }
}


public readonly record struct PrismSparsePolynomialTerm
{

    public PrismSparsePolynomialTerm(
        float coefficient,
        byte pupilXExponent,
        byte pupilYExponent,
        byte radiusExponent,
        byte inverseRadiusExponent,
        byte incidenceAngleExponent,
        byte wavelengthExponent)
    {
        if (!float.IsFinite(coefficient))
        {
            throw new ArgumentOutOfRangeException(nameof(coefficient));
        }
        if (pupilXExponent > 2 ||
            pupilYExponent > 2 ||
            radiusExponent > 2 ||
            inverseRadiusExponent > 2 ||
            incidenceAngleExponent > 2 ||
            wavelengthExponent > 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pupilXExponent),
                "Sparse polynomial exponents must be in [0, 2].");
        }

        Coefficient = coefficient;
        PupilXExponent = pupilXExponent;
        PupilYExponent = pupilYExponent;
        RadiusExponent = radiusExponent;
        InverseRadiusExponent = inverseRadiusExponent;
        IncidenceAngleExponent = incidenceAngleExponent;
        WavelengthExponent = wavelengthExponent;
    }


    public float Coefficient { get; }


    public byte PupilXExponent { get; }


    public byte PupilYExponent { get; }


    public byte RadiusExponent { get; }


    public byte InverseRadiusExponent { get; }


    public byte IncidenceAngleExponent { get; }


    public byte WavelengthExponent { get; }
}


public readonly record struct PrismLensFlarePolynomialInput(
    Vector2 PupilPosition,
    float Radius,
    float InverseRadius,
    float NormalizedIncidenceAngle,
    float NormalizedWavelength);
