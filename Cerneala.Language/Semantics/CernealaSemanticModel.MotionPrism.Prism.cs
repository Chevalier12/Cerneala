using System.Globalization;
using Cerneala.Language.Features;
using Cerneala.Language.Prism.Catalog;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private void BindPrismClipResource(ResourceDefinition resource, CancellationToken cancellationToken)
    {
        (string text, int offset) = BuildDirectTextBuffer(resource.Element);
        EmbeddedParseResult<PrismClipModelSyntax> parsed = PrismSyntaxParser.ParseComposition(text, offset);
        AddPrismSyntaxDiagnostics(parsed.Diagnostics);
        PrismClipDefinition definition = BindPrismClip(
            resource.Name ?? "PrismClip",
            resource.Element,
            parsed.Syntax,
            cancellationToken);
        prismClips[resource] = definition;
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismClip,
            resource.Name ?? "PrismClip",
            "Cerneala.UI.Prism.Definitions.PrismClipDefinition",
            resource.NameSpan,
            definitionLocation: resource.Location));

        IReadOnlyList<PrismCatalogProperty> compositionProperties = PrismCatalog.Value.GetCommonProperties("composition");
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (AttributeSyntax attribute in resource.Element.Attributes.Where(candidate => candidate.NameToken.Text != "Name"))
        {
            string name = attribute.NameToken.Text;
            PrismCatalogProperty? property = compositionProperties.FirstOrDefault(candidate => candidate.Name == name);
            if (property is null)
            {
                AddPrismDiagnostic("PRISM2001", attribute.NameToken.Span, "Unknown Prism property '" + name + "'.");
                continue;
            }

            if (!seen.Add(name))
            {
                AddPrismDiagnostic("PRISM2003", attribute.NameToken.Span, "Prism property '" + name + "' is assigned more than once.");
                continue;
            }

            string value = Unquote(attribute.ValueToken.Text);
            PrismValueModelSyntax syntax = new(value, PrismSyntaxParser.ClassifyValue(value), AttributeContentSpan(attribute));
            if (BindPrismValue(resource.Element, syntax, property, scope: null))
            {
                ILanguageTypeSymbol? type = ResolvePrismType(property.ValueType);
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.PrismProperty,
                    property.Name,
                    type?.MetadataName ?? property.ValueType,
                    attribute.NameToken.Span,
                    type,
                    value: value));
            }
        }
    }

    // One `@prism` of an Aspect body; `owner` is the Aspect element (resource
    // or inline property element), the scope for resources and bindings.
    private PrismClipDefinition? BindPrismApplication(
        ElementSyntax owner,
        PrismApplicationModelSyntax application,
        CancellationToken cancellationToken)
    {
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismDirective,
            "@prism",
            "Cerneala.UI.Prism.Definitions.PrismClipDefinition",
            new TextSpan(application.Span.Start, Math.Min("@prism".Length, application.Span.Length))));

        PrismClipDefinition? definition;
        if (application.Composition is not null)
        {
            definition = BindPrismClip(
                "InlinePrism@" + application.Span.Start.ToString(CultureInfo.InvariantCulture),
                owner,
                application.Composition,
                cancellationToken);
        }
        else
        {
            string name = application.ResourceName ?? string.Empty;
            ResourceDefinition? resource = FindResource(owner, name);
            if (resource is null || !prismClips.TryGetValue(resource, out definition))
            {
                AddPrismDiagnostic("PRISM2002", application.ResourceSpan, "Unknown PrismClip resource '$" + name + "'.");
                return null;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.ResourceReference,
                name,
                "Cerneala.UI.Prism.Definitions.PrismClipDefinition",
                application.ResourceSpan,
                definitionLocation: resource.Location));
            BindPrismApplicationArguments(owner, application, definition);
        }

        return definition;
    }

    private void BindPrismApplicationArguments(
        ElementSyntax owner,
        PrismApplicationModelSyntax application,
        PrismClipDefinition definition)
    {
        HashSet<string> supplied = new(StringComparer.Ordinal);
        foreach (PrismAssignmentModelSyntax argument in application.Arguments)
        {
            if (!definition.Parameters.TryGetValue(argument.Name, out PrismParameterDefinition? parameter))
            {
                AddPrismDiagnostic("PRISM2004", argument.NameSpan, "Unknown Prism parameter path '" + argument.Name + "'.");
                continue;
            }

            if (!supplied.Add(argument.Name))
            {
                AddPrismDiagnostic("PRISM2003", argument.NameSpan, "Prism parameter '" + argument.Name + "' is assigned more than once.");
                continue;
            }

            PrismCatalogProperty schema = SyntheticPrismProperty(argument.Name, parameter.TypeName, required: true);
            BindPrismValue(owner, argument.Value, schema, scope: null);
        }

        PrismParameterDefinition? missing = definition.Parameters.Values.FirstOrDefault(parameter =>
            parameter.DefaultValue is null && !supplied.Contains(parameter.Path));
        if (missing is not null)
        {
            AddPrismDiagnostic("PRISM2004", application.ResourceSpan, "Required Prism parameter '" + missing.Path + "' has no application value.");
        }
    }

    private PrismClipDefinition BindPrismClip(
        string name,
        ElementSyntax source,
        PrismClipModelSyntax syntax,
        CancellationToken cancellationToken)
    {
        PrismClipDefinition definition = new(name, source);
        PrismParameterScope rootScope = new(parent: null);
        BindPrismParameters(syntax.Members, rootScope, string.Empty, definition);
        BindPrismAssignments(
            source,
            syntax.Members,
            PrismCatalog.Value.GetCommonProperties("composition"),
            rootScope,
            "composition");

        PrismContainerModelSyntax[] nodes = syntax.Members.OfType<PrismContainerModelSyntax>().ToArray();
        foreach (PrismContainerModelSyntax node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PrismNodeDefinition? bound = BindPrismNode(source, node, rootScope, string.Empty, definition, parentKind: null, cancellationToken);
            if (bound is not null)
            {
                definition.RootNodes.Add(bound);
            }
        }

        if (nodes.Length == 0)
        {
            AddPrismDiagnostic(
                "PRISM2013",
                new TextSpan(source.NameToken.Span.Start, Math.Min(1, source.NameToken.Span.Length)),
                "A Prism composition must contain at least one layer or group.");
        }

        ValidatePrismClipToBelow(definition.RootNodes);
        return definition;
    }

    private PrismNodeDefinition? BindPrismNode(
        ElementSyntax source,
        PrismContainerModelSyntax syntax,
        PrismParameterScope parentScope,
        string parentPath,
        PrismClipDefinition composition,
        PrismContainerModelKind? parentKind,
        CancellationToken cancellationToken)
    {
        if (parentKind is not null && parentKind != PrismContainerModelKind.Group)
        {
            AddPrismDiagnostic("PRISM2005", syntax.NameSpan.Length == 0 ? syntax.Span : syntax.NameSpan,
                "@" + syntax.Kind.ToString().ToLowerInvariant() + " cannot be nested inside @" + parentKind.Value.ToString().ToLowerInvariant() + ".");
            return null;
        }

        string nodeName = syntax.Name!;
        string path = parentPath.Length == 0 ? nodeName : parentPath + "." + nodeName;
        PrismNodeDefinition node = new(nodeName, path, syntax.Kind, syntax.NameSpan.Length == 0 ? syntax.Span : syntax.NameSpan);
        if (composition.Nodes.ContainsKey(path))
        {
            AddPrismDiagnostic("PRISM2003", node.Span, "Prism node name '" + nodeName + "' is duplicated in the same address scope.");
            return null;
        }

        composition.Nodes.Add(path, node);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismNode,
            nodeName,
            "Cerneala.UI.Prism.Definitions.Prism" + syntax.Kind + "Definition",
            node.Span,
            definitionLocation: new LanguageSourceLocation(document.Path, node.Span),
            value: path));

        PrismParameterScope scope = new(parentScope);
        BindPrismParameters(syntax.Members, scope, path, composition);
        string family = syntax.Kind.ToString().ToLowerInvariant();
        node.Properties.AddRange(BindPrismAssignments(
            source,
            syntax.Members,
            PrismCatalog.Value.GetCommonProperties(family),
            scope,
            family));

        PrismContainerModelSyntax[] children = syntax.Members.OfType<PrismContainerModelSyntax>().ToArray();
        foreach (PrismContainerModelSyntax child in children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PrismNodeDefinition? boundChild = BindPrismNode(source, child, scope, path, composition, syntax.Kind, cancellationToken);
            if (boundChild is not null)
            {
                node.Children.Add(boundChild);
            }
        }

        bool maskSeen = false;
        foreach (PrismOperationModelSyntax operation in syntax.Members.OfType<PrismOperationModelSyntax>())
        {
            if (operation.Kind == PrismOperationModelKind.Mask && maskSeen)
            {
                AddPrismDiagnostic("PRISM2005", operation.Span, "A Prism node may declare only one @mask.");
                continue;
            }

            maskSeen |= operation.Kind == PrismOperationModelKind.Mask;
            BindPrismOperation(source, operation, scope);
        }

        return node;
    }

    private void BindPrismOperation(
        ElementSyntax source,
        PrismOperationModelSyntax syntax,
        PrismParameterScope scope)
    {
        string kind = syntax.Kind.ToString().ToLowerInvariant();
        IReadOnlyList<PrismCatalogProperty> properties;
        if (syntax.Kind == PrismOperationModelKind.Mask)
        {
            properties = PrismCatalog.Value.GetCommonProperties("mask");
        }
        else
        {
            PrismCatalogSymbol? catalogSymbol = syntax.TypeName is null
                ? null
                : PrismCatalog.Value.FindSymbol(kind, syntax.TypeName);
            if (catalogSymbol is null)
            {
                AddPrismDiagnostic("PRISM2002", syntax.TypeSpan.Length == 0 ? syntax.Span : syntax.TypeSpan,
                    "Unknown Prism " + kind + " '" + (syntax.TypeName ?? string.Empty) + "'.");
                return;
            }

            properties = PrismCatalog.Value.GetCommonProperties(kind)
                .Concat(catalogSymbol.Properties)
                .GroupBy(property => property.Name, StringComparer.Ordinal)
                .Select(group => group.Last())
                .ToArray();
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismOperation,
            syntax.TypeName ?? "Mask",
            "Cerneala.UI.Prism.Definitions.Prism" + syntax.Kind + "Definition",
            syntax.TypeSpan.Length == 0 ? syntax.Span : syntax.TypeSpan,
            value: kind));
        BindPrismAssignments(source, syntax.Members, properties, scope, kind);
    }

    private void BindPrismParameters(
        IReadOnlyList<PrismMemberModelSyntax> members,
        PrismParameterScope scope,
        string path,
        PrismClipDefinition composition)
    {
        foreach (PrismParameterModelSyntax syntax in members.OfType<PrismParameterModelSyntax>())
        {
            if (!TryNormalizePrismType(syntax.TypeName, out string typeName))
            {
                AddPrismDiagnostic("PRISM2004", syntax.TypeSpan, "Unknown Prism parameter type '" + syntax.TypeName + "'.");
                continue;
            }

            if (scope.ContainsLocal(syntax.Name))
            {
                AddPrismDiagnostic("PRISM2003", syntax.NameSpan, "Prism parameter '" + syntax.Name + "' is duplicated in the same scope.");
                continue;
            }

            string parameterPath = path.Length == 0 ? syntax.Name : path + "." + syntax.Name;
            PrismParameterDefinition parameter = new(syntax.Name, parameterPath, typeName, syntax.DefaultValue, syntax.NameSpan);
            scope.Add(parameter);
            composition.Parameters[parameterPath] = parameter;
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.PrismParameter,
                syntax.Name,
                typeName,
                syntax.NameSpan,
                ResolvePrismType(typeName),
                definitionLocation: new LanguageSourceLocation(document.Path, syntax.NameSpan),
                value: parameterPath));
            if (syntax.DefaultValue is not null)
            {
                BindPrismValue(composition.Source, syntax.DefaultValue, SyntheticPrismProperty(syntax.Name, typeName, required: false), scope);
            }
        }
    }

    private List<PrismBoundProperty> BindPrismAssignments(
        ElementSyntax source,
        IReadOnlyList<PrismMemberModelSyntax> members,
        IReadOnlyList<PrismCatalogProperty> schemas,
        PrismParameterScope? scope,
        string family)
    {
        Dictionary<string, PrismCatalogProperty> byName = schemas
            .GroupBy(property => property.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        List<PrismBoundProperty> result = new();
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (PrismAssignmentModelSyntax assignment in members.OfType<PrismAssignmentModelSyntax>())
        {
            if (!byName.TryGetValue(assignment.Name, out PrismCatalogProperty? schema))
            {
                AddPrismDiagnostic("PRISM2001", assignment.NameSpan, "Unknown Prism property '" + assignment.Name + "'.");
                continue;
            }

            if (!seen.Add(assignment.Name))
            {
                AddPrismDiagnostic("PRISM2003", assignment.NameSpan, "Prism property '" + assignment.Name + "' is assigned more than once.");
                continue;
            }

            if (BindPrismValue(source, assignment.Value, schema, scope))
            {
                result.Add(new PrismBoundProperty(schema, assignment.NameSpan, assignment.Value));
                ILanguageTypeSymbol? type = ResolvePrismType(schema.ValueType);
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.PrismProperty,
                    schema.Name,
                    type?.MetadataName ?? schema.ValueType,
                    assignment.NameSpan,
                    type,
                    value: assignment.Value.Text));
            }
        }

        PrismCatalogProperty? missing = schemas.FirstOrDefault(property =>
            property.Required && property.DefaultValue is null && !seen.Contains(property.Name));
        if (missing is not null)
        {
            TextSpan span = members.FirstOrDefault()?.Span ?? source.NameToken.Span;
            AddPrismDiagnostic("PRISM2009", span, "Required Prism property '" + missing.Name + "' is missing.");
        }

        return result;
    }

    private bool BindPrismValue(
        ElementSyntax source,
        PrismValueModelSyntax value,
        PrismCatalogProperty schema,
        PrismParameterScope? scope)
    {
        if (value.Kind is PrismValueModelKind.Binding or PrismValueModelKind.DirectReference)
        {
            return BindPrismValueBinding(source, value, schema);
        }

        if (value.Kind == PrismValueModelKind.Identifier && scope?.Resolve(value.Text) is PrismParameterDefinition parameter)
        {
            if (!CanConvertPrismType(parameter.TypeName, schema.ValueType))
            {
                AddPrismDiagnostic("PRISM2009", value.Span,
                    "Prism parameter '" + parameter.Name + "' has type " + parameter.TypeName + ", not " + schema.ValueType + ".");
                return false;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.PrismValue,
                parameter.Name,
                parameter.TypeName,
                value.Span,
                ResolvePrismType(parameter.TypeName),
                definitionLocation: new LanguageSourceLocation(document.Path, parameter.Span)));
            return true;
        }

        float vectorX = default;
        float vectorY = default;
        bool valid = schema.ValueType switch
        {
            "boolean" => value.Kind == PrismValueModelKind.BooleanLiteral,
            "integer" => value.Kind == PrismValueModelKind.NumberLiteral &&
                int.TryParse(value.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "number" => value.Kind == PrismValueModelKind.NumberLiteral &&
                float.TryParse(value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) &&
                !float.IsNaN(number) && !float.IsInfinity(number),
            "color" => value.Kind == PrismValueModelKind.ColorLiteral && IsHexColor(value.Text),
            "vector" => value.Kind == PrismValueModelKind.TupleLiteral &&
                IsPrismVector(value.Text, out vectorX, out vectorY),
            "symbol" => value.Kind == PrismValueModelKind.Identifier,
            "resource" => value.Kind is PrismValueModelKind.ResourceReference or PrismValueModelKind.NullLiteral,
            _ => false
        };
        if (!valid)
        {
            AddPrismDiagnostic("PRISM2009", value.Span,
                "Value '" + value.Text + "' is not a valid " + schema.ValueType + " Prism value.");
            return false;
        }

        if (schema.ValueType is "integer" or "number" &&
            double.TryParse(value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double numeric) &&
            (schema.Minimum is double minimum && numeric < minimum || schema.Maximum is double maximum && numeric > maximum))
        {
            AddPrismDiagnostic("PRISM2009", value.Span,
                "Prism property '" + schema.Name + "' value '" + value.Text + "' is outside catalog domain '" + CanonicalPrismDomain(schema) + "'.");
            return false;
        }

        if (schema.ValueType == "vector" &&
            schema.DomainKind == "positive-xy-components" &&
            (vectorX <= 0 || vectorY <= 0))
        {
            AddPrismDiagnostic("PRISM2009", value.Span,
                "Prism property '" + schema.Name + "' value '" + value.Text + "' is outside catalog domain '" + CanonicalPrismDomain(schema) + "'.");
            return false;
        }

        if (schema.ValueType == "symbol" && schema.Symbols.Count > 0 && !schema.Symbols.Contains(value.Text))
        {
            AddPrismDiagnostic("PRISM2009", value.Span,
                "Unknown Prism symbol '" + value.Text + "' for property '" + schema.Name + "'.");
            return false;
        }

        if (schema.ValueType == "resource" && value.Kind == PrismValueModelKind.ResourceReference)
        {
            string resourceName = value.Text.Substring(1);
            ResourceDefinition? resource = FindResource(source, resourceName);
            if (resource?.Kind != ResourceKind.Brush)
            {
                AddPrismDiagnostic("PRISM2009", value.Span,
                    "Unknown or incompatible typed Prism resource '$" + resourceName + "'.");
                return false;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.ResourceReference,
                resourceName,
                resource.Type?.MetadataName ?? "Cerneala.UI.Media.Brush",
                new TextSpan(value.Span.Start + 1, resourceName.Length),
                resource.Type,
                definitionLocation: resource.Location));
        }

        ILanguageTypeSymbol? type = ResolvePrismType(schema.ValueType);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismValue,
            value.Text,
            type?.MetadataName ?? schema.ValueType,
            value.Span,
            type,
            value: value.Text));
        return true;
    }

    private bool BindPrismValueBinding(
        ElementSyntax source,
        PrismValueModelSyntax value,
        PrismCatalogProperty schema)
    {
        EmbeddedParseResult<BindingValueSyntax> parsed = BindingSyntaxParser.Parse(value.Text, value.Span.Start);
        if (parsed.Diagnostics.Count > 0 ||
            parsed.Syntax.Kind != BindingValueKind.Direct ||
            parsed.Syntax.Binding is not BindingPathSyntax path)
        {
            EmbeddedDiagnostic diagnostic = parsed.Diagnostics.FirstOrDefault() ?? new EmbeddedDiagnostic(
                "CERNEALAUI007",
                "The Prism binding expression is incomplete.",
                value.Span);
            AddBindingDiagnostic(value.Text, diagnostic.Span, diagnostic.Message);
            return false;
        }

        BindingResolution? resolution = ResolveBindingPath(
            source,
            path,
            GetCompletionDataType(source),
            validateClrObservability: value.Kind == PrismValueModelKind.Binding);
        if (resolution is null)
        {
            return false;
        }

        ILanguageTypeSymbol? targetType = ResolvePrismType(schema.ValueType);
        if (!IsPrismBindingTypeCompatible(resolution.Type, targetType))
        {
            AddPrismDiagnostic("PRISM2009", value.Span,
                "Prism binding '" + value.Text + "' has type " +
                (resolution.Type?.MetadataName ?? "unknown") + ", not " + schema.ValueType + ".");
            return false;
        }

        if (value.Kind == PrismValueModelKind.Binding &&
            path.Mode == BindingModeSyntax.TwoWay &&
            !resolution.CanWrite)
        {
            AddBindingSemanticDiagnostic(source, value.Text, path.Span,
                "A TwoWay Prism binding requires a writable source property.");
            return false;
        }

        if (value.Kind == PrismValueModelKind.Binding)
        {
            AddBindingModeSymbol(path, resolution.Type);
        }
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismValue,
            value.Text,
            targetType?.MetadataName ?? schema.ValueType,
            value.Span,
            targetType,
            value: value.Text));
        return true;
    }

    private static bool IsPrismBindingTypeCompatible(
        ILanguageTypeSymbol? source,
        ILanguageTypeSymbol? target)
    {
        if (source is null || target is null)
        {
            return false;
        }

        string sourceName = source.MetadataName.TrimEnd('?');
        string targetName = target.MetadataName.TrimEnd('?');
        return string.Equals(sourceName, targetName, StringComparison.Ordinal) ||
            sourceName == "System.Int32" && targetName == "System.Single" ||
            source.IsOrImplements(targetName);
    }

    private void ValidatePrismClipToBelow(IReadOnlyList<PrismNodeDefinition> nodes)
    {
        for (int index = 0; index < nodes.Count; index++)
        {
            PrismNodeDefinition node = nodes[index];
            PrismBoundProperty? clip = node.Properties.FirstOrDefault(property => property.Schema.Name == "ClipToBelow");
            if (clip?.Value.Text != "true")
            {
                continue;
            }

            bool hasBase = nodes.Skip(index + 1).Any(candidate =>
                candidate.Kind == PrismContainerModelKind.Group ||
                candidate.Properties.All(property => property.Schema.Name != "ClipToBelow" || property.Value.Text == "false"));
            if (!hasBase)
            {
                AddPrismDiagnostic("PRISM2008", clip.NameSpan,
                    "ClipToBelow requires an unclipped normal sibling beneath the layer.");
            }
        }

        foreach (PrismNodeDefinition group in nodes.Where(node => node.Kind == PrismContainerModelKind.Group))
        {
            ValidatePrismClipToBelow(group.Children);
        }
    }

    private void AddPrismDiagnostic(string id, TextSpan span, string message) =>
        AddDiagnostic(id, span, Path.GetFileName(document.Path), message);

    private void AddPrismSyntaxDiagnostics(IEnumerable<EmbeddedDiagnostic> syntaxDiagnostics)
    {
        foreach (EmbeddedDiagnostic diagnostic in syntaxDiagnostics)
        {
            AddPrismDiagnostic(diagnostic.Id, diagnostic.Span, diagnostic.Message);
        }
    }

    private static bool TryNormalizePrismType(string typeName, out string normalized)
    {
        normalized = typeName switch
        {
            "bool" or "boolean" => "boolean",
            "int" or "integer" => "integer",
            "float" or "number" => "number",
            "color" => "color",
            "vector" or "vector4" => "vector",
            "symbol" => "symbol",
            "resource" => "resource",
            _ => string.Empty
        };
        return normalized.Length > 0;
    }

    private ILanguageTypeSymbol? ResolvePrismType(string typeName) => typeName switch
    {
        "boolean" => compilation.FindType("System.Boolean"),
        "integer" => compilation.FindType("System.Int32"),
        "number" => compilation.FindType("System.Single"),
        "color" => compilation.FindType("Cerneala.Drawing.Color"),
        "symbol" => compilation.FindType("System.String"),
        "resource" or "vector" => compilation.FindType("System.Object"),
        _ => ResolveIntrinsicType(typeName)
    };

    private static bool CanConvertPrismType(string source, string target) =>
        source == target || source == "integer" && target == "number";

    private static PrismCatalogProperty SyntheticPrismProperty(string name, string typeName, bool required) =>
        new(name, typeName, required, "none", null, null, Array.Empty<string>(), defaultValue: null);

    private static bool IsHexColor(string value)
    {
        if (value.Length is not (7 or 9) || value[0] != '#')
        {
            return false;
        }

        return value.Skip(1).All(character => Uri.IsHexDigit(character));
    }

    private static string CanonicalPrismDomain(PrismCatalogProperty schema) =>
        schema.DomainKind + ":" +
        (schema.Minimum.HasValue ? schema.Minimum.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty) + ":" +
        (schema.Maximum.HasValue ? schema.Maximum.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty);

    private static bool IsPrismVector(
        string value,
        out float x,
        out float y)
    {
        x = default;
        y = default;
        if (value.Length < 2 || value[0] != '(' || value[value.Length - 1] != ')')
        {
            return false;
        }

        string[] components = value.Substring(1, value.Length - 2).Split(',');
        if (components.Length is < 2 or > 4)
        {
            return false;
        }

        for (int index = 0; index < components.Length; index++)
        {
            if (!float.TryParse(
                    components[index].Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float number) ||
                float.IsNaN(number) ||
                float.IsInfinity(number))
            {
                return false;
            }

            if (index == 0)
            {
                x = number;
            }
            else if (index == 1)
            {
                y = number;
            }
        }

        return true;
    }

    private sealed class PrismClipDefinition
    {
        public PrismClipDefinition(string name, ElementSyntax source)
        {
            Name = name;
            Source = source;
        }

        public string Name { get; }

        public ElementSyntax Source { get; }

        public Dictionary<string, PrismParameterDefinition> Parameters { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, PrismNodeDefinition> Nodes { get; } = new(StringComparer.Ordinal);

        public List<PrismNodeDefinition> RootNodes { get; } = new();
    }

    private sealed class PrismNodeDefinition
    {
        public PrismNodeDefinition(string name, string path, PrismContainerModelKind kind, TextSpan span)
        {
            Name = name;
            Path = path;
            Kind = kind;
            Span = span;
        }

        public string Name { get; }

        public string Path { get; }

        public PrismContainerModelKind Kind { get; }

        public TextSpan Span { get; }

        public List<PrismBoundProperty> Properties { get; } = new();

        public List<PrismNodeDefinition> Children { get; } = new();
    }

    private sealed class PrismParameterDefinition
    {
        public PrismParameterDefinition(string name, string path, string typeName, PrismValueModelSyntax? defaultValue, TextSpan span)
        {
            Name = name;
            Path = path;
            TypeName = typeName;
            DefaultValue = defaultValue;
            Span = span;
        }

        public string Name { get; }

        public string Path { get; }

        public string TypeName { get; }

        public PrismValueModelSyntax? DefaultValue { get; }

        public TextSpan Span { get; }
    }

    private sealed class PrismParameterScope
    {
        private readonly Dictionary<string, PrismParameterDefinition> parameters = new(StringComparer.Ordinal);

        public PrismParameterScope(PrismParameterScope? parent)
        {
            Parent = parent;
        }

        public PrismParameterScope? Parent { get; }

        public bool ContainsLocal(string name) => parameters.ContainsKey(name);

        public void Add(PrismParameterDefinition parameter) => parameters.Add(parameter.Name, parameter);

        public PrismParameterDefinition? Resolve(string name)
        {
            for (PrismParameterScope? scope = this; scope is not null; scope = scope.Parent)
            {
                if (scope.parameters.TryGetValue(name, out PrismParameterDefinition? parameter))
                {
                    return parameter;
                }
            }

            return null;
        }
    }

    private sealed class PrismBoundProperty
    {
        public PrismBoundProperty(PrismCatalogProperty schema, TextSpan nameSpan, PrismValueModelSyntax value)
        {
            Schema = schema;
            NameSpan = nameSpan;
            Value = value;
        }

        public PrismCatalogProperty Schema { get; }

        public TextSpan NameSpan { get; }

        public PrismValueModelSyntax Value { get; }
    }
}
