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
        private const string TimbreClipType = "global::Cerneala.Timbre.TimbreClip";

        private sealed class TimbreClipResource
        {
            public TimbreClipResource(string name, string variable, BoundTimbreClip clip, MarkupElement element)
            {
                Name = name;
                Variable = variable;
                Clip = clip;
                Element = element;
            }

            public string Name { get; }

            public string Variable { get; }

            public BoundTimbreClip Clip { get; }

            public MarkupElement Element { get; }
        }

        // Lowers a <TimbreClip> resource to the same immutable definition C#
        // builds with the core constructors. Validity was decided by the bound
        // Cerneala.Language Timbre model; nothing is opened or played here.
        private void ReadTimbreClip(ResourceScope scope, MarkupElement resource)
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

            if (!semanticModel.Timbre.Clips.TryGetValue(resource.Span.Start, out BoundTimbreClip? clip) ||
                !clip.IsValid ||
                clip.Source is null)
            {
                Report(InvalidDirective, resource, Path.GetFileName(file.Path), "TimbreClip '" + name + "' has no bound Timbre definition.");
                return;
            }

            string variable = CreateIdentifier(name) + "TimbreClip" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            Dictionary<string, string> parameterVariables = new();
            foreach (BoundTimbreParameter parameter in clip.Parameters)
            {
                string parameterVariable = variable + "_" + parameter.Name;
                parameterVariables.Add(parameter.Name, parameterVariable);
                currentLines.Add(
                    "global::Cerneala.Timbre.TimbreParameter<float> " + parameterVariable +
                    " = new global::Cerneala.Timbre.TimbreParameter<float>(" + Literal(parameter.Name) + ", " +
                    FloatLiteral(parameter.DefaultValue) + ");");
            }

            List<string> arguments =
            [
                "global::Cerneala.Timbre.TimbreSource.FromFile(" + Literal(clip.Source) + ")",
                "volume: " + FloatLiteral(clip.Volume),
                "loop: " + (clip.Loop ? "true" : "false")
            ];
            if (clip.Parameters.Count > 0)
            {
                arguments.Add(
                    "parameters: new global::Cerneala.Timbre.TimbreParameter[] { " +
                    string.Join(", ", clip.Parameters.Select(parameter => parameterVariables[parameter.Name])) + " }");
            }

            if (clip.Modifiers.Count > 0)
            {
                arguments.Add(
                    "modifiers: new global::Cerneala.Timbre.TimbreModifier[] { " +
                    string.Join(", ", clip.Modifiers.Select(modifier => EmitTimbreModifier(modifier, parameterVariables))) + " }");
            }

            currentLines.Add(TimbreClipType + " " + variable + " = new " + TimbreClipType + "(" + string.Join(", ", arguments) + ");");
            TimbreClipResource declaration = new(name, variable, clip, resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.TimbreClip, declaration));
            scope.RuntimeResources.Add(declaration);
        }

        private static string EmitTimbreModifier(BoundTimbreModifier modifier, IReadOnlyDictionary<string, string> parameterVariables)
        {
            IEnumerable<string> inputs = modifier.Inputs.Select(input =>
                char.ToLowerInvariant(input.Name[0]) + input.Name.Substring(1) + ": new global::Cerneala.Timbre.TimbreInput<float>(" +
                (input.Parameter is null ? FloatLiteral(input.Value) : parameterVariables[input.Parameter.Name]) + ")");
            return "new global::Cerneala.Timbre." + modifier.Kind + "(" + string.Join(", ", inputs) + ")";
        }

        private static string FloatLiteral(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture) + "f";

        private readonly HashSet<MotionCancelNode> timbreCancels = new();
        private readonly Dictionary<DirectiveNode, string> timbreActionNames = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), string> motionSessionNames = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), IReadOnlyList<(DirectiveOnNode Trigger, IEventSymbol Event)>> resolvedTimbreTriggers = new();

        // Attaches the bound Timbre model of the declaring document and marks
        // every @cancel of a Timbre handle as a Timbre action.
        private void BindAspectTimbre(AspectResource aspect)
        {
            if (!semanticModel.Timbre.Aspects.TryGetValue(aspect.Source.Span.Start, out BoundTimbreAspect? sound))
            {
                return;
            }

            aspect.Timbre = sound;
            foreach (MotionCancelNode cancel in EnumerateActionNodes(aspect).OfType<MotionCancelNode>())
            {
                if (sound.Handles.TryGetValue(cancel.HandleName, out TimbreHandleKind kind) && kind == TimbreHandleKind.Timbre)
                {
                    timbreCancels.Add(cancel);
                }
            }
        }

        private bool IsTimbreNode(DirectiveNode node) =>
            node is TimbreActionNode || node is MotionCancelNode cancel && timbreCancels.Contains(cancel);

        private bool ContainsTimbreAction(DirectiveWhenNode when) =>
            (when.BooleanBody is not null && ContainsTimbreAction(when.BooleanBody)) ||
            when.Branches.Any(branch => ContainsTimbreAction(branch.Body));

        private bool ContainsTimbreAction(IReadOnlyList<DirectiveNode> nodes) =>
            nodes.Any(node => IsTimbreOwnedNode(node) || node is DirectiveWhenNode when && ContainsTimbreAction(when));

        private bool HasTimbreActivation(ReactiveRule rule) => rule.TimbreBody.Any(IsTimbreOwnedNode);

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
            TimbreActionNode sound => sound.Sequence,
            MotionCancelNode cancel => cancel.Sequence,
            _ => -1
        };

        // Lowers the Timbre actions of one concrete Aspect application: a Timbre
        // session owned by the element, one Action per bound statement and
        // the event handlers of @on bodies that contain Timbre actions.
        private void EmitTimbreActivations(
            MarkupElement element,
            string variable,
            AspectResource aspect,
            bool bindToElementAspect)
        {
            if (aspect.Timbre is not BoundTimbreAspect sound || sound.Actions.Count == 0)
            {
                if (EnumerateActionNodes(aspect).Any(node => node is TimbreActionNode))
                {
                    Report(InvalidDirective, aspect.Source, Path.GetFileName(file.Path), "Timbre actions have no bound Timbre model.");
                }

                return;
            }

            DirectiveNode[] nodes = EnumerateActionNodes(aspect)
                .Where(IsTimbreNode)
                .Distinct()
                .OrderBy(SequenceOf)
                .ToArray();
            if (nodes.Length != sound.Actions.Count ||
                nodes.Zip(sound.Actions, (node, action) => !Matches(node, action)).Any(mismatch => mismatch))
            {
                Report(InvalidDirective, aspect.Source, Path.GetFileName(file.Path), "Timbre actions do not match their bound Timbre model.");
                return;
            }

            string session = "timbreSession" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            currentPostLines.Add(
                "global::System.IDisposable " + session +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.AttachTimbreSession(" + variable +
                (bindToElementAspect ? ", " + variable + ".Aspect!" : string.Empty) + ");");
            if (templateEmissionContexts.Count > 0)
            {
                currentPostLines.Add(templateEmissionContexts.Peek().ContextVariable + ".RegisterLifetime(" + session + ");");
            }

            for (int index = 0; index < nodes.Length; index++)
            {
                string name = "timbreAction" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                timbreActionNames[nodes[index]] = name;
                currentPostLines.Add("global::System.Action " + name + " = " + EmitTimbreAction(session, sound.Actions[index]) + ";");
            }

            EmitAudioMotionExecutions(element, variable, aspect, session);
            if (!resolvedTimbreTriggers.TryGetValue((aspect, element), out IReadOnlyList<(DirectiveOnNode Trigger, IEventSymbol Event)>? triggers))
            {
                return;
            }

            string? motionSession = motionSessionNames.TryGetValue((aspect, element), out string? motion) ? motion : null;
            foreach ((DirectiveOnNode trigger, IEventSymbol eventSymbol) in triggers)
            {
                string handler = "timbreEventHandler" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                INamedTypeSymbol delegateType = (INamedTypeSymbol)eventSymbol.Type;
                string parameters = string.Join(", ", delegateType.DelegateInvokeMethod!.Parameters
                    .Select((_, index) => "eventArg" + index.ToString(CultureInfo.InvariantCulture)));
                string calls = string.Join(" ", trigger.Actions.Select(node => timbreActionNames.TryGetValue(node, out string? action)
                    ? action + "();"
                    : "if (global::Cerneala.UI.Markup.GeneratedMarkup.CanStartMotionExecution(" + motionSession + ")) " +
                        GetMotionExecutionName((MotionExecutionNode)node) + "();"));
                currentPostLines.Add(
                    delegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " " + handler +
                    " = (" + parameters + ") => { " + calls + " };");
                currentPostLines.Add(
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AddTimbreTrigger(" + session +
                    ", () => " + variable + "." + eventSymbol.Name + " += " + handler +
                    ", () => " + variable + "." + eventSymbol.Name + " -= " + handler + ");");
            }
        }

        // A root plan re-collects the root Aspect's conditions for its values;
        // the audio activations belong only to the plan ApplyAspects emits.
        private static void ExcludeTimbreActivations(ReactivePlan plan)
        {
            plan.Rules.RemoveAll(rule => rule.TimbreBody.Count > 0 &&
                rule.Assignments.Count == 0 && rule.Elements.Count == 0 && rule.Activations.Count == 0);
            foreach (ReactiveRule rule in plan.Rules)
            {
                rule.TimbreBody = [];
            }
        }

        // The audio activation of a reactive rule: its Timbre actions in source
        // order, with the visual activation (when due) at the Motion position.
        private string? EmitTimbreActivationCode(ReactiveRule rule)
        {
            if (!rule.TimbreBody.Any(node => timbreActionNames.ContainsKey(node)))
            {
                return null;
            }

            List<string> calls = [];
            bool visualPlaced = false;
            foreach (DirectiveNode node in rule.TimbreBody)
            {
                if (timbreActionNames.TryGetValue(node, out string? action))
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

        private static bool Matches(DirectiveNode node, BoundTimbreAction action) => node switch
        {
            MotionCancelNode cancel => action.Kind == TimbreActionKind.Cancel && cancel.HandleName == action.HandleName,
            TimbreActionNode sound => TimbreMarkupSyntax.TryGetActionKind(sound.Keyword, out TimbreActionKind kind) && kind == action.Kind,
            _ => false
        };

        private static string EmitTimbreAction(string session, BoundTimbreAction action)
        {
            const string helpers = "global::Cerneala.UI.Markup.GeneratedMarkup.";
            string handle = action.HandleName is null ? "null" : Literal(action.HandleName);
            switch (action.Kind)
            {
                case TimbreActionKind.Play:
                    List<string> overrides = [];
                    if (action.Volume is float volume)
                    {
                        overrides.Add("timbreStart.Volume = " + FloatLiteral(volume) + ";");
                    }

                    if (action.Loop is bool loop)
                    {
                        overrides.Add("timbreStart.Loop = " + (loop ? "true" : "false") + ";");
                    }

                    foreach (BoundTimbreArgument argument in action.Arguments)
                    {
                        overrides.Add(
                            "timbreStart.Set(" + helpers + "GetTimbreParameter(timbreClip, " + Literal(argument.Name) + "), " +
                            FloatLiteral(argument.Value) + ");");
                    }

                    string configure = overrides.Count == 0
                        ? "null"
                        : "(timbreClip, timbreStart) => { " + string.Join(" ", overrides) + " }";
                    return "() => " + helpers + "PlayTimbre(" + session +
                        ", new global::Cerneala.UI.Resources.ResourceId<" + TimbreClipType + ">(" + Literal(action.ClipName!) + "), " +
                        configure + ", " + handle + ")";
                case TimbreActionKind.Cancel:
                    return "() => " + helpers + "CancelTimbre(" + session + ", " + handle + ")";
                case TimbreActionKind.Pause:
                    return "() => " + helpers + "PauseTimbre(" + session + ", " + handle + ")";
                case TimbreActionKind.Resume:
                    return "() => " + helpers + "ResumeTimbre(" + session + ", " + handle + ")";
                default:
                    return "() => { _ = " + helpers + "SeekTimbre(" + session + ", " + handle +
                        ", global::System.TimeSpan.FromTicks(" + action.SeekTicks.ToString(CultureInfo.InvariantCulture) + "L)); }";
            }
        }
    }
}
