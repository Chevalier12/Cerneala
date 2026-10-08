using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

// `@timbre` and `@prism` are attachments of an Aspect: written once each at
// the top of its body and alive as long as the Aspect application. They are
// bound here and blanked from the Aspect text every other binder reads, so a
// `Source = …` or `Radius = …` inside them is never an Aspect setter or a
// Motion statement.
internal sealed partial class CernealaSemanticModel
{
    private const string TimbreSingleMessage = "An Aspect can declare only one @timbre.";
    private const string PrismSingleMessage = "An Aspect can declare only one @prism.";
    private const string TimbreNestedMessage =
        "@timbre is written at the top of the Aspect body, not inside @on, @when or @if; use @play $self.timbre.Sound to start a sound.";
    private const string PrismNestedMessage = "@prism is written at the top of the Aspect body, not inside @on, @when or @if.";
    private const string PrismContentMessage = "@prism is written only in an Aspect body.";

    private readonly Dictionary<ElementSyntax, IReadOnlyList<AspectAttachmentSyntax>> aspectAttachmentSyntax = new();
    private readonly Dictionary<ElementSyntax, AspectAttachmentBinding> aspectAttachmentBindings = new();

    private bool IsAspectBody(ElementSyntax element) =>
        resourceElements.TryGetValue(element, out ResourceDefinition? resource) && resource.Kind == ResourceKind.Aspect;

    private IReadOnlyList<AspectAttachmentSyntax> GetAspectAttachmentSyntax(ElementSyntax aspect)
    {
        if (!aspectAttachmentSyntax.TryGetValue(aspect, out IReadOnlyList<AspectAttachmentSyntax>? attachments))
        {
            (string text, int offset) = BuildRawDirectTextBuffer(aspect);
            attachments = AspectAttachmentScanner.Scan(text, offset);
            aspectAttachmentSyntax.Add(aspect, attachments);
        }

        return attachments;
    }

    // The attachments of an Aspect of this document, bound once; diagnostics
    // are reported on the first call. Aspects of other documents (Application
    // resources) are bound by their own document.
    private AspectAttachmentBinding GetAspectAttachments(ResourceDefinition aspect)
    {
        if (aspectAttachmentBindings.TryGetValue(aspect.Element, out AspectAttachmentBinding? binding))
        {
            return binding;
        }

        binding = new AspectAttachmentBinding();
        aspectAttachmentBindings.Add(aspect.Element, binding);
        if (!string.Equals(aspect.Path, document.Path, StringComparison.OrdinalIgnoreCase))
        {
            return binding;
        }

        List<EmbeddedDiagnostic> diagnostics = new();
        bool timbreSeen = false;
        bool prismSeen = false;
        foreach (AspectAttachmentSyntax attachment in GetAspectAttachmentSyntax(aspect.Element))
        {
            bool timbre = attachment.Keyword == "@timbre";
            if (attachment.Depth > 0)
            {
                if (timbre)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ContextId, TimbreNestedMessage, attachment.KeywordSpan));
                }
                else
                {
                    AddPrismDiagnostic("PRISM2013", attachment.KeywordSpan, PrismNestedMessage);
                }

                continue;
            }

            if (!attachment.HasBlock && !attachment.Terminated)
            {
                if (timbre)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupSyntax.SyntaxId, "Timbre directive '@timbre' must end with ';'.", attachment.Extent));
                }
                else
                {
                    AddPrismDiagnostic("PRISM1002", attachment.Extent, "Prism directive '@prism' must end with ';'.");
                }

                continue;
            }

            if (timbre)
            {
                if (timbreSeen)
                {
                    diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ContextId, TimbreSingleMessage, attachment.KeywordSpan));
                    continue;
                }

                timbreSeen = true;
                binding.Timbre = BindTimbreAttachment(aspect, attachment, diagnostics);
            }
            else
            {
                if (prismSeen)
                {
                    AddPrismDiagnostic("PRISM2013", attachment.KeywordSpan, PrismSingleMessage);
                    continue;
                }

                prismSeen = true;
                binding.Prism = BindPrismAttachment(aspect, attachment);
            }
        }

        ReportEmbedded(diagnostics);
        return binding;
    }

    private BoundTimbreAttachment? BindTimbreAttachment(
        ResourceDefinition aspect,
        AspectAttachmentSyntax attachment,
        ICollection<EmbeddedDiagnostic> diagnostics)
    {
        AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreDirective, "@timbre", "Cerneala.Timbre.TimbreClipDefinition", attachment.KeywordSpan);
        (string text, int offset) = BuildRawDirectTextBuffer(aspect.Element);
        if (attachment.HasBlock)
        {
            TimbreClipBodySyntax body = TimbreMarkupSyntax.ParseClipBody(
                text.Substring(attachment.BodySpan.Start - offset, attachment.BodySpan.Length),
                attachment.BodySpan.Start);
            BoundTimbreClip inline = TimbreMarkupBinder.BindClip(null, body, attachment.KeywordSpan, diagnostics);
            AddTimbreClipSymbols(body, inline, document.Path);
            return new BoundTimbreAttachment(attachment.KeywordSpan, inline, resourceName: null, Array.Empty<BoundTimbreArgument>());
        }

        TimbreClipReferenceSyntax? reference = TimbreMarkupSyntax.ParseClipReference(
            attachment.KeywordSpan,
            text.Substring(attachment.HeaderSpan.Start - offset, attachment.HeaderSpan.Length),
            attachment.HeaderSpan.Start,
            diagnostics);
        if (reference is null)
        {
            return null;
        }

        ResourceDefinition? resource = FindResource(aspect.Element, reference.ClipName);
        if (resource is null)
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ReferenceId, "Unknown TimbreClip resource '$" + reference.ClipName + "'.", reference.ClipSpan));
            return null;
        }

        if (resource.Kind != ResourceKind.TimbreClip)
        {
            diagnostics.Add(new EmbeddedDiagnostic(TimbreMarkupBinder.ReferenceId, "Resource '$" + reference.ClipName + "' is not a TimbreClip.", reference.ClipSpan));
            return null;
        }

        symbols.Add(new CernealaSemanticSymbol(
            CernealaSemanticSymbolKind.ResourceReference,
            reference.ClipName,
            TimbreClipDefinitionTypeName,
            new TextSpan(reference.ClipSpan.Start + 1, reference.ClipName.Length),
            resource.Type,
            definitionLocation: resource.Location));
        BoundTimbreClip clip = GetBoundTimbreClip(resource);
        foreach (TimbreValueSyntax argument in reference.Arguments)
        {
            if (clip.FindParameter(argument.Name) is BoundTimbreParameter parameter)
            {
                AddTimbreSymbol(
                    CernealaSemanticSymbolKind.TimbreParameter,
                    parameter.Name,
                    "System.Single",
                    argument.NameSpan,
                    new LanguageSourceLocation(resource.Path, parameter.NameSpan));
            }
        }

        IReadOnlyList<BoundTimbreArgument> arguments = TimbreMarkupBinder.BindArguments(reference, clip, diagnostics);
        return new BoundTimbreAttachment(attachment.KeywordSpan, clip, reference.ClipName, arguments);
    }

    private PrismClipDefinition? BindPrismAttachment(ResourceDefinition aspect, AspectAttachmentSyntax attachment)
    {
        (string text, int offset) = BuildRawDirectTextBuffer(aspect.Element);
        EmbeddedParseResult<IReadOnlyList<PrismApplicationModelSyntax>> parsed = PrismSyntaxParser.ParseApplications(
            text.Substring(attachment.Extent.Start - offset, attachment.Extent.Length),
            attachment.Extent.Start);
        AddPrismSyntaxDiagnostics(parsed.Diagnostics);
        return parsed.Diagnostics.Count == 0 && parsed.Syntax.Count == 1
            ? BindPrismApplication(aspect.Element, parsed.Syntax[0], CancellationToken.None)
            : null;
    }

    // `@prism` written in element content (not in an Aspect body).
    private void ReportPrismInContent(string text, int offset)
    {
        int index = 0;
        while ((index = text.IndexOf("@prism", index, StringComparison.Ordinal)) >= 0)
        {
            int end = index + "@prism".Length;
            if (end >= text.Length || !char.IsLetterOrDigit(text[end]) && text[end] != '_')
            {
                AddPrismDiagnostic("PRISM2013", new TextSpan(offset + index, "@prism".Length), PrismContentMessage);
            }

            index = end;
        }
    }

    // The Aspect an element gets statically: its inline <X.Aspect>, its
    // Aspect="$Name" attribute, or the default Aspect of its type in scope.
    private ResourceDefinition? FindStaticAspect(ElementSyntax element)
    {
        foreach (ElementSyntax property in element.Children.OfType<ElementSyntax>())
        {
            if (inlineAspectProperties.TryGetValue(property, out ResourceDefinition? inline))
            {
                return inline;
            }
        }

        if (FindAttribute(element, "Aspect") is AttributeSyntax attribute)
        {
            string name = Unquote(attribute.ValueToken.Text).Trim();
            return name.StartsWith("$", StringComparison.Ordinal) &&
                FindResource(element, name.Substring(1)) is ResourceDefinition resource &&
                resource.Kind == ResourceKind.Aspect
                ? resource
                : null;
        }

        return GetElementType(element, ReferenceEquals(element, root)) is ILanguageTypeSymbol type
            ? FindDefaultAspect(element, type)
            : null;
    }

    private sealed class AspectAttachmentBinding
    {
        public BoundTimbreAttachment? Timbre { get; set; }

        public PrismClipDefinition? Prism { get; set; }
    }
}
