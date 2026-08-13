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
    private static readonly Lazy<PrismLanguageCatalog> PrismCatalog =
        new(PrismLanguageCatalog.LoadDefault);

    private readonly Dictionary<ResourceDefinition, MotionSpecDefinition> motionSpecs = new();
    private readonly Dictionary<ResourceDefinition, MotionClipDefinition> motionClips = new();
    private readonly Dictionary<ResourceDefinition, PrismCompositionDefinition> prismCompositions = new();
    private readonly Dictionary<ElementSyntax, PrismCompositionDefinition> prismApplications = new();
    private readonly HashSet<ElementSyntax> boundEmbeddedResources = new();
    private readonly HashSet<ElementSyntax> boundPrismApplications = new();

    private void PrepareMotionPrismResources(CancellationToken cancellationToken)
    {
        foreach (ResourceDefinition resource in resourceElements.Values
            .Distinct()
            .Where(candidate => candidate.Kind is ResourceKind.MotionSpec or ResourceKind.MotionClip or ResourceKind.PrismComposition)
            .OrderBy(candidate => candidate.Element.Span.Start))
        {
            BindEmbeddedResource(resource, cancellationToken);
        }

        foreach (ElementSyntax element in document.Syntax.DescendantElements().OrderBy(candidate => candidate.Span.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            (string text, int offset) = BuildDirectTextBuffer(element);
            if (text.IndexOf("@prism", StringComparison.Ordinal) >= 0)
            {
                BindPrismApplications(element, text, offset, cancellationToken);
            }
        }
    }

    private void BindEmbeddedResource(ResourceDefinition resource, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!boundEmbeddedResources.Add(resource.Element))
        {
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Resource,
            resource.Name ?? resource.Element.Name,
            resource.Type?.MetadataName ?? "System.Object",
            resource.NameSpan,
            resource.Type,
            definitionLocation: resource.Location));

        switch (resource.Kind)
        {
            case ResourceKind.MotionSpec:
                BindMotionSpecResource(resource);
                break;
            case ResourceKind.MotionClip:
                BindMotionClipResource(resource, cancellationToken);
                break;
            case ResourceKind.PrismComposition:
                BindPrismCompositionResource(resource, cancellationToken);
                break;
        }
    }

    private void BindMotionSpecResource(ResourceDefinition resource)
    {
        ElementSyntax element = resource.Element;
        bool valid = resource.Name is not null;
        if (resource.Name is null)
        {
            AddShapeDiagnostic(element.NameToken.Span, element.Name + " resource requires a non-empty Name.");
        }

        if (element.Name == "Tween")
        {
            valid &= ValidateDurationAttribute(element, "Duration", required: true, allowZero: false);
            valid &= ValidateDurationAttribute(element, "Delay", required: false, allowZero: true);
            valid &= ValidateEnumAttribute(element, "Easing", ["Linear", "Standard", "Emphasized", "EaseIn", "EaseOut", "EaseInOut", "Sharp"]);
            valid &= ValidateEnumAttribute(element, "FillMode", ["None", "Backwards", "Forwards", "Both"]);
        }
        else
        {
            valid &= ValidatePositiveFloatAttribute(element, "Stiffness", allowZero: false);
            valid &= ValidatePositiveFloatAttribute(element, "Damping", allowZero: true);
            valid &= ValidatePositiveFloatAttribute(element, "Mass", allowZero: false);
            valid &= ValidatePositiveFloatAttribute(element, "RestSpeed", allowZero: true);
            valid &= ValidatePositiveFloatAttribute(element, "RestDelta", allowZero: true);
            valid &= ValidateEnumAttribute(element, "VelocityMode", ["Preserve", "Reset"]);
        }

        MotionSpecDefinition definition = new(resource, element.Name, valid);
        motionSpecs[resource] = definition;
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionSpec,
            resource.Name ?? element.Name,
            "Cerneala.UI.Motion.Specs.MotionSpec",
            resource.NameSpan,
            definitionLocation: resource.Location,
            value: element.Name));
    }

    private bool ValidateDurationAttribute(ElementSyntax element, string name, bool required, bool allowZero)
    {
        AttributeSyntax? attribute = FindAttribute(element, name);
        if (attribute is null)
        {
            if (required)
            {
                AddDiagnostic("CERNEALAUI004", element.NameToken.Span, element.Name, name, string.Empty);
                return false;
            }

            return true;
        }

        string value = Unquote(attribute.ValueToken.Text).Trim();
        if (TryParseDuration(value, allowZero))
        {
            return true;
        }

        AddDiagnostic("CERNEALAUI004", AttributeContentSpan(attribute), element.Name, name, value);
        return false;
    }

    private bool ValidatePositiveFloatAttribute(ElementSyntax element, string name, bool allowZero)
    {
        AttributeSyntax? attribute = FindAttribute(element, name);
        if (attribute is null)
        {
            return true;
        }

        string valueText = Unquote(attribute.ValueToken.Text).Trim();
        bool valid = float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) &&
            !float.IsNaN(value) && !float.IsInfinity(value) && (allowZero ? value >= 0 : value > 0);
        if (!valid)
        {
            AddDiagnostic("CERNEALAUI004", AttributeContentSpan(attribute), element.Name, name, valueText);
        }

        return valid;
    }

    private bool ValidateEnumAttribute(ElementSyntax element, string name, IReadOnlyCollection<string> values)
    {
        AttributeSyntax? attribute = FindAttribute(element, name);
        if (attribute is null)
        {
            return true;
        }

        string value = Unquote(attribute.ValueToken.Text).Trim();
        if (values.Contains(value))
        {
            return true;
        }

        AddDiagnostic("CERNEALAUI004", AttributeContentSpan(attribute), element.Name, name, value);
        return false;
    }

    private void BindMotionClipResource(ResourceDefinition resource, CancellationToken cancellationToken)
    {
        ElementSyntax element = resource.Element;
        if (root?.Name == "Application")
        {
            AddDiagnostic(
                "CERNEALAUI013",
                element.Span,
                Path.GetFileName(document.Path),
                "MotionClip is not valid in Application.Resources because Application has no visual namescope.");
            return;
        }

        AttributeSyntax? targetAttribute = FindAttribute(element, "TargetType");
        ILanguageTypeSymbol? targetType = targetAttribute is null
            ? null
            : ResolveTypeReference(targetAttribute) ?? ResolveUnqualifiedType(Unquote(targetAttribute.ValueToken.Text));
        if (targetType is null)
        {
            AddMotionDiagnostic(
                "CERNEALAUI023",
                targetAttribute is null ? element.NameToken.Span : AttributeContentSpan(targetAttribute),
                "MotionClip requires an accessible TargetType.");
        }

        (string text, int offset) = BuildDirectTextBuffer(element);
        MotionProgram program = ParseMotionProgram(text, offset);
        MotionClipDefinition clip = new(resource, targetType);
        motionClips[resource] = clip;
        BindMotionParameters(program, clip);
        BindMotionProgram(element, targetType, program, clip, isAspect: false, cancellationToken);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionComposition,
            resource.Name ?? "MotionClip",
            targetType?.MetadataName ?? "System.Object",
            resource.NameSpan,
            targetType,
            definitionLocation: resource.Location,
            value: "MotionClip"));
    }

    private void BindMotionAspect(ResourceDefinition aspect, CancellationToken cancellationToken)
    {
        (string text, int offset) = BuildDirectTextBuffer(aspect.Element);
        if (!ContainsMotionProgram(text))
        {
            return;
        }

        MotionProgram program = ParseMotionProgram(text, offset);
        ElementSyntax source = FindAspectApplicationElement(aspect) ?? aspect.Element;
        BindMotionProgram(source, aspect.TargetType, program, clip: null, isAspect: true, cancellationToken);
    }

    private ElementSyntax? FindAspectApplicationElement(ResourceDefinition aspect)
    {
        if (aspect.Name is not null)
        {
            string reference = "$" + aspect.Name;
            ElementSyntax? application = document.Syntax.DescendantElements()
                .FirstOrDefault(element => FindAttribute(element, "Aspect") is AttributeSyntax attribute &&
                    string.Equals(Unquote(attribute.ValueToken.Text).Trim(), reference, StringComparison.Ordinal));
            if (application is not null)
            {
                return application;
            }
        }

        ElementSyntax? current = aspect.Element;
        while (parents.TryGetValue(current, out ElementSyntax? parent) && parent is not null)
        {
            if (parent.Kind == SyntaxKind.PropertyElement && parent.Name.EndsWith(".Aspect", StringComparison.Ordinal) &&
                parents.TryGetValue(parent, out ElementSyntax? owner))
            {
                return owner;
            }

            current = parent;
        }

        return null;
    }

    private MotionProgram ParseMotionProgram(string text, int offset)
    {
        EmbeddedParseResult<DirectiveDocumentSyntax> parsed = MotionSyntaxParser.Parse(text, offset);
        EmbeddedDiagnostic? primaryDiagnostic = parsed.Diagnostics.FirstOrDefault();
        if (primaryDiagnostic is not null &&
            text.IndexOf("@presence", StringComparison.Ordinal) >= 0 &&
            primaryDiagnostic.Message.IndexOf("@enter", StringComparison.Ordinal) >= 0)
        {
            primaryDiagnostic = new EmbeddedDiagnostic(
                primaryDiagnostic.Id,
                "@presence does not support a custom @enter block.",
                primaryDiagnostic.Span);
        }
        if (primaryDiagnostic is not null)
        {
            AddMotionDiagnostic(primaryDiagnostic.Id, primaryDiagnostic.Span, primaryDiagnostic.Message);
        }

        DirectiveRegion[] regions = parsed.Syntax.Directives
            .Select(directive => CreateDirectiveRegion(text, offset, directive))
            .ToArray();
        return new MotionProgram(text, offset, parsed.Syntax, regions, primaryDiagnostic is not null);
    }

    private void BindMotionParameters(MotionProgram program, MotionClipDefinition clip)
    {
        bool executionSeen = false;
        foreach (DirectiveRegion region in program.Regions.OrderBy(candidate => candidate.KeywordSpan.Start))
        {
            if (region.Keyword != "@parameter")
            {
                if (region.Depth == 0 && IsMotionExecutionDirective(region.Keyword))
                {
                    executionSeen = true;
                }

                continue;
            }

            if (executionSeen)
            {
                AddMotionDiagnostic("CERNEALAUI020", region.KeywordSpan, "MotionClip parameters must be declared before execution directives.");
            }

            string header = document.Text.Substring(region.HeaderSpan).Trim().TrimEnd(';').Trim();
            int colon = header.IndexOf(':');
            if (colon <= 0)
            {
                AddMotionDiagnostic("CERNEALAUI020", region.HeaderSpan, "MotionClip parameter requires 'Name: Type'.");
                continue;
            }

            string name = header.Substring(0, colon).Trim();
            string declaration = header.Substring(colon + 1).Trim();
            int equals = declaration.IndexOf('=');
            string typeName = (equals < 0 ? declaration : declaration.Substring(0, equals)).Trim();
            string? defaultValue = equals < 0 ? null : declaration.Substring(equals + 1).Trim();
            TextSpan nameSpan = FindSubspan(region.HeaderSpan, name);
            TextSpan typeSpan = FindSubspan(region.HeaderSpan, typeName);
            if (!TryNormalizeMotionParameterType(typeName, out string normalizedType))
            {
                AddMotionDiagnostic(
                    "CERNEALAUI023",
                    typeSpan,
                    typeName.Contains("&lt;", StringComparison.Ordinal)
                        ? "Use MotionSpec[float] rather than C# generic syntax."
                        : "MotionClip parameter has an unsupported type '" + typeName + "'.");
                continue;
            }

            if (clip.Parameters.ContainsKey(name))
            {
                AddMotionDiagnostic("CERNEALAUI020", nameSpan, "Duplicate MotionClip parameter '" + name + "'.");
                continue;
            }

            MotionParameterDefinition parameter = new(name, normalizedType, defaultValue, nameSpan);
            clip.Parameters.Add(name, parameter);
            ILanguageTypeSymbol? type = ResolveIntrinsicType(normalizedType);
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionParameter,
                name,
                normalizedType,
                nameSpan,
                type,
                value: defaultValue));
            if (defaultValue is not null && !ValidateMotionParameterValue(parameter, defaultValue))
            {
                AddMotionDiagnostic("CERNEALAUI023", FindSubspan(region.HeaderSpan, defaultValue), "Default value is not compatible with MotionClip parameter '" + name + "'.");
            }
        }
    }

    private void BindMotionProgram(
        ElementSyntax source,
        ILanguageTypeSymbol? targetType,
        MotionProgram program,
        MotionClipDefinition? clip,
        bool isAspect,
        CancellationToken cancellationToken)
    {
        if (program.HasSyntaxErrors)
        {
            return;
        }

        ElementSyntax? xmlControl = document.Syntax.DescendantElements()
            .Where(element => !ReferenceEquals(element, source))
            .FirstOrDefault(element => program.Regions.Any(region =>
                IsMotionExecutionDirective(region.Keyword) && region.BodySpan.Contains(element.NameToken.Span.Start)));
        if (xmlControl is not null)
        {
            AddMotionDiagnostic(
                "CERNEALAUI020",
                xmlControl.NameToken.Span,
                "XML controls are not allowed inside Motion execution bodies.");
            return;
        }

        HashSet<string> lifecycle = new(StringComparer.Ordinal);
        Dictionary<string, TextSpan> handles = new(StringComparer.Ordinal);
        foreach (DirectiveRegion region in program.Regions.OrderBy(candidate => candidate.KeywordSpan.Start))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CernealaLanguageFacts.MotionDirectiveKeywords.Contains(region.Keyword))
            {
                continue;
            }

            CernealaSemanticSymbolKind directiveKind = region.Keyword is "@parallel" or "@sequence"
                ? CernealaSemanticSymbolKind.MotionComposition
                : region.Keyword is "@presence" or "@layout" or "@scroll" or "@drag" or "@gesture"
                    ? CernealaSemanticSymbolKind.MotionLifecycle
                    : CernealaSemanticSymbolKind.MotionDirective;
            symbols.Add(new CernealaSemanticSymbol(
                directiveKind,
                region.Keyword,
                targetType?.MetadataName ?? "System.Object",
                region.KeywordSpan,
                targetType));

            if (region.Keyword == "@parameter" && isAspect)
            {
                AddMotionDiagnostic("CERNEALAUI020", region.KeywordSpan, "@parameter is available only inside MotionClip.");
                continue;
            }

            if (region.Keyword is "@handle" or "@cancel" && !isAspect)
            {
                AddMotionDiagnostic("CERNEALAUI020", region.KeywordSpan, "MotionClip cannot contain " + region.Keyword + ".");
                continue;
            }

            if (region.Keyword == "@on")
            {
                if (!isAspect)
                {
                    AddMotionDiagnostic("CERNEALAUI020", region.KeywordSpan, "@on is available only inside Aspect.");
                }
                else
                {
                    BindMotionEvent(source, targetType, region);
                }
            }

            if (region.Keyword is "@presence" or "@layout" or "@scroll" or "@drag" or "@gesture")
            {
                if (!isAspect)
                {
                    AddMotionDiagnostic("CERNEALAUI025", region.KeywordSpan, region.Keyword + " is available only inside Aspect.");
                }
                else if (!lifecycle.Add(region.Keyword))
                {
                    AddMotionDiagnostic(
                        "CERNEALAUI025",
                        new TextSpan(program.Offset, 0),
                        "An Aspect may declare only one " + region.Keyword + " block.");
                }
            }

            if (region.Keyword is "@parallel" or "@sequence")
            {
                bool hasChild = program.Regions.Any(candidate =>
                    candidate.Depth == region.Depth + 1 && region.BodySpan.Contains(candidate.KeywordSpan.Start) &&
                    IsMotionExecutionDirective(candidate.Keyword));
                if (!hasChild)
                {
                    AddMotionDiagnostic(
                        "CERNEALAUI024",
                        new TextSpan(program.Offset, 0),
                        region.Keyword + " requires at least one child execution body.");
                }
            }

            if (region.Keyword == "@animate")
            {
                bool hasTo = program.Regions.Any(candidate => candidate.Keyword == "@to" &&
                    candidate.Depth == region.Depth + 1 && region.BodySpan.Contains(candidate.KeywordSpan.Start));
                if (!hasTo)
                {
                    AddMotionDiagnostic("CERNEALAUI020", new TextSpan(program.Offset, 0), "@animate requires an @to block.");
                }

                bool insideKeyframes = program.Regions.Any(candidate =>
                    candidate.Keyword == "@keyframes" && candidate.BodySpan.Contains(region.KeywordSpan.Start));
                string header = document.Text.Substring(region.HeaderSpan);
                bool usesKeyframeOnlySyntax = header.IndexOf(" hold", StringComparison.Ordinal) >= 0 ||
                    header.IndexOf("with Step(", StringComparison.Ordinal) >= 0;
                if (usesKeyframeOnlySyntax && !insideKeyframes)
                {
                    AddMotionDiagnostic(
                        "CERNEALAUI020",
                        region.HeaderSpan,
                        "Motion hold and Step easing are allowed only inside @keyframes.");
                }
                else
                {
                    BindMotionSpecHeader(source, region, clip, insideKeyframes);
                }
            }

            if (region.Keyword == "@handle")
            {
                BindMotionHandle(region, handles);
            }
            else if (region.Keyword is "@run" or "@cancel")
            {
                BindMotionCommand(source, region, clip, handles, isAspect);
            }
        }

        ValidateMotionAssignments(source, targetType, program, clip, cancellationToken);
        ValidateMotionFromTo(program);
    }

    private void BindMotionEvent(ElementSyntax source, ILanguageTypeSymbol? targetType, DirectiveRegion region)
    {
        string eventName = FirstWord(document.Text.Substring(region.HeaderSpan));
        TextSpan eventSpan = FindSubspan(region.HeaderSpan, eventName);
        ILanguageMemberSymbol? member = targetType?.GetMembers(eventName)
            .FirstOrDefault(candidate => candidate.Kind == LanguageMemberKind.Event);
        if (member is null)
        {
            ILanguageTypeSymbol? concreteType = GetElementType(source, ReferenceEquals(source, root));
            ILanguageMemberSymbol? concreteEvent = concreteType?.GetMembers(eventName)
                .FirstOrDefault(candidate => candidate.Kind == LanguageMemberKind.Event);
            string suggestion = concreteEvent is null || concreteType is null
                ? string.Empty
                : " The event exists on concrete type '" + concreteType.MetadataName +
                    "'; use TargetType=\"" + concreteType.MetadataName + "\".";
            AddMotionDiagnostic(
                "CERNEALAUI022",
                eventSpan,
                "Motion event '" + eventName + "' was not found or is not accessible on TargetType '" +
                (targetType?.Name ?? "<unknown>") + "'." + suggestion);
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionEvent,
            eventName,
            member.ValueTypeMetadataName,
            eventSpan,
            member.ValueType ?? targetType,
            member,
            definitionLocation: member.Locations.FirstOrDefault()));
    }

    private void BindMotionSpecHeader(
        ElementSyntax source,
        DirectiveRegion region,
        MotionClipDefinition? clip,
        bool insideKeyframes)
    {
        string header = document.Text.Substring(region.HeaderSpan).Trim();
        int with = header.IndexOf("with", StringComparison.Ordinal);
        if (with < 0)
        {
            return;
        }

        string spec = header.Substring(with + "with".Length).Trim();
        TextSpan span = FindSubspan(region.HeaderSpan, spec);
        if (spec.StartsWith("$", StringComparison.Ordinal))
        {
            string name = spec.Substring(1);
            ResourceDefinition? resource = FindResource(source, name);
            if (resource is null || !motionSpecs.ContainsKey(resource))
            {
                AddMotionDiagnostic("CERNEALAUI023", span, "Unknown Motion resource '$" + name + "'.");
                return;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.ResourceReference,
                name,
                "Cerneala.UI.Motion.Specs.MotionSpec",
                new TextSpan(span.Start + 1, name.Length),
                definitionLocation: resource.Location));
            return;
        }

        string kind = FirstWord(spec).Split('(')[0];
        if (clip?.Parameters.TryGetValue(kind, out MotionParameterDefinition? parameter) == true &&
            parameter.TypeName.StartsWith("MotionSpec[", StringComparison.Ordinal))
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionSpec,
                kind,
                parameter.TypeName,
                span,
                definitionLocation: new LanguageSourceLocation(document.Path, parameter.Span)));
            return;
        }

        if (kind == "Decay")
        {
            AddMotionDiagnostic("CERNEALAUI026", span, "Unsupported inline Motion spec 'Decay'.");
            return;
        }

        if (kind == "Step" && insideKeyframes)
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionSpec,
                kind,
                "Cerneala.UI.Motion.Specs.StepEasing",
                span,
                value: spec));
            return;
        }

        if (region.Depth > 0 && new[] { "Linear", "Standard", "Emphasized", "EaseIn", "EaseOut", "EaseInOut", "Sharp" }.Contains(kind))
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionSpec,
                kind,
                "Cerneala.UI.Motion.Specs.Easing",
                span,
                value: spec));
            return;
        }

        if (!CernealaLanguageFacts.MotionSpecKinds.Contains(kind))
        {
            AddMotionDiagnostic("CERNEALAUI023", span, "Unknown Motion spec '" + kind + "'.");
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionSpec,
            kind,
            "Cerneala.UI.Motion.Specs.MotionSpec",
            span,
            value: spec));
    }

    private void BindMotionHandle(DirectiveRegion region, IDictionary<string, TextSpan> handles)
    {
        string name = FirstWord(document.Text.Substring(region.HeaderSpan));
        TextSpan span = FindSubspan(region.HeaderSpan, name);
        if (handles.ContainsKey(name))
        {
            AddMotionDiagnostic("CERNEALAUI020", span, "Duplicate Motion handle '" + name + "'.");
            return;
        }

        handles.Add(name, span);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionHandle,
            name,
            "Cerneala.UI.Markup.MarkupMotionExecution",
            span,
            definitionLocation: new LanguageSourceLocation(document.Path, span)));
    }

    private void BindMotionCommand(
        ElementSyntax source,
        DirectiveRegion region,
        MotionClipDefinition? currentClip,
        IReadOnlyDictionary<string, TextSpan> handles,
        bool isAspect)
    {
        string header = document.Text.Substring(region.HeaderSpan).Trim().TrimEnd(';').Trim();
        if (region.Keyword == "@cancel")
        {
            string handle = FirstWord(header);
            TextSpan span = FindSubspan(region.HeaderSpan, handle);
            if (!handles.TryGetValue(handle, out TextSpan declaration) || declaration.Start > span.Start)
            {
                AddMotionDiagnostic("CERNEALAUI020", span, "Motion handle '" + handle + "' is undeclared or used before its declaration.");
            }

            return;
        }

        if (!isAspect && currentClip is not null)
        {
            // Nested clips are valid; resolution uses the resource scope of the clip.
        }

        int dollar = header.IndexOf('$');
        if (dollar < 0)
        {
            AddMotionDiagnostic("CERNEALAUI020", region.HeaderSpan, "@run requires a $MotionClip resource.");
            return;
        }

        int nameEnd = dollar + 1;
        while (nameEnd < header.Length && (char.IsLetterOrDigit(header[nameEnd]) || header[nameEnd] == '_'))
        {
            nameEnd++;
        }

        string name = header.Substring(dollar + 1, nameEnd - dollar - 1);
        TextSpan nameSpan = FindSubspan(region.HeaderSpan, "$" + name);
        ResourceDefinition? resource = FindResource(source, name);
        if (resource is null || !motionClips.TryGetValue(resource, out MotionClipDefinition? clip))
        {
            AddMotionDiagnostic("CERNEALAUI023", nameSpan, "Unknown MotionClip resource '$" + name + "'.");
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ResourceReference,
            name,
            clip.TargetType?.MetadataName ?? "System.Object",
            new TextSpan(nameSpan.Start + 1, name.Length),
            clip.TargetType,
            definitionLocation: resource.Location));
        ValidateMotionRunArguments(header, region.HeaderSpan, clip);

        int asIndex = header.LastIndexOf(" as ", StringComparison.Ordinal);
        if (asIndex >= 0)
        {
            string handle = header.Substring(asIndex + 4).Trim();
            TextSpan handleSpan = FindSubspan(region.HeaderSpan, handle, fromEnd: true);
            if (!handles.TryGetValue(handle, out TextSpan declaration) || declaration.Start > handleSpan.Start)
            {
                AddMotionDiagnostic("CERNEALAUI020", handleSpan, "Motion handle '" + handle + "' is undeclared or used before its declaration.");
            }
        }
    }

    private void ValidateMotionRunArguments(string header, TextSpan headerSpan, MotionClipDefinition clip)
    {
        int opening = header.IndexOf('(');
        int closing = header.LastIndexOf(')');
        Dictionary<string, string> supplied = new(StringComparer.Ordinal);
        if (opening >= 0 && closing > opening)
        {
            foreach (string argumentText in SplitTopLevel(header.Substring(opening + 1, closing - opening - 1), ','))
            {
                int equals = argumentText.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                string name = argumentText.Substring(0, equals).Trim();
                string value = argumentText.Substring(equals + 1).Trim();
                TextSpan nameSpan = FindSubspan(headerSpan, name);
                if (!clip.Parameters.TryGetValue(name, out MotionParameterDefinition? parameter))
                {
                    AddMotionDiagnostic("CERNEALAUI020", nameSpan, "Unknown parameter '" + name + "' for MotionClip.");
                    continue;
                }

                if (supplied.ContainsKey(name))
                {
                    AddMotionDiagnostic("CERNEALAUI020", nameSpan, "Duplicate MotionClip argument '" + name + "'.");
                    continue;
                }

                supplied.Add(name, value);
                if (!ValidateMotionParameterValue(parameter, value))
                {
                    AddMotionDiagnostic("CERNEALAUI023", FindSubspan(headerSpan, value), "Argument is not compatible with MotionClip parameter '" + name + "'.");
                }
            }
        }

        MotionParameterDefinition? missing = clip.Parameters.Values.FirstOrDefault(parameter =>
            parameter.DefaultValue is null && !supplied.ContainsKey(parameter.Name));
        if (missing is not null)
        {
            AddMotionDiagnostic("CERNEALAUI020", headerSpan, "MotionClip parameter '" + missing.Name + "' requires argument.");
        }
    }

    private void ValidateMotionAssignments(
        ElementSyntax source,
        ILanguageTypeSymbol? targetType,
        MotionProgram program,
        MotionClipDefinition? clip,
        CancellationToken cancellationToken)
    {
        foreach (AssignmentSyntax assignment in program.Syntax.Assignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DirectiveRegion? owner = InnermostRegion(program.Regions, assignment.NameSpan.Start);
            if (owner is null)
            {
                continue;
            }

            if (owner.Keyword == "@animate" && CernealaLanguageFacts.MotionOptions.Any(option => option.Name == assignment.Name))
            {
                continue;
            }

            if (owner.Keyword is not ("@from" or "@to" or "@set" or "@scroll"))
            {
                continue;
            }

            BindMotionAssignment(source, targetType, assignment, clip, prismOnly: false);
        }
    }

    private void ValidateMotionOption(AssignmentSyntax assignment)
    {
        string value = document.Text.Substring(assignment.ValueSpan).Trim();
        bool valid = assignment.Name switch
        {
            "retarget" => value is "Restart" or "PreserveProgress",
            "holdOnComplete" => bool.TryParse(value, out _),
            "debugName" => value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"',
            _ => false
        };
        if (!valid)
        {
            AddMotionDiagnostic(
                "CERNEALAUI020",
                assignment.NameSpan,
                "Unsupported or invalid Motion option '" + assignment.Name + "'. Supported options are retarget, holdOnComplete and debugName.");
        }
    }

    private void BindMotionAssignment(
        ElementSyntax source,
        ILanguageTypeSymbol? defaultTargetType,
        AssignmentSyntax assignment,
        MotionClipDefinition? clip,
        bool prismOnly)
    {
        string targetPath = assignment.Name;
        string[] segments = targetPath.Split('.');
        if (segments.Length >= 4 && segments[1] == "prism")
        {
            BindPrismMotionTarget(source, assignment, segments, reportMissingApplication: prismOnly);
            return;
        }

        if (prismOnly)
        {
            return;
        }

        ILanguageTypeSymbol? ownerType = defaultTargetType;
        LanguageSourceLocation? ownerLocation = null;
        int propertyIndex = segments.Length - 1;
        string ownerName = "self";
        if (segments.Length > 1)
        {
            ownerName = segments[0].TrimStart('$');
            if (ownerName == "self")
            {
                ownerType = defaultTargetType;
            }
            else if (ownerName == "owner")
            {
                ownerType = templateContexts.TryGetValue(source, out SemanticTemplateContext? template)
                    ? template.OwnerType
                    : null;
            }
            else if (FindNamedElement(source, ownerName) is NamedElementDefinition named)
            {
                ownerType = named.Type;
                ownerLocation = new LanguageSourceLocation(document.Path, named.Span);
            }
            else
            {
                AddMotionDiagnostic("CERNEALAUI021", MotionSegmentSpan(assignment.NameSpan, targetPath, 0), "Motion target '" + segments[0] + "' is not available in this namescope.");
                return;
            }

            if (segments.Length >= 4 && segments[1] == "parts")
            {
                string partName = segments[2].TrimStart('$');
                NamedElementDefinition? part = templateContexts.Values
                    .Distinct()
                    .Where(context => context.OwnerType?.MetadataName == ownerType?.MetadataName)
                    .Select(context => context.Parts.TryGetValue(partName, out NamedElementDefinition? candidate) ? candidate : null)
                    .FirstOrDefault(candidate => candidate is not null);
                if (part is null)
                {
                    AddMotionDiagnostic("CERNEALAUI021", MotionSegmentSpan(assignment.NameSpan, targetPath, 2), "Template part '$" + partName + "' does not exist.");
                    return;
                }

                ownerType = part.Type;
                ownerLocation = new LanguageSourceLocation(document.Path, part.Span);
            }
        }

        TextSpan ownerSpan = segments.Length == 1
            ? new TextSpan(assignment.NameSpan.Start, 0)
            : MotionSegmentSpan(assignment.NameSpan, targetPath, 0);
        if (ownerSpan.Length > 0)
        {
            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionTarget,
                ownerName,
                ownerType?.MetadataName ?? "System.Object",
                ownerSpan,
                ownerType,
                definitionLocation: ownerLocation));
        }

        string propertyName = segments[propertyIndex];
        TextSpan propertySpan = MotionSegmentSpan(assignment.NameSpan, targetPath, propertyIndex);
        ILanguageMemberSymbol? member = FindProperty(ownerType, propertyName);
        if (member is null)
        {
            AddMotionDiagnostic(
                "CERNEALAUI021",
                propertySpan,
                "Motion property '" + propertyName + "' does not exist on target type '" +
                (ownerType?.Name ?? "<unknown>") + "'.");
            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionProperty,
            propertyName,
            member.ValueTypeMetadataName,
            propertySpan,
            member.ValueType,
            member,
            definitionLocation: member.Locations.FirstOrDefault(),
            isWritable: member.CanWrite));

        string value = document.Text.Substring(assignment.ValueSpan).Trim();
        if (value.IndexOf("..", StringComparison.Ordinal) >= 0 ||
            value.IndexOf("=>", StringComparison.Ordinal) >= 0 ||
            value.IndexOf('?') >= 0 && value.IndexOf(':') >= 0)
        {
            return;
        }

        int with = value.IndexOf(" with ", StringComparison.Ordinal);
        if (with >= 0)
        {
            value = value.Substring(0, with).Trim();
        }

        if (value == "current" || value.StartsWith("$", StringComparison.Ordinal) ||
            clip?.Parameters.ContainsKey(value) == true)
        {
            return;
        }

        if (!TryConvertLiteral(Unquote(value), member, out _))
        {
            AddMotionDiagnostic(
                "CERNEALAUI023",
                TrimmedSpan(assignment.ValueSpan),
                "Motion value for property '" + targetPath + "' is not compatible with type '" + member.ValueTypeMetadataName + "'.");
        }
    }

    private void ValidateMotionFromTo(MotionProgram program)
    {
        foreach (DirectiveRegion animate in program.Regions.Where(region => region.Keyword == "@animate"))
        {
            DirectiveRegion? from = program.Regions.FirstOrDefault(region => region.Keyword == "@from" &&
                region.Depth == animate.Depth + 1 && animate.BodySpan.Contains(region.KeywordSpan.Start));
            DirectiveRegion? to = program.Regions.FirstOrDefault(region => region.Keyword == "@to" &&
                region.Depth == animate.Depth + 1 && animate.BodySpan.Contains(region.KeywordSpan.Start));
            if (from is null || to is null)
            {
                continue;
            }

            HashSet<string> destinations = new(
                program.Syntax.Assignments
                    .Where(assignment => to.BodySpan.Contains(assignment.NameSpan.Start))
                    .Select(assignment => assignment.Name),
                StringComparer.Ordinal);
            AssignmentSyntax? missing = program.Syntax.Assignments.FirstOrDefault(assignment =>
                from.BodySpan.Contains(assignment.NameSpan.Start) && !destinations.Contains(assignment.Name));
            if (missing is not null)
            {
                AddMotionDiagnostic(
                    "CERNEALAUI020",
                    missing.NameSpan,
                    "Motion property '" + missing.Name + "' appears in @from but not @to.");
            }
        }
    }

    private void BindElementEmbeddedSemantics(
        ElementSyntax element,
        ILanguageTypeSymbol elementType,
        ILanguageTypeSymbol? dataType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        (string text, int offset) = BuildDirectTextBuffer(element);
        if (text.IndexOf("@prism", StringComparison.Ordinal) >= 0)
        {
            BindPrismApplications(element, text, offset, cancellationToken);
        }

        AttributeSyntax? aspectAttribute = FindAttribute(element, "Aspect");
        if (aspectAttribute is null)
        {
            return;
        }

        string aspectName = Unquote(aspectAttribute.ValueToken.Text).Trim().TrimStart('$');
        ResourceDefinition? aspect = FindResource(element, aspectName);
        if (aspect?.Kind != ResourceKind.Aspect || aspect.TargetType is null)
        {
            return;
        }

        BindAppliedPrismMotion(aspect, element, cancellationToken);
    }

    private void BindAppliedPrismMotion(ResourceDefinition aspect, ElementSyntax application, CancellationToken cancellationToken)
    {
        if (!string.Equals(aspect.Path, document.Path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        (string text, int offset) = BuildDirectTextBuffer(aspect.Element);
        if (text.IndexOf(".prism.", StringComparison.Ordinal) < 0)
        {
            return;
        }

        MotionProgram program = ParseMotionProgram(text, offset);
        foreach (AssignmentSyntax assignment in program.Syntax.Assignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DirectiveRegion? owner = InnermostRegion(program.Regions, assignment.NameSpan.Start);
            if (owner?.Keyword is "@from" or "@to" or "@set" or "@scroll")
            {
                BindMotionAssignment(application, aspect.TargetType, assignment, clip: null, prismOnly: true);
            }
        }
    }

    private bool ShouldBindAspectAssignment(ElementSyntax aspect, int position)
    {
        string source = document.Text.ToString();
        foreach (string keyword in CernealaLanguageFacts.MotionDirectiveKeywords.Where(keyword => keyword is not "@when" and not "@if"))
        {
            if (FindDirectiveBlocks(source, keyword).Any(block =>
                aspect.Span.Contains(block.KeywordSpan.Start) && block.BodySpan.Contains(position)))
            {
                return false;
            }
        }

        return new[] { "@default", "@when", "@if" }
            .SelectMany(keyword => FindDirectiveBlocks(source, keyword))
            .Any(block => aspect.Span.Contains(block.KeywordSpan.Start) && block.BodySpan.Contains(position));
    }

    private void BindPrismCompositionResource(ResourceDefinition resource, CancellationToken cancellationToken)
    {
        (string text, int offset) = BuildDirectTextBuffer(resource.Element);
        EmbeddedParseResult<PrismCompositionModelSyntax> parsed = PrismSyntaxParser.ParseComposition(text, offset);
        AddPrismSyntaxDiagnostics(parsed.Diagnostics);
        PrismCompositionDefinition definition = BindPrismComposition(
            resource.Name ?? "PrismComposition",
            resource.Element,
            parsed.Syntax,
            cancellationToken);
        prismCompositions[resource] = definition;
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismComposition,
            resource.Name ?? "PrismComposition",
            "Cerneala.UI.Prism.Definitions.PrismCompositionDefinition",
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
            PrismValueModelSyntax syntax = new(value, ClassifyPrismValue(value), AttributeContentSpan(attribute));
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

    private void BindPrismApplications(
        ElementSyntax owner,
        string text,
        int offset,
        CancellationToken cancellationToken)
    {
        if (!boundPrismApplications.Add(owner))
        {
            return;
        }

        EmbeddedParseResult<IReadOnlyList<PrismApplicationModelSyntax>> parsed =
            PrismSyntaxParser.ParseApplications(text, offset);
        AddPrismSyntaxDiagnostics(parsed.Diagnostics);
        if (parsed.Diagnostics.Count > 0)
        {
            return;
        }
        if (parsed.Syntax.Count > 1)
        {
            AddPrismDiagnostic("PRISM2013", parsed.Syntax[1].Span, "An element may declare only one @prism application.");
            return;
        }

        if (parsed.Syntax.Count == 0)
        {
            return;
        }

        PrismApplicationModelSyntax application = parsed.Syntax[0];
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismDirective,
            "@prism",
            "Cerneala.UI.Prism.Definitions.PrismCompositionDefinition",
            new TextSpan(application.Span.Start, Math.Min("@prism".Length, application.Span.Length))));

        PrismCompositionDefinition? definition;
        if (application.Composition is not null)
        {
            definition = BindPrismComposition(
                "InlinePrism@" + application.Span.Start.ToString(CultureInfo.InvariantCulture),
                owner,
                application.Composition,
                cancellationToken);
        }
        else
        {
            string name = application.ResourceName ?? string.Empty;
            ResourceDefinition? resource = FindResource(owner, name);
            if (resource is null || !prismCompositions.TryGetValue(resource, out definition))
            {
                AddPrismDiagnostic("PRISM2002", application.ResourceSpan, "Unknown PrismComposition resource '$" + name + "'.");
                return;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.ResourceReference,
                name,
                "Cerneala.UI.Prism.Definitions.PrismCompositionDefinition",
                application.ResourceSpan,
                definitionLocation: resource.Location));
            BindPrismApplicationArguments(owner, application, definition);
        }

        prismApplications[owner] = definition;
    }

    private void BindPrismApplicationArguments(
        ElementSyntax owner,
        PrismApplicationModelSyntax application,
        PrismCompositionDefinition definition)
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

    private PrismCompositionDefinition BindPrismComposition(
        string name,
        ElementSyntax source,
        PrismCompositionModelSyntax syntax,
        CancellationToken cancellationToken)
    {
        PrismCompositionDefinition definition = new(name, source);
        PrismParameterScope rootScope = new(parent: null);
        BindPrismParameters(syntax.Members, rootScope, string.Empty, definition);
        BindPrismAssignments(
            source,
            syntax.Members,
            PrismCatalog.Value.GetCommonProperties("composition"),
            rootScope,
            "composition");

        PrismContainerModelSyntax[] nodes = syntax.Members.OfType<PrismContainerModelSyntax>().ToArray();
        ValidatePrismBackdropShape(nodes);
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
                "A Prism composition must contain at least one layer, group, or backdrop.");
        }

        ValidatePrismClipToBelow(definition.RootNodes);
        return definition;
    }

    private PrismNodeDefinition? BindPrismNode(
        ElementSyntax source,
        PrismContainerModelSyntax syntax,
        PrismParameterScope parentScope,
        string parentPath,
        PrismCompositionDefinition composition,
        PrismContainerModelKind? parentKind,
        CancellationToken cancellationToken)
    {
        if (parentKind is not null && parentKind != PrismContainerModelKind.Group)
        {
            AddPrismDiagnostic("PRISM2005", syntax.NameSpan.Length == 0 ? syntax.Span : syntax.NameSpan,
                "@" + syntax.Kind.ToString().ToLowerInvariant() + " cannot be nested inside @" + parentKind.Value.ToString().ToLowerInvariant() + ".");
            return null;
        }

        if (parentKind == PrismContainerModelKind.Group && syntax.Kind == PrismContainerModelKind.Backdrop)
        {
            AddPrismDiagnostic("PRISM2005", syntax.NameSpan.Length == 0 ? syntax.Span : syntax.NameSpan, "@backdrop cannot be nested inside @group.");
            return null;
        }

        string nodeName = syntax.Name ?? "Backdrop";
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
        PrismCompositionDefinition composition)
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

        bool valid = schema.ValueType switch
        {
            "boolean" => value.Kind == PrismValueModelKind.BooleanLiteral,
            "integer" => value.Kind == PrismValueModelKind.NumberLiteral &&
                int.TryParse(value.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "number" => value.Kind == PrismValueModelKind.NumberLiteral &&
                float.TryParse(value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) &&
                !float.IsNaN(number) && !float.IsInfinity(number),
            "color" => value.Kind == PrismValueModelKind.ColorLiteral && IsHexColor(value.Text),
            "vector" => value.Kind == PrismValueModelKind.TupleLiteral && IsPrismVector(value.Text),
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
            string canonicalDomain = schema.DomainKind + ":" +
                (schema.Minimum.HasValue ? schema.Minimum.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty) + ":" +
                (schema.Maximum.HasValue ? schema.Maximum.Value.ToString("R", CultureInfo.InvariantCulture) : string.Empty);
            AddPrismDiagnostic("PRISM2009", value.Span,
                "Prism property '" + schema.Name + "' value '" + value.Text + "' is outside catalog domain '" + canonicalDomain + "'.");
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

    private void ValidatePrismBackdropShape(IReadOnlyList<PrismContainerModelSyntax> nodes)
    {
        PrismContainerModelSyntax[] backdrops = nodes.Where(node => node.Kind == PrismContainerModelKind.Backdrop).ToArray();
        if (backdrops.Length > 1)
        {
            AddPrismDiagnostic("PRISM2006", backdrops[1].NameSpan.Length == 0 ? backdrops[1].Span : backdrops[1].NameSpan,
                "A Prism composition may declare at most one backdrop.");
            return;
        }

        if (backdrops.Length == 1)
        {
            int index = Array.IndexOf(nodes.ToArray(), backdrops[0]);
            PrismContainerModelSyntax? following = nodes.Skip(index + 1).FirstOrDefault(node => node.Kind != PrismContainerModelKind.Backdrop);
            if (following is not null)
            {
                AddPrismDiagnostic("PRISM2007", following.NameSpan.Length == 0 ? following.Span : following.NameSpan,
                    "The Prism backdrop must be the last direct composition child.");
            }
        }
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

    private void BindPrismMotionTarget(
        ElementSyntax source,
        AssignmentSyntax assignment,
        string[] segments,
        bool reportMissingApplication)
    {
        ElementSyntax? targetElement;
        string ownerName = segments[0].TrimStart('$');
        LanguageSourceLocation? ownerLocation = null;
        if (ownerName == "self")
        {
            targetElement = source;
        }
        else if (ownerName == "owner")
        {
            targetElement = templateContexts.TryGetValue(source, out SemanticTemplateContext? template)
                ? template.OwnerElement
                : null;
        }
        else if (FindNamedElement(source, ownerName) is NamedElementDefinition named)
        {
            targetElement = named.Element;
            ownerLocation = new LanguageSourceLocation(document.Path, named.Span);
        }
        else
        {
            AddPrismDiagnostic("PRISM2010", MotionSegmentSpan(assignment.NameSpan, assignment.Name, 0),
                "Prism Motion target named element '" + ownerName + "' is not available at this application site.");
            return;
        }

        if (targetElement is null || !prismApplications.TryGetValue(targetElement, out PrismCompositionDefinition? composition))
        {
            if (reportMissingApplication)
            {
                AddPrismDiagnostic("PRISM2010", MotionSegmentSpan(assignment.NameSpan, assignment.Name, 0),
                    "Motion target '" + segments[0] + "' has no statically attached Prism composition.");
            }

            return;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionTarget,
            ownerName,
            GetElementType(targetElement, ReferenceEquals(targetElement, root))?.MetadataName ?? "System.Object",
            MotionSegmentSpan(assignment.NameSpan, assignment.Name, 0),
            GetElementType(targetElement, ReferenceEquals(targetElement, root)),
            definitionLocation: ownerLocation));

        string nodePath = string.Join(".", segments.Skip(2).Take(segments.Length - 3));
        if (!composition.Nodes.TryGetValue(nodePath, out PrismNodeDefinition? node))
        {
            AddPrismDiagnostic("PRISM2011", MotionSegmentSpan(assignment.NameSpan, assignment.Name, 2),
                "Prism node path '" + nodePath + "' does not exist.");
            return;
        }

        int nodeStart = assignment.NameSpan.Start + segments[0].Length + ".prism.".Length;
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.PrismNode,
            nodePath,
            "Cerneala.UI.Prism.Definitions.Prism" + node.Kind + "Definition",
            new TextSpan(nodeStart, nodePath.Length),
            definitionLocation: new LanguageSourceLocation(document.Path, node.Span)));

        string propertyName = segments[segments.Length - 1];
        PrismCatalogProperty? property = PrismCatalog.Value.GetCommonProperties(node.Kind.ToString().ToLowerInvariant())
            .FirstOrDefault(candidate => candidate.Name == propertyName);
        PrismParameterDefinition? parameter = composition.Parameters.TryGetValue(nodePath + "." + propertyName, out PrismParameterDefinition? candidateParameter)
            ? candidateParameter
            : null;
        string valueType = parameter?.TypeName ?? property?.ValueType ?? string.Empty;
        TextSpan propertySpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, segments.Length - 1);
        if (valueType.Length == 0)
        {
            AddPrismDiagnostic("PRISM2012", propertySpan,
                "Prism node '" + nodePath + "' has no property or scoped parameter named '" + propertyName + "'.");
            return;
        }

        if (valueType is "vector" or "resource")
        {
            AddPrismDiagnostic("PRISM2012", propertySpan,
                "Prism property or parameter '" + nodePath + "." + propertyName + "' is not animatable.");
            return;
        }

        ILanguageTypeSymbol? type = ResolvePrismType(valueType);
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.MotionProperty,
            propertyName,
            type?.MetadataName ?? valueType,
            propertySpan,
            type,
            definitionLocation: parameter is null ? null : new LanguageSourceLocation(document.Path, parameter.Span),
            isWritable: true));
    }

    private void AddMotionDiagnostic(string id, TextSpan span, string message) =>
        AddDiagnostic(id, span, Path.GetFileName(document.Path), message);

    private void AddPrismDiagnostic(string id, TextSpan span, string message) =>
        AddDiagnostic(id, span, Path.GetFileName(document.Path), message);

    private void AddPrismSyntaxDiagnostics(IEnumerable<EmbeddedDiagnostic> syntaxDiagnostics)
    {
        foreach (EmbeddedDiagnostic diagnostic in syntaxDiagnostics)
        {
            AddPrismDiagnostic(diagnostic.Id, diagnostic.Span, diagnostic.Message);
        }
    }

    private static bool TryParseDuration(string value, bool allowZero)
    {
        int unitLength = value.EndsWith("ms", StringComparison.Ordinal) ? 2 :
            value.EndsWith("s", StringComparison.Ordinal) ? 1 : 0;
        if (unitLength == 0 || !double.TryParse(
            value.Substring(0, value.Length - unitLength),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double numeric) || double.IsNaN(numeric) || double.IsInfinity(numeric))
        {
            return false;
        }

        return allowZero ? numeric >= 0 : numeric > 0;
    }

    private static DirectiveRegion CreateDirectiveRegion(string text, int offset, DirectiveSyntax syntax)
    {
        int relativeStart = Math.Max(0, syntax.Span.End - offset);
        int semicolon = FindDelimiter(text, relativeStart, ';');
        int opening = FindDelimiter(text, relativeStart, '{');
        bool hasBlock = opening >= 0 && (semicolon < 0 || opening < semicolon);
        if (!hasBlock)
        {
            int end = semicolon < 0 ? FindLineEnd(text, relativeStart) : semicolon + 1;
            return new DirectiveRegion(
                syntax.Keyword,
                syntax.Span,
                new TextSpan(offset + relativeStart, Math.Max(0, end - relativeStart)),
                new TextSpan(offset + end, 0),
                syntax.Depth);
        }

        int closing = FindMatchingBrace(text, opening);
        int bodyEnd = closing < 0 ? text.Length : closing;
        return new DirectiveRegion(
            syntax.Keyword,
            syntax.Span,
            new TextSpan(offset + relativeStart, Math.Max(0, opening - relativeStart)),
            new TextSpan(offset + opening + 1, Math.Max(0, bodyEnd - opening - 1)),
            syntax.Depth);
    }

    private static int FindDelimiter(string text, int start, char delimiter)
    {
        bool quoted = false;
        char quote = '\0';
        for (int index = start; index < text.Length; index++)
        {
            char character = text[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || text[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == delimiter && !(delimiter == ';' && IsXmlEntityTerminator(text, index)))
            {
                return index;
            }
            else if (character == '}' && delimiter != '}')
            {
                return -1;
            }
        }

        return -1;
    }

    private static bool IsXmlEntityTerminator(string text, int index)
    {
        int ampersand = index - 1;
        while (ampersand >= 0 && (char.IsLetterOrDigit(text[ampersand]) || text[ampersand] == '#'))
        {
            ampersand--;
        }

        return ampersand >= 0 && text[ampersand] == '&' && ampersand + 1 < index;
    }

    private static int FindMatchingBrace(string text, int opening)
    {
        int depth = 1;
        bool quoted = false;
        char quote = '\0';
        for (int index = opening + 1; index < text.Length; index++)
        {
            char character = text[index];
            if (quoted)
            {
                if (character == quote && text[index - 1] != '\\')
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '{')
            {
                depth++;
            }
            else if (character == '}' && --depth == 0)
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindLineEnd(string text, int start)
    {
        int end = start;
        while (end < text.Length && text[end] is not ('\r' or '\n' or '}'))
        {
            end++;
        }

        return end;
    }

    private static bool IsMotionExecutionDirective(string keyword) => keyword is
        "@set" or "@animate" or "@keyframes" or "@stagger" or "@parallel" or "@sequence" or "@run";

    private static bool ContainsMotionProgram(string text) =>
        CernealaLanguageFacts.MotionDirectiveKeywords
            .Where(keyword => keyword is not "@when" and not "@if")
            .Any(keyword => text.IndexOf(keyword, StringComparison.Ordinal) >= 0);

    private TextSpan FindSubspan(TextSpan container, string value, bool fromEnd = false)
    {
        string source = document.Text.Substring(container);
        int relative = fromEnd
            ? source.LastIndexOf(value, StringComparison.Ordinal)
            : source.IndexOf(value, StringComparison.Ordinal);
        return relative < 0
            ? new TextSpan(container.Start, 0)
            : new TextSpan(container.Start + relative, value.Length);
    }

    private static string FirstWord(string value)
    {
        value = value.Trim();
        int end = 0;
        while (end < value.Length && !char.IsWhiteSpace(value[end]) && value[end] is not (';' or '{'))
        {
            end++;
        }

        return value.Substring(0, end);
    }

    private static IEnumerable<string> SplitTopLevel(string text, char separator)
    {
        int start = 0;
        int parentheses = 0;
        bool quoted = false;
        char quote = '\0';
        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];
            if (quoted)
            {
                if (character == quote && (index == 0 || text[index - 1] != '\\'))
                {
                    quoted = false;
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quoted = true;
                quote = character;
            }
            else if (character == '(')
            {
                parentheses++;
            }
            else if (character == ')')
            {
                parentheses--;
            }
            else if (character == separator && parentheses == 0)
            {
                yield return text.Substring(start, index - start).Trim();
                start = index + 1;
            }
        }

        if (start <= text.Length)
        {
            yield return text.Substring(start).Trim();
        }
    }

    private static DirectiveRegion? InnermostRegion(IEnumerable<DirectiveRegion> regions, int position) => regions
        .Where(region => region.BodySpan.Contains(position))
        .OrderBy(region => region.BodySpan.Length)
        .FirstOrDefault();

    private static TextSpan MotionSegmentSpan(TextSpan targetSpan, string target, int segmentIndex)
    {
        int start = 0;
        for (int index = 0; index < segmentIndex; index++)
        {
            int separator = target.IndexOf('.', start);
            start = separator < 0 ? target.Length : separator + 1;
        }

        int end = target.IndexOf('.', start);
        if (end < 0)
        {
            end = target.Length;
        }

        return new TextSpan(targetSpan.Start + start, Math.Max(0, end - start));
    }

    private static bool TryNormalizeMotionParameterType(string typeName, out string normalized)
    {
        normalized = typeName switch
        {
            "float" or "System.Single" => "System.Single",
            "double" or "System.Double" => "System.Double",
            "int" or "System.Int32" => "System.Int32",
            "bool" or "System.Boolean" => "System.Boolean",
            "string" or "System.String" => "System.String",
            _ when typeName.StartsWith("MotionSpec[", StringComparison.Ordinal) && typeName.EndsWith("]", StringComparison.Ordinal) => typeName,
            _ => string.Empty
        };
        return normalized.Length > 0;
    }

    private static bool ValidateMotionParameterValue(MotionParameterDefinition parameter, string value)
    {
        value = value.Trim();
        if (parameter.TypeName.StartsWith("MotionSpec[", StringComparison.Ordinal))
        {
            return value.StartsWith("$", StringComparison.Ordinal) ||
                new[] { "Tween", "Spring", "Repeat", "PingPong" }.Any(kind => value.StartsWith(kind + "(", StringComparison.Ordinal));
        }

        return parameter.TypeName switch
        {
            "System.Single" => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            "System.Double" => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            "System.Int32" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "System.Boolean" => bool.TryParse(value, out _),
            "System.String" => value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"',
            _ => false
        };
    }

    private ILanguageTypeSymbol? ResolveIntrinsicType(string typeName)
    {
        if (typeName.StartsWith("MotionSpec[", StringComparison.Ordinal))
        {
            return compilation.FindType("System.Object");
        }

        return compilation.FindType(typeName);
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

    private static bool IsPrismVector(string value)
    {
        if (value.Length < 2 || value[0] != '(' || value[value.Length - 1] != ')')
        {
            return false;
        }

        string[] components = value.Substring(1, value.Length - 2).Split(',');
        return components.Length is >= 2 and <= 4 && components.All(component =>
            float.TryParse(component.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float number) &&
            !float.IsNaN(number) && !float.IsInfinity(number));
    }

    private static PrismValueModelKind ClassifyPrismValue(string value)
    {
        if (value == "null")
        {
            return PrismValueModelKind.NullLiteral;
        }

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            return PrismValueModelKind.ResourceReference;
        }

        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            return PrismValueModelKind.ColorLiteral;
        }

        if (value.Length >= 2 && value[0] is '\'' or '"' && value[value.Length - 1] == value[0])
        {
            return PrismValueModelKind.StringLiteral;
        }

        if (value.StartsWith("(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal))
        {
            return PrismValueModelKind.TupleLiteral;
        }

        if (bool.TryParse(value, out _))
        {
            return PrismValueModelKind.BooleanLiteral;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
            ? PrismValueModelKind.NumberLiteral
            : PrismValueModelKind.Identifier;
    }

    private sealed class MotionProgram
    {
        public MotionProgram(
            string text,
            int offset,
            DirectiveDocumentSyntax syntax,
            IReadOnlyList<DirectiveRegion> regions,
            bool hasSyntaxErrors)
        {
            Text = text;
            Offset = offset;
            Syntax = syntax;
            Regions = regions;
            HasSyntaxErrors = hasSyntaxErrors;
        }

        public string Text { get; }

        public int Offset { get; }

        public DirectiveDocumentSyntax Syntax { get; }

        public IReadOnlyList<DirectiveRegion> Regions { get; }

        public bool HasSyntaxErrors { get; }
    }

    private sealed class DirectiveRegion
    {
        public DirectiveRegion(string keyword, TextSpan keywordSpan, TextSpan headerSpan, TextSpan bodySpan, int depth)
        {
            Keyword = keyword;
            KeywordSpan = keywordSpan;
            HeaderSpan = headerSpan;
            BodySpan = bodySpan;
            Depth = depth;
        }

        public string Keyword { get; }

        public TextSpan KeywordSpan { get; }

        public TextSpan HeaderSpan { get; }

        public TextSpan BodySpan { get; }

        public int Depth { get; }
    }

    private sealed class MotionSpecDefinition
    {
        public MotionSpecDefinition(ResourceDefinition resource, string kind, bool isValid)
        {
            Resource = resource;
            Kind = kind;
            IsValid = isValid;
        }

        public ResourceDefinition Resource { get; }

        public string Kind { get; }

        public bool IsValid { get; }
    }

    private sealed class MotionClipDefinition
    {
        public MotionClipDefinition(ResourceDefinition resource, ILanguageTypeSymbol? targetType)
        {
            Resource = resource;
            TargetType = targetType;
        }

        public ResourceDefinition Resource { get; }

        public ILanguageTypeSymbol? TargetType { get; }

        public Dictionary<string, MotionParameterDefinition> Parameters { get; } = new(StringComparer.Ordinal);
    }

    private sealed class MotionParameterDefinition
    {
        public MotionParameterDefinition(string name, string typeName, string? defaultValue, TextSpan span)
        {
            Name = name;
            TypeName = typeName;
            DefaultValue = defaultValue;
            Span = span;
        }

        public string Name { get; }

        public string TypeName { get; }

        public string? DefaultValue { get; }

        public TextSpan Span { get; }
    }

    private sealed class PrismCompositionDefinition
    {
        public PrismCompositionDefinition(string name, ElementSyntax source)
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
