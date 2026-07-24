using System.Collections.Immutable;
using System.Globalization;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Catalog;

public enum PrismCatalogOperationKind
{
    Filter,
    Style
}

public enum PrismCatalogValueKind
{
    Boolean,
    Integer,
    Number,
    Color,
    Vector,
    Symbol,
    Resource
}

public sealed class PrismCatalogParameterInfo
{
    internal PrismCatalogParameterInfo(
        PrismCatalogOperationKind operationKind,
        int operationStableId,
        int typeSlot,
        string id,
        string name,
        PrismCatalogValueKind valueKind,
        bool required,
        string? defaultValue,
        string domain,
        string unit,
        ImmutableArray<string> symbolOptions)
    {
        OperationKind = operationKind;
        OperationStableId = operationStableId;
        TypeSlot = typeSlot;
        Id = id;
        Name = name;
        ValueKind = valueKind;
        IsRequired = required;
        DefaultValue = defaultValue;
        Unit = unit;
        SymbolOptions = symbolOptions;
        (DomainKind, Minimum, Maximum) = ParseDomain(domain);
    }

    public string Id { get; }

    public string Name { get; }

    public PrismCatalogValueKind ValueKind { get; }

    public bool IsRequired { get; }

    public string? DefaultValue { get; }

    public string DomainKind { get; }

    public double? Minimum { get; }

    public double? Maximum { get; }

    public string Unit { get; }

    public ImmutableArray<string> SymbolOptions { get; }

    public bool RequiresResource => ValueKind == PrismCatalogValueKind.Resource && IsRequired;

    internal PrismCatalogOperationKind OperationKind { get; }

    internal int OperationStableId { get; }

    internal int TypeSlot { get; }

    internal int ResolveSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (!SymbolOptions.Contains(symbol, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(
                nameof(symbol),
                symbol,
                $"'{symbol}' is not a catalog option for '{Name}'.");
        }

        return PrismCatalogRuntime.ResolveSymbol(Name, symbol);
    }

    internal string ResolveSymbol(int value)
    {
        foreach (string symbol in SymbolOptions)
        {
            if (PrismCatalogRuntime.ResolveSymbol(Name, symbol) == value)
            {
                return symbol;
            }
        }

        throw new InvalidOperationException(
            $"Stored symbol value '{value}' is not represented by the catalog metadata for '{Name}'.");
    }

    private static (string Kind, double? Minimum, double? Maximum) ParseDomain(string domain)
    {
        string[] parts = domain.Split(':');
        double? minimum = parts.Length > 1 &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMinimum)
                ? parsedMinimum
                : null;
        double? maximum = parts.Length > 2 &&
            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedMaximum)
                ? parsedMaximum
                : null;
        return (parts[0], minimum, maximum);
    }
}

public sealed class PrismCatalogOperationInfo
{
    internal PrismCatalogOperationInfo(
        int stableId,
        string id,
        string symbol,
        PrismCatalogOperationKind kind,
        string category,
        ImmutableArray<PrismCatalogParameterInfo> parameters)
    {
        StableId = stableId;
        Id = id;
        Symbol = symbol;
        Kind = kind;
        Category = category;
        Parameters = parameters;
    }

    public int StableId { get; }

    public string Id { get; }

    public string Symbol { get; }

    public PrismCatalogOperationKind Kind { get; }

    public string Category { get; }

    public ImmutableArray<PrismCatalogParameterInfo> Parameters { get; }

    public bool RequiresResource => Parameters.Any(parameter => parameter.RequiresResource);
}

public static class PrismCatalog
{
    private static readonly ImmutableArray<PrismCatalogOperationInfo> operations = BuildOperations();

    public static string Version => PrismCatalogGenerated.CatalogVersion;

    public static ImmutableArray<PrismCatalogOperationInfo> Filters { get; } =
        operations.Where(operation => operation.Kind == PrismCatalogOperationKind.Filter).ToImmutableArray();

    public static ImmutableArray<PrismCatalogOperationInfo> Styles { get; } =
        operations.Where(operation => operation.Kind == PrismCatalogOperationKind.Style).ToImmutableArray();

    public static PrismCatalogOperationInfo GetFilter(PrismFilterId filter) =>
        Filters.Single(operation => operation.StableId == (int)filter);

    public static PrismCatalogOperationInfo GetStyle(PrismStyleId style) =>
        Styles.Single(operation => operation.StableId == (int)style);

    private static ImmutableArray<PrismCatalogOperationInfo> BuildOperations()
    {
        PrismCatalogEntryDescriptor[] entries = PrismCatalogGenerated.Entries;
        return entries
            .Where(entry => entry.Kind is "filter" or "style")
            .Select(entry => BuildOperation(entry, entries))
            .ToImmutableArray();
    }

    private static PrismCatalogOperationInfo BuildOperation(
        PrismCatalogEntryDescriptor entry,
        PrismCatalogEntryDescriptor[] entries)
    {
        PrismCatalogOperationKind kind = entry.Kind == "filter"
            ? PrismCatalogOperationKind.Filter
            : PrismCatalogOperationKind.Style;
        ImmutableArray<PrismCatalogParameterInfo> parameters = entry.Properties
            .Select(property => new PrismCatalogParameterInfo(
                kind,
                entry.StableId,
                property.TypeSlot,
                property.Id,
                property.Name,
                Convert(property.ValueType),
                property.Required,
                property.DefaultValue,
                property.Domain,
                property.Unit,
                SymbolOptions(property, entries)))
            .ToImmutableArray();
        return new PrismCatalogOperationInfo(
            entry.StableId,
            entry.Id,
            entry.Symbol,
            kind,
            entry.Category,
            parameters);
    }

    private static ImmutableArray<string> SymbolOptions(
        PrismCatalogPropertyDescriptor property,
        PrismCatalogEntryDescriptor[] entries)
    {
        if (property.ValueType != PrismCatalogValueType.Symbol)
        {
            return [];
        }

        IEnumerable<string> options = entries
            .SelectMany(entry => entry.Properties)
            .Where(candidate =>
                candidate.ValueType == PrismCatalogValueType.Symbol &&
                string.Equals(candidate.Name, property.Name, StringComparison.Ordinal) &&
                candidate.DefaultValue is not null)
            .Select(candidate => candidate.DefaultValue!);
        if (string.Equals(property.Name, "BlendMode", StringComparison.Ordinal))
        {
            options = options.Concat(
                Enum.GetNames<PrismBlendMode>().Where(name => name != nameof(PrismBlendMode.PassThrough)));
        }
        else if (string.Equals(property.Name, "Sampling", StringComparison.Ordinal))
        {
            options = options.Concat(Enum.GetNames<PrismSampling>());
        }

        return options.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
    }

    private static PrismCatalogValueKind Convert(PrismCatalogValueType valueType) => valueType switch
    {
        PrismCatalogValueType.Boolean => PrismCatalogValueKind.Boolean,
        PrismCatalogValueType.Integer => PrismCatalogValueKind.Integer,
        PrismCatalogValueType.Number => PrismCatalogValueKind.Number,
        PrismCatalogValueType.Color => PrismCatalogValueKind.Color,
        PrismCatalogValueType.Vector => PrismCatalogValueKind.Vector,
        PrismCatalogValueType.Symbol => PrismCatalogValueKind.Symbol,
        PrismCatalogValueType.Resource => PrismCatalogValueKind.Resource,
        _ => throw new InvalidOperationException($"Unknown Prism catalog value type '{valueType}'.")
    };
}
