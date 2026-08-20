using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private bool TryBindPropertyValue(
        ILanguageTypeSymbol ownerType,
        ElementSyntax element,
        AttributeSyntax attribute,
        ILanguageMemberSymbol target,
        string value,
        ILanguageTypeSymbol? dataType,
        out ILanguageTypeSymbol? resultType)
    {
        resultType = null;
        TextSpan contentSpan = AttributeContentSpan(attribute);
        if (TryBindDirectResourceReference(element, target, value, contentSpan, out resultType))
        {
            return true;
        }

        if (!ContainsBinding(value) || IsEncodedMarkupLiteral(value, target))
        {
            return false;
        }

        EmbeddedParseResult<BindingValueSyntax> parsed = BindingSyntaxParser.Parse(value, contentSpan.Start);
        if (parsed.Diagnostics.Count > 0 || parsed.Syntax.Kind == BindingValueKind.Invalid)
        {
            EmbeddedDiagnostic diagnostic = parsed.Diagnostics.FirstOrDefault() ?? new EmbeddedDiagnostic(
                "CERNEALAUI007",
                "The binding expression is incomplete.",
                parsed.Syntax.Span);
            AddBindingDiagnostic(value, diagnostic.Span, diagnostic.Message);
            return true;
        }

        if (parsed.Syntax.Kind == BindingValueKind.Direct && parsed.Syntax.Binding is BindingPathSyntax direct)
        {
            BindingResolution? resolution = ResolveBindingPath(
                element,
                direct,
                dataType,
                validateClrObservability: direct.ModeSpan.Length > 0);
            if (resolution is null)
            {
                return true;
            }

            resultType = resolution.Type;
            ValidateBindingCompatibility(value, direct, resolution, target, ownerType, element);
            AddBindingModeSymbol(direct, resolution.Type);
            if (target.Name == "ItemsSource")
            {
                itemsSourceTypes[element] = resolution.Type?.CollectionElementType ??
                    resolution.Type?.TypeArguments.FirstOrDefault();
            }

            return true;
        }

        if (parsed.Syntax.Kind == BindingValueKind.Interpolation)
        {
            if (target.ValueTypeMetadataName.TrimEnd('?') is not "string" and not "System.String")
            {
                AddBindingDiagnostic(value, contentSpan, "An interpolated string binding requires a string target property.");
                return true;
            }

            foreach (BindingFragmentSyntax fragment in parsed.Syntax.Fragments.OfType<BindingFragmentSyntax>())
            {
                ResolveBindingPath(element, fragment.Binding, dataType);
            }

            return true;
        }

        return false;
    }

    private bool TryBindDirectResourceReference(
        ElementSyntax element,
        ILanguageMemberSymbol target,
        string value,
        TextSpan contentSpan,
        out ILanguageTypeSymbol? resultType)
    {
        resultType = null;
        if (value.Length < 2 || value[0] != '$' || value.IndexOf('.') >= 0 || value.IndexOf(':') >= 0)
        {
            return false;
        }

        string name = value.Substring(1);
        if (name is "DataContext" or "root" or "self" or "owner")
        {
            return false;
        }

        ResourceDefinition? resource = FindResource(element, name);
        if (resource is null)
        {
            AddDiagnostic("CERNEALAUI004", contentSpan, element.Name, target.Name, value);
            return true;
        }

        ILanguageTypeSymbol? resourceType = resource.Kind == ResourceKind.Aspect ? resource.TargetType : resource.Type;
        TextSpan referenceSpan = new(contentSpan.Start + 1, name.Length);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ResourceReference,
            name,
            resourceType?.MetadataName ?? "System.Object",
            referenceSpan,
            resourceType,
            definitionLocation: resource.Location));
        resultType = resourceType;
        if (resource.Kind == ResourceKind.Aspect || !IsTypeCompatible(resourceType, target))
        {
            AddDiagnostic("CERNEALAUI004", contentSpan, element.Name, target.Name, value);
        }

        return true;
    }

    private BindingResolution? ResolveBindingPath(
        ElementSyntax source,
        BindingPathSyntax path,
        ILanguageTypeSymbol? dataType,
        bool validateClrObservability = true)
    {
        if (path.Segments.Count == 0)
        {
            AddBindingDiagnostic(path.Text, path.Span, "A binding path requires a source.");
            return null;
        }

        bool templatePartCandidate = path.Segments.Count >= 2 &&
            string.Equals(path.Segments[1].Name, "parts", StringComparison.OrdinalIgnoreCase);
        if (templatePartCandidate && !IsTemplatePartPath(path))
        {
            AddBindingDiagnostic(path.Text, path.Span, "Template parts use $control.parts.$part.Property; 'parts' is lowercase.");
            return null;
        }

        if (IsTemplatePartPath(path))
        {
            return ResolveTemplatePartPath(source, path);
        }

        BindingPathSegmentSyntax sourceSegment = path.Segments[0];
        string sourceName = sourceSegment.Name.TrimStart('$');
        ILanguageTypeSymbol? currentType;
        LanguageSourceLocation? sourceLocation = null;
        CernealaSemanticSymbolKind sourceKind = CernealaSemanticSymbolKind.BindingSource;
        bool validateObservability = false;
        bool allowPropertyChain = false;
        bool requiresUiProperty = false;
        if (sourceName == "DataContext")
        {
            currentType = dataType;
            validateObservability = validateClrObservability;
            allowPropertyChain = true;
            if (currentType is null)
            {
                AddBindingDiagnostic(path.Text, sourceSegment.Span, "DataType is required on the root element or current ContentTemplate.");
                return null;
            }
        }
        else if (sourceName == "root")
        {
            currentType = rootType;
            requiresUiProperty = true;
        }
        else if (sourceName == "self")
        {
            currentType = resourceElements.TryGetValue(source, out ResourceDefinition? aspect) &&
                aspect.Kind == ResourceKind.Aspect
                ? aspect.TargetType
                : GetElementType(source, ReferenceEquals(source, root));
            requiresUiProperty = true;
        }
        else if (sourceName == "owner")
        {
            currentType = templateContexts.TryGetValue(source, out SemanticTemplateContext? template)
                ? template.OwnerType
                : null;
            if (currentType is null)
            {
                AddBindingDiagnostic(path.Text, sourceSegment.Span, "$owner is available only inside @template.");
                return null;
            }
        }
        else if (FindNamedElement(source, sourceName) is NamedElementDefinition named)
        {
            currentType = named.Type;
            sourceLocation = new LanguageSourceLocation(document.Path, named.Span);
            requiresUiProperty = true;
        }
        else if (FindResource(source, sourceName) is ResourceDefinition resource)
        {
            currentType = resource.Type;
            sourceLocation = resource.Location;
            sourceKind = CernealaSemanticSymbolKind.ResourceReference;
        }
        else if (templateContexts.ContainsKey(source) && nameScopes
            .Where(pair => !templateContexts.ContainsKey(pair.Key))
            .Select(pair => pair.Value)
            .Any(scope => scope.Elements.ContainsKey(sourceName)))
        {
            AddBindingDiagnostic(path.Text, sourceSegment.Span, "The named element is outside the current template name scope.");
            return null;
        }
        else
        {
            AddBindingDiagnostic(path.Text, sourceSegment.Span, "Unknown named element.");
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            sourceKind,
            sourceName,
            currentType?.MetadataName ?? "System.Object",
            sourceSegment.Span,
            currentType,
            definitionLocation: sourceLocation));

        ILanguageMemberSymbol? finalMember = null;
        for (int index = 1; index < path.Segments.Count; index++)
        {
            BindingPathSegmentSyntax segment = path.Segments[index];
            if (!allowPropertyChain && index > 1)
            {
                AddBindingDiagnostic(path.Text, segment.Span, "This binding source supports exactly one UI property segment.");
                return null;
            }

            if (currentType is null)
            {
                AddBindingDiagnostic(path.Text, segment.Span, "The preceding binding segment has no resolvable type.");
                return null;
            }

            if (validateObservability && !IsObservablePathOwner(currentType))
            {
                AddBindingDiagnostic(
                    path.Text,
                    segment.Span,
                    "CLR path owner '" + currentType.MetadataName + "' must implement INotifyPropertyChanged.");
                return null;
            }

            string memberName = segment.Name.TrimStart('$');
            ILanguageMemberSymbol? member = FindProperty(currentType, memberName);
            if (member is null || !member.CanRead)
            {
                AddBindingDiagnostic(
                    path.Text,
                    segment.Span,
                    requiresUiProperty
                        ? "No supported UI property '" + memberName + "' exists on '" + currentType.MetadataName + "'."
                        : "Readable property '" + memberName + "' was not found on '" + currentType.MetadataName + "'.");
                return null;
            }

            currentType = member.ValueType;
            finalMember = member;
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.BindingSegment,
                memberName,
                member.ValueTypeMetadataName,
                segment.Span,
                currentType,
                member,
                definitionLocation: member.Locations.FirstOrDefault(),
                isWritable: member.CanWrite));
        }

        return new BindingResolution(currentType, finalMember, finalMember?.CanWrite == true);
    }

    private BindingResolution? ResolveTemplatePartPath(ElementSyntax source, BindingPathSyntax path)
    {
        BindingPathSegmentSyntax ownerSegment = path.Segments[0];
        string ownerName = ownerSegment.Name.TrimStart('$');
        NamedElementDefinition? owner = FindNamedElement(source, ownerName);
        if (owner is null)
        {
            AddBindingDiagnostic(path.Text, ownerSegment.Span, "Unknown named control.");
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.BindingSource,
            ownerName,
            owner.Type?.MetadataName ?? "System.Object",
            ownerSegment.Span,
            owner.Type,
            definitionLocation: new LanguageSourceLocation(document.Path, owner.Span)));
        BindingPathSegmentSyntax partsSegment = path.Segments[1];
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.BindingSegment,
            "parts",
            owner.Type?.MetadataName ?? "System.Object",
            partsSegment.Span,
            owner.Type));

        string partName = path.Segments[2].Name.TrimStart('$');
        NamedElementDefinition? part = templateContexts.Values
            .Distinct()
            .Where(context => ReferenceEquals(context.OwnerElement, owner.Element))
            .Select(context => context.Parts.TryGetValue(partName, out NamedElementDefinition? candidate) ? candidate : null)
            .FirstOrDefault(candidate => candidate is not null);
        if (part is null)
        {
            AddBindingDiagnostic(path.Text, path.Segments[2].Span, "The named control template has no part named '" + partName + "'.");
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.TemplatePart,
            partName,
            part.Type?.MetadataName ?? "System.Object",
            path.Segments[2].Span,
            part.Type,
            definitionLocation: new LanguageSourceLocation(document.Path, part.Span)));
        ILanguageTypeSymbol? currentType = part.Type;
        ILanguageMemberSymbol? finalMember = null;
        for (int index = 3; index < path.Segments.Count; index++)
        {
            BindingPathSegmentSyntax segment = path.Segments[index];
            string memberName = segment.Name.TrimStart('$');
            ILanguageMemberSymbol? member = FindProperty(currentType, memberName);
            if (member is null || !member.CanRead)
            {
                AddBindingDiagnostic(path.Text, segment.Span, "No supported UI property with this name exists on the template part.");
                return null;
            }

            currentType = member.ValueType;
            finalMember = member;
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.BindingSegment,
                memberName,
                member.ValueTypeMetadataName,
                segment.Span,
                currentType,
                member,
                definitionLocation: member.Locations.FirstOrDefault(),
                isWritable: member.CanWrite));
        }

        return new BindingResolution(currentType, finalMember, finalMember?.CanWrite == true);
    }

    private void ValidateBindingCompatibility(
        string expression,
        BindingPathSyntax path,
        BindingResolution source,
        ILanguageMemberSymbol target,
        ILanguageTypeSymbol ownerType,
        ElementSyntax element)
    {
        if (!target.CanWrite && target.Kind == LanguageMemberKind.Property)
        {
            AddBindingSemanticDiagnostic(element, expression, path.Span, "The target UI property is read-only.");
            return;
        }

        if (path.Segments.Count == 2 && path.Segments[0].Name.TrimStart('$') == "self" &&
            source.Member?.Name == target.Name)
        {
            AddBindingSemanticDiagnostic(element, expression, path.Span, "A UI property cannot bind directly to itself.");
            return;
        }

        bool stringProjection = target.ValueTypeMetadataName.TrimEnd('?') is "string" or "System.String" &&
            path.Mode == BindingModeSyntax.OneWay;
        bool dataContextAssignment = target.Name == "DataContext" &&
            target.ValueTypeMetadataName.TrimEnd('?') is "object" or "System.Object";
        if (target.ValueTypeMetadataName.TrimEnd('?') is "string" or "System.String" &&
            path.Mode == BindingModeSyntax.TwoWay &&
            source.Type?.MetadataName.TrimEnd('?') is not ("string" or "System.String"))
        {
            AddBindingSemanticDiagnostic(element, expression, path.ModeSpan, "String projection bindings are OneWay only.");
            return;
        }

        if (!IsTypeCompatible(source.Type, target) && !stringProjection && !dataContextAssignment)
        {
            AddBindingSemanticDiagnostic(
                element,
                expression,
                path.Span,
                "Source type '" + (source.Type?.MetadataName ?? "<unknown>") +
                "' is not compatible with target type '" + target.ValueTypeMetadataName + "'.");
            return;
        }

        if (path.Mode == BindingModeSyntax.TwoWay && !source.CanWrite)
        {
            AddBindingSemanticDiagnostic(
                element,
                expression,
                path.ModeSpan.Length == 0 ? path.Span : path.ModeSpan,
                "TwoWay requires a writable source endpoint.");
        }
    }

    private void AddBindingSemanticDiagnostic(ElementSyntax source, string expression, TextSpan span, string message)
    {
        if (templateContexts.TryGetValue(source, out SemanticTemplateContext? template) && !template.IsContentTemplate)
        {
            AddDiagnostic("CERNEALAUI012", span, Path.GetFileName(document.Path), message);
            return;
        }

        AddBindingDiagnostic(expression, span, message);
    }

    private void AddBindingModeSymbol(BindingPathSyntax path, ILanguageTypeSymbol? type)
    {
        if (path.ModeSpan.Length <= 1)
        {
            return;
        }

        TextSpan valueSpan = new(path.ModeSpan.Start + 1, path.ModeSpan.Length - 1);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.BindingMode,
            path.Mode.ToString(),
            "Cerneala.UI.Data.BindingMode",
            valueSpan,
            type,
            value: path.Mode));
    }

    private void BindAspectApplication(
        ILanguageTypeSymbol elementType,
        ElementSyntax element,
        AttributeSyntax attribute,
        ILanguageTypeSymbol? dataType)
    {
        string value = Unquote(attribute.ValueToken.Text).Trim();
        TextSpan span = AttributeContentSpan(attribute);
        if (!value.StartsWith("$", StringComparison.Ordinal) || value.Length == 1 || value.IndexOf('.') >= 0)
        {
            AddDiagnostic("CERNEALAUI004", span, element.Name, "Aspect", value);
            return;
        }

        string name = value.Substring(1);
        ResourceDefinition? resource = FindResource(element, name);
        if (resource?.Kind != ResourceKind.Aspect || resource.TargetType is null ||
            !elementType.IsOrDerivesFrom(resource.TargetType.MetadataName))
        {
            AddDiagnostic("CERNEALAUI004", span, element.Name, "Aspect", value);
            return;
        }

        TextSpan referenceSpan = new(span.Start + 1, name.Length);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ResourceReference,
            name,
            resource.TargetType.MetadataName,
            referenceSpan,
            resource.TargetType,
            definitionLocation: resource.Location));
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.AspectApplication,
            name,
            resource.TargetType.MetadataName,
            attribute.NameToken.Span,
            resource.TargetType,
            definitionLocation: resource.Location));
    }

    private void BindAspectResource(
        ResourceDefinition aspect,
        ILanguageTypeSymbol? dataType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AttributeSyntax? targetAttribute = FindAttribute(aspect.Element, "TargetType");
        if (aspect.TargetType is null)
        {
            AddDiagnostic(
                "CERNEALAUI004",
                targetAttribute?.ValueToken.Span ?? aspect.Element.NameToken.Span,
                "Aspect",
                "TargetType",
                targetAttribute is null ? string.Empty : Unquote(targetAttribute.ValueToken.Text));
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Aspect,
            aspect.Name ?? aspect.TargetType.Name,
            aspect.TargetType.MetadataName,
            aspect.NameSpan,
            aspect.TargetType,
            definitionLocation: aspect.Location));
        if (targetAttribute is not null)
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.TypeReference,
                aspect.TargetType.Name,
                aspect.TargetType.MetadataName,
                AttributeContentSpan(targetAttribute),
                aspect.TargetType,
                definitionLocation: aspect.TargetType.Locations.FirstOrDefault()));
        }

        (string text, int offset) = BuildDirectTextBuffer(aspect.Element);
        EmbeddedParseResult<DirectiveDocumentSyntax> parsed = DirectiveSyntaxParser.Parse(text, offset);
        foreach (EmbeddedDiagnostic diagnostic in parsed.Diagnostics.Where(diagnostic =>
            ShouldBindAspectAssignment(aspect.Element, diagnostic.Span.Start)))
        {
            AddDiagnostic(
                diagnostic.Id,
                diagnostic.Span,
                Path.GetFileName(document.Path),
                diagnostic.Message);
        }

        foreach (DirectiveSyntax directive in parsed.Syntax.Directives.Where(directive =>
            directive.Keyword is "@when" or "@if" or "@default" or "@template"))
        {
            CernealaSemanticSymbolKind kind = directive.Keyword is "@when" or "@if"
                ? CernealaSemanticSymbolKind.AspectCondition
                : CernealaSemanticSymbolKind.Aspect;
            symbols.Add(new CernealaSemanticSymbol(
                kind,
                directive.Keyword,
                aspect.TargetType.MetadataName,
                directive.Span,
                aspect.TargetType,
                definitionLocation: aspect.Location));
            if (kind == CernealaSemanticSymbolKind.AspectCondition)
            {
                BindConditionPaths(
                    aspect.Element,
                    parsed.Syntax.Text,
                    parsed.Syntax.AbsoluteOffset,
                    directive,
                    aspect.TargetType,
                    dataType);
            }
        }

        BindMotionAspect(aspect, cancellationToken);

        IReadOnlyList<DirectiveBlock> defaultBlocks = FindDirectiveBlocks(document.Text.ToString(), "@default");
        Dictionary<int, HashSet<string>> assignedByDefault = new();
        foreach (AssignmentSyntax assignment in parsed.Syntax.Assignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ShouldBindAspectAssignment(aspect.Element, assignment.NameSpan.Start))
            {
                continue;
            }

            string propertyName = assignment.Name.Contains('.')
                ? assignment.Name.Substring(assignment.Name.LastIndexOf('.') + 1)
                : assignment.Name;
            ILanguageMemberSymbol? member = FindProperty(aspect.TargetType, propertyName);
            if (member is null)
            {
                AddDiagnostic("CERNEALAUI003", assignment.NameSpan, aspect.TargetType.Name, propertyName);
                continue;
            }

            DirectiveBlock? defaultBlock = null;
            foreach (DirectiveBlock candidate in defaultBlocks)
            {
                if (candidate.BodySpan.Contains(assignment.NameSpan.Start))
                {
                    defaultBlock = candidate;
                    break;
                }
            }

            if (defaultBlock is DirectiveBlock block)
            {
                if (!assignedByDefault.TryGetValue(block.KeywordSpan.Start, out HashSet<string>? assigned))
                {
                    assigned = new HashSet<string>(StringComparer.Ordinal);
                    assignedByDefault.Add(block.KeywordSpan.Start, assigned);
                }

                if (!assigned.Add(propertyName))
                {
                    AddShapeDiagnostic(assignment.NameSpan, "Aspect assigns property '" + propertyName + "' more than once in @default.");
                    continue;
                }
            }

            string rawValue = document.Text.Substring(assignment.ValueSpan).Trim();
            TextSpan valueSpan = TrimmedSpan(assignment.ValueSpan);
            if (rawValue.StartsWith("$", StringComparison.Ordinal) &&
                rawValue.IndexOf('.') < 0 && rawValue.IndexOf(':') < 0)
            {
                string resourceName = rawValue.Substring(1);
                ResourceDefinition? referenced = FindResource(aspect.Element, resourceName);
                if (referenced is null)
                {
                    AddDiagnostic("CERNEALAUI004", valueSpan, aspect.TargetType.Name, propertyName, rawValue);
                }
                else
                {
                    symbols.Add(new CernealaSemanticSymbol(
                        CernealaSemanticSymbolKind.ResourceReference,
                        resourceName,
                        referenced.Type?.MetadataName ?? "System.Object",
                        new TextSpan(valueSpan.Start + 1, resourceName.Length),
                        referenced.Type,
                        definitionLocation: referenced.Location));
                }
            }
            else if (rawValue.StartsWith("$", StringComparison.Ordinal))
            {
                BindingResolution? resolution = ResolveDirectiveReference(
                    aspect.Element,
                    rawValue,
                    valueSpan,
                    dataType,
                    out BindingPathSyntax? path);
                if (resolution is not null && path is not null)
                {
                    if (!IsTypeCompatible(resolution.Type, member))
                    {
                        AddDiagnostic("CERNEALAUI004", valueSpan, aspect.TargetType.Name, propertyName, rawValue);
                    }

                    if (path.ModeSpan.Length > 0)
                    {
                        if (path.Mode == BindingModeSyntax.TwoWay && !resolution.CanWrite)
                        {
                            AddBindingSemanticDiagnostic(
                                aspect.Element,
                                rawValue,
                                path.ModeSpan,
                                "TwoWay requires a writable source endpoint.");
                        }

                        AddBindingModeSymbol(path, resolution.Type);
                    }
                }
            }
            else if (!TryConvertLiteral(Unquote(rawValue), member, out _))
            {
                AddDiagnostic("CERNEALAUI004", valueSpan, aspect.TargetType.Name, propertyName, rawValue);
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.AspectAssignment,
                propertyName,
                member.ValueTypeMetadataName,
                assignment.NameSpan,
                member.ValueType,
                member,
                rawValue,
                isWritable: member.CanWrite));
        }
    }

    private BindingResolution? ResolveDirectiveReference(
        ElementSyntax source,
        string expression,
        TextSpan span,
        ILanguageTypeSymbol? dataType,
        out BindingPathSyntax? path)
    {
        EmbeddedParseResult<BindingValueSyntax> parsed = BindingSyntaxParser.Parse(expression, span.Start);
        path = parsed.Syntax.Kind == BindingValueKind.Direct ? parsed.Syntax.Binding : null;
        if (parsed.Diagnostics.Count > 0 || path is null)
        {
            EmbeddedDiagnostic diagnostic = parsed.Diagnostics.FirstOrDefault() ?? new EmbeddedDiagnostic(
                "CERNEALAUI007",
                "The reference expression is incomplete.",
                span);
            AddBindingDiagnostic(expression, diagnostic.Span, diagnostic.Message);
            return null;
        }

        return ResolveBindingPath(
            source,
            path,
            dataType,
            validateClrObservability: path.ModeSpan.Length > 0);
    }

    private void BindConditionPaths(
        ElementSyntax source,
        string text,
        int absoluteOffset,
        DirectiveSyntax directive,
        ILanguageTypeSymbol targetType,
        ILanguageTypeSymbol? dataType)
    {
        int relativeStart = directive.Span.End - absoluteOffset;
        int brace = text.IndexOf('{', Math.Max(0, relativeStart));
        int end = brace < 0 ? text.Length : brace;
        foreach ((string name, TextSpan span) in ExtractConditionPropertyCandidates(
            text,
            relativeStart,
            end,
            absoluteOffset))
        {
            ILanguageMemberSymbol? member = FindProperty(targetType, name);
            if (member is null || !member.CanRead)
            {
                continue;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.AspectConditionProperty,
                name,
                member.ValueTypeMetadataName,
                span,
                member.ValueType,
                member,
                definitionLocation: member.Locations.FirstOrDefault(),
                isWritable: member.CanWrite));
        }

        foreach ((string expression, int offset) in ExtractBindingExpressions(text, relativeStart, end, absoluteOffset))
        {
            EmbeddedParseResult<BindingValueSyntax> parsed = BindingSyntaxParser.Parse(expression, offset);
            if (parsed.Syntax.Binding is not null && parsed.Diagnostics.Count == 0)
            {
                ResolveBindingPath(source, parsed.Syntax.Binding, dataType);
            }
            else if (parsed.Diagnostics.FirstOrDefault() is EmbeddedDiagnostic diagnostic)
            {
                AddBindingDiagnostic(expression, diagnostic.Span, diagnostic.Message);
            }
        }
    }

    private static IEnumerable<(string Name, TextSpan Span)> ExtractConditionPropertyCandidates(
        string text,
        int start,
        int end,
        int absoluteOffset)
    {
        int position = Math.Max(0, start);
        while (position < end && position < text.Length)
        {
            char character = text[position];
            if (char.IsWhiteSpace(character) || character is '(' or ')' or '<' or '>' or '=' or '!')
            {
                position++;
                continue;
            }

            if (character == '"')
            {
                position++;
                bool escaped = false;
                while (position < end && position < text.Length)
                {
                    char quoted = text[position++];
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (quoted == '\\')
                    {
                        escaped = true;
                    }
                    else if (quoted == '"')
                    {
                        break;
                    }
                }

                continue;
            }

            int candidateStart = position;
            while (position < end && position < text.Length &&
                !char.IsWhiteSpace(text[position]) &&
                text[position] is not '(' and not ')' and not '<' and not '>' and not '=' and not '!')
            {
                position++;
            }

            string candidate = text.Substring(candidateStart, position - candidateStart);
            if (candidate.Length == 0 || candidate[0] == '$' || candidate is "and" or "or" or "value" ||
                !candidate.All(current => char.IsLetterOrDigit(current) || current == '_'))
            {
                continue;
            }

            yield return (
                candidate,
                new TextSpan(absoluteOffset + candidateStart, candidate.Length));
        }
    }

    private (string Text, int Offset) BuildDirectTextBuffer(ElementSyntax element)
    {
        int start = element.OpenEndToken.Span.End;
        int end = element.CloseLessThanToken.IsMissing ? element.Span.End : element.CloseLessThanToken.Span.Start;
        if (end <= start)
        {
            return (string.Empty, start);
        }

        char[] buffer = Enumerable.Repeat(' ', end - start).ToArray();
        foreach (TextSyntax text in element.Children.OfType<TextSyntax>())
        {
            string value = document.Text.Substring(text.Span);
            value.CopyTo(0, buffer, text.Span.Start - start, value.Length);
        }

        return (new string(buffer), start);
    }

    private TextSpan TrimmedSpan(TextSpan span)
    {
        string value = document.Text.Substring(span);
        int leading = value.Length - value.TrimStart().Length;
        int trailing = value.Length - value.TrimEnd().Length;
        return new TextSpan(span.Start + leading, Math.Max(0, span.Length - leading - trailing));
    }

    private static IEnumerable<(string Expression, int Offset)> ExtractBindingExpressions(
        string text,
        int start,
        int end,
        int absoluteOffset)
    {
        int position = Math.Max(0, start);
        while (position < end && position < text.Length)
        {
            if (text[position] != '$')
            {
                position++;
                continue;
            }

            int expressionStart = position++;
            while (position < end && position < text.Length &&
                (char.IsLetterOrDigit(text[position]) || text[position] is '_' or '.' or '$' or ':'))
            {
                position++;
            }

            yield return (text.Substring(expressionStart, position - expressionStart), absoluteOffset + expressionStart);
        }
    }

    private static bool IsTemplatePartPath(BindingPathSyntax path) =>
        path.Segments.Count >= 4 && path.Segments[1].Name == "parts" &&
        path.Segments[2].Name.StartsWith("$", StringComparison.Ordinal);

    private static bool ContainsBinding(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            if (value[index] == '$' && (index == 0 || value[index - 1] != '\\'))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEncodedMarkupLiteral(string value, ILanguageMemberSymbol target) =>
        target.ValueTypeMetadataName.TrimEnd('?') is "string" or "System.String" &&
        (value.IndexOf("&lt;", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("&#x3C;", StringComparison.OrdinalIgnoreCase) >= 0);

    private static bool IsObservablePathOwner(ILanguageTypeSymbol type) =>
        type.IsOrImplements("Cerneala.UI.Core.UiObject") ||
        type.IsOrImplements("System.ComponentModel.INotifyPropertyChanged");

    private static bool IsTypeCompatible(ILanguageTypeSymbol? source, ILanguageMemberSymbol target)
    {
        string targetName = target.ValueTypeMetadataName.TrimEnd('?');
        if (targetName is "object" or "System.Object")
        {
            return true;
        }

        if (source is null)
        {
            return false;
        }

        string sourceName = source.MetadataName.TrimEnd('?');
        return string.Equals(sourceName, targetName, StringComparison.Ordinal) ||
            source.IsOrImplements(targetName);
    }

    private sealed class BindingResolution
    {
        public BindingResolution(ILanguageTypeSymbol? type, ILanguageMemberSymbol? member, bool canWrite)
        {
            Type = type;
            Member = member;
            CanWrite = canWrite;
        }

        public ILanguageTypeSymbol? Type { get; }

        public ILanguageMemberSymbol? Member { get; }

        public bool CanWrite { get; }
    }
}
