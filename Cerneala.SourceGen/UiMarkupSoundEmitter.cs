using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Cerneala.Language.Timbre;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private const string SoundClipType = "global::Cerneala.Timbre.SoundClip";

        private sealed class SoundClipResource
        {
            public SoundClipResource(string name, string variable, BoundSoundClip clip, MarkupElement element)
            {
                Name = name;
                Variable = variable;
                Clip = clip;
                Element = element;
            }

            public string Name { get; }

            public string Variable { get; }

            public BoundSoundClip Clip { get; }

            public MarkupElement Element { get; }
        }

        // Lowers a <SoundClip> resource to the same immutable definition C#
        // builds with the core constructors. Validity was decided by the bound
        // Cerneala.Language Sound model; nothing is opened or played here.
        private void ReadSoundClip(ResourceScope scope, MarkupElement resource)
        {
            string? name = RequiredName(resource);
            if (name is null)
            {
                return;
            }

            if (scope.NamedResources.ContainsKey(name))
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate resource Name '" + name + "' in the same scope.");
                return;
            }

            if (!semanticModel.Sound.Clips.TryGetValue(resource.Span.Start, out BoundSoundClip? clip) ||
                !clip.IsValid ||
                clip.Source is null)
            {
                Report(InvalidDirective, resource, Path.GetFileName(file.Path), "SoundClip '" + name + "' has no bound Sound definition.");
                return;
            }

            string variable = CreateIdentifier(name) + "SoundClip" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            Dictionary<string, string> parameterVariables = new();
            foreach (BoundSoundParameter parameter in clip.Parameters)
            {
                string parameterVariable = variable + "_" + parameter.Name;
                parameterVariables.Add(parameter.Name, parameterVariable);
                currentLines.Add(
                    "global::Cerneala.Timbre.SoundParameter<float> " + parameterVariable +
                    " = new global::Cerneala.Timbre.SoundParameter<float>(" + Literal(parameter.Name) + ", " +
                    FloatLiteral(parameter.DefaultValue) + ");");
            }

            List<string> arguments =
            [
                "global::Cerneala.Timbre.SoundSource.FromFile(" + Literal(clip.Source) + ")",
                "volume: " + FloatLiteral(clip.Volume),
                "loop: " + (clip.Loop ? "true" : "false")
            ];
            if (clip.Parameters.Count > 0)
            {
                arguments.Add(
                    "parameters: new global::Cerneala.Timbre.SoundParameter[] { " +
                    string.Join(", ", clip.Parameters.Select(parameter => parameterVariables[parameter.Name])) + " }");
            }

            if (clip.Modifiers.Count > 0)
            {
                arguments.Add(
                    "modifiers: new global::Cerneala.Timbre.SoundModifier[] { " +
                    string.Join(", ", clip.Modifiers.Select(modifier => EmitSoundModifier(modifier, parameterVariables))) + " }");
            }

            currentLines.Add(SoundClipType + " " + variable + " = new " + SoundClipType + "(" + string.Join(", ", arguments) + ");");
            SoundClipResource declaration = new(name, variable, clip, resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.SoundClip, declaration));
            scope.RuntimeResources.Add(declaration);
        }

        private static string EmitSoundModifier(BoundSoundModifier modifier, IReadOnlyDictionary<string, string> parameterVariables)
        {
            IEnumerable<string> inputs = modifier.Inputs.Select(input =>
                char.ToLowerInvariant(input.Name[0]) + input.Name.Substring(1) + ": new global::Cerneala.Timbre.SoundInput<float>(" +
                (input.Parameter is null ? FloatLiteral(input.Value) : parameterVariables[input.Parameter.Name]) + ")");
            return "new global::Cerneala.Timbre." + modifier.Kind + "(" + string.Join(", ", inputs) + ")";
        }

        private static string FloatLiteral(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture) + "f";

        private readonly HashSet<MotionCancelNode> soundCancels = new();
        private readonly Dictionary<DirectiveNode, string> soundActionNames = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), string> motionSessionNames = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), IReadOnlyList<(DirectiveOnNode Trigger, IEventSymbol Event)>> resolvedSoundTriggers = new();

        // Attaches the bound Sound model of the declaring document and marks
        // every @cancel of a Sound handle as a Sound action.
        private void BindAspectSound(AspectResource aspect)
        {
            if (!semanticModel.Sound.Aspects.TryGetValue(aspect.Source.Span.Start, out BoundSoundAspect? sound))
            {
                return;
            }

            aspect.Sound = sound;
            foreach (MotionCancelNode cancel in EnumerateActionNodes(aspect).OfType<MotionCancelNode>())
            {
                if (sound.Handles.TryGetValue(cancel.HandleName, out SoundHandleKind kind) && kind == SoundHandleKind.Sound)
                {
                    soundCancels.Add(cancel);
                }
            }
        }

        private bool IsSoundNode(DirectiveNode node) =>
            node is SoundActionNode || node is MotionCancelNode cancel && soundCancels.Contains(cancel);

        private bool ContainsSoundAction(DirectiveWhenNode when) =>
            (when.BooleanBody is not null && ContainsSoundAction(when.BooleanBody)) ||
            when.Branches.Any(branch => ContainsSoundAction(branch.Body));

        private bool ContainsSoundAction(IReadOnlyList<DirectiveNode> nodes) =>
            nodes.Any(node => IsSoundOwnedNode(node) || node is DirectiveWhenNode when && ContainsSoundAction(when));

        private bool HasSoundActivation(ReactiveRule rule) => rule.SoundBody.Any(IsSoundOwnedNode);

        private static IEnumerable<DirectiveNode> EnumerateActionNodes(AspectResource aspect) =>
            aspect.EventTriggers.SelectMany(trigger => trigger.Actions)
                .Concat(aspect.Conditions.SelectMany(EnumerateConditionNodes));

        private static IEnumerable<DirectiveNode> EnumerateConditionNodes(DirectiveWhenNode when)
        {
            IEnumerable<DirectiveNode> bodies = (when.BooleanBody ?? [])
                .Concat(when.Branches.SelectMany(branch => branch.Body));
            foreach (DirectiveNode node in bodies)
            {
                if (node is DirectiveWhenNode nested)
                {
                    foreach (DirectiveNode child in EnumerateConditionNodes(nested))
                    {
                        yield return child;
                    }
                }
                else
                {
                    yield return node;
                }
            }
        }

        private static int SequenceOf(DirectiveNode node) => node switch
        {
            SoundActionNode sound => sound.Sequence,
            MotionCancelNode cancel => cancel.Sequence,
            _ => -1
        };

        // Lowers the Sound actions of one concrete Aspect application: a Sound
        // session owned by the element, one Action per bound statement and
        // the event handlers of @on bodies that contain Sound actions.
        private void EmitSoundActivations(
            MarkupElement element,
            string variable,
            AspectResource aspect,
            bool bindToElementAspect)
        {
            if (aspect.Sound is not BoundSoundAspect sound || sound.Actions.Count == 0)
            {
                if (EnumerateActionNodes(aspect).Any(node => node is SoundActionNode))
                {
                    Report(InvalidDirective, aspect.Source, Path.GetFileName(file.Path), "Sound actions have no bound Sound model.");
                }

                return;
            }

            DirectiveNode[] nodes = EnumerateActionNodes(aspect)
                .Where(IsSoundNode)
                .Distinct()
                .OrderBy(SequenceOf)
                .ToArray();
            if (nodes.Length != sound.Actions.Count ||
                nodes.Zip(sound.Actions, (node, action) => !Matches(node, action)).Any(mismatch => mismatch))
            {
                Report(InvalidDirective, aspect.Source, Path.GetFileName(file.Path), "Sound actions do not match their bound Sound model.");
                return;
            }

            string session = "soundSession" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            currentPostLines.Add(
                "global::System.IDisposable " + session +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.AttachSoundSession(" + variable +
                (bindToElementAspect ? ", " + variable + ".Aspect!" : string.Empty) + ");");
            if (templateEmissionContexts.Count > 0)
            {
                currentPostLines.Add(templateEmissionContexts.Peek().ContextVariable + ".RegisterLifetime(" + session + ");");
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                string name = "soundAction" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                soundActionNames[nodes[index]] = name;
                currentPostLines.Add("global::System.Action " + name + " = " + EmitSoundAction(session, sound.Actions[index]) + ";");
            }

            EmitAudioMotionExecutions(element, variable, aspect, session);
            if (!resolvedSoundTriggers.TryGetValue((aspect, element), out IReadOnlyList<(DirectiveOnNode Trigger, IEventSymbol Event)>? triggers))
            {
                return;
            }

            string? motionSession = motionSessionNames.TryGetValue((aspect, element), out string? motion) ? motion : null;
            foreach ((DirectiveOnNode trigger, IEventSymbol eventSymbol) in triggers)
            {
                string handler = "soundEventHandler" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                INamedTypeSymbol delegateType = (INamedTypeSymbol)eventSymbol.Type;
                string parameters = string.Join(", ", delegateType.DelegateInvokeMethod!.Parameters
                    .Select((_, index) => "eventArg" + index.ToString(CultureInfo.InvariantCulture)));
                string calls = string.Join(" ", trigger.Actions.Select(node => soundActionNames.TryGetValue(node, out string? action)
                    ? action + "();"
                    : "if (global::Cerneala.UI.Markup.GeneratedMarkup.CanStartMotionExecution(" + motionSession + ")) " +
                        GetMotionExecutionName((MotionExecutionNode)node) + "();"));
                currentPostLines.Add(
                    delegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " " + handler +
                    " = (" + parameters + ") => { " + calls + " };");
                currentPostLines.Add(
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AddSoundTrigger(" + session +
                    ", () => " + variable + "." + eventSymbol.Name + " += " + handler +
                    ", () => " + variable + "." + eventSymbol.Name + " -= " + handler + ");");
            }
        }

        // A root plan re-collects the root Aspect's conditions for its values;
        // the audio activations belong only to the plan ApplyAspects emits.
        private static void ExcludeSoundActivations(ReactivePlan plan)
        {
            plan.Rules.RemoveAll(rule => rule.SoundBody.Count > 0 &&
                rule.Assignments.Count == 0 && rule.Elements.Count == 0 && rule.Activations.Count == 0);
            foreach (ReactiveRule rule in plan.Rules)
            {
                rule.SoundBody = [];
            }
        }

        // The audio activation of a reactive rule: its Sound actions in source
        // order, with the visual activation (when due) at the Motion position.
        private string? EmitSoundActivationCode(ReactiveRule rule)
        {
            if (!rule.SoundBody.Any(node => soundActionNames.ContainsKey(node)))
            {
                return null;
            }

            List<string> calls = [];
            bool visualPlaced = false;
            foreach (DirectiveNode node in rule.SoundBody)
            {
                if (soundActionNames.TryGetValue(node, out string? action))
                {
                    calls.Add(action + "();");
                }
                else if (!visualPlaced && rule.Activations.Count > 0)
                {
                    calls.Add("visualActivation?.Invoke();");
                    visualPlaced = true;
                }
            }

            if (!visualPlaced && rule.Activations.Count > 0)
            {
                calls.Add("visualActivation?.Invoke();");
            }

            return "visualActivation => { " + string.Join(" ", calls) + " }";
        }

        private static bool Matches(DirectiveNode node, BoundSoundAction action) => node switch
        {
            MotionCancelNode cancel => action.Kind == SoundActionKind.Cancel && cancel.HandleName == action.HandleName,
            SoundActionNode sound => SoundMarkupSyntax.TryGetActionKind(sound.Keyword, out SoundActionKind kind) && kind == action.Kind,
            _ => false
        };

        private static string EmitSoundAction(string session, BoundSoundAction action)
        {
            const string helpers = "global::Cerneala.UI.Markup.GeneratedMarkup.";
            string handle = action.HandleName is null ? "null" : Literal(action.HandleName);
            switch (action.Kind)
            {
                case SoundActionKind.Play:
                    List<string> overrides = [];
                    if (action.Volume is float volume)
                    {
                        overrides.Add("soundStart.Volume = " + FloatLiteral(volume) + ";");
                    }

                    if (action.Loop is bool loop)
                    {
                        overrides.Add("soundStart.Loop = " + (loop ? "true" : "false") + ";");
                    }

                    foreach (BoundSoundArgument argument in action.Arguments)
                    {
                        overrides.Add(
                            "soundStart.Set(" + helpers + "GetSoundParameter(soundClip, " + Literal(argument.Name) + "), " +
                            FloatLiteral(argument.Value) + ");");
                    }

                    string configure = overrides.Count == 0
                        ? "null"
                        : "(soundClip, soundStart) => { " + string.Join(" ", overrides) + " }";
                    return "() => " + helpers + "PlaySound(" + session +
                        ", new global::Cerneala.UI.Resources.ResourceId<" + SoundClipType + ">(" + Literal(action.ClipName!) + "), " +
                        configure + ", " + handle + ")";
                case SoundActionKind.Cancel:
                    return "() => " + helpers + "CancelSound(" + session + ", " + handle + ")";
                case SoundActionKind.Pause:
                    return "() => " + helpers + "PauseSound(" + session + ", " + handle + ")";
                case SoundActionKind.Resume:
                    return "() => " + helpers + "ResumeSound(" + session + ", " + handle + ")";
                default:
                    return "() => { _ = " + helpers + "SeekSound(" + session + ", " + handle +
                        ", global::System.TimeSpan.FromTicks(" + action.SeekTicks.ToString(CultureInfo.InvariantCulture) + "L)); }";
            }
        }
    }
}
