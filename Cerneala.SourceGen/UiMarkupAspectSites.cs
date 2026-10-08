using System.Collections.Generic;
using System.Linq;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        // Elements standing for "the element a resource Aspect is applied to"
        // while its program is compiled once (see PrepareAspectBehavior).
        private readonly HashSet<MarkupElement> aspectBehaviorTargets = new();

        private sealed class AspectApplicationSite
        {
            public AspectApplicationSite(MarkupElement element, MarkupElement? templateOwner, MarkupElement? templateRoot)
            {
                Element = element;
                TemplateOwner = templateOwner;
                TemplateRoot = templateRoot;
            }

            public MarkupElement Element { get; }

            // The control whose component template contains Element, if any,
            // and that template's root (a template tree's ancestors continue
            // past its root into the declaring markup, so it is kept here).
            public MarkupElement? TemplateOwner { get; }

            public MarkupElement? TemplateRoot { get; }
        }

        private bool IsAspectBehaviorTarget(MarkupElement element) => aspectBehaviorTargets.Contains(element);

        // `$self.prism` / `$owner.prism` of a resource Aspect: every markup site
        // must carry the same Prism composition; the generated code checks the
        // composition of the element it reaches at runtime.
        private bool TryResolvePrismElementFromSites(
            AspectResource aspect,
            bool owner,
            MotionAssignmentSyntax assignment,
            out MarkupElement? element,
            out string? definitionName)
        {
            element = null;
            definitionName = null;
            string path = owner ? "$owner.prism" : "$self.prism";
            MarkupElement[] candidates = FindStaticApplicationSites(aspect)
                .Select(site => owner ? site.TemplateOwner : site.Element)
                .OfType<MarkupElement>()
                .ToArray();
            if (candidates.Length == 0)
            {
                ReportPrismBinding(
                    PrismMotionTargetDiagnostic,
                    PrismMotionSegmentLocation(assignment, 0),
                    "Cannot determine the Prism composition of '" + path + "': the Aspect is not applied " +
                    (owner ? "inside a component template " : string.Empty) + "in this markup.");
                return false;
            }

            foreach (MarkupElement candidate in candidates)
            {
                if (!boundPrismApplications.TryGetValue(candidate, out BoundPrismApplication application))
                {
                    ReportPrismBinding(
                        PrismMotionTargetDiagnostic,
                        PrismMotionSegmentLocation(assignment, 0),
                        "'" + candidate.Name.LocalName + "' at an application site of this Aspect has no Prism composition for '" + path + "'.");
                    return false;
                }

                if (definitionName is not null && !string.Equals(definitionName, application.Composition.Name, System.StringComparison.Ordinal))
                {
                    ReportPrismBinding(
                        PrismMotionTargetDiagnostic,
                        PrismMotionSegmentLocation(assignment, 0),
                        "'" + path + "' reaches different Prism compositions at the application sites of this Aspect.");
                    return false;
                }

                element ??= candidate;
                definitionName = application.Composition.Name;
            }

            return true;
        }

        // `$owner.parts.$Name` of a resource Aspect: the part must exist with
        // one type in the owner template of every markup site; the generated
        // code looks it up at runtime and throws for any other owner.
        private bool TryResolveOwnerPartFromSites(
            AspectResource aspect,
            string partName,
            MotionAssignmentSyntax assignment,
            out MarkupElement? part)
        {
            part = null;
            AspectApplicationSite[] sites = FindStaticApplicationSites(aspect)
                .Where(site => site.TemplateOwner is not null)
                .ToArray();
            if (sites.Length == 0)
            {
                ReportMotion(
                    MotionDiagnosticKind.Target,
                    assignment.Location,
                    "Cannot determine the type of '$owner.parts.$" + partName + "': the Aspect is not applied inside a component template in this markup.");
                return false;
            }

            foreach (AspectApplicationSite site in sites)
            {
                MarkupElement templateRoot = site.TemplateRoot!;
                MarkupElement[] matches = templateRoot.DescendantsAndSelf()
                    .Where(element => string.Equals(element.Attribute("Name")?.Value?.Trim(), partName, System.StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length != 1)
                {
                    ReportMotion(
                        MotionDiagnosticKind.Target,
                        assignment.Location,
                        "The component template of '" + site.TemplateOwner!.Name.LocalName + "' has no unique part named '" + partName + "'.");
                    return false;
                }

                if (part is not null && !string.Equals(part.Name.LocalName, matches[0].Name.LocalName, System.StringComparison.Ordinal))
                {
                    ReportMotion(
                        MotionDiagnosticKind.Target,
                        assignment.Location,
                        "Template part '" + partName + "' is a '" + part.Name.LocalName + "' at one application site and a '" +
                        matches[0].Name.LocalName + "' at another.");
                    return false;
                }

                part ??= matches[0];
            }

            return true;
        }

        // Where a resource Aspect is applied in this document's markup. Facts
        // that depend on the application ($owner.parts, $owner.prism,
        // $self.prism) are taken from these sites, which must agree; an
        // application made at runtime is checked by the generated code.
        private IReadOnlyList<AspectApplicationSite> FindStaticApplicationSites(AspectResource aspect)
        {
            List<AspectApplicationSite> sites = [];
            HashSet<MarkupElement> visited = new();
            Visit(document.Root, null, null);
            return sites;

            void Visit(MarkupElement root, MarkupElement? owner, MarkupElement? templateRoot)
            {
                foreach (MarkupElement element in root.DescendantsAndSelf().ToArray())
                {
                    if (!visited.Add(element))
                    {
                        continue;
                    }

                    IReadOnlyList<AspectResource> applied = ResolveAspects(element);
                    if (applied.Any(candidate => ReferenceEquals(candidate, aspect) || ReferenceEquals(candidate.Source, aspect.Source)))
                    {
                        sites.Add(new AspectApplicationSite(element, owner, templateRoot));
                    }

                    IEnumerable<DirectiveTemplateNode> templates = GetDirectiveContent(
                            element,
                            DirectiveContentKind.Elements | DirectiveContentKind.Templates)
                        .Nodes.OfType<DirectiveTemplateNode>()
                        .Concat(applied.Select(candidate => candidate.Template).OfType<DirectiveTemplateNode>());
                    foreach (DirectiveTemplateNode template in templates)
                    {
                        Visit(template.Root, element, template.Root);
                    }
                }
            }
        }
    }
}
