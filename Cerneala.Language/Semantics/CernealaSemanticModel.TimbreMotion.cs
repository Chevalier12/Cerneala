using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

// `$Owner.timbre.Sound.Property` Motion targets. The Motion program of an
// Aspect defers them here; they are bound once the Aspect's @timbre and the
// sounds of its targets are known.
internal sealed partial class CernealaSemanticModel
{
    private const string TimbreMotionShapeMessage = "Timbre Motion targets must be $self.timbre.Sound.Property, $owner.timbre.Sound.Property or $Name.timbre.Sound.Property.";

    private readonly Dictionary<ElementSyntax, PendingTimbreMotion> pendingTimbreMotion = new();
    private PendingTimbreMotion? collectingTimbreMotion;

    private static bool IsTimbreMotionPath(string target)
    {
        string[] segments = target.Split('.');
        return segments.Length >= 3 && segments[1] == "timbre";
    }

    private void CollectTimbreMotionTarget(AssignmentSyntax assignment, DirectiveRegion owner)
    {
        if (collectingTimbreMotion is null)
        {
            AddDiagnostic(
                TimbreMarkupBinder.ContextId,
                assignment.NameSpan,
                Path.GetFileName(document.Path),
                "Timbre Motion targets are available only in @animate bodies of an Aspect.");
            return;
        }

        collectingTimbreMotion.Targets.Add((assignment, owner));
    }

    // Validates every deferred `.timbre.` target of one Aspect: the sound must
    // exist on the target and the property must be Volume or a parameter the
    // sound uses. SourceGen consumes the result and never re-validates it.
    private void BindTimbreMotionTargets(ResourceDefinition aspect, AspectAttachmentBinding attachments)
    {
        if (!pendingTimbreMotion.TryGetValue(aspect.Element, out PendingTimbreMotion? pending))
        {
            return;
        }

        pendingTimbreMotion.Remove(aspect.Element);
        HashSet<DirectiveRegion> mixedRoots = new();
        foreach ((AssignmentSyntax assignment, DirectiveRegion owner) in pending.Targets)
        {
            if (!ValidateTimbreMotionContext(pending.Program, assignment, owner, mixedRoots))
            {
                continue;
            }

            string[] segments = assignment.Name.Split('.');
            if (segments.Length != 4 || segments[0].Length < 2 || segments[0][0] != '$' || segments[2].Length == 0 || segments[3].Length == 0)
            {
                AddMotionDiagnostic("CERNEALAUI021", assignment.NameSpan, TimbreMotionShapeMessage);
                continue;
            }

            string ownerName = segments[0].Substring(1);
            string sound = segments[2];
            string property = segments[3];
            TextSpan ownerSpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 0);
            TextSpan soundSpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 2);
            TextSpan propertySpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 3);
            List<EmbeddedDiagnostic> diagnostics = new();
            bool resolved = TryResolveTimbreSound(aspect, attachments, ownerName, ownerSpan, sound, soundSpan, diagnostics,
                out TimbreCommandTarget target, out BoundTimbreSound? boundSound);
            ReportEmbedded(diagnostics);
            if (!resolved)
            {
                continue;
            }

            float minimum = 0f;
            float maximum = 1f;
            BoundTimbreParameter? parameter = null;
            if (property != "Volume")
            {
                // $owner is checked at runtime; its sounds are not known here.
                if (target == TimbreCommandTarget.Owner)
                {
                    minimum = float.NegativeInfinity;
                    maximum = float.PositiveInfinity;
                }
                else
                {
                    parameter = boundSound!.Parameters.FirstOrDefault(candidate => candidate.Name == property);
                    if (parameter is null)
                    {
                        AddDiagnostic(
                            TimbreMarkupBinder.ReferenceId,
                            propertySpan,
                            Path.GetFileName(document.Path),
                            "Sound '" + sound + "' does not use parameter '" + property + "'.");
                        continue;
                    }

                    minimum = parameter.Minimum;
                    maximum = parameter.Maximum;
                }
            }

            string value = document.Text.Substring(assignment.ValueSpan).Trim();
            if (value != "current" &&
                (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) ||
                    float.IsNaN(number) || float.IsInfinity(number) || number < minimum || number > maximum))
            {
                AddDiagnostic(
                    TimbreMarkupBinder.ValueId,
                    assignment.ValueSpan,
                    Path.GetFileName(document.Path),
                    "Timbre Motion value '" + value + "' for '" + sound + "." + property + "' must be current or a number within " +
                    minimum.ToString(CultureInfo.InvariantCulture) + "–" + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                continue;
            }

            if (parameter is null)
            {
                AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreProperty, property, "System.Single", propertySpan);
            }
            else
            {
                AddTimbreSymbol(
                    CernealaSemanticSymbolKind.TimbreParameter,
                    property,
                    "System.Single",
                    propertySpan,
                    FindTimbreParameterDefinition(aspect, attachments, ownerName, parameter));
            }
        }
    }

    // A `.timbre.` target is written in @from/@to of an @animate (directly or
    // inside @keyframes, @parallel or @sequence) and its execution root
    // targets nothing but sound.
    private bool ValidateTimbreMotionContext(
        MotionProgram program,
        AssignmentSyntax assignment,
        DirectiveRegion owner,
        ISet<DirectiveRegion> mixedRoots)
    {
        DirectiveRegion[] enclosing = program.Regions
            .Where(region => region.BodySpan.Contains(assignment.NameSpan.Start))
            .ToArray();
        if (owner.Keyword is not ("@from" or "@to") || enclosing.Any(region => region.Keyword is "@stagger" or "@run"))
        {
            AddDiagnostic(
                TimbreMarkupBinder.ContextId,
                assignment.NameSpan,
                Path.GetFileName(document.Path),
                "Timbre Motion targets are available only in @from and @to blocks of @animate.");
            return false;
        }

        DirectiveRegion? root = enclosing
            .Where(region => IsMotionExecutionDirective(region.Keyword))
            .OrderBy(region => region.Depth)
            .FirstOrDefault();
        if (root is null)
        {
            return true;
        }

        bool mixed = program.Syntax.Assignments.Any(other =>
                root.BodySpan.Contains(other.NameSpan.Start) &&
                !IsTimbreMotionPath(other.Name) &&
                InnermostRegion(program.Regions, other.NameSpan.Start)?.Keyword is "@from" or "@to" or "@set") ||
            program.Regions.Any(region =>
                region.Keyword is "@run" or "@stagger" && root.BodySpan.Contains(region.KeywordSpan.Start));
        if (!mixed)
        {
            return true;
        }

        if (mixedRoots.Add(root))
        {
            AddMotionDiagnostic(
                "CERNEALAUI021",
                assignment.NameSpan,
                "A Motion execution cannot mix Timbre targets with element, Prism or MotionClip targets; animate them in separate executions.");
        }

        return false;
    }


    private LanguageSourceLocation? FindTimbreParameterDefinition(
        ResourceDefinition aspect,
        AspectAttachmentBinding attachments,
        string ownerName,
        BoundTimbreParameter parameter)
    {
        BoundTimbreClip? clip = ownerName == "self"
            ? attachments.Timbre?.Clip
            : FindNamedElement(aspect.Element, ownerName) is NamedElementDefinition named &&
                FindStaticAspect(named.Element) is ResourceDefinition targetAspect
                ? GetAspectAttachments(targetAspect).Timbre?.Clip
                : null;
        return clip is not null && timbreClipPaths.TryGetValue(clip, out string? path)
            ? new LanguageSourceLocation(path, parameter.NameSpan)
            : null;
    }

    // Completion support: the sounds of the @timbre of the Aspect around
    // `element` and the animatable properties of one of them.
    internal IReadOnlyList<string> GetCompletionTimbreMotionSounds(ElementSyntax? element) =>
        FindCompletionTimbreClip(element)?.Sounds
            .Select(sound => sound.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

    internal IReadOnlyList<string> GetCompletionTimbreMotionParameters(ElementSyntax? element, string sound) =>
        FindCompletionTimbreClip(element)?.FindSound(sound) is BoundTimbreSound bound
            ? new[] { "Volume" }.Concat(bound.Parameters.Select(parameter => parameter.Name)).ToArray()
            : Array.Empty<string>();

    private BoundTimbreClip? FindCompletionTimbreClip(ElementSyntax? element)
    {
        ElementSyntax? scope = element;
        while (scope is not null && !IsAspectBody(scope))
        {
            scope = parents.TryGetValue(scope, out ElementSyntax? parent) ? parent : null;
        }

        return scope is not null && resourceElements.TryGetValue(scope, out ResourceDefinition? aspect)
            ? GetAspectAttachments(aspect).Timbre?.Clip
            : null;
    }

    private sealed class PendingTimbreMotion(MotionProgram program)
    {
        public MotionProgram Program { get; } = program;

        public List<(AssignmentSyntax Assignment, DirectiveRegion Owner)> Targets { get; } = new();
    }
}
