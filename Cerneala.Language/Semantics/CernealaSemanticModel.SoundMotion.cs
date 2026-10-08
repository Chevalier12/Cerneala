using System.Globalization;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Syntax.Embedded;
using Cerneala.Language.Text;
using Cerneala.Language.Timbre;

namespace Cerneala.Language.Semantics;

// `$self.sound.Handle.Parameter` Motion targets. The Motion program of an
// Aspect defers them here; they are bound once the Aspect's Sound handles
// and the clips each handle can play are known.
internal sealed partial class CernealaSemanticModel
{
    private const string SoundMotionShapeMessage = "Sound Motion targets must be $self.sound.Handle.Parameter.";

    private readonly Dictionary<ElementSyntax, PendingSoundMotion> pendingSoundMotion = new();
    private PendingSoundMotion? collectingSoundMotion;

    private static bool IsSoundMotionPath(string target)
    {
        string[] segments = target.Split('.');
        return segments.Length >= 3 && segments[1] == "sound";
    }

    private void CollectSoundMotionTarget(AssignmentSyntax assignment, DirectiveRegion owner)
    {
        if (collectingSoundMotion is null)
        {
            AddDiagnostic(
                SoundMarkupBinder.ContextId,
                assignment.NameSpan,
                Path.GetFileName(document.Path),
                "Sound Motion targets are available only in @animate bodies of an Aspect.");
            return;
        }

        collectingSoundMotion.Targets.Add((assignment, owner));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<BoundSoundHandleParameter>> BindSoundHandleParameters(
        IReadOnlyDictionary<string, SoundHandleKind> handles,
        IReadOnlyList<BoundSoundAction> actions)
    {
        Dictionary<string, IReadOnlyList<BoundSoundHandleParameter>> result = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, SoundHandleKind> handle in handles.Where(handle => handle.Value == SoundHandleKind.Sound))
        {
            BoundSoundClip[] clips = actions
                .Where(action => action.Kind == SoundActionKind.Play && action.HandleName == handle.Key && action.Clip is not null)
                .Select(action => action.Clip!)
                .ToArray();
            List<BoundSoundHandleParameter> parameters = new();
            if (clips.Length > 0)
            {
                foreach (BoundSoundParameter first in clips[0].Parameters)
                {
                    BoundSoundParameter[] matches = clips
                        .Select(clip => clip.FindParameter(first.Name))
                        .Where(parameter => parameter is not null)
                        .Select(parameter => parameter!)
                        .ToArray();
                    float minimum = matches.Max(parameter => parameter.Minimum);
                    float maximum = matches.Min(parameter => parameter.Maximum);
                    if (matches.Length == clips.Length && minimum <= maximum)
                    {
                        parameters.Add(new BoundSoundHandleParameter(first.Name, minimum, maximum));
                    }
                }
            }

            result.Add(handle.Key, parameters);
        }

        return result;
    }

    // Validates every deferred `.sound.` target of one Aspect against its
    // handle typing and parameter schema; reports diagnostics and editor
    // symbols. SourceGen consumes the schema and never re-validates it.
    private void BindSoundMotionTargets(
        ElementSyntax aspect,
        IReadOnlyDictionary<string, SoundHandleKind> handles,
        IReadOnlyDictionary<string, TextSpan> declarations,
        IReadOnlyList<BoundSoundAction> actions,
        IReadOnlyDictionary<string, IReadOnlyList<BoundSoundHandleParameter>> handleParameters)
    {
        if (!pendingSoundMotion.TryGetValue(aspect, out PendingSoundMotion? pending))
        {
            return;
        }

        pendingSoundMotion.Remove(aspect);
        HashSet<DirectiveRegion> mixedRoots = new();
        foreach ((AssignmentSyntax assignment, DirectiveRegion owner) in pending.Targets)
        {
            if (!ValidateSoundMotionContext(pending.Program, assignment, owner, mixedRoots))
            {
                continue;
            }

            string[] segments = assignment.Name.Split('.');
            if (segments.Length != 4 || segments[0] != "$self" || segments[2].Length == 0 || segments[3].Length == 0)
            {
                AddMotionDiagnostic("CERNEALAUI021", assignment.NameSpan, SoundMotionShapeMessage);
                continue;
            }

            string handle = segments[2];
            string parameter = segments[3];
            TextSpan handleSpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 2);
            TextSpan parameterSpan = MotionSegmentSpan(assignment.NameSpan, assignment.Name, 3);
            if (!handles.TryGetValue(handle, out SoundHandleKind kind) || kind != SoundHandleKind.Sound)
            {
                AddDiagnostic(
                    SoundMarkupBinder.ReferenceId,
                    handleSpan,
                    Path.GetFileName(document.Path),
                    "'" + handle + "' is not a Sound handle of this Aspect; declare it with @handle and start it with @sound … as " + handle + ".");
                continue;
            }

            float minimum = 0f;
            float maximum = 1f;
            BoundSoundHandleParameter? custom = null;
            if (parameter != "Volume")
            {
                custom = handleParameters.TryGetValue(handle, out IReadOnlyList<BoundSoundHandleParameter>? schema)
                    ? schema.FirstOrDefault(candidate => candidate.Name == parameter)
                    : null;
                if (custom is null)
                {
                    AddDiagnostic(
                        SoundMarkupBinder.ReferenceId,
                        parameterSpan,
                        Path.GetFileName(document.Path),
                        "Sound handle '" + handle + "' has no animatable parameter '" + parameter +
                        "': only Volume and float parameters declared by every SoundClip it plays can be animated.");
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
                    SoundMarkupBinder.ValueId,
                    assignment.ValueSpan,
                    Path.GetFileName(document.Path),
                    "Sound Motion value '" + value + "' for '" + handle + "." + parameter + "' must be current or a number within " +
                    minimum.ToString(CultureInfo.InvariantCulture) + "–" + maximum.ToString(CultureInfo.InvariantCulture) + ".");
                continue;
            }

            symbols.Add(new CernealaSemanticSymbol(
                CernealaSemanticSymbolKind.MotionHandle,
                handle,
                "Cerneala.Timbre.SoundHandle",
                handleSpan,
                definitionLocation: declarations.TryGetValue(handle, out TextSpan declaration)
                    ? new LanguageSourceLocation(document.Path, declaration)
                    : null));
            if (custom is null)
            {
                AddSoundSymbol(CernealaSemanticSymbolKind.SoundProperty, parameter, "System.Single", parameterSpan);
            }
            else
            {
                AddSoundSymbol(
                    CernealaSemanticSymbolKind.SoundParameter,
                    parameter,
                    "System.Single",
                    parameterSpan,
                    FindSoundParameterDefinition(actions, handle, parameter));
            }
        }
    }

    // A `.sound.` target is written in @from/@to of an @animate (directly or
    // inside @keyframes, @parallel or @sequence) and its execution root
    // targets nothing but sound.
    private bool ValidateSoundMotionContext(
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
                SoundMarkupBinder.ContextId,
                assignment.NameSpan,
                Path.GetFileName(document.Path),
                "Sound Motion targets are available only in @from and @to blocks of @animate.");
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
                !IsSoundMotionPath(other.Name) &&
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
                "A Motion execution cannot mix Sound targets with element, Prism or MotionClip targets; animate them in separate executions.");
        }

        return false;
    }

    private LanguageSourceLocation? FindSoundParameterDefinition(
        IReadOnlyList<BoundSoundAction> actions,
        string handle,
        string parameter)
    {
        BoundSoundClip? clip = actions
            .FirstOrDefault(action => action.Kind == SoundActionKind.Play && action.HandleName == handle && action.Clip is not null)?
            .Clip;
        return clip?.FindParameter(parameter) is BoundSoundParameter declared &&
            soundClipResources.TryGetValue(clip, out ResourceDefinition? resource)
            ? new LanguageSourceLocation(resource.Path, declared.NameSpan)
            : null;
    }

    // Completion support: the Sound handles of the Aspect around `element`
    // and the animatable parameters of one of them. The statement being typed
    // leaves the Aspect program unbound, so the `@sound $Clip … as Handle`
    // statements are read lexically and their clips bound by resource lookup.
    internal IReadOnlyList<string> GetCompletionSoundMotionHandles(ElementSyntax? element) =>
        FindCompletionSoundStarts(element)
            .Select(start => start.Handle)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    internal IReadOnlyList<string> GetCompletionSoundMotionParameters(ElementSyntax? element, string handle)
    {
        BoundSoundClip[] clips = FindCompletionSoundStarts(element)
            .Where(start => start.Handle == handle)
            .Select(start => FindResource(start.Aspect, start.Clip))
            .Where(resource => resource?.Kind == ResourceKind.SoundClip)
            .Select(resource => GetBoundSoundClip(resource!))
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

    private IEnumerable<(ElementSyntax Aspect, string Clip, string Handle)> FindCompletionSoundStarts(ElementSyntax? element)
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
            .Matches(document.Text.Substring(scope.Span), @"@sound\s+\$([A-Za-z_][A-Za-z0-9_]*)\s*(?:\([^)]*\))?\s+as\s+([A-Za-z_][A-Za-z0-9_]*)\s*;")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(match => (aspect, match.Groups[1].Value, match.Groups[2].Value))
            .ToArray();
    }

    private sealed class PendingSoundMotion(MotionProgram program)
    {
        public MotionProgram Program { get; } = program;

        public List<(AssignmentSyntax Assignment, DirectiveRegion Owner)> Targets { get; } = new();
    }
}
