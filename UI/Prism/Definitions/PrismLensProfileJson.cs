using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cerneala.UI.Prism.Definitions;


public static class PrismLensProfileJson
{
    private static readonly JsonSerializerOptions CompactOptions =
        CreateOptions(indented: false);
    private static readonly JsonSerializerOptions IndentedOptions =
        CreateOptions(indented: true);


    public static PrismLensProfileResource Parse(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ProfileDocument document =
            JsonSerializer.Deserialize<ProfileDocument>(
                json,
                CompactOptions) ??
            throw new JsonException(
                "The JSON document did not contain a lens profile.");
        return ToResource(document);
    }


    public static PrismLensProfileResource Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ProfileDocument document =
            JsonSerializer.Deserialize<ProfileDocument>(
                stream,
                CompactOptions) ??
            throw new JsonException(
                "The JSON stream did not contain a lens profile.");
        return ToResource(document);
    }


    public static string Serialize(
        PrismLensProfileResource profile,
        bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return JsonSerializer.Serialize(
            FromResource(profile),
            indented ? IndentedOptions : CompactOptions);
    }


    public static void Save(
        Stream stream,
        PrismLensProfileResource profile,
        bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(profile);
        JsonSerializer.Serialize(
            stream,
            FromResource(profile),
            indented ? IndentedOptions : CompactOptions);
    }

    private static JsonSerializerOptions CreateOptions(bool indented) =>
        new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow,
            WriteIndented = indented
        };

    private static ProfileDocument FromResource(
        PrismLensProfileResource resource) =>
        new(
            resource.Ghosts.Select(ghost =>
                new GhostDocument(
                    ghost.Regions.Select(region =>
                        new RegionDocument(
                            region.MinimumIncidenceAngleDegrees,
                            region.MaximumIncidenceAngleDegrees,
                            FromPolynomial(region.ApertureX),
                            FromPolynomial(region.ApertureY),
                            FromPolynomial(region.SensorX),
                            FromPolynomial(region.SensorY),
                            FromPolynomial(region.Transmission),
                            FromPolynomial(region.RelativeRadius)))
                        .ToArray()))
                .ToArray(),
            resource.PupilGridSize);

    private static PolynomialDocument FromPolynomial(
        PrismSparsePolynomial polynomial) =>
        new(
            polynomial.Terms.Select(term =>
                new TermDocument(
                    term.Coefficient,
                    term.PupilXExponent,
                    term.PupilYExponent,
                    term.RadiusExponent,
                    term.InverseRadiusExponent,
                    term.IncidenceAngleExponent,
                    term.WavelengthExponent))
                .ToArray());

    private static PrismLensProfileResource ToResource(
        ProfileDocument document)
    {
        try
        {
            return new PrismLensProfileResource(
                document.Ghosts.Select(ghost =>
                    new PrismLensFlareGhost(
                        ghost.Regions.Select(region =>
                            new PrismLensFlarePolynomialRegion(
                                region.MinimumIncidenceAngleDegrees,
                                region.MaximumIncidenceAngleDegrees,
                                ToPolynomial(region.ApertureX),
                                ToPolynomial(region.ApertureY),
                                ToPolynomial(region.SensorX),
                                ToPolynomial(region.SensorY),
                                ToPolynomial(region.Transmission),
                                ToPolynomial(region.RelativeRadius))))),
                document.PupilGridSize);
        }
        catch (Exception exception)
            when (exception is ArgumentException or
                NullReferenceException)
        {
            throw new JsonException(
                "The JSON document contains an invalid lens profile.",
                exception);
        }
    }

    private static PrismSparsePolynomial ToPolynomial(
        PolynomialDocument polynomial) =>
        new(
            polynomial.Terms.Select(term =>
                new PrismSparsePolynomialTerm(
                    term.Coefficient,
                    term.PupilXExponent,
                    term.PupilYExponent,
                    term.RadiusExponent,
                    term.InverseRadiusExponent,
                    term.IncidenceAngleExponent,
                    term.WavelengthExponent)));

    private sealed record ProfileDocument(
        GhostDocument[] Ghosts,
        int PupilGridSize);

    private sealed record GhostDocument(
        RegionDocument[] Regions);

    private sealed record RegionDocument(
        float MinimumIncidenceAngleDegrees,
        float MaximumIncidenceAngleDegrees,
        PolynomialDocument ApertureX,
        PolynomialDocument ApertureY,
        PolynomialDocument SensorX,
        PolynomialDocument SensorY,
        PolynomialDocument Transmission,
        PolynomialDocument RelativeRadius);

    private sealed record PolynomialDocument(
        TermDocument[] Terms);

    private sealed record TermDocument(
        float Coefficient,
        byte PupilXExponent,
        byte PupilYExponent,
        byte RadiusExponent,
        byte InverseRadiusExponent,
        byte IncidenceAngleExponent,
        byte WavelengthExponent);
}
