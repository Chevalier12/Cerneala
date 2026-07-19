using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cerneala.SourceGen.Prism;

internal static class PrismCatalogCompiler
{
    private static readonly HashSet<string> RootFields = Set(
        "$schema",
        "schemaVersion",
        "catalogVersion",
        "defaultColorProfile",
        "commonProperties",
        "entries");

    private static readonly HashSet<string> CommonPropertyFields = Set(
        "composition",
        "layer",
        "group",
        "backdrop",
        "mask",
        "filter",
        "style");
    private static readonly HashSet<string> EntryFields = Set(
        "stableId",
        "id",
        "symbol",
        "kind",
        "category",
        "properties",
        "capabilities",
        "deterministic",
        "cacheable",
        "coverage",
        "transferFunction",
        "gamut",
        "intendedUse");
    private static readonly HashSet<string> PropertyFields = Set(
        "id",
        "name",
        "valueType",
        "required",
        "default",
        "domain",
        "unit");
    private static readonly HashSet<string> DomainFields = Set("kind", "minimum", "maximum");
    private static readonly HashSet<string> CoverageFields = Set("runtime", "kernel", "test", "documentation");
    private static readonly HashSet<string> EntryKinds = Set(
        "filter",
        "style",
        "blend-mode",
        "color-profile",
        "sampling");
    private static readonly HashSet<string> ValueTypes = Set(
        "boolean",
        "integer",
        "number",
        "color",
        "vector",
        "symbol",
        "resource");
    private static readonly Regex StableIdentifier = new(
        "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*:[a-z][a-z0-9]*(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex PropertyIdentifier = new(
        "^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex SymbolIdentifier = new(
        "^[A-Z][A-Za-z0-9]*$",
        RegexOptions.CultureInvariant);
    private static readonly Regex ArgbColor = new(
        "^#[0-9A-Fa-f]{8}$",
        RegexOptions.CultureInvariant);

    public static PrismCatalogCompilation Compile(string json)
    {
        List<PrismCatalogIssue> issues = new();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            issues.Add(Issue("PRISM3001", $"Catalog JSON is invalid: {exception.Message}"));
            return new PrismCatalogCompilation(null, issues.ToImmutableArray());
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Issue("PRISM3001", "Catalog root must be a JSON object."));
                return new PrismCatalogCompilation(null, issues.ToImmutableArray());
            }

            ValidateUnknownFields(root, RootFields, "catalog root", issues);
            ValidateRequiredString(root, "$schema", "catalog root", issues);
            int schemaVersion = ReadRequiredInt(root, "schemaVersion", "catalog root", issues);
            if (schemaVersion != 1)
            {
                issues.Add(Issue("PRISM3001", $"Unsupported schemaVersion '{schemaVersion}'. Expected 1."));
            }

            string catalogVersion = ReadRequiredString(root, "catalogVersion", "catalog root", issues);
            string defaultColorProfile = ReadRequiredString(root, "defaultColorProfile", "catalog root", issues);
            List<CatalogProperty> commonCompositionProperties = new();
            List<CatalogProperty> commonLayerProperties = new();
            List<CatalogProperty> commonGroupProperties = new();
            List<CatalogProperty> commonBackdropProperties = new();
            List<CatalogProperty> commonMaskProperties = new();
            List<CatalogProperty> commonFilterProperties = new();
            List<CatalogProperty> commonStyleProperties = new();
            ParseCommonProperties(
                root,
                commonCompositionProperties,
                commonLayerProperties,
                commonGroupProperties,
                commonBackdropProperties,
                commonMaskProperties,
                commonFilterProperties,
                commonStyleProperties,
                issues);
            List<CatalogEntry> entries = ParseEntries(root, issues);

            ValidateCatalog(
                entries,
                defaultColorProfile,
                commonCompositionProperties,
                commonLayerProperties,
                commonGroupProperties,
                commonBackdropProperties,
                commonMaskProperties,
                commonFilterProperties,
                commonStyleProperties,
                issues);
            if (issues.Count > 0)
            {
                return new PrismCatalogCompilation(null, issues.ToImmutableArray());
            }

            string generatedSource = GenerateSource(
                catalogVersion,
                defaultColorProfile,
                commonCompositionProperties,
                commonLayerProperties,
                commonGroupProperties,
                commonBackdropProperties,
                commonMaskProperties,
                commonFilterProperties,
                commonStyleProperties,
                entries);
            return new PrismCatalogCompilation(generatedSource, ImmutableArray<PrismCatalogIssue>.Empty);
        }
    }

    private static void ParseCommonProperties(
        JsonElement root,
        List<CatalogProperty> compositionProperties,
        List<CatalogProperty> layerProperties,
        List<CatalogProperty> groupProperties,
        List<CatalogProperty> backdropProperties,
        List<CatalogProperty> maskProperties,
        List<CatalogProperty> filterProperties,
        List<CatalogProperty> styleProperties,
        List<PrismCatalogIssue> issues)
    {
        if (!root.TryGetProperty("commonProperties", out JsonElement common) ||
            common.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Issue("PRISM3001", "Catalog root is missing object 'commonProperties'."));
            return;
        }

        ValidateUnknownFields(common, CommonPropertyFields, "commonProperties", issues);
        ParsePropertyArray(common, "composition", "commonProperties.composition", compositionProperties, issues);
        ParsePropertyArray(common, "layer", "commonProperties.layer", layerProperties, issues);
        ParsePropertyArray(common, "group", "commonProperties.group", groupProperties, issues);
        ParsePropertyArray(common, "backdrop", "commonProperties.backdrop", backdropProperties, issues);
        ParsePropertyArray(common, "mask", "commonProperties.mask", maskProperties, issues);
        ParsePropertyArray(common, "filter", "commonProperties.filter", filterProperties, issues);
        ParsePropertyArray(common, "style", "commonProperties.style", styleProperties, issues);
    }

    private static List<CatalogEntry> ParseEntries(JsonElement root, List<PrismCatalogIssue> issues)
    {
        List<CatalogEntry> entries = new();
        if (!root.TryGetProperty("entries", out JsonElement entriesElement) ||
            entriesElement.ValueKind != JsonValueKind.Array)
        {
            issues.Add(Issue("PRISM3001", "Catalog root is missing array 'entries'."));
            return entries;
        }

        int index = 0;
        foreach (JsonElement element in entriesElement.EnumerateArray())
        {
            string context = $"entries[{index}]";
            index++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Issue("PRISM3001", $"{context} must be an object."));
                continue;
            }

            ValidateUnknownFields(element, EntryFields, context, issues);
            int stableId = ReadRequiredInt(element, "stableId", context, issues);
            string id = ReadRequiredString(element, "id", context, issues);
            string symbol = ReadRequiredString(element, "symbol", context, issues);
            string kind = ReadRequiredString(element, "kind", context, issues);
            string category = ReadRequiredString(element, "category", context, issues);
            bool deterministic = ReadRequiredBoolean(element, "deterministic", context, issues);
            bool cacheable = ReadRequiredBoolean(element, "cacheable", context, issues);
            List<CatalogProperty> properties = new();
            ParsePropertyArray(element, "properties", $"{context}.properties", properties, issues);
            List<string> capabilities = ReadStringArray(element, "capabilities", context, issues);
            CatalogCoverage coverage = ParseCoverage(element, id, context, issues);
            entries.Add(new CatalogEntry(
                stableId,
                id,
                symbol,
                kind,
                category,
                properties,
                capabilities,
                deterministic,
                cacheable,
                coverage));
        }

        return entries;
    }

    private static void ParsePropertyArray(
        JsonElement owner,
        string field,
        string context,
        List<CatalogProperty> properties,
        List<PrismCatalogIssue> issues)
    {
        if (!owner.TryGetProperty(field, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            issues.Add(Issue("PRISM3001", $"{context} must be an array."));
            return;
        }

        int index = 0;
        foreach (JsonElement element in array.EnumerateArray())
        {
            string propertyContext = $"{context}[{index}]";
            index++;
            if (element.ValueKind != JsonValueKind.Object)
            {
                issues.Add(Issue("PRISM3001", $"{propertyContext} must be an object."));
                continue;
            }

            ValidateUnknownFields(element, PropertyFields, propertyContext, issues);
            string id = ReadRequiredString(element, "id", propertyContext, issues);
            string name = ReadRequiredString(element, "name", propertyContext, issues);
            string valueType = ReadRequiredString(element, "valueType", propertyContext, issues);
            bool required = ReadRequiredBoolean(element, "required", propertyContext, issues);
            string unit = ReadRequiredString(element, "unit", propertyContext, issues);
            CatalogDomain domain = ParseDomain(element, propertyContext, issues);
            bool hasDefault = element.TryGetProperty("default", out JsonElement defaultValue);

            if (required && hasDefault)
            {
                issues.Add(Issue("PRISM3003", $"{propertyContext} is required and cannot declare a default."));
            }
            else if (!required && !hasDefault)
            {
                issues.Add(Issue("PRISM3003", $"{propertyContext} must declare a default."));
            }

            if (!ValueTypes.Contains(valueType))
            {
                issues.Add(Issue("PRISM3003", $"{propertyContext} has unknown valueType '{valueType}'."));
            }
            else if (hasDefault && !IsCompatibleDefault(valueType, defaultValue))
            {
                issues.Add(Issue(
                    "PRISM3003",
                    $"{propertyContext} default is incompatible with valueType '{valueType}'."));
            }

            if (hasDefault &&
                valueType is "integer" or "number" &&
                defaultValue.ValueKind == JsonValueKind.Number)
            {
                ValidateNumericDefault(defaultValue, domain, propertyContext, issues);
            }

            properties.Add(new CatalogProperty(
                id,
                name,
                valueType,
                required,
                hasDefault ? CanonicalValue(defaultValue) : null,
                domain,
                unit));
        }
    }

    private static CatalogDomain ParseDomain(
        JsonElement property,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!property.TryGetProperty("domain", out JsonElement domain) ||
            domain.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Issue("PRISM3001", $"{context}.domain must be an object."));
            return new CatalogDomain(string.Empty, null, null);
        }

        ValidateUnknownFields(domain, DomainFields, $"{context}.domain", issues);
        string kind = ReadRequiredString(domain, "kind", $"{context}.domain", issues);
        double? minimum = ReadOptionalFiniteNumber(domain, "minimum", $"{context}.domain", issues);
        double? maximum = ReadOptionalFiniteNumber(domain, "maximum", $"{context}.domain", issues);
        if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
        {
            issues.Add(Issue(
                "PRISM3004",
                $"{context}.domain minimum {FormatNumber(minimum.Value)} exceeds maximum {FormatNumber(maximum.Value)}."));
        }

        return new CatalogDomain(kind, minimum, maximum);
    }

    private static CatalogCoverage ParseCoverage(
        JsonElement entry,
        string entryId,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!entry.TryGetProperty("coverage", out JsonElement coverage) ||
            coverage.ValueKind != JsonValueKind.Object)
        {
            issues.Add(Issue("PRISM3005", $"Catalog entry '{entryId}' is missing coverage."));
            return CatalogCoverage.Empty;
        }

        ValidateUnknownFields(coverage, CoverageFields, $"{context}.coverage", issues);
        return new CatalogCoverage(
            ReadCoverageOwner(coverage, entryId, "runtime", issues),
            ReadCoverageOwner(coverage, entryId, "kernel", issues),
            ReadCoverageOwner(coverage, entryId, "test", issues),
            ReadCoverageOwner(coverage, entryId, "documentation", issues));
    }

    private static string ReadCoverageOwner(
        JsonElement coverage,
        string entryId,
        string owner,
        List<PrismCatalogIssue> issues)
    {
        if (!coverage.TryGetProperty(owner, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            issues.Add(Issue(
                "PRISM3005",
                $"Catalog entry '{entryId}' is missing coverage owner '{owner}'."));
            return string.Empty;
        }

        return value.GetString()!;
    }

    private static void ValidateCatalog(
        List<CatalogEntry> entries,
        string defaultColorProfile,
        List<CatalogProperty> commonCompositionProperties,
        List<CatalogProperty> commonLayerProperties,
        List<CatalogProperty> commonGroupProperties,
        List<CatalogProperty> commonBackdropProperties,
        List<CatalogProperty> commonMaskProperties,
        List<CatalogProperty> commonFilterProperties,
        List<CatalogProperty> commonStyleProperties,
        List<PrismCatalogIssue> issues)
    {
        ValidatePropertySet(commonCompositionProperties, "commonProperties.composition", issues);
        ValidatePropertySet(commonLayerProperties, "commonProperties.layer", issues);
        ValidatePropertySet(commonGroupProperties, "commonProperties.group", issues);
        ValidatePropertySet(commonBackdropProperties, "commonProperties.backdrop", issues);
        ValidatePropertySet(commonMaskProperties, "commonProperties.mask", issues);
        ValidatePropertySet(commonFilterProperties, "commonProperties.filter", issues);
        ValidatePropertySet(commonStyleProperties, "commonProperties.style", issues);

        HashSet<int> stableIds = new();
        HashSet<string> identifiers = new(StringComparer.Ordinal);
        HashSet<string> symbols = new(StringComparer.Ordinal);
        foreach (CatalogEntry entry in entries)
        {
            if (entry.StableId <= 0 || !stableIds.Add(entry.StableId))
            {
                issues.Add(Issue("PRISM3002", $"Duplicate or invalid stableId '{entry.StableId}'."));
            }
            if (!StableIdentifier.IsMatch(entry.Id) || !identifiers.Add(entry.Id))
            {
                issues.Add(Issue("PRISM3002", $"Duplicate or invalid catalog identifier '{entry.Id}'."));
            }
            if (!EntryKinds.Contains(entry.Kind))
            {
                issues.Add(Issue("PRISM3001", $"Catalog entry '{entry.Id}' has unknown kind '{entry.Kind}'."));
            }
            if (!SymbolIdentifier.IsMatch(entry.Symbol) || !symbols.Add($"{entry.Kind}:{entry.Symbol}"))
            {
                issues.Add(Issue(
                    "PRISM3002",
                    $"Duplicate or invalid symbol '{entry.Symbol}' for kind '{entry.Kind}'."));
            }
            if (string.IsNullOrWhiteSpace(entry.Category))
            {
                issues.Add(Issue("PRISM3001", $"Catalog entry '{entry.Id}' has an empty category."));
            }
            if (entry.Capabilities.Count == 0 || entry.Capabilities.Any(string.IsNullOrWhiteSpace))
            {
                issues.Add(Issue("PRISM3001", $"Catalog entry '{entry.Id}' must declare capabilities."));
            }

            ValidatePropertySet(entry.Properties, entry.Id, issues);
        }

        if (!entries.Any(entry =>
                entry.Kind == "color-profile" &&
                string.Equals(entry.Symbol, defaultColorProfile, StringComparison.Ordinal)))
        {
            issues.Add(Issue(
                "PRISM3001",
                $"Default color profile '{defaultColorProfile}' has no color-profile entry."));
        }
    }

    private static void ValidatePropertySet(
        List<CatalogProperty> properties,
        string entryId,
        List<PrismCatalogIssue> issues)
    {
        HashSet<string> identifiers = new(StringComparer.Ordinal);
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (CatalogProperty property in properties)
        {
            if (!PropertyIdentifier.IsMatch(property.Id) || !identifiers.Add(property.Id))
            {
                issues.Add(Issue(
                    "PRISM3002",
                    $"Catalog entry '{entryId}' has duplicate or invalid property identifier '{property.Id}'."));
            }
            if (!SymbolIdentifier.IsMatch(property.Name) || !names.Add(property.Name))
            {
                issues.Add(Issue(
                    "PRISM3002",
                    $"Catalog entry '{entryId}' has duplicate or invalid property name '{property.Name}'."));
            }
        }
    }

    private static bool IsCompatibleDefault(string valueType, JsonElement value)
    {
        switch (valueType)
        {
            case "boolean":
                return value.ValueKind is JsonValueKind.True or JsonValueKind.False;
            case "integer":
                return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out _);
            case "number":
                return value.ValueKind == JsonValueKind.Number &&
                    value.TryGetDouble(out double number) &&
                    !double.IsNaN(number) &&
                    !double.IsInfinity(number);
            case "color":
                return value.ValueKind == JsonValueKind.String &&
                    ArgbColor.IsMatch(value.GetString() ?? string.Empty);
            case "vector":
                if (value.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                JsonElement[] components = value.EnumerateArray().ToArray();
                return components.Length is >= 2 and <= 4 &&
                    components.All(component =>
                        component.ValueKind == JsonValueKind.Number &&
                        component.TryGetDouble(out double number) &&
                        !double.IsNaN(number) &&
                        !double.IsInfinity(number));
            case "symbol":
                return value.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(value.GetString());
            case "resource":
                return value.ValueKind == JsonValueKind.Null ||
                    (value.ValueKind == JsonValueKind.String &&
                     !string.IsNullOrWhiteSpace(value.GetString()));
            default:
                return false;
        }
    }

    private static void ValidateNumericDefault(
        JsonElement value,
        CatalogDomain domain,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!value.TryGetDouble(out double number))
        {
            return;
        }
        if (domain.Minimum.HasValue && number < domain.Minimum.Value)
        {
            issues.Add(Issue(
                "PRISM3003",
                $"{context} default {FormatNumber(number)} is below minimum {FormatNumber(domain.Minimum.Value)}."));
        }
        if (domain.Maximum.HasValue && number > domain.Maximum.Value)
        {
            issues.Add(Issue(
                "PRISM3003",
                $"{context} default {FormatNumber(number)} exceeds maximum {FormatNumber(domain.Maximum.Value)}."));
        }
    }

    private static string GenerateSource(
        string catalogVersion,
        string defaultColorProfile,
        List<CatalogProperty> commonCompositionProperties,
        List<CatalogProperty> commonLayerProperties,
        List<CatalogProperty> commonGroupProperties,
        List<CatalogProperty> commonBackdropProperties,
        List<CatalogProperty> commonMaskProperties,
        List<CatalogProperty> commonFilterProperties,
        List<CatalogProperty> commonStyleProperties,
        List<CatalogEntry> entries)
    {
        CatalogEntry[] orderedEntries = entries.OrderBy(entry => entry.StableId).ToArray();
        StringBuilder source = new();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine("namespace Cerneala.Drawing.Prism.Catalog;");
        source.AppendLine();
        AppendEnum(source, "PrismFilterId", orderedEntries.Where(entry => entry.Kind == "filter"));
        AppendEnum(source, "PrismStyleId", orderedEntries.Where(entry => entry.Kind == "style"));
        AppendEnum(source, "PrismBlendMode", orderedEntries.Where(entry => entry.Kind == "blend-mode"));
        AppendEnum(source, "PrismColorProfile", orderedEntries.Where(entry => entry.Kind == "color-profile"));
        AppendEnum(source, "PrismSampling", orderedEntries.Where(entry => entry.Kind == "sampling"));
        source.AppendLine("internal enum PrismCatalogValueType");
        source.AppendLine("{");
        source.AppendLine("    Boolean,");
        source.AppendLine("    Integer,");
        source.AppendLine("    Number,");
        source.AppendLine("    Color,");
        source.AppendLine("    Vector,");
        source.AppendLine("    Symbol,");
        source.AppendLine("    Resource");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine("internal readonly record struct PrismCatalogPropertyDescriptor(");
        source.AppendLine("    int Slot,");
        source.AppendLine("    int TypeSlot,");
        source.AppendLine("    string Id,");
        source.AppendLine("    string Name,");
        source.AppendLine("    PrismCatalogValueType ValueType,");
        source.AppendLine("    bool Required,");
        source.AppendLine("    string? DefaultValue,");
        source.AppendLine("    string Domain,");
        source.AppendLine("    string Unit);");
        source.AppendLine();
        source.AppendLine("internal readonly record struct PrismCatalogCoverageDescriptor(");
        source.AppendLine("    string Runtime,");
        source.AppendLine("    string Kernel,");
        source.AppendLine("    string Test,");
        source.AppendLine("    string Documentation);");
        source.AppendLine();
        source.AppendLine("internal readonly record struct PrismCatalogEntryDescriptor(");
        source.AppendLine("    int StableId,");
        source.AppendLine("    string Id,");
        source.AppendLine("    string Symbol,");
        source.AppendLine("    string Kind,");
        source.AppendLine("    string Category,");
        source.AppendLine("    PrismCatalogPropertyDescriptor[] Properties,");
        source.AppendLine("    string[] Capabilities,");
        source.AppendLine("    bool Deterministic,");
        source.AppendLine("    bool Cacheable,");
        source.AppendLine("    PrismCatalogCoverageDescriptor Coverage);");
        source.AppendLine();
        source.AppendLine("internal static class PrismCatalogGenerated");
        source.AppendLine("{");
        source.Append("    internal const string CatalogVersion = \"")
            .Append(Escape(catalogVersion))
            .AppendLine("\";");
        source.Append("    internal const PrismColorProfile DefaultColorProfile = PrismColorProfile.")
            .Append(defaultColorProfile)
            .AppendLine(";");
        AppendDefinitionDefaults(
            source,
            commonCompositionProperties,
            commonLayerProperties,
            commonGroupProperties,
            commonBackdropProperties,
            commonMaskProperties,
            commonFilterProperties,
            commonStyleProperties);
        AppendPropertyArray(source, "CommonCompositionProperties", commonCompositionProperties, 1);
        AppendPropertyArray(source, "CommonLayerProperties", commonLayerProperties, 1);
        AppendPropertyArray(source, "CommonGroupProperties", commonGroupProperties, 1);
        AppendPropertyArray(source, "CommonBackdropProperties", commonBackdropProperties, 1);
        AppendPropertyArray(source, "CommonMaskProperties", commonMaskProperties, 1);
        AppendPropertyArray(source, "CommonFilterProperties", commonFilterProperties, 1);
        AppendPropertyArray(source, "CommonStyleProperties", commonStyleProperties, 1);
        AppendParameterKeys(source, "PrismCompositionPropertyKeys", 0, commonCompositionProperties, 1);
        AppendParameterKeys(source, "PrismLayerPropertyKeys", 0, commonLayerProperties, 1);
        AppendParameterKeys(source, "PrismGroupPropertyKeys", 0, commonGroupProperties, 1);
        AppendParameterKeys(source, "PrismBackdropPropertyKeys", 0, commonBackdropProperties, 1);
        AppendParameterKeys(source, "PrismMaskPropertyKeys", 0, commonMaskProperties, 1);
        AppendParameterKeys(source, "PrismFilterCommonParameterKeys", 0, commonFilterProperties, 1);
        AppendParameterKeys(source, "PrismStyleCommonParameterKeys", 0, commonStyleProperties, 1);
        AppendEntryParameterKeys(
            source,
            "PrismFilterParameterKeys",
            orderedEntries.Where(entry => entry.Kind == "filter"));
        AppendEntryParameterKeys(
            source,
            "PrismStyleParameterKeys",
            orderedEntries.Where(entry => entry.Kind == "style"));
        source.AppendLine("    internal static readonly PrismCatalogEntryDescriptor[] Entries =");
        source.AppendLine("    [");
        foreach (CatalogEntry entry in orderedEntries)
        {
            source.AppendLine("        new(");
            source.Append("            ").Append(entry.StableId).AppendLine(",");
            source.Append("            \"").Append(Escape(entry.Id)).AppendLine("\",");
            source.Append("            \"").Append(Escape(entry.Symbol)).AppendLine("\",");
            source.Append("            \"").Append(Escape(entry.Kind)).AppendLine("\",");
            source.Append("            \"").Append(Escape(entry.Category)).AppendLine("\",");
            AppendInlineProperties(source, entry.Properties, 3);
            source.AppendLine(",");
            source.Append("            [")
                .Append(string.Join(
                    ", ",
                    entry.Capabilities
                        .OrderBy(capability => capability, StringComparer.Ordinal)
                        .Select(capability => $"\"{Escape(capability)}\"")))
                .AppendLine("],");
            source.Append("            ").Append(entry.Deterministic ? "true" : "false").AppendLine(",");
            source.Append("            ").Append(entry.Cacheable ? "true" : "false").AppendLine(",");
            source.Append("            new(\"").Append(Escape(entry.Coverage.Runtime))
                .Append("\", \"").Append(Escape(entry.Coverage.Kernel))
                .Append("\", \"").Append(Escape(entry.Coverage.Test))
                .Append("\", \"").Append(Escape(entry.Coverage.Documentation))
                .AppendLine("\")),");
        }
        source.AppendLine("    ];");
        source.AppendLine("}");
        return source.ToString();
    }

    private static void AppendDefinitionDefaults(
        StringBuilder source,
        List<CatalogProperty> composition,
        List<CatalogProperty> layer,
        List<CatalogProperty> group,
        List<CatalogProperty> backdrop,
        List<CatalogProperty> mask,
        List<CatalogProperty> filter,
        List<CatalogProperty> style)
    {
        AppendConstant(source, "CompositionWorkingColorProfile", "PrismColorProfile", composition, "WorkingColorProfile");
        AppendConstant(source, "CompositionGlobalLightAngle", "float", composition, "GlobalLightAngle");
        AppendConstant(source, "CompositionGlobalLightAltitude", "float", composition, "GlobalLightAltitude");
        AppendConstant(source, "LayerVisible", "bool", layer, "Visible");
        AppendConstant(source, "LayerOpacity", "float", layer, "Opacity");
        AppendConstant(source, "LayerFill", "float", layer, "Fill");
        AppendConstant(source, "LayerBlendMode", "PrismBlendMode", layer, "BlendMode");
        AppendConstant(source, "LayerClipToBelow", "bool", layer, "ClipToBelow");
        AppendConstant(source, "GroupVisible", "bool", group, "Visible");
        AppendConstant(source, "GroupOpacity", "float", group, "Opacity");
        AppendConstant(source, "GroupBlendMode", "PrismBlendMode", group, "BlendMode");
        AppendConstant(source, "BackdropVisible", "bool", backdrop, "Visible");
        AppendConstant(source, "BackdropOpacity", "float", backdrop, "Opacity");
        AppendConstant(
            source,
            "MaskChannel",
            "global::Cerneala.UI.Prism.Definitions.PrismMaskChannel",
            mask,
            "Channel");
        AppendConstant(source, "MaskFeather", "float", mask, "Feather");
        AppendConstant(source, "MaskDensity", "float", mask, "Density");
        AppendConstant(source, "MaskInvert", "bool", mask, "Invert");
        AppendConstant(source, "FilterVisible", "bool", filter, "Visible");
        AppendConstant(source, "FilterOpacity", "float", filter, "Opacity");
        AppendConstant(source, "FilterBlendMode", "PrismBlendMode", filter, "BlendMode");
        AppendConstant(source, "StyleVisible", "bool", style, "Visible");
    }

    private static void AppendConstant(
        StringBuilder source,
        string constantName,
        string typeName,
        List<CatalogProperty> properties,
        string propertyName)
    {
        CatalogProperty property = properties.Single(candidate =>
            string.Equals(candidate.Name, propertyName, StringComparison.Ordinal));
        source.Append("    internal const ")
            .Append(typeName)
            .Append(' ')
            .Append(constantName)
            .Append(" = ")
            .Append(DefaultExpression(property, typeName))
            .AppendLine(";");
    }

    private static string DefaultExpression(CatalogProperty property, string typeName)
    {
        string value = property.DefaultValue
            ?? throw new InvalidOperationException($"Property '{property.Name}' has no default.");
        if (typeName == "bool")
        {
            return value;
        }
        if (typeName == "float")
        {
            return $"{value}f";
        }

        return $"{typeName}.{value}";
    }

    private static void AppendEntryParameterKeys(
        StringBuilder source,
        string className,
        IEnumerable<CatalogEntry> entries)
    {
        source.Append("internal static class ").AppendLine(className);
        source.AppendLine("{");
        foreach (CatalogEntry entry in entries)
        {
            source.Append("    internal static class ").AppendLine(entry.Symbol);
            source.AppendLine("    {");
            AppendParameterKeys(source, null, entry.StableId, entry.Properties, 2);
            source.AppendLine("    }");
        }
        source.AppendLine("}");
        source.AppendLine();
    }

    private static void AppendParameterKeys(
        StringBuilder source,
        string? className,
        int entryStableId,
        List<CatalogProperty> properties,
        int indent)
    {
        if (className is not null)
        {
            source.Append(' ', indent * 4)
                .Append("internal static class ")
                .AppendLine(className);
            source.Append(' ', indent * 4).AppendLine("{");
            indent++;
        }

        CatalogProperty[] ordered = properties.OrderBy(property => property.Id, StringComparer.Ordinal).ToArray();
        Dictionary<string, int> typeSlots = new(StringComparer.Ordinal);
        foreach (CatalogProperty property in ordered)
        {
            string storageType = StorageType(property.ValueType);
            typeSlots.TryGetValue(storageType, out int typeSlot);
            typeSlots[storageType] = typeSlot + 1;
            source.Append(' ', indent * 4)
                .Append("internal static readonly global::Cerneala.UI.Prism.Definitions.PrismParameterKey<")
                .Append(ParameterType(property.ValueType))
                .Append("> ")
                .Append(property.Name)
                .Append("Key")
                .Append(" = new(")
                .Append(entryStableId)
                .Append(", ")
                .Append(typeSlot)
                .AppendLine(");");
        }

        if (className is not null)
        {
            indent--;
            source.Append(' ', indent * 4).AppendLine("}");
        }
    }

    private static void AppendEnum(
        StringBuilder source,
        string name,
        IEnumerable<CatalogEntry> entries)
    {
        source.Append("public enum ").AppendLine(name);
        source.AppendLine("{");
        foreach (CatalogEntry entry in entries)
        {
            source.Append("    ").Append(entry.Symbol).Append(" = ").Append(entry.StableId).AppendLine(",");
        }
        source.AppendLine("}");
        source.AppendLine();
    }

    private static void AppendPropertyArray(
        StringBuilder source,
        string fieldName,
        List<CatalogProperty> properties,
        int indent)
    {
        source.Append(' ', indent * 4)
            .Append("internal static readonly PrismCatalogPropertyDescriptor[] ")
            .Append(fieldName)
            .Append(" = ");
        AppendInlineProperties(source, properties, indent);
        source.AppendLine(";");
    }

    private static void AppendInlineProperties(
        StringBuilder source,
        List<CatalogProperty> properties,
        int indent)
    {
        CatalogProperty[] ordered = properties.OrderBy(property => property.Id, StringComparer.Ordinal).ToArray();
        if (ordered.Length == 0)
        {
            source.Append("[]");
            return;
        }

        source.AppendLine("[");
        Dictionary<string, int> typeSlots = new(StringComparer.Ordinal);
        for (int index = 0; index < ordered.Length; index++)
        {
            CatalogProperty property = ordered[index];
            string storageType = StorageType(property.ValueType);
            typeSlots.TryGetValue(storageType, out int typeSlot);
            typeSlots[storageType] = typeSlot + 1;
            source.Append(' ', (indent + 1) * 4)
                .Append("new(")
                .Append(index)
                .Append(", ")
                .Append(typeSlot)
                .Append(", \"")
                .Append(Escape(property.Id))
                .Append("\", \"")
                .Append(Escape(property.Name))
                .Append("\", PrismCatalogValueType.")
                .Append(ToPascalCase(property.ValueType))
                .Append(", ")
                .Append(property.Required ? "true" : "false")
                .Append(", ")
                .Append(property.DefaultValue is null ? "null" : $"\"{Escape(property.DefaultValue)}\"")
                .Append(", \"")
                .Append(Escape(property.Domain.Canonical))
                .Append("\", \"")
                .Append(Escape(property.Unit))
                .AppendLine("\"),");
        }
        source.Append(' ', indent * 4).Append(']');
    }

    private static string CanonicalValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Null:
                return "null";
            case JsonValueKind.True:
                return "true";
            case JsonValueKind.False:
                return "false";
            case JsonValueKind.Number:
                return value.GetDouble().ToString("R", CultureInfo.InvariantCulture);
            case JsonValueKind.String:
                return value.GetString() ?? string.Empty;
            case JsonValueKind.Array:
                return string.Join(
                    ",",
                    value.EnumerateArray().Select(CanonicalValue));
            default:
                return value.GetRawText();
        }
    }

    private static void ValidateUnknownFields(
        JsonElement element,
        HashSet<string> allowed,
        string context,
        List<PrismCatalogIssue> issues)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                issues.Add(Issue(
                    "PRISM3006",
                    $"{context} contains unknown property '{property.Name}'."));
            }
        }
    }

    private static string ReadRequiredString(
        JsonElement owner,
        string field,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!owner.TryGetProperty(field, out JsonElement value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
        {
            issues.Add(Issue("PRISM3001", $"{context} is missing non-empty string '{field}'."));
            return string.Empty;
        }

        return value.GetString()!;
    }

    private static void ValidateRequiredString(
        JsonElement owner,
        string field,
        string context,
        List<PrismCatalogIssue> issues)
    {
        _ = ReadRequiredString(owner, field, context, issues);
    }

    private static int ReadRequiredInt(
        JsonElement owner,
        string field,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!owner.TryGetProperty(field, out JsonElement value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out int result))
        {
            issues.Add(Issue("PRISM3001", $"{context} is missing integer '{field}'."));
            return 0;
        }

        return result;
    }

    private static bool ReadRequiredBoolean(
        JsonElement owner,
        string field,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!owner.TryGetProperty(field, out JsonElement value) ||
            value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            issues.Add(Issue("PRISM3001", $"{context} is missing boolean '{field}'."));
            return false;
        }

        return value.GetBoolean();
    }

    private static List<string> ReadStringArray(
        JsonElement owner,
        string field,
        string context,
        List<PrismCatalogIssue> issues)
    {
        List<string> values = new();
        if (!owner.TryGetProperty(field, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            issues.Add(Issue("PRISM3001", $"{context} is missing array '{field}'."));
            return values;
        }

        foreach (JsonElement value in array.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            {
                issues.Add(Issue("PRISM3001", $"{context}.{field} contains a non-string or empty value."));
                continue;
            }
            values.Add(value.GetString()!);
        }

        return values;
    }

    private static double? ReadOptionalFiniteNumber(
        JsonElement owner,
        string field,
        string context,
        List<PrismCatalogIssue> issues)
    {
        if (!owner.TryGetProperty(field, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.Number ||
            !value.TryGetDouble(out double result) ||
            double.IsNaN(result) ||
            double.IsInfinity(result))
        {
            issues.Add(Issue("PRISM3004", $"{context}.{field} must be a finite number or null."));
            return null;
        }

        return result;
    }

    private static PrismCatalogIssue Issue(string id, string message)
    {
        return new PrismCatalogIssue(id, message);
    }

    private static HashSet<string> Set(params string[] values)
    {
        return new HashSet<string>(values, StringComparer.Ordinal);
    }

    private static string Escape(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private static string ToPascalCase(string value)
    {
        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static string ParameterType(string valueType)
    {
        return valueType switch
        {
            "boolean" => "bool",
            "integer" => "int",
            "number" => "float",
            "color" => "global::Cerneala.Drawing.Color",
            "vector" => "global::System.Numerics.Vector4",
            "symbol" => "int",
            "resource" => "global::Cerneala.UI.Prism.Definitions.PrismResourceId",
            _ => throw new InvalidOperationException($"Unknown parameter value type '{valueType}'.")
        };
    }

    private static string StorageType(string valueType)
    {
        return valueType is "integer" or "symbol" ? "int" : valueType;
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private sealed class CatalogEntry
    {
        public CatalogEntry(
            int stableId,
            string id,
            string symbol,
            string kind,
            string category,
            List<CatalogProperty> properties,
            List<string> capabilities,
            bool deterministic,
            bool cacheable,
            CatalogCoverage coverage)
        {
            StableId = stableId;
            Id = id;
            Symbol = symbol;
            Kind = kind;
            Category = category;
            Properties = properties;
            Capabilities = capabilities;
            Deterministic = deterministic;
            Cacheable = cacheable;
            Coverage = coverage;
        }

        public int StableId { get; }
        public string Id { get; }
        public string Symbol { get; }
        public string Kind { get; }
        public string Category { get; }
        public List<CatalogProperty> Properties { get; }
        public List<string> Capabilities { get; }
        public bool Deterministic { get; }
        public bool Cacheable { get; }
        public CatalogCoverage Coverage { get; }
    }

    private sealed class CatalogProperty
    {
        public CatalogProperty(
            string id,
            string name,
            string valueType,
            bool required,
            string? defaultValue,
            CatalogDomain domain,
            string unit)
        {
            Id = id;
            Name = name;
            ValueType = valueType;
            Required = required;
            DefaultValue = defaultValue;
            Domain = domain;
            Unit = unit;
        }

        public string Id { get; }
        public string Name { get; }
        public string ValueType { get; }
        public bool Required { get; }
        public string? DefaultValue { get; }
        public CatalogDomain Domain { get; }
        public string Unit { get; }
    }

    private sealed class CatalogDomain
    {
        public CatalogDomain(string kind, double? minimum, double? maximum)
        {
            Kind = kind;
            Minimum = minimum;
            Maximum = maximum;
        }

        public string Kind { get; }
        public double? Minimum { get; }
        public double? Maximum { get; }
        public string Canonical =>
            $"{Kind}:{(Minimum.HasValue ? FormatNumber(Minimum.Value) : string.Empty)}:{(Maximum.HasValue ? FormatNumber(Maximum.Value) : string.Empty)}";
    }

    private sealed class CatalogCoverage
    {
        public static readonly CatalogCoverage Empty = new(string.Empty, string.Empty, string.Empty, string.Empty);

        public CatalogCoverage(string runtime, string kernel, string test, string documentation)
        {
            Runtime = runtime;
            Kernel = kernel;
            Test = test;
            Documentation = documentation;
        }

        public string Runtime { get; }
        public string Kernel { get; }
        public string Test { get; }
        public string Documentation { get; }
    }
}

internal sealed class PrismCatalogCompilation
{
    public PrismCatalogCompilation(string? generatedSource, ImmutableArray<PrismCatalogIssue> issues)
    {
        GeneratedSource = generatedSource;
        Issues = issues;
    }

    public string? GeneratedSource { get; }

    public ImmutableArray<PrismCatalogIssue> Issues { get; }
}

internal sealed class PrismCatalogIssue
{
    public PrismCatalogIssue(string id, string message)
    {
        Id = id;
        Message = message;
    }

    public string Id { get; }

    public string Message { get; }
}
