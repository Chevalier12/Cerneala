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
                BindMotionAssignment(
                    application,
                    aspect.TargetType,
                    assignment,
                    clip: null,
                    prismOnly: true,
                    allowExplicitBinding: owner.Keyword == "@to");
            }
        }
    }

    private bool ShouldBindAspectAssignment(ElementSyntax aspect, int position)
    {
        if (IsInsideTimbreStatement(aspect, position))
        {
            return false;
        }

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

    private void BindPrismMotionTarget(
        ElementSyntax source,
        AssignmentSyntax assignment,
        string[] segments,
        bool reportMissingApplication,
        bool allowExplicitBinding)
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

        string value = document.Text.Substring(assignment.ValueSpan).Trim();
        int with = value.IndexOf(" with ", StringComparison.Ordinal);
        if (with >= 0)
        {
            value = value.Substring(0, with).Trim();
        }

        if (value.StartsWith("$", StringComparison.Ordinal))
        {
            BindMotionReferenceValue(
                source,
                value,
                TrimmedSpan(assignment.ValueSpan),
                type,
                assignment.Name,
                allowExplicitBinding);
        }
    }

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

    private ILanguageTypeSymbol? ResolveIntrinsicType(string typeName)
    {
        if (typeName.StartsWith("MotionSpec[", StringComparison.Ordinal))
        {
            return compilation.FindType("System.Object");
        }

        return compilation.FindType(typeName);
    }

}
