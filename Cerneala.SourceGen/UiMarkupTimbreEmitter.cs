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
        private const string TimbreClipDefinitionType = "global::Cerneala.Timbre.TimbreClipDefinition";

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

        private int nextInlineTimbreClipId;

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

            if (!semanticModel.Timbre.Clips.TryGetValue(resource.Span.Start, out BoundTimbreClip? clip) || !clip.IsValid)
            {
                Report(InvalidDirective, resource, Path.GetFileName(file.Path), "TimbreClip '" + name + "' has no bound Timbre definition.");
                return;
            }

            string variable = CreateIdentifier(name) + "TimbreClip" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            EmitTimbreClipDefinition(clip, name, variable, currentLines);
            TimbreClipResource declaration = new(name, variable, clip, resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.TimbreClip, declaration));
            scope.RuntimeResources.Add(declaration);
        }

        // Declares `variable` as the TimbreClipDefinition of `clip` in `lines`:
        // the clip parameters once, then one TimbreSound per @sound with the
        // parameters its modifiers use.
        private static void EmitTimbreClipDefinition(BoundTimbreClip clip, string name, string variable, List<string> lines)
        {
            Dictionary<string, string> parameterVariables = new();
            foreach (BoundTimbreParameter parameter in clip.Parameters)
            {
                string parameterVariable = variable + "_" + parameter.Name;
                parameterVariables.Add(parameter.Name, parameterVariable);
                lines.Add(
                    "global::Cerneala.Timbre.TimbreParameter<float> " + parameterVariable +
                    " = new global::Cerneala.Timbre.TimbreParameter<float>(" + Literal(parameter.Name) + ", " +
                    FloatLiteral(parameter.DefaultValue) + ");");
            }

            List<string> sounds = [];
            foreach (BoundTimbreSound sound in clip.Sounds)
            {
                List<string> arguments =
                [
                    "global::Cerneala.Timbre.TimbreSource.FromFile(" + Literal(sound.Source!) + ")",
                    "volume: " + FloatLiteral(sound.Volume),
                    "loop: " + (sound.Loop ? "true" : "false")
                ];
                if (sound.Parameters.Count > 0)
                {
                    arguments.Add(
                        "parameters: new global::Cerneala.Timbre.TimbreParameter[] { " +
                        string.Join(", ", sound.Parameters.Select(parameter => parameterVariables[parameter.Name])) + " }");
                }

                if (sound.Modifiers.Count > 0)
                {
                    arguments.Add(
                        "modifiers: new global::Cerneala.Timbre.TimbreModifier[] { " +
                        string.Join(", ", sound.Modifiers.Select(modifier => EmitTimbreModifier(modifier, parameterVariables))) + " }");
                }

                sounds.Add(
                    "new global::Cerneala.Timbre.TimbreClipSound(" + Literal(sound.Name) +
                    ", new global::Cerneala.Timbre.TimbreSound(" + string.Join(", ", arguments) + ")" +
                    (sound.AutoPlay ? ", autoPlay: true" : string.Empty) + ")");
            }

            string parameters = clip.Parameters.Count == 0
                ? string.Empty
                : ", new global::Cerneala.Timbre.TimbreParameter[] { " +
                    string.Join(", ", clip.Parameters.Select(parameter => parameterVariables[parameter.Name])) + " }";
            lines.Add(
                TimbreClipDefinitionType + " " + variable + " = new " + TimbreClipDefinitionType + "(" + Literal(name) +
                ", new global::Cerneala.Timbre.TimbreClipSound[] { " + string.Join(", ", sounds) + " }" + parameters + ");");
        }

        // An inline `@timbre { … }` is one immutable definition shared by every
        // application: a static field of the generated type.
        private string EmitInlineTimbreClip(BoundTimbreClip clip, string name)
        {
            string id = nextInlineTimbreClipId.ToString(CultureInfo.InvariantCulture);
            nextInlineTimbreClipId++;
            string field = "__CernealaTimbreClip" + id;
            List<string> body = [];
            EmitTimbreClipDefinition(clip, name, "clip", body);
            PrismDeclarationLines.Add("private static readonly " + TimbreClipDefinitionType + " " + field + " = __CernealaCreateTimbreClip" + id + "();");
            PrismDeclarationLines.Add("private static " + TimbreClipDefinitionType + " __CernealaCreateTimbreClip" + id + "()");
            PrismDeclarationLines.Add("{");
            PrismDeclarationLines.AddRange(body.Select(line => "    " + line));
            PrismDeclarationLines.Add("    return clip;");
            PrismDeclarationLines.Add("}");
            return field;
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

        private readonly Dictionary<DirectiveNode, string> timbreActionNames = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), string> motionSessionNames = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), IReadOnlyList<(DirectiveOnNode Trigger, IEventSymbol Event)>> resolvedTimbreTriggers = new();

        // Attaches the bound Timbre model of the declaring document.
        private void BindAspectTimbre(AspectResource aspect)
        {
            if (semanticModel.Timbre.Aspects.TryGetValue(aspect.Source.Span.Start, out BoundTimbreAspect? sound))
            {
                aspect.Timbre = sound;
            }
        }

        private static bool IsTimbreNode(DirectiveNode node) => node is TimbreActionNode;

        private bool ContainsTimbreAction(DirectiveWhenNode when) =>
            (when.BooleanBody is not null && ContainsTimbreAction(when.BooleanBody)) ||
            when.Branches.Any(branch => ContainsTimbreAction(branch.Body));

        private bool ContainsTimbreAction(IReadOnlyList<DirectiveNode> nodes) =>
            nodes.Any(node => IsTimbreOwnedNode(node) || node is DirectiveWhenNode when && ContainsTimbreAction(when));

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

        // Lowers the Timbre program of one Aspect application to `variable`:
        // the @timbre attachment (a lifetime that starts the AutoPlay sounds),
        // one Action per command and the Timbre session owning the handlers
        // of @on bodies with commands or audio Motion.
        private void EmitTimbreActivations(
            MarkupElement element,
            string variable,
            AspectResource aspect)
        {
            BoundTimbreAspect? sound = aspect.Timbre;
            if (sound?.Attachment is BoundTimbreAttachment attachment)
            {
                string clipCode = attachment.ResourceName is string resourceName
                    ? "new global::Cerneala.UI.Resources.ResourceId<" + TimbreClipDefinitionType + ">(" + Literal(resourceName) + ")"
                    : EmitInlineTimbreClip(attachment.Clip, (aspect.Name ?? "Inline") + "." + element.Name.LocalName);
                string arguments = attachment.Arguments.Count == 0
                    ? "null"
                    : "new global::System.Collections.Generic.Dictionary<string, float> { " +
                        string.Join(", ", attachment.Arguments.Select(argument => "[" + Literal(argument.Name) + "] = " + FloatLiteral(argument.Value))) + " }";
                string lifetime = "timbreAttachment" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                currentLines.Add(
                    "global::System.IDisposable " + lifetime + " = global::Cerneala.UI.Markup.GeneratedMarkup.AttachTimbre(" +
                    variable + ", " + clipCode + ", " + arguments + ");");
            }

            DirectiveNode[] nodes = EnumerateActionNodes(aspect)
                .Where(IsTimbreNode)
                .Distinct()
                .OrderBy(node => ((TimbreActionNode)node).Sequence)
                .ToArray();
            IReadOnlyList<BoundTimbreCommand> commands = sound?.Commands ?? [];
            if (nodes.Length != commands.Count ||
                nodes.Zip(commands, (node, command) => !Matches((TimbreActionNode)node, command)).Any(mismatch => mismatch))
            {
                Report(InvalidDirective, aspect.Source, Path.GetFileName(file.Path), "Timbre commands do not match their bound Timbre model.");
                return;
            }

            bool hasAudioMotion = EnumerateActionNodes(aspect).OfType<MotionExecutionNode>().Any(IsAudioMotionNode);
            if (nodes.Length == 0 && !hasAudioMotion)
            {
                return;
            }

            string session = "timbreSession" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            currentPostLines.Add(
                "global::System.IDisposable " + session +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.AttachTimbreSession(" + variable + ");");

            for (int index = 0; index < nodes.Length; index++)
            {
                string name = "timbreAction" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                timbreActionNames[nodes[index]] = name;
                currentPostLines.Add("global::System.Action " + name + " = " + EmitTimbreCommand(commands[index], variable) + ";");
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
        // its Motion and Timbre activations belong to the Aspect behavior.
        private static void ExcludeAspectProgramActivations(ReactivePlan plan)
        {
            plan.Rules.RemoveAll(rule => rule.Assignments.Count == 0 && rule.Elements.Count == 0);
            foreach (ReactiveRule rule in plan.Rules)
            {
                rule.Activations = [];
                rule.TimbreBody = [];
            }
        }

        // The audio activation of a reactive rule: its Timbre commands in source
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

        private static bool Matches(TimbreActionNode node, BoundTimbreCommand command) =>
            TimbreMarkupSyntax.TryGetCommandKind(node.Keyword, out TimbreCommandKind kind) && kind == command.Kind;

        // The element a command or audio Motion addresses, from the Aspect
        // behavior's `target`.
        private string EmitTimbreTargetCode(TimbreCommandTarget target, string? targetName, string variable) => target switch
        {
            TimbreCommandTarget.Self => variable,
            TimbreCommandTarget.Owner => templateEmissionContexts.Count == 0
                ? "global::Cerneala.UI.Markup.GeneratedMarkup.GetTemplateOwner(" + variable + ")"
                : templateEmissionContexts.Peek().OwnerVariable,
            _ => CreateIdentifier(targetName!)
        };

        private string EmitTimbreCommand(BoundTimbreCommand command, string variable)
        {
            const string helpers = "global::Cerneala.UI.Markup.GeneratedMarkup.";
            string target = EmitTimbreTargetCode(command.Target, command.TargetName, variable);
            string sound = Literal(command.Sound);
            return command.Kind switch
            {
                TimbreCommandKind.Play => "() => " + helpers + "PlayTimbre(" + target + ", " + sound + ")",
                TimbreCommandKind.Stop => "() => " + helpers + "StopTimbre(" + target + ", " + sound + ")",
                TimbreCommandKind.Pause => "() => " + helpers + "PauseTimbre(" + target + ", " + sound + ")",
                TimbreCommandKind.Resume => "() => " + helpers + "ResumeTimbre(" + target + ", " + sound + ")",
                _ => "() => { _ = " + helpers + "SeekTimbre(" + target + ", " + sound +
                    ", global::System.TimeSpan.FromTicks(" + command.SeekTicks.ToString(CultureInfo.InvariantCulture) + "L)); }"
            };
        }
    }
}
