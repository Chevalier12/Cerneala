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
            : ResolveTargetTypeReference(targetAttribute);
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
        collectingSoundMotion = new PendingSoundMotion(program);
        try
        {
            BindMotionProgram(source, aspect.TargetType, program, clip: null, isAspect: true, cancellationToken);
            if (collectingSoundMotion.Targets.Count > 0)
            {
                pendingSoundMotion[aspect.Element] = collectingSoundMotion;
            }
        }
        finally
        {
            collectingSoundMotion = null;
        }
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
        return new MotionProgram(offset, parsed.Syntax, regions, primaryDiagnostic is not null);
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
                BindMotionCommand(source, region, handles);
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
        IReadOnlyDictionary<string, TextSpan> handles)
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
            else
            {
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.MotionHandle,
                    handle,
                    "Cerneala.UI.Markup.MarkupMotionExecution",
                    span,
                    definitionLocation: new LanguageSourceLocation(document.Path, declaration)));
            }

            return;
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
        // Nested clips are valid; resolve against the source's resource scope.
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
            else
            {
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.MotionHandle,
                    handle,
                    "Cerneala.UI.Markup.MarkupMotionExecution",
                    handleSpan,
                    definitionLocation: new LanguageSourceLocation(document.Path, declaration)));
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

            if (IsSoundMotionPath(assignment.Name))
            {
                CollectSoundMotionTarget(assignment, owner);
                continue;
            }

            BindMotionAssignment(
                source,
                targetType,
                assignment,
                clip,
                prismOnly: false,
                allowExplicitBinding: owner.Keyword == "@to");
        }
    }

    private void BindMotionAssignment(
        ElementSyntax source,
        ILanguageTypeSymbol? defaultTargetType,
        AssignmentSyntax assignment,
        MotionClipDefinition? clip,
        bool prismOnly,
        bool allowExplicitBinding)
    {
        string targetPath = assignment.Name;
        string[] segments = targetPath.Split('.');
        if (segments.Length >= 4 && segments[1] == "prism")
        {
            BindPrismMotionTarget(
                source,
                assignment,
                segments,
                reportMissingApplication: prismOnly,
                allowExplicitBinding: allowExplicitBinding);
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

        if (value == "current" || clip?.Parameters.ContainsKey(value) == true)
        {
            return;
        }

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            BindMotionReferenceValue(
                source,
                value,
                TrimmedSpan(assignment.ValueSpan),
                member.ValueType,
                targetPath,
                allowExplicitBinding);
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

    private void BindMotionReferenceValue(
        ElementSyntax source,
        string expression,
        TextSpan span,
        ILanguageTypeSymbol? targetType,
        string targetPath,
        bool allowExplicitBinding)
    {
        BindingResolution? resolution = ResolveDirectiveReference(
            source,
            expression,
            span,
            GetCompletionDataType(source),
            out BindingPathSyntax? path);
        if (resolution is null || path is null)
        {
            return;
        }

        bool explicitBinding = path.ModeSpan.Length > 0;
        if (explicitBinding && !allowExplicitBinding)
        {
            AddMotionDiagnostic(
                "CERNEALAUI023",
                path.ModeSpan,
                "An explicit Motion binding is allowed only as an @animate destination. @from, @set and keyframe values are snapshots.");
            return;
        }

        if (!IsMotionReferenceTypeCompatible(resolution.Type, targetType))
        {
            AddMotionDiagnostic(
                "CERNEALAUI023",
                span,
                "Motion reference '" + expression + "' for property '" + targetPath + "' has incompatible type '" +
                (resolution.Type?.MetadataName ?? "<unknown>") + "'.");
            return;
        }

        if (!explicitBinding)
        {
            return;
        }

        if (path.Mode == BindingModeSyntax.TwoWay && !resolution.CanWrite)
        {
            AddBindingSemanticDiagnostic(
                source,
                expression,
                path.ModeSpan,
                "A TwoWay Motion binding requires a writable source property.");
            return;
        }

        AddBindingModeSymbol(path, resolution.Type);
    }

    private static bool IsMotionReferenceTypeCompatible(
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

    private void AddMotionDiagnostic(string id, TextSpan span, string message) =>
        AddDiagnostic(id, span, Path.GetFileName(document.Path), message);

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
            .Any(keyword => text.IndexOf(keyword, StringComparison.Ordinal) >= 0) ||
        ContainsSoundSyntax(text);

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

    private sealed class MotionProgram
    {
        public MotionProgram(
            int offset,
            DirectiveDocumentSyntax syntax,
            IReadOnlyList<DirectiveRegion> regions,
            bool hasSyntaxErrors)
        {
            Offset = offset;
            Syntax = syntax;
            Regions = regions;
            HasSyntaxErrors = hasSyntaxErrors;
        }

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

}
