using System.Collections.Immutable;
using System.Numerics;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.Drawing.Prism.Filters;


public static class PrismLensProfileFitter
{

    public static PrismLensProfileResource Fit(
        IEnumerable<PrismLensFlareRaySample> samples,
        PrismLensProfileFitOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(samples);
        PrismLensProfileFitOptions settings =
            options ?? new PrismLensProfileFitOptions();
        settings.Validate();
        ImmutableArray<PrismLensFlareRaySample> source =
            samples.ToImmutableArray();
        if (source.IsEmpty)
        {
            throw new ArgumentException(
                "Lens profile fitting requires ray samples.",
                nameof(samples));
        }

        PrismLensFlareGhost[] ghosts = source
            .GroupBy(sample => sample.GhostIndex)
            .OrderBy(group => group.Key)
            .Select(group => FitGhost(group.ToArray(), settings))
            .ToArray();
        return new PrismLensProfileResource(
            ghosts,
            settings.PupilGridSize);
    }

    internal static PrismLensFlarePolynomialInput Normalize(
        Vector2 pupilPosition,
        float incidenceAngleDegrees,
        float wavelengthNanometers)
    {
        float radius = Math.Clamp(pupilPosition.Length(), 0, 1);
        return new PrismLensFlarePolynomialInput(
            pupilPosition,
            radius,
            1 - radius,
            Math.Clamp(incidenceAngleDegrees / 60, 0, 1),
            Math.Clamp((wavelengthNanometers - 550) / 200, -1, 1));
    }

    private static PrismLensFlareGhost FitGhost(
        PrismLensFlareRaySample[] samples,
        PrismLensProfileFitOptions options)
    {
        float minimum = samples.Min(sample => sample.IncidenceAngleDegrees);
        float maximum = samples.Max(sample => sample.IncidenceAngleDegrees);
        float span = Math.Max(maximum - minimum, 0.001f);
        List<PrismLensFlarePolynomialRegion> regions = [];
        for (int regionIndex = 0;
            regionIndex < options.RegionCount;
            regionIndex++)
        {
            float lower = minimum +
                (span * regionIndex / options.RegionCount);
            float upper = regionIndex == options.RegionCount - 1
                ? maximum + 0.001f
                : minimum +
                    (span * (regionIndex + 1) / options.RegionCount);
            PrismLensFlareRaySample[] regionSamples = samples
                .Where(sample =>
                    sample.IncidenceAngleDegrees >= lower &&
                    sample.IncidenceAngleDegrees <= upper)
                .ToArray();
            if (regionSamples.Length < options.MinimumSamplesPerRegion)
            {
                continue;
            }
            regions.Add(FitRegion(regionSamples, lower, upper, options));
        }

        if (regions.Count == 0)
        {
            throw new ArgumentException(
                "Each ghost needs enough ray samples to fit at least one angular region.",
                nameof(samples));
        }
        return new PrismLensFlareGhost(regions);
    }

    private static PrismLensFlarePolynomialRegion FitRegion(
        PrismLensFlareRaySample[] samples,
        float lower,
        float upper,
        PrismLensProfileFitOptions options)
    {
        Exponents[] candidates = CandidateTerms();
        double[][] inputs = samples
            .Select(sample => Features(sample, candidates))
            .ToArray();
        double[][] outputs = samples
            .Select(sample => new[]
            {
                (double)sample.AperturePosition.X,
                sample.AperturePosition.Y,
                sample.SensorPosition.X,
                sample.SensorPosition.Y,
                sample.IsValid ? sample.Transmission : 0,
                sample.IsValid
                    ? sample.RelativeRadius
                    : Math.Max(1.01f, sample.RelativeRadius)
            })
            .ToArray();

        List<int> selected = [0];
        while (selected.Count < Math.Min(
            options.MaximumTermCount,
            candidates.Length))
        {
            double[][] coefficients = SolveOutputs(
                inputs,
                outputs,
                selected,
                options.Ridge);
            int best = -1;
            double bestScore = options.MinimumCorrelation;
            for (int candidate = 1;
                candidate < candidates.Length;
                candidate++)
            {
                if (selected.Contains(candidate))
                {
                    continue;
                }
                double score = ResidualCorrelation(
                    inputs,
                    outputs,
                    selected,
                    coefficients,
                    candidate);
                if (score > bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
            if (best < 0)
            {
                break;
            }
            selected.Add(best);
        }

        double[][] fitted = SolveOutputs(
            inputs,
            outputs,
            selected,
            options.Ridge);
        PrismSparsePolynomial Polynomial(int output) =>
            new(selected.Select((candidate, index) =>
            {
                Exponents term = candidates[candidate];
                return new PrismSparsePolynomialTerm(
                    (float)fitted[output][index],
                    term.X,
                    term.Y,
                    term.Radius,
                    term.InverseRadius,
                    term.Angle,
                    term.Wavelength);
            }));

        return new PrismLensFlarePolynomialRegion(
            lower,
            upper,
            Polynomial(0),
            Polynomial(1),
            Polynomial(2),
            Polynomial(3),
            Polynomial(4),
            Polynomial(5));
    }

    private static Exponents[] CandidateTerms()
    {
        List<Exponents> result = [];
        for (byte x = 0; x <= 2; x++)
        for (byte y = 0; y <= 2; y++)
        for (byte radius = 0; radius <= 2; radius++)
        for (byte inverse = 0; inverse <= 2; inverse++)
        for (byte angle = 0; angle <= 2; angle++)
        for (byte wavelength = 0; wavelength <= 2; wavelength++)
        {
            int degree = x + y + radius + inverse + angle + wavelength;
            if (degree <= 2)
            {
                result.Add(new(
                    x,
                    y,
                    radius,
                    inverse,
                    angle,
                    wavelength));
            }
        }
        return result
            .OrderBy(term => term.Degree)
            .ThenBy(term => term.X)
            .ThenBy(term => term.Y)
            .ThenBy(term => term.Radius)
            .ThenBy(term => term.InverseRadius)
            .ThenBy(term => term.Angle)
            .ThenBy(term => term.Wavelength)
            .ToArray();
    }

    private static double[] Features(
        PrismLensFlareRaySample sample,
        Exponents[] terms)
    {
        PrismLensFlarePolynomialInput input = Normalize(
            sample.PupilPosition,
            sample.IncidenceAngleDegrees,
            sample.WavelengthNanometers);
        return terms.Select(term =>
            Power(input.PupilPosition.X, term.X) *
            Power(input.PupilPosition.Y, term.Y) *
            Power(input.Radius, term.Radius) *
            Power(input.InverseRadius, term.InverseRadius) *
            Power(input.NormalizedIncidenceAngle, term.Angle) *
            Power(input.NormalizedWavelength, term.Wavelength))
            .ToArray();
    }

    private static double[][] SolveOutputs(
        double[][] inputs,
        double[][] outputs,
        IReadOnlyList<int> selected,
        double ridge)
    {
        double[,] normal = new double[selected.Count, selected.Count];
        for (int row = 0; row < selected.Count; row++)
        {
            for (int column = 0; column < selected.Count; column++)
            {
                normal[row, column] = inputs.Sum(input =>
                    input[selected[row]] * input[selected[column]]);
            }
            normal[row, row] += ridge;
        }

        double[][] result = new double[outputs[0].Length][];
        for (int output = 0; output < result.Length; output++)
        {
            double[] right = selected.Select(candidate =>
                inputs.Select((input, sampleIndex) =>
                        input[candidate] * outputs[sampleIndex][output])
                    .Sum())
                .ToArray();
            result[output] = Solve(normal, right);
        }
        return result;
    }

    private static double ResidualCorrelation(
        double[][] inputs,
        double[][] outputs,
        IReadOnlyList<int> selected,
        double[][] coefficients,
        int candidate)
    {
        double[,] normal = new double[selected.Count, selected.Count];
        double[] right = new double[selected.Count];
        for (int row = 0; row < selected.Count; row++)
        {
            for (int column = 0;
                column < selected.Count;
                column++)
            {
                normal[row, column] = inputs.Sum(input =>
                    input[selected[row]] *
                    input[selected[column]]);
            }
            normal[row, row] += 1e-10;
            right[row] = inputs.Sum(input =>
                input[selected[row]] * input[candidate]);
        }
        double[] projection = Solve(normal, right);
        double[] candidateResidual = inputs
            .Select(input =>
            {
                double predicted = 0;
                for (int term = 0;
                    term < selected.Count;
                    term++)
                {
                    predicted += projection[term] *
                        input[selected[term]];
                }
                return input[candidate] - predicted;
            })
            .ToArray();
        double candidateEnergy =
            candidateResidual.Sum(value => value * value);
        if (candidateEnergy < 1e-16)
        {
            return 0;
        }

        double score = 0;
        for (int output = 0;
            output < outputs[0].Length;
            output++)
        {
            double correlation = 0;
            double residualEnergy = 0;
            for (int sample = 0;
                sample < inputs.Length;
                sample++)
            {
                double predicted = 0;
                for (int term = 0; term < selected.Count; term++)
                {
                    predicted += coefficients[output][term] *
                        inputs[sample][selected[term]];
                }
                double residual =
                    outputs[sample][output] - predicted;
                correlation +=
                    residual * candidateResidual[sample];
                residualEnergy += residual * residual;
            }
            if (residualEnergy > 1e-16)
            {
                score += Math.Abs(correlation) /
                    Math.Sqrt(candidateEnergy * residualEnergy);
            }
        }
        return score;
    }

    private static double[] Solve(double[,] matrix, double[] right)
    {
        int count = right.Length;
        double[,] augmented = new double[count, count + 1];
        for (int row = 0; row < count; row++)
        {
            for (int column = 0; column < count; column++)
            {
                augmented[row, column] = matrix[row, column];
            }
            augmented[row, count] = right[row];
        }

        for (int pivot = 0; pivot < count; pivot++)
        {
            int best = pivot;
            for (int row = pivot + 1; row < count; row++)
            {
                if (Math.Abs(augmented[row, pivot]) >
                    Math.Abs(augmented[best, pivot]))
                {
                    best = row;
                }
            }
            if (Math.Abs(augmented[best, pivot]) < 1e-12)
            {
                continue;
            }
            if (best != pivot)
            {
                for (int column = pivot; column <= count; column++)
                {
                    (augmented[pivot, column], augmented[best, column]) =
                        (augmented[best, column], augmented[pivot, column]);
                }
            }
            double scale = augmented[pivot, pivot];
            for (int column = pivot; column <= count; column++)
            {
                augmented[pivot, column] /= scale;
            }
            for (int row = 0; row < count; row++)
            {
                if (row == pivot)
                {
                    continue;
                }
                double factor = augmented[row, pivot];
                for (int column = pivot; column <= count; column++)
                {
                    augmented[row, column] -=
                        factor * augmented[pivot, column];
                }
            }
        }
        return Enumerable.Range(0, count)
            .Select(row => augmented[row, count])
            .ToArray();
    }

    private static double Power(float value, byte exponent) =>
        exponent switch
        {
            0 => 1,
            1 => value,
            _ => value * value
        };

    private readonly record struct Exponents(
        byte X,
        byte Y,
        byte Radius,
        byte InverseRadius,
        byte Angle,
        byte Wavelength)
    {
        public int Degree =>
            X + Y + Radius + InverseRadius + Angle + Wavelength;
    }
}


public readonly record struct PrismLensFlareRaySample(
    int GhostIndex,
    Vector2 PupilPosition,
    float IncidenceAngleDegrees,
    float WavelengthNanometers,
    Vector2 AperturePosition,
    Vector2 SensorPosition,
    float Transmission,
    float RelativeRadius,
    bool IsValid = true);


public sealed class PrismLensProfileFitOptions
{

    public int RegionCount { get; init; } = 4;


    public int MaximumTermCount { get; init; } = 12;


    public int MinimumSamplesPerRegion { get; init; } = 12;


    public int PupilGridSize { get; init; } = 8;


    public double Ridge { get; init; } = 1e-7;


    public double MinimumCorrelation { get; init; } = 1e-7;

    internal void Validate()
    {
        if (RegionCount is < 1 or > 32 ||
            MaximumTermCount is < 1 or > 28 ||
            MinimumSamplesPerRegion < 6 ||
            PupilGridSize is < 2 or > 32 ||
            !double.IsFinite(Ridge) ||
            Ridge <= 0 ||
            !double.IsFinite(MinimumCorrelation) ||
            MinimumCorrelation < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RegionCount),
                "Lens profile fitting options are outside their supported ranges.");
        }
    }
}
