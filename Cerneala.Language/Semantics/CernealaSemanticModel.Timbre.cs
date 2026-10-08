using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

internal sealed partial class CernealaSemanticModel
{
    private const string TimbreClipDefinitionTypeName = "Cerneala.Timbre.TimbreClipDefinition";
    private const string ApplicationTimbreTargetMessage = "An Aspect in Application resources can address only $self and $owner.";

    private readonly Dictionary<ElementSyntax, BoundTimbreClip> timbreClips = new();
    private readonly Dictionary<int, BoundTimbreClip> timbreClipsByElement = new();
    private readonly Dictionary<int, BoundTimbreAspect> timbreAspects = new();
    private readonly Dictionary<ElementSyntax, TextSpan[]> timbreStatementSpans = new();
    private readonly HashSet<ElementSyntax> boundTimbreAspects = new();
    private readonly Dictionary<BoundTimbreClip, string> timbreClipPaths = new();

    internal TimbreMarkupModel Timbre => new(
        new Dictionary<int, BoundTimbreClip>(timbreClipsByElement),
        new Dictionary<int, BoundTimbreAspect>(timbreAspects));

    private void BindTimbreClipResource(ResourceDefinition resource)
    {
        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.Resource,
            resource.Name ?? resource.Element.Name,
            TimbreClipDefinitionTypeName,
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
            clip = new BoundTimbreClip(clip.Name, clip.Parameters, clip.Sounds, isValid: false);
        }

        timbreClips[element] = clip;
        timbreClipPaths[clip] = resource.Path;
        if (local)
        {
            timbreClipsByElement[element.Span.Start] = clip;
            ReportEmbedded(diagnostics);
            AddTimbreClipSymbols(body, clip, resource.Path);
        }

        return clip;
    }

    private void AddTimbreClipSymbols(TimbreClipBodySyntax body, BoundTimbreClip clip, string path)
    {
        timbreClipPaths[clip] = path;
        foreach (object statement in body.Statements)
        {
            switch (statement)
            {
                case TimbreParameterSyntax parameter:
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, "@parameter", "Cerneala.Timbre.TimbreParameter", parameter.KeywordSpan);
                    AddTimbreSymbol(
                        CernealaSemanticSymbolKind.TimbreParameter,
                        parameter.Name,
                        "System.Single",
                        parameter.NameSpan,
                        new LanguageSourceLocation(path, parameter.NameSpan));
                    break;
                case TimbreSoundSyntax sound:
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, "@sound", "Cerneala.Timbre.TimbreClipSound", sound.KeywordSpan);
                    AddTimbreSymbol(
                        CernealaSemanticSymbolKind.TimbreSound,
                        sound.Name,
                        "Cerneala.Timbre.TimbreClipSound",
                        sound.NameSpan,
                        new LanguageSourceLocation(path, sound.NameSpan));
                    AddTimbreSoundSymbols(sound, clip, path);
                    break;
            }
        }
    }

    private void AddTimbreSoundSymbols(TimbreSoundSyntax sound, BoundTimbreClip clip, string path)
    {
        foreach (object statement in sound.Statements)
        {
            switch (statement)
            {
                case TimbreValueSyntax property:
                    AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreProperty, property.Name, TimbrePropertyType(property.Name), property.NameSpan);
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
                                new LanguageSourceLocation(path, parameter.NameSpan));
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
        "Loop" or "AutoPlay" => "System.Boolean",
        _ => "System.Single"
    };

    // Binds the @timbre attachment and the Timbre commands of one Aspect of
    // this document.
    private void BindTimbreAspect(ResourceDefinition aspect)
    {
        ElementSyntax element = aspect.Element;
        if (!boundTimbreAspects.Add(element) ||
            !string.Equals(aspect.Path, document.Path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        AspectAttachmentBinding attachments = GetAspectAttachments(aspect);
        (string text, int offset) = BuildDirectTextBuffer(element);
        List<BoundTimbreCommand> commands = new();
        if (ContainsTimbreSyntax(text))
        {
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
            List<EmbeddedDiagnostic> diagnostics = new();
            foreach (DirectiveRegion region in regions)
            {
                if (region.Keyword == "@modifier" || region.Keyword == "@sound")
                {
                    diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ContextId, TimbreMarkupBinder.ContextMessage(region.Keyword), region.KeywordSpan));
                    continue;
                }

                if (!TimbreMarkupSyntax.TryGetCommandKind(region.Keyword, out TimbreCommandKind kind) ||
                    !ValidateTimbreActionContext(region, regions, diagnostics))
                {
                    continue;
                }

                string header = document.Text.Substring(region.HeaderSpan);
                int semicolon = header.LastIndexOf(';');
                string statementText = semicolon < 0 ? header : header.Substring(0, semicolon);
                TimbreCommandSyntax? syntax = TimbreMarkupSyntax.ParseCommand(kind, region.KeywordSpan, statementText, region.HeaderSpan.Start, diagnostics);
                if (syntax is null)
                {
                    continue;
                }

                long seekTicks = 0;
                if (kind == TimbreCommandKind.Seek && !TimbreMarkupBinder.TryBindSeek(syntax, diagnostics, out seekTicks))
                {
                    continue;
                }

                if (!TryResolveTimbreSound(aspect, attachments, syntax.Owner, syntax.OwnerSpan, syntax.Sound, syntax.SoundSpan, diagnostics,
                    out TimbreCommandTarget target, out _))
                {
                    continue;
                }

                AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, region.Keyword, "Cerneala.Timbre.TimbrePlayback", region.KeywordSpan);
                commands.Add(new BoundTimbreCommand(
                    kind,
                    region.KeywordSpan,
                    target,
                    target == TimbreCommandTarget.Named ? syntax.Owner : null,
                    syntax.Sound,
                    seekTicks));
            }

            ReportEmbedded(diagnostics);
        }

        if (attachments.Timbre is not null || commands.Count > 0)
        {
            timbreAspects[element.Span.Start] = new BoundTimbreAspect(element.Span.Start, attachments.Timbre, commands);
        }

        BindTimbreMotionTargets(aspect, attachments);
    }

    // Resolves `$Owner.timbre.Sound` written in `aspect`. $self is checked
    // against the Aspect's own @timbre; $Name against the static Aspect of the
    // named element in the namescope where the Aspect is written; $owner is
    // checked at runtime.
    private bool TryResolveTimbreSound(
        ResourceDefinition aspect,
        AspectAttachmentBinding attachments,
        string owner,
        TextSpan ownerSpan,
        string sound,
        TextSpan soundSpan,
        ICollection<EmbeddedDiagnostic> diagnostics,
        out TimbreCommandTarget target,
        out BoundTimbreSound? resolved)
    {
        resolved = null;
        BoundTimbreClip? clip;
        string ownerText = "$" + owner;
        if (owner == "self")
        {
            target = TimbreCommandTarget.Self;
            clip = attachments.Timbre?.Clip;
        }
        else if (owner == "owner")
        {
            target = TimbreCommandTarget.Owner;
            return true;
        }
        else
        {
            target = TimbreCommandTarget.Named;
            if (root?.Name == "Application")
            {
                diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ReferenceId, ApplicationTimbreTargetMessage, ownerSpan));
                return false;
            }

            if (FindNamedElement(aspect.Element, owner) is not NamedElementDefinition named)
            {
                diagnostics.Add(new EmbeddedDiagnostic(
                    TimbreMarkupBinder.ReferenceId,
                    "Named element '" + ownerText + "' is not available in the namescope where this Aspect is written.",
                    ownerSpan));
                return false;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionTarget,
                owner,
                named.Type?.MetadataName ?? "System.Object",
                ownerSpan,
                named.Type,
                definitionLocation: new LanguageSourceLocation(document.Path, named.Span)));
            clip = FindStaticAspect(named.Element) is ResourceDefinition targetAspect
                ? GetAspectAttachments(targetAspect).Timbre?.Clip
                : null;
        }

        resolved = clip?.FindSound(sound);
        if (resolved is null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(
                TimbreMarkupBinder.ReferenceId,
                "'" + ownerText + "' has no sound '" + sound + "' in its Aspect.",
                soundSpan));
            return false;
        }

        AddTimbreSymbol(
            CernealaSemanticSymbolKind.TimbreSound,
            sound,
            "Cerneala.Timbre.TimbreClipSound",
            soundSpan,
            timbreClipPaths.TryGetValue(clip!, out string? path) ? new LanguageSourceLocation(path, resolved.NameSpan) : null);
        return true;
    }

    // Completion support: the parameters a `@timbre $Clip(...)` can set.
    internal IReadOnlyList<CompletionParameterDefinition>? GetCompletionTimbreParameters(ElementSyntax? element, string clipName)
    {
        ResourceDefinition? resource = element is null ? null : FindResource(element, clipName);
        if (resource?.Kind != ResourceKind.TimbreClip)
        {
            return null;
        }

        return GetBoundTimbreClip(resource).Parameters
            .Select(parameter => new CompletionParameterDefinition(parameter.Name, "System.Single", false))
            .ToArray();
    }

    // Completion support: the parameters a TimbreClip body has declared
    // before the offset.
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
                TimbreMarkupSyntax.IsCommandKeyword(directive.Keyword) || directive.Keyword is "@timbre" or "@sound" or "@modifier"))
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
                    .Where(directive => TimbreMarkupSyntax.IsCommandKeyword(directive.Keyword))
                    .Select(directive => CreateDirectiveRegion(text, offset, directive))
                    .Select(region => new TextSpan(region.KeywordSpan.Start, Math.Max(0, region.HeaderSpan.End - region.KeywordSpan.Start)))
                    .ToArray()
                : Array.Empty<TextSpan>();
            timbreStatementSpans.Add(aspect, spans);
        }

        return spans.Any(span => span.Contains(position));
    }

    private static bool ContainsTimbreSyntax(string text) =>
        TimbreMarkupSyntax.CommandKeywords.Any(keyword => text.IndexOf(keyword, StringComparison.Ordinal) >= 0) ||
        text.IndexOf("@timbre", StringComparison.Ordinal) >= 0 ||
        text.IndexOf("@sound", StringComparison.Ordinal) >= 0 ||
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
