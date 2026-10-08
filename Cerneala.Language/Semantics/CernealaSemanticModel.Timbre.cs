using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private const string TimbreClipTypeName = "Cerneala.Timbre.TimbreClip";

    private readonly Dictionary<ElementSyntax, BoundTimbreClip> timbreClips = new();
    private readonly Dictionary<int, BoundTimbreClip> timbreClipsByElement = new();
    private readonly Dictionary<int, BoundTimbreAspect> timbreAspects = new();
    private readonly Dictionary<ElementSyntax, TextSpan[]> timbreStatementSpans = new();
    private readonly HashSet<ElementSyntax> boundTimbreAspects = new();
    private readonly Dictionary<BoundTimbreClip, ResourceDefinition> timbreClipResources = new();

    internal TimbreMarkupModel Timbre => new(
        new Dictionary<int, BoundTimbreClip>(timbreClipsByElement),
        new Dictionary<int, BoundTimbreAspect>(timbreAspects));

    private void BindTimbreClipResource(ResourceDefinition resource)
    {
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Resource,
            resource.Name ?? resource.Element.Name,
            TimbreClipTypeName,
            resource.NameSpan,
            resource.Type,
            definitionLocation: resource.Location));
        _ = GetBoundTimbreClip(resource);
    }

    // Binds a TimbreClip once; diagnostics are reported only for clips of the
    // current document. Application clips of other documents are bound
    // silently because their own document reports them.
    private BoundTimbreClip GetBoundTimbreClip(ResourceDefinition resource)
    {
        if (timbreClips.TryGetValue(resource.Element, out BoundTimbreClip? cached))
        {
            return cached;
        }

        bool local = string.Equals(resource.Path, document.Path, StringComparison.OrdinalIgnoreCase);
        CernealaDocument owner = local
            ? document
            : documents.FirstOrDefault(candidate => string.Equals(candidate.Path, resource.Path, StringComparison.OrdinalIgnoreCase)) ?? document;
        ElementSyntax element = resource.Element;
        List<EmbeddedDiagnostic> diagnostics = new();
        if (resource.Name is null)
        {
            if (local)
            {
                AddDiagnostic("CERNEALAUI004", element.NameToken.Span, "TimbreClip", "Name", string.Empty);
            }
        }

        AttributeSyntax? unsupported = element.Attributes.FirstOrDefault(attribute =>
            attribute.NameToken.Text is not "Name" && !attribute.NameToken.Text.StartsWith("xmlns", StringComparison.Ordinal));
        if (unsupported is not null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupSyntax.SyntaxId, "TimbreClip supports only the Name attribute.", unsupported.NameToken.Span));
        }

        ElementSyntax? child = element.Children.OfType<ElementSyntax>().FirstOrDefault();
        if (child is not null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupSyntax.SyntaxId, "TimbreClip does not accept child elements.", child.NameToken.Span));
        }

        (string text, int offset) = BuildTimbreTextBuffer(owner, element);
        TimbreClipBodySyntax body = TimbreMarkupSyntax.ParseClipBody(text, offset);
        BoundTimbreClip clip = TimbreMarkupBinder.BindClip(resource.Name, body, element.NameToken.Span, diagnostics);
        if (diagnostics.Count > 0)
        {
            clip = new BoundTimbreClip(clip.Name, clip.Source, clip.Volume, clip.Loop, clip.Parameters, clip.Modifiers, isValid: false);
        }

        timbreClips[element] = clip;
        timbreClipResources[clip] = resource;
        if (local)
        {
            timbreClipsByElement[element.Span.Start] = clip;
            ReportEmbedded(diagnostics);
            AddTimbreClipSymbols(body, clip, resource);
        }

        return clip;
    }

    private void AddTimbreClipSymbols(TimbreClipBodySyntax body, BoundTimbreClip clip, ResourceDefinition resource)
    {
        foreach (object statement in body.Statements)
        {
            switch (statement)
            {
                case TimbreValueSyntax property:
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreProperty, property.Name, TimbrePropertyType(property.Name), property.NameSpan);
                    break;
                case TimbreParameterSyntax parameter:
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, "@parameter", "Cerneala.Timbre.TimbreParameter", parameter.KeywordSpan);
                    AddTimbreSymbol(
                        CernealaSemanticSymbolKind.TimbreParameter,
                        parameter.Name,
                        "System.Single",
                        parameter.NameSpan,
                        new LanguageSourceLocation(resource.Path, parameter.NameSpan));
                    break;
                case TimbreModifierSyntax modifier:
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, "@modifier", "Cerneala.Timbre.TimbreModifier", modifier.KeywordSpan);
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreModifier, modifier.Kind, "Cerneala.Timbre." + modifier.Kind, modifier.KindSpan);
                    foreach (TimbreValueSyntax input in modifier.Inputs)
                    {
                        AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreProperty, input.Name, "System.Single", input.NameSpan);
                        if (clip.FindParameter(input.Value.Trim()) is BoundTimbreParameter parameter)
                        {
                            AddTimbreSymbol(
                                CernealaSemanticSymbolKind.TimbreParameter,
                                parameter.Name,
                                "System.Single",
                                input.ValueSpan,
                                new LanguageSourceLocation(resource.Path, parameter.NameSpan));
                        }
                    }

                    break;
            }
        }
    }

    private void AddTimbreSymbol(
        CernealaSemanticSymbolKind kind,
        string name,
        string valueType,
        TextSpan span,
        LanguageSourceLocation? definition = null) =>
        symbols.Add(new CernealaSemanticSymbol(kind, name, valueType, span, definitionLocation: definition));

    private static string TimbrePropertyType(string name) => name switch
    {
        "Source" => "Cerneala.Timbre.TimbreSource",
        "Loop" => "System.Boolean",
        _ => "System.Single"
    };

    private void BindTimbreAspect(ResourceDefinition aspect)
    {
        ElementSyntax element = aspect.Element;
        if (!boundTimbreAspects.Add(element) ||
            !string.Equals(aspect.Path, document.Path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        (string text, int offset) = BuildDirectTextBuffer(element);
        if (!ContainsTimbreSyntax(text))
        {
            BindTimbreMotionTargets(
                element,
                new Dictionary<string, TimbreHandleKind>(),
                new Dictionary<string, TextSpan>(),
                Array.Empty<BoundTimbreAction>(),
                new Dictionary<string, IReadOnlyList<BoundTimbreHandleParameter>>());
            return;
        }

        EmbeddedParseResult<DirectiveDocumentSyntax> parsed = MotionSyntaxParser.Parse(text, offset);
        if (parsed.Diagnostics.Count > 0)
        {
            // ParseMotionProgram already reported the first syntax diagnostic.
            return;
        }

        DirectiveRegion[] regions = parsed.Syntax.Directives
            .Select(directive => CreateDirectiveRegion(text, offset, directive))
            .OrderBy(region => region.KeywordSpan.Start)
            .ToArray();
        Dictionary<string, TextSpan> declared = new(StringComparer.Ordinal);
        foreach (DirectiveRegion region in regions.Where(region => region.Keyword == "@handle"))
        {
            string name = FirstWord(document.Text.Substring(region.HeaderSpan));
            if (name.Length > 0 && !declared.ContainsKey(name))
            {
                declared.Add(name, FindSubspan(region.HeaderSpan, name));
            }
        }

        HashSet<string> motionHandles = new(StringComparer.Ordinal);
        foreach (DirectiveRegion region in regions.Where(region => region.Keyword == "@run"))
        {
            string header = document.Text.Substring(region.HeaderSpan).Trim().TrimEnd(';').Trim();
            int asIndex = header.LastIndexOf(" as ", StringComparison.Ordinal);
            if (asIndex >= 0)
            {
                motionHandles.Add(header.Substring(asIndex + 4).Trim());
            }
        }

        List<EmbeddedDiagnostic> diagnostics = new();
        List<(DirectiveRegion Region, TimbreActionSyntax Syntax)> statements = new();
        Dictionary<string, TimbreHandleKind> handles = declared.Keys.ToDictionary(
            name => name,
            name => motionHandles.Contains(name) ? TimbreHandleKind.Motion : TimbreHandleKind.Unused,
            StringComparer.Ordinal);
        HashSet<string> conflicts = new(StringComparer.Ordinal);
        foreach (DirectiveRegion region in regions)
        {
            if (region.Keyword == "@modifier")
            {
                diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ContextId, TimbreMarkupBinder.ContextMessage("@modifier"), region.KeywordSpan));
                continue;
            }

            if (!TimbreMarkupSyntax.IsActionKeyword(region.Keyword) ||
                !TimbreMarkupSyntax.TryGetActionKind(region.Keyword, out TimbreActionKind kind))
            {
                continue;
            }

            if (!ValidateTimbreActionContext(region, regions, diagnostics))
            {
                continue;
            }

            string header = document.Text.Substring(region.HeaderSpan);
            int semicolon = header.LastIndexOf(';');
            string statementText = semicolon < 0 ? header : header.Substring(0, semicolon);
            TimbreActionSyntax? syntax = TimbreMarkupSyntax.ParseAction(kind, region.KeywordSpan, statementText, region.HeaderSpan.Start, diagnostics);
            if (syntax is null)
            {
                continue;
            }

            if (syntax.HandleName is string handle)
            {
                if (!declared.TryGetValue(handle, out TextSpan declaration) || declaration.Start > syntax.HandleSpan.Start)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(
                        TimbreMarkupBinder.ReferenceId,
                        "Timbre handle '" + handle + "' is undeclared or used before its declaration.",
                        syntax.HandleSpan));
                    continue;
                }

                if (motionHandles.Contains(handle))
                {
                    if (conflicts.Add(handle))
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(
                            TimbreMarkupBinder.ReferenceId,
                            "Handle '" + handle + "' is used by both Timbre and Motion actions.",
                            syntax.HandleSpan));
                    }

                    continue;
                }

                handles[handle] = TimbreHandleKind.Timbre;
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.MotionHandle,
                    handle,
                    "Cerneala.Timbre.TimbreHandle",
                    syntax.HandleSpan,
                    definitionLocation: new LanguageSourceLocation(document.Path, declaration)));
            }

            AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, region.Keyword, "Cerneala.Timbre.TimbrePlayback", region.KeywordSpan);
            statements.Add((region, syntax));
        }

        List<BoundTimbreAction> actions = new();
        foreach (DirectiveRegion region in regions)
        {
            if (region.Keyword == "@cancel")
            {
                string handle = FirstWord(document.Text.Substring(region.HeaderSpan).TrimEnd(';'));
                if (handles.TryGetValue(handle, out TimbreHandleKind cancelKind) && cancelKind == TimbreHandleKind.Timbre)
                {
                    actions.Add(new BoundTimbreAction(TimbreActionKind.Cancel, region.KeywordSpan, handle));
                }

                continue;
            }

            (DirectiveRegion Region, TimbreActionSyntax Syntax) statement = statements.FirstOrDefault(candidate => ReferenceEquals(candidate.Region, region));
            if (statement.Syntax is null)
            {
                continue;
            }

            TimbreActionSyntax action = statement.Syntax;
            switch (action.Kind)
            {
                case TimbreActionKind.Play:
                    if (BindTimbrePlay(element, action, diagnostics) is BoundTimbreAction play)
                    {
                        actions.Add(play);
                    }

                    break;
                case TimbreActionKind.Seek:
                    if (TimbreMarkupBinder.TryBindSeek(action, diagnostics, out long ticks))
                    {
                        actions.Add(new BoundTimbreAction(TimbreActionKind.Seek, action.KeywordSpan, action.HandleName, seekTicks: ticks));
                    }

                    break;
                default:
                    actions.Add(new BoundTimbreAction(action.Kind, action.KeywordSpan, action.HandleName));
                    break;
            }
        }

        ReportEmbedded(diagnostics);
        IReadOnlyDictionary<string, IReadOnlyList<BoundTimbreHandleParameter>> handleParameters = BindTimbreHandleParameters(handles, actions);
        timbreAspects[element.Span.Start] = new BoundTimbreAspect(element.Span.Start, actions, handles, handleParameters);
        BindTimbreMotionTargets(element, handles, declared, actions, handleParameters);
    }

    private BoundTimbreAction? BindTimbrePlay(
        ElementSyntax aspect,
        TimbreActionSyntax syntax,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        string name = syntax.ClipName!;
        ResourceDefinition? resource = FindResource(aspect, name);
        if (resource is null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ReferenceId, "Unknown TimbreClip resource '$" + name + "'.", syntax.ClipSpan));
            return null;
        }

        if (resource.Kind != ResourceKind.TimbreClip)
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ReferenceId, "Resource '$" + name + "' is not a TimbreClip.", syntax.ClipSpan));
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ResourceReference,
            name,
            TimbreClipTypeName,
            new TextSpan(syntax.ClipSpan.Start + 1, name.Length),
            resource.Type,
            definitionLocation: resource.Location));
        BoundTimbreClip clip = GetBoundTimbreClip(resource);
        foreach (TimbreValueSyntax argument in syntax.Arguments)
        {
            if (argument.Name is "Volume" or "Loop")
            {
                AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreProperty, argument.Name, TimbrePropertyType(argument.Name), argument.NameSpan);
            }
            else if (clip.FindParameter(argument.Name) is BoundTimbreParameter parameter)
            {
                AddTimbreSymbol(
                    CernealaSemanticSymbolKind.TimbreParameter,
                    parameter.Name,
                    "System.Single",
                    argument.NameSpan,
                    new LanguageSourceLocation(resource.Path, parameter.NameSpan));
            }
        }

        return TimbreMarkupBinder.BindPlay(syntax, clip, diagnostics);
    }

    // Completion support: the parameters a $Clip(...) call can override.
    internal IReadOnlyList<CompletionParameterDefinition>? GetCompletionTimbreParameters(ElementSyntax? element, string clipName)
    {
        ResourceDefinition? resource = element is null ? null : FindResource(element, clipName);
        if (resource?.Kind != ResourceKind.TimbreClip)
        {
            return null;
        }

        BoundTimbreClip clip = GetBoundTimbreClip(resource);
        return new[]
            {
                new CompletionParameterDefinition("Volume", "System.Single", false),
                new CompletionParameterDefinition("Loop", "System.Boolean", false)
            }
            .Concat(clip.Parameters.Select(parameter => new CompletionParameterDefinition(parameter.Name, "System.Single", false)))
            .ToArray();
    }

    // Completion support: the inputs of a modifier and the parameters a
    // TimbreClip body has declared before the offset.
    internal IReadOnlyList<string> GetCompletionTimbreClipParameters(ElementSyntax? clipElement, int offset) =>
        clipElement is not null && timbreClips.TryGetValue(clipElement, out BoundTimbreClip? clip)
            ? clip.Parameters.Where(parameter => parameter.NameSpan.Start < offset).Select(parameter => parameter.Name).ToArray()
            : Array.Empty<string>();

    private static bool ValidateTimbreActionContext(
        DirectiveRegion action,
        IReadOnlyList<DirectiveRegion> regions,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        DirectiveRegion[] enclosing = regions
            .Where(region => !ReferenceEquals(region, action) && region.BodySpan.Length > 0 &&
                region.BodySpan.Contains(action.KeywordSpan.Start))
            .OrderBy(region => region.BodySpan.Length)
            .ToArray();
        if (enclosing.Any(region => region.Keyword is "@parallel" or "@sequence"))
        {
            diagnostics.Add(new EmbeddedDiagnostic(
                TimbreMarkupBinder.ContextId,
                action.Keyword + " cannot be orchestrated inside @parallel or @sequence.",
                action.KeywordSpan));
            return false;
        }

        if (enclosing.Length == 0 || enclosing[0].Keyword is not ("@on" or "@when" or "@if"))
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ContextId, TimbreMarkupBinder.ContextMessage(action.Keyword), action.KeywordSpan));
            return false;
        }

        return true;
    }

    // Element traversal stops at an Aspect body, so TimbreClip resources and
    // inline Aspects nested in another Aspect's @template are bound here; every
    // Timbre statement of the document then has exactly one bound owner.
    private void BindNestedTimbreOwners()
    {
        foreach (ResourceDefinition resource in resourceElements.Values
            .Where(resource => resource.Kind is ResourceKind.TimbreClip or ResourceKind.Aspect &&
                string.Equals(resource.Path, document.Path, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(resource => resource.Element.Span.Start))
        {
            if (resource.Kind == ResourceKind.TimbreClip)
            {
                if (!timbreClips.ContainsKey(resource.Element))
                {
                    BindTimbreClipResource(resource);
                }
            }
            else
            {
                BindTimbreAspect(resource);
            }
        }
    }

    // Reports Timbre directives written outside TimbreClip and Aspect bodies:
    // element content, MotionClip and every other directive host.
    private void ReportTimbreDirectivesOutsideAspects()
    {
        foreach (ElementSyntax element in document.Syntax.DescendantElements())
        {
            if (inlineAspectProperties.ContainsKey(element) ||
                resourceElements.TryGetValue(element, out ResourceDefinition? resource) &&
                resource.Kind is ResourceKind.TimbreClip or ResourceKind.Aspect)
            {
                continue;
            }

            // Check the direct text nodes first: building the blanked buffer
            // spans the whole element content and would cost O(size x depth).
            if (!element.Children.OfType<TextSyntax>().Any(text =>
                text.Kind != SyntaxKind.Comment && ContainsTimbreSyntax(document.Text.Substring(text.Span))))
            {
                continue;
            }

            (string text, int offset) = BuildTimbreTextBuffer(document, element);
            EmbeddedParseResult<DirectiveDocumentSyntax> parsed = DirectiveSyntaxParser.Parse(text, offset);
            foreach (DirectiveSyntax directive in parsed.Syntax.Directives.Where(directive =>
                TimbreMarkupSyntax.IsActionKeyword(directive.Keyword) || directive.Keyword == "@modifier"))
            {
                AddDiagnostic(
                    TimbreMarkupBinder.ContextId,
                    directive.Span,
                    Path.GetFileName(document.Path),
                    TimbreMarkupBinder.ContextMessage(directive.Keyword));
            }
        }
    }

    private bool IsInsideTimbreStatement(ElementSyntax aspect, int position)
    {
        if (!timbreStatementSpans.TryGetValue(aspect, out TextSpan[]? spans))
        {
            (string text, int offset) = BuildDirectTextBuffer(aspect);
            spans = ContainsTimbreSyntax(text)
                ? DirectiveSyntaxParser.Parse(text, offset).Syntax.Directives
                    .Where(directive => TimbreMarkupSyntax.IsActionKeyword(directive.Keyword))
                    .Select(directive => CreateDirectiveRegion(text, offset, directive))
                    .Select(region => new TextSpan(region.KeywordSpan.Start, Math.Max(0, region.HeaderSpan.End - region.KeywordSpan.Start)))
                    .ToArray()
                : Array.Empty<TextSpan>();
            timbreStatementSpans.Add(aspect, spans);
        }

        return spans.Any(span => span.Contains(position));
    }

    private static bool ContainsTimbreSyntax(string text) =>
        TimbreMarkupSyntax.ActionKeywords.Any(keyword => text.IndexOf(keyword, StringComparison.Ordinal) >= 0) ||
        text.IndexOf("@modifier", StringComparison.Ordinal) >= 0;

    // Direct text of an element in any analyzed document, with child elements
    // and comments blanked so offsets stay absolute.
    private static (string Text, int Offset) BuildTimbreTextBuffer(CernealaDocument owner, ElementSyntax element)
    {
        int start = element.OpenEndToken.Span.End;
        int end = element.CloseLessThanToken.IsMissing ? element.Span.End : element.CloseLessThanToken.Span.Start;
        if (end <= start)
        {
            return (string.Empty, start);
        }

        char[] buffer = Enumerable.Repeat(' ', end - start).ToArray();
        foreach (TextSyntax text in element.Children.OfType<TextSyntax>().Where(text => text.Kind != SyntaxKind.Comment))
        {
            string value = owner.Text.Substring(text.Span);
            value.CopyTo(0, buffer, text.Span.Start - start, value.Length);
        }

        return (new string(buffer), start);
    }

    private void ReportEmbedded(IEnumerable<EmbeddedDiagnostic> diagnostics)
    {
        foreach (EmbeddedDiagnostic diagnostic in diagnostics)
        {
            AddDiagnostic(diagnostic.Id, diagnostic.Span, Path.GetFileName(document.Path), diagnostic.Message);
        }
    }
}
