using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

// `$self.timbre.Handle.Parameter` Motion targets. The Motion program of an
// Aspect defers them here; they are bound once the Aspect's Timbre handles
// and the clips each handle can play are known.
internal sealed partial class CernealaSemanticModel
{
    private const string TimbreMotionShapeMessage = "Timbre Motion targets must be $self.timbre.Handle.Parameter.";

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

    private static IReadOnlyDictionary<string, IReadOnlyList<BoundTimbreHandleParameter>> BindTimbreHandleParameters(
        IReadOnlyDictionary<string, TimbreHandleKind> handles,
        IReadOnlyList<BoundTimbreAction> actions)
    {
        Dictionary<string, IReadOnlyList<BoundTimbreHandleParameter>> result = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, TimbreHandleKind> handle in handles.Where(handle => handle.Value == TimbreHandleKind.Timbre))
        {
            BoundTimbreClip[] clips = actions
                .Where(action => action.Kind == TimbreActionKind.Play && action.HandleName == handle.Key && action.Clip is not null)
                .Select(action => action.Clip!)
                .ToArray();
            List<BoundTimbreHandleParameter> parameters = new();
            if (clips.Length > 0)
            {
                foreach (BoundTimbreParameter first in clips[0].Parameters)
                {
                    BoundTimbreParameter[] matches = clips
                        .Select(clip => clip.FindParameter(first.Name))
                        .Where(parameter => parameter is not null)
                        .Select(parameter => parameter!)
                        .ToArray();
                    float minimum = matches.Max(parameter => parameter.Minimum);
                    float maximum = matches.Min(parameter => parameter.Maximum);
                    if (matches.Length == clips.Length && minimum <= maximum)
                    {
                        parameters.Add(new BoundTimbreHandleParameter(first.Name, minimum, maximum));
                    }
                }
            }

            result.Add(handle.Key, parameters);
        }

        return result;
    }

    // Validates every deferred `.timbre.` target of one Aspect against its
    // handle typing and parameter schema; reports diagnostics and editor
    // symbols. SourceGen consumes the schema and never re-validates it.
    private void BindTimbreMotionTargets(
        ElementSyntax aspect,
        IReadOnlyDictionary<string, TimbreHandleKind> handles,
        IReadOnlyDictionary<string, TextSpan> declarations,
        IReadOnlyList<BoundTimbreAction> actions,
        IReadOnlyDictionary<string, IReadOnlyList<BoundTimbreHandleParameter>> handleParameters)
    {
        if (!pendingTimbreMotion.TryGetValue(aspect, out PendingTimbreMotion? pending))
        {
            return;
        }

        pendingTimbreMotion.Remove(aspect);
        HashSet<DirectiveRegion> mixedRoots = new();
        foreach ((AssignmentSyntax assignment, DirectiveRegion owner) in pending.Targets)
        {
            if (!ValidateTimbreMotionContext(pending.Program, assignment, owner, mixedRoots))
            {
                continue;
            }

            string[] segments = assignment.Name.Split('.');
            if (segments.Length != 4 || segments[0] != "$self" || segments[2].Length == 0 || segments[3].Length == 0)
            {
                AddMotionDiagnostic("CERNEALAUI021", assignment.NameSpan, TimbreMotionShapeMessage);
                continue;
            }

            string handle = segments[2];
            string parameter = segments[3];
            TextSpan handleSpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 2);
            TextSpan parameterSpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 3);
            if (!handles.TryGetValue(handle, out TimbreHandleKind kind) || kind != TimbreHandleKind.Timbre)
            {
                AddDiagnostic(
                    TimbreMarkupBinder.ReferenceId,
                    handleSpan,
                    Path.GetFileName(document.Path),
                    "'" + handle + "' is not a Timbre handle of this Aspect; declare it with @handle and start it with @timbre … as " + handle + ".");
                continue;
            }

            float minimum = 0f;
            float maximum = 1f;
            BoundTimbreHandleParameter? custom = null;
            if (parameter != "Volume")
            {
                custom = handleParameters.TryGetValue(handle, out IReadOnlyList<BoundTimbreHandleParameter>? schema)
                    ? schema.FirstOrDefault(candidate => candidate.Name == parameter)
                    : null;
                if (custom is null)
                {
                    AddDiagnostic(
                        TimbreMarkupBinder.ReferenceId,
                        parameterSpan,
                        Path.GetFileName(document.Path),
                        "Timbre handle '" + handle + "' has no animatable parameter '" + parameter +
                        "': only Volume and float parameters declared by every TimbreClip it plays can be animated.");
                    continue;
                }

                minimum = custom.Minimum;
                maximum = custom.Maximum;
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
                    "Timbre Motion value '" + value + "' for '" + handle + "." + parameter + "' must be current or a number within " +
                    minimum.ToString(CultureInfo.InvariantCulture) + "–" + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                continue;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionHandle,
                handle,
                "Cerneala.Timbre.TimbreHandle",
                handleSpan,
                definitionLocation: declarations.TryGetValue(handle, out TextSpan declaration)
                    ? new LanguageSourceLocation(document.Path, declaration)
                    : null));
            if (custom is null)
            {
                AddTimbreSymbol(CernealaSemanticSymbolKind.TimbreProperty, parameter, "System.Single", parameterSpan);
            }
            else
            {
                AddTimbreSymbol(
                    CernealaSemanticSymbolKind.TimbreParameter,
                    parameter,
                    "System.Single",
                    parameterSpan,
                    FindTimbreParameterDefinition(actions, handle, parameter));
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
        IReadOnlyList<BoundTimbreAction> actions,
        string handle,
        string parameter)
    {
        BoundTimbreClip? clip = actions
            .FirstOrDefault(action => action.Kind == TimbreActionKind.Play && action.HandleName == handle && action.Clip is not null)?
            .Clip;
        return clip?.FindParameter(parameter) is BoundTimbreParameter declared &&
            timbreClipResources.TryGetValue(clip, out ResourceDefinition? resource)
            ? new LanguageSourceLocation(resource.Path, declared.NameSpan)
            : null;
    }

    // Completion support: the Timbre handles of the Aspect around `element`
    // and the animatable parameters of one of them. The statement being typed
    // leaves the Aspect program unbound, so the `@timbre $Clip … as Handle`
    // statements are read lexically and their clips bound by resource lookup.
    internal IReadOnlyList<string> GetCompletionTimbreMotionHandles(ElementSyntax? element) =>
        FindCompletionTimbreStarts(element)
            .Select(start => start.Handle)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    internal IReadOnlyList<string> GetCompletionTimbreMotionParameters(ElementSyntax? element, string handle)
    {
        BoundTimbreClip[] clips = FindCompletionTimbreStarts(element)
            .Where(start => start.Handle == handle)
            .Select(start => FindResource(start.Aspect, start.Clip))
            .Where(resource => resource?.Kind == ResourceKind.TimbreClip)
            .Select(resource => GetBoundTimbreClip(resource!))
            .ToArray();
        if (clips.Length == 0)
        {
            return Array.Empty<string>();
        }

        return new[] { "Volume" }
            .Concat(clips[0].Parameters
                .Select(parameter => parameter.Name)
                .Where(name => clips.All(clip => clip.FindParameter(name) is not null)))
            .ToArray();
    }

    private IEnumerable<(ElementSyntax Aspect, string Clip, string Handle)> FindCompletionTimbreStarts(ElementSyntax? element)
    {
        ElementSyntax? scope = element;
        while (scope is not null &&
            !string.Equals(scope.Name.Split(':').Last(), "Aspect", StringComparison.Ordinal) &&
            !scope.Name.EndsWith(".Aspect", StringComparison.Ordinal))
        {
            scope = parents.TryGetValue(scope, out ElementSyntax? parent) ? parent : null;
        }

        if (scope is null)
        {
            return Array.Empty<(ElementSyntax, string, string)>();
        }

        ElementSyntax aspect = scope;
        return System.Text.RegularExpressions.Regex
            .Matches(document.Text.Substring(scope.Span), @"@timbre\s+\$([A-Za-z_][A-Za-z0-9_]*)\s*(?:\([^)]*\))?\s+as\s+([A-Za-z_][A-Za-z0-9_]*)\s*;")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(match => (aspect, match.Groups[1].Value, match.Groups[2].Value))
            .ToArray();
    }

    private sealed class PendingTimbreMotion(MotionProgram program)
    {
        public MotionProgram Program { get; } = program;

        public List<(AssignmentSyntax Assignment, DirectiveRegion Owner)> Targets { get; } = new();
    }
}
