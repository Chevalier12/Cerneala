using System.Reflection;
using System.Text.Json;

namespace Cerneala.Language.Prism.Catalog;

internal sealed class PrismLanguageCatalog
{
    private const string ResourceName = "Cerneala.Language.Prism.prism-catalog.json";

    private PrismLanguageCatalog(
        int schemaVersion,
        string catalogVersion,
        IReadOnlyDictionary<string, IReadOnlyList<PrismCatalogProperty>> commonProperties,
        IReadOnlyList<PrismCatalogSymbol> symbols)
    {
        SchemaVersion = schemaVersion;
        CatalogVersion = catalogVersion;
        CommonProperties = commonProperties;
        Symbols = symbols;
    }

    public int SchemaVersion { get; }

    public string CatalogVersion { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<PrismCatalogProperty>> CommonProperties { get; }

    public IReadOnlyList<PrismCatalogSymbol> Symbols { get; }

    public static PrismLanguageCatalog LoadDefault()
    {
        Assembly assembly = typeof(PrismLanguageCatalog).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded Prism catalog was not found.");
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;
        Dictionary<string, IReadOnlyList<PrismCatalogProperty>> commonProperties =
            new(StringComparer.Ordinal);
        foreach (JsonProperty family in root.GetProperty("commonProperties").EnumerateObject())
        {
            commonProperties.Add(
                family.Name,
                family.Value.EnumerateArray().Select(ParseProperty).ToArray());
        }

        List<PrismCatalogSymbol> symbols = new();
        foreach (JsonElement entry in root.GetProperty("entries").EnumerateArray())
        {
            PrismCatalogProperty[] properties = entry.GetProperty("properties")
                .EnumerateArray()
                .Select(ParseProperty)
                .ToArray();

            symbols.Add(new PrismCatalogSymbol(
                entry.GetProperty("stableId").GetInt32(),
                entry.GetProperty("id").GetString()!,
                entry.GetProperty("symbol").GetString()!,
                entry.GetProperty("kind").GetString()!,
                properties));
        }

        return new PrismLanguageCatalog(
            root.GetProperty("schemaVersion").GetInt32(),
            root.GetProperty("catalogVersion").GetString()!,
            commonProperties,
            symbols);
    }

    public PrismCatalogSymbol? FindSymbol(string kind, string symbol) => Symbols.FirstOrDefault(candidate =>
        string.Equals(candidate.Kind, kind, StringComparison.Ordinal) &&
        string.Equals(candidate.Symbol, symbol, StringComparison.Ordinal));

    public IReadOnlyList<PrismCatalogProperty> GetCommonProperties(string family) =>
        CommonProperties.TryGetValue(family, out IReadOnlyList<PrismCatalogProperty>? properties)
            ? properties
            : Array.Empty<PrismCatalogProperty>();

    private static PrismCatalogProperty ParseProperty(JsonElement property)
    {
        JsonElement domain = property.GetProperty("domain");
        double? minimum = domain.TryGetProperty("minimum", out JsonElement minimumElement) &&
            minimumElement.ValueKind == JsonValueKind.Number
            ? minimumElement.GetDouble()
            : null;
        double? maximum = domain.TryGetProperty("maximum", out JsonElement maximumElement) &&
            maximumElement.ValueKind == JsonValueKind.Number
            ? maximumElement.GetDouble()
            : null;
        string[] symbols = property.TryGetProperty("symbols", out JsonElement symbolsElement)
            ? symbolsElement.EnumerateArray().Select(value => value.GetString()!).ToArray()
            : Array.Empty<string>();
        string? defaultValue = property.TryGetProperty("default", out JsonElement defaultElement)
            ? defaultElement.ToString()
            : null;
        return new PrismCatalogProperty(
            property.GetProperty("name").GetString()!,
            property.GetProperty("valueType").GetString()!,
            property.GetProperty("required").GetBoolean(),
            domain.GetProperty("kind").GetString()!,
            minimum,
            maximum,
            symbols,
            defaultValue);
    }
}

internal sealed class PrismCatalogSymbol
{
    public PrismCatalogSymbol(
        int stableId,
        string id,
        string symbol,
        string kind,
        IReadOnlyList<PrismCatalogProperty> properties)
    {
        StableId = stableId;
        Id = id;
        Symbol = symbol;
        Kind = kind;
        Properties = properties;
    }

    public int StableId { get; }

    public string Id { get; }

    public string Symbol { get; }

    public string Kind { get; }

    public IReadOnlyList<PrismCatalogProperty> Properties { get; }
}

internal sealed class PrismCatalogProperty
{
    public PrismCatalogProperty(
        string name,
        string valueType,
        bool required,
        string domainKind,
        double? minimum,
        double? maximum,
        IReadOnlyList<string> symbols,
        string? defaultValue)
    {
        Name = name;
        ValueType = valueType;
        Required = required;
        DomainKind = domainKind;
        Minimum = minimum;
        Maximum = maximum;
        Symbols = symbols;
        DefaultValue = defaultValue;
    }

    public string Name { get; }

    public string ValueType { get; }

    public bool Required { get; }

    public string DomainKind { get; }

    public double? Minimum { get; }

    public double? Maximum { get; }

    public IReadOnlyList<string> Symbols { get; }

    public string? DefaultValue { get; }
}
