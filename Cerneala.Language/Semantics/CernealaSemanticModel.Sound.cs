using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private const string SoundClipTypeName = "Cerneala.Timbre.SoundClip";

    private readonly Dictionary<ElementSyntax, BoundSoundClip> soundClips = new();
    private readonly Dictionary<int, BoundSoundClip> soundClipsByElement = new();
    private readonly Dictionary<int, BoundSoundAspect> soundAspects = new();
    private readonly Dictionary<ElementSyntax, TextSpan[]> soundStatementSpans = new();
    private readonly HashSet<ElementSyntax> boundSoundAspects = new();
    private readonly Dictionary<BoundSoundClip, ResourceDefinition> soundClipResources = new();

    internal SoundMarkupModel Sound => new(
        new Dictionary<int, BoundSoundClip>(soundClipsByElement),
        new Dictionary<int, BoundSoundAspect>(soundAspects));

    private void BindSoundClipResource(ResourceDefinition resource)
    {
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Resource,
            resource.Name ?? resource.Element.Name,
            SoundClipTypeName,
            resource.NameSpan,
            resource.Type,
            definitionLocation: resource.Location));
        _ = GetBoundSoundClip(resource);
    }

    // Binds a SoundClip once; diagnostics are reported only for clips of the
    // current document. Application clips of other documents are bound
    // silently because their own document reports them.
    private BoundSoundClip GetBoundSoundClip(ResourceDefinition resource)
    {
        if (soundClips.TryGetValue(resource.Element, out BoundSoundClip? cached))
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
                AddDiagnostic("CERNEALAUI004", element.NameToken.Span, "SoundClip", "Name", string.Empty);
            }
        }

        AttributeSyntax? unsupported = element.Attributes.FirstOrDefault(attribute =>
            attribute.NameToken.Text is not "Name" && !attribute.NameToken.Text.StartsWith("xmlns", StringComparison.Ordinal));
        if (unsupported is not null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SoundMarkupSyntax.SyntaxId, "SoundClip supports only the Name attribute.", unsupported.NameToken.Span));
        }

        ElementSyntax? child = element.Children.OfType<ElementSyntax>().FirstOrDefault();
        if (child is not null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SoundMarkupSyntax.SyntaxId, "SoundClip does not accept child elements.", child.NameToken.Span));
        }

        (string text, int offset) = BuildSoundTextBuffer(owner, element);
        SoundClipBodySyntax body = SoundMarkupSyntax.ParseClipBody(text, offset);
        BoundSoundClip clip = SoundMarkupBinder.BindClip(resource.Name, body, element.NameToken.Span, diagnostics);
        if (diagnostics.Count > 0)
        {
            clip = new BoundSoundClip(clip.Name, clip.Source, clip.Volume, clip.Loop, clip.Parameters, clip.Modifiers, isValid: false);
        }

        soundClips[element] = clip;
        soundClipResources[clip] = resource;
        if (local)
        {
            soundClipsByElement[element.Span.Start] = clip;
            ReportEmbedded(diagnostics);
            AddSoundClipSymbols(body, clip, resource);
        }

        return clip;
    }

    private void AddSoundClipSymbols(SoundClipBodySyntax body, BoundSoundClip clip, ResourceDefinition resource)
    {
        foreach (object statement in body.Statements)
        {
            switch (statement)
            {
                case SoundValueSyntax property:
                    AddSoundSymbol(CernealaSemanticSymbolKind.SoundProperty, property.Name, SoundPropertyType(property.Name), property.NameSpan);
                    break;
                case SoundParameterSyntax parameter:
                    AddSoundSymbol(CernealaSemanticSymbolKind.SoundDirective, "@parameter", "Cerneala.Timbre.SoundParameter", parameter.KeywordSpan);
                    AddSoundSymbol(
                        CernealaSemanticSymbolKind.SoundParameter,
                        parameter.Name,
                        "System.Single",
                        parameter.NameSpan,
                        new LanguageSourceLocation(resource.Path, parameter.NameSpan));
                    break;
                case SoundModifierSyntax modifier:
                    AddSoundSymbol(CernealaSemanticSymbolKind.SoundDirective, "@modifier", "Cerneala.Timbre.SoundModifier", modifier.KeywordSpan);
                    AddSoundSymbol(CernealaSemanticSymbolKind.SoundModifier, modifier.Kind, "Cerneala.Timbre." + modifier.Kind, modifier.KindSpan);
                    foreach (SoundValueSyntax input in modifier.Inputs)
                    {
                        AddSoundSymbol(CernealaSemanticSymbolKind.SoundProperty, input.Name, "System.Single", input.NameSpan);
                        if (clip.FindParameter(input.Value.Trim()) is BoundSoundParameter parameter)
                        {
                            AddSoundSymbol(
                                CernealaSemanticSymbolKind.SoundParameter,
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

    private void AddSoundSymbol(
        CernealaSemanticSymbolKind kind,
        string name,
        string valueType,
        TextSpan span,
        LanguageSourceLocation? definition = null) =>
        symbols.Add(new CernealaSemanticSymbol(kind, name, valueType, span, definitionLocation: definition));

    private static string SoundPropertyType(string name) => name switch
    {
        "Source" => "Cerneala.Timbre.SoundSource",
        "Loop" => "System.Boolean",
        _ => "System.Single"
    };

    private void BindSoundAspect(ResourceDefinition aspect)
    {
        ElementSyntax element = aspect.Element;
        if (!boundSoundAspects.Add(element) ||
            !string.Equals(aspect.Path, document.Path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        (string text, int offset) = BuildDirectTextBuffer(element);
        if (!ContainsSoundSyntax(text))
        {
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
        List<(DirectiveRegion Region, SoundActionSyntax Syntax)> statements = new();
        Dictionary<string, SoundHandleKind> handles = declared.Keys.ToDictionary(
            name => name,
            name => motionHandles.Contains(name) ? SoundHandleKind.Motion : SoundHandleKind.Unused,
            StringComparer.Ordinal);
        HashSet<string> conflicts = new(StringComparer.Ordinal);
        foreach (DirectiveRegion region in regions)
        {
            if (region.Keyword == "@modifier")
            {
                diagnostics.Add(new EmbeddedDiagnostic(SoundMarkupBinder.ContextId, SoundMarkupBinder.ContextMessage("@modifier"), region.KeywordSpan));
                continue;
            }

            if (!SoundMarkupSyntax.IsActionKeyword(region.Keyword) ||
                !SoundMarkupSyntax.TryGetActionKind(region.Keyword, out SoundActionKind kind))
            {
                continue;
            }

            if (!ValidateSoundActionContext(region, regions, diagnostics))
            {
                continue;
            }

            string header = document.Text.Substring(region.HeaderSpan);
            int semicolon = header.LastIndexOf(';');
            string statementText = semicolon < 0 ? header : header.Substring(0, semicolon);
            SoundActionSyntax? syntax = SoundMarkupSyntax.ParseAction(kind, region.KeywordSpan, statementText, region.HeaderSpan.Start, diagnostics);
            if (syntax is null)
            {
                continue;
            }

            if (syntax.HandleName is string handle)
            {
                if (!declared.TryGetValue(handle, out TextSpan declaration) || declaration.Start > syntax.HandleSpan.Start)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(
                        SoundMarkupBinder.ReferenceId,
                        "Sound handle '" + handle + "' is undeclared or used before its declaration.",
                        syntax.HandleSpan));
                    continue;
                }

                if (motionHandles.Contains(handle))
                {
                    if (conflicts.Add(handle))
                    {
                        diagnostics.Add(new EmbeddedDiagnostic(
                            SoundMarkupBinder.ReferenceId,
                            "Handle '" + handle + "' is used by both Sound and Motion actions.",
                            syntax.HandleSpan));
                    }

                    continue;
                }

                handles[handle] = SoundHandleKind.Sound;
                symbols.Add(new CernealaSemanticSymbol(
                    CernealaSemanticSymbolKind.MotionHandle,
                    handle,
                    "Cerneala.Timbre.SoundHandle",
                    syntax.HandleSpan,
                    definitionLocation: new LanguageSourceLocation(document.Path, declaration)));
            }

            AddSoundSymbol(CernealaSemanticSymbolKind.SoundDirective, region.Keyword, "Cerneala.Timbre.SoundPlayback", region.KeywordSpan);
            statements.Add((region, syntax));
        }

        List<BoundSoundAction> actions = new();
        foreach (DirectiveRegion region in regions)
        {
            if (region.Keyword == "@cancel")
            {
                string handle = FirstWord(document.Text.Substring(region.HeaderSpan).TrimEnd(';'));
                if (handles.TryGetValue(handle, out SoundHandleKind cancelKind) && cancelKind == SoundHandleKind.Sound)
                {
                    actions.Add(new BoundSoundAction(SoundActionKind.Cancel, region.KeywordSpan, handle));
                }

                continue;
            }

            (DirectiveRegion Region, SoundActionSyntax Syntax) statement = statements.FirstOrDefault(candidate => ReferenceEquals(candidate.Region, region));
            if (statement.Syntax is null)
            {
                continue;
            }

            SoundActionSyntax action = statement.Syntax;
            switch (action.Kind)
            {
                case SoundActionKind.Play:
                    if (BindSoundPlay(element, action, diagnostics) is BoundSoundAction play)
                    {
                        actions.Add(play);
                    }

                    break;
                case SoundActionKind.Seek:
                    if (SoundMarkupBinder.TryBindSeek(action, diagnostics, out long ticks))
                    {
                        actions.Add(new BoundSoundAction(SoundActionKind.Seek, action.KeywordSpan, action.HandleName, seekTicks: ticks));
                    }

                    break;
                default:
                    actions.Add(new BoundSoundAction(action.Kind, action.KeywordSpan, action.HandleName));
                    break;
            }
        }

        ReportEmbedded(diagnostics);
        soundAspects[element.Span.Start] = new BoundSoundAspect(element.Span.Start, actions, handles);
    }

    private BoundSoundAction? BindSoundPlay(
        ElementSyntax aspect,
        SoundActionSyntax syntax,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        string name = syntax.ClipName!;
        ResourceDefinition? resource = FindResource(aspect, name);
        if (resource is null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SoundMarkupBinder.ReferenceId, "Unknown SoundClip resource '$" + name + "'.", syntax.ClipSpan));
            return null;
        }

        if (resource.Kind != ResourceKind.SoundClip)
        {
            diagnostics.Add(new EmbeddedDiagnostic(SoundMarkupBinder.ReferenceId, "Resource '$" + name + "' is not a SoundClip.", syntax.ClipSpan));
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ResourceReference,
            name,
            SoundClipTypeName,
            new TextSpan(syntax.ClipSpan.Start + 1, name.Length),
            resource.Type,
            definitionLocation: resource.Location));
        BoundSoundClip clip = GetBoundSoundClip(resource);
        foreach (SoundValueSyntax argument in syntax.Arguments)
        {
            if (argument.Name is "Volume" or "Loop")
            {
                AddSoundSymbol(CernealaSemanticSymbolKind.SoundProperty, argument.Name, SoundPropertyType(argument.Name), argument.NameSpan);
            }
            else if (clip.FindParameter(argument.Name) is BoundSoundParameter parameter)
            {
                AddSoundSymbol(
                    CernealaSemanticSymbolKind.SoundParameter,
                    parameter.Name,
                    "System.Single",
                    argument.NameSpan,
                    new LanguageSourceLocation(resource.Path, parameter.NameSpan));
            }
        }

        return SoundMarkupBinder.BindPlay(syntax, clip, diagnostics);
    }

    // Completion support: the parameters a $Clip(...) call can override.
    internal IReadOnlyList<CompletionParameterDefinition>? GetCompletionSoundParameters(ElementSyntax? element, string clipName)
    {
        ResourceDefinition? resource = element is null ? null : FindResource(element, clipName);
        if (resource?.Kind != ResourceKind.SoundClip)
        {
            return null;
        }

        BoundSoundClip clip = GetBoundSoundClip(resource);
        return new[]
            {
                new CompletionParameterDefinition("Volume", "System.Single", false),
                new CompletionParameterDefinition("Loop", "System.Boolean", false)
            }
            .Concat(clip.Parameters.Select(parameter => new CompletionParameterDefinition(parameter.Name, "System.Single", false)))
            .ToArray();
    }

    // Completion support: the inputs of a modifier and the parameters a
    // SoundClip body has declared before the offset.
    internal IReadOnlyList<string> GetCompletionSoundClipParameters(ElementSyntax? clipElement, int offset) =>
        clipElement is not null && soundClips.TryGetValue(clipElement, out BoundSoundClip? clip)
            ? clip.Parameters.Where(parameter => parameter.NameSpan.Start < offset).Select(parameter => parameter.Name).ToArray()
            : Array.Empty<string>();

    private static bool ValidateSoundActionContext(
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
                SoundMarkupBinder.ContextId,
                action.Keyword + " cannot be orchestrated inside @parallel or @sequence.",
                action.KeywordSpan));
            return false;
        }

        if (enclosing.Length == 0 || enclosing[0].Keyword is not ("@on" or "@when" or "@if"))
        {
            diagnostics.Add(new EmbeddedDiagnostic(SoundMarkupBinder.ContextId, SoundMarkupBinder.ContextMessage(action.Keyword), action.KeywordSpan));
            return false;
        }

        return true;
    }

    // Element traversal stops at an Aspect body, so SoundClip resources and
    // inline Aspects nested in another Aspect's @template are bound here; every
    // Sound statement of the document then has exactly one bound owner.
    private void BindNestedSoundOwners()
    {
        foreach (ResourceDefinition resource in resourceElements.Values
            .Where(resource => resource.Kind is ResourceKind.SoundClip or ResourceKind.Aspect &&
                string.Equals(resource.Path, document.Path, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .OrderBy(resource => resource.Element.Span.Start))
        {
            if (resource.Kind == ResourceKind.SoundClip)
            {
                if (!soundClips.ContainsKey(resource.Element))
                {
                    BindSoundClipResource(resource);
                }
            }
            else
            {
                BindSoundAspect(resource);
            }
        }
    }

    // Reports Sound directives written outside SoundClip and Aspect bodies:
    // element content, MotionClip and every other directive host.
    private void ReportSoundDirectivesOutsideAspects()
    {
        foreach (ElementSyntax element in document.Syntax.DescendantElements())
        {
            if (inlineAspectProperties.ContainsKey(element) ||
                resourceElements.TryGetValue(element, out ResourceDefinition? resource) &&
                resource.Kind is ResourceKind.SoundClip or ResourceKind.Aspect)
            {
                continue;
            }

            // Check the direct text nodes first: building the blanked buffer
            // spans the whole element content and would cost O(size x depth).
            if (!element.Children.OfType<TextSyntax>().Any(text =>
                text.Kind != SyntaxKind.Comment && ContainsSoundSyntax(document.Text.Substring(text.Span))))
            {
                continue;
            }

            (string text, int offset) = BuildSoundTextBuffer(document, element);
            EmbeddedParseResult<DirectiveDocumentSyntax> parsed = DirectiveSyntaxParser.Parse(text, offset);
            foreach (DirectiveSyntax directive in parsed.Syntax.Directives.Where(directive =>
                SoundMarkupSyntax.IsActionKeyword(directive.Keyword) || directive.Keyword == "@modifier"))
            {
                AddDiagnostic(
                    SoundMarkupBinder.ContextId,
                    directive.Span,
                    Path.GetFileName(document.Path),
                    SoundMarkupBinder.ContextMessage(directive.Keyword));
            }
        }
    }

    private bool IsInsideSoundStatement(ElementSyntax aspect, int position)
    {
        if (!soundStatementSpans.TryGetValue(aspect, out TextSpan[]? spans))
        {
            (string text, int offset) = BuildDirectTextBuffer(aspect);
            spans = ContainsSoundSyntax(text)
                ? DirectiveSyntaxParser.Parse(text, offset).Syntax.Directives
                    .Where(directive => SoundMarkupSyntax.IsActionKeyword(directive.Keyword))
                    .Select(directive => CreateDirectiveRegion(text, offset, directive))
                    .Select(region => new TextSpan(region.KeywordSpan.Start, Math.Max(0, region.HeaderSpan.End - region.KeywordSpan.Start)))
                    .ToArray()
                : Array.Empty<TextSpan>();
            soundStatementSpans.Add(aspect, spans);
        }

        return spans.Any(span => span.Contains(position));
    }

    private static bool ContainsSoundSyntax(string text) =>
        SoundMarkupSyntax.ActionKeywords.Any(keyword => text.IndexOf(keyword, StringComparison.Ordinal) >= 0) ||
        text.IndexOf("@modifier", StringComparison.Ordinal) >= 0;

    // Direct text of an element in any analyzed document, with child elements
    // and comments blanked so offsets stay absolute.
    private static (string Text, int Offset) BuildSoundTextBuffer(CernealaDocument owner, ElementSyntax element)
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
