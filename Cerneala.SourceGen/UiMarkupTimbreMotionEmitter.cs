using System.IO;
using System.Linq;
using Cerneala.Language.Timbre;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private static bool IsTimbreMotionTarget(string target)
        {
            string[] segments = target.Split('.');
            return segments.Length >= 3 && segments[1] == "timbre";
        }

        // An execution animating `.timbre.` targets. The bound Language model
        // guarantees such an execution targets nothing else.
        private static bool IsAudioMotionNode(MotionExecutionNode node) => node switch
        {
            MotionAnimateNode animate => animate.To.Concat(animate.From).Any(assignment => IsTimbreMotionTarget(assignment.Target)),
            MotionKeyframesNode keyframes => keyframes.Segments.Any(segment => IsAudioMotionNode(segment.Animation)),
            MotionCompositionNode composition => composition.Children.Any(IsAudioMotionNode),
            _ => false
        };

        private static bool IsAudioMotion(ResolvedMotionAnimation animation) =>
            animation.Properties.Any(property => property.Target.Timbre is not null);

        private static bool IsAudioMotion(ResolvedMotionComposition composition) =>
            composition.Syntax is not null && IsAudioMotionNode(composition.Syntax);

        // Nodes the Timbre session runs: Timbre actions and audio Motion.
        private bool IsTimbreOwnedNode(DirectiveNode node) =>
            IsTimbreNode(node) || node is MotionExecutionNode execution && IsAudioMotionNode(execution);

        // `$self|$owner|$Name.timbre.Sound.Property`: validity (the sound exists
        // on the target, the property is Volume or a parameter the sound uses)
        // was decided by Cerneala.Language; here only the target element is
        // resolved, as for any Motion target.
        private bool TryResolveTimbreMotionTarget(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionAssignmentSyntax assignment,
            out ResolvedMotionTarget? target,
            out PropertySpec? property)
        {
            target = null;
            property = null;
            string[] segments = assignment.Target.Split('.');
            ResolvedMotionTargetKind kind = segments[0] switch
            {
                "$self" => ResolvedMotionTargetKind.Self,
                "$owner" => ResolvedMotionTargetKind.Owner,
                _ => ResolvedMotionTargetKind.Named
            };
            MarkupElement? element = kind == ResolvedMotionTargetKind.Named
                ? FindMotionNamedElement(applicationElement, aspect, segments[0].Substring(1))
                : applicationElement;
            if (segments.Length != 4 || element is null)
            {
                ReportMotion(
                    MotionDiagnosticKind.Target,
                    assignment.Location,
                    "Timbre Motion target '" + assignment.Target + "' has no bound sound.");
                return false;
            }

            property = new PropertySpec(
                segments[3],
                MarkupValueKind.Float,
                "__sound_" + segments[2] + "_" + segments[3],
                compilation.GetSpecialType(SpecialType.System_Single));
            target = new ResolvedMotionTarget(
                kind,
                element,
                ownerName: kind == ResolvedMotionTargetKind.Named ? segments[0].Substring(1) : null,
                sound: new ResolvedTimbreMotionTarget(segments[2], segments[3]));
            return true;
        }

        // Audio executions of one concrete Aspect application, started through
        // the Timbre session so hiding the owner does not cancel them. Their
        // root Actions join the Timbre actions in source order.
        private void EmitAudioMotionExecutions(MarkupElement element, string variable, AspectResource aspect, string session)
        {
            if (!resolvedMotionAspects.TryGetValue((aspect, element), out ResolvedMotionAspect? resolved))
            {
                return;
            }

            foreach (ResolvedMotionAnimation animation in resolved.Animations.Where(IsAudioMotion))
            {
                EmitMotionAnimationActivation(animation, variable, session, "StartTimbreMotion");
            }

            foreach (ResolvedMotionComposition composition in resolved.Compositions.Where(IsAudioMotion))
            {
                EmitMotionCompositionActivation(composition, session, "StartTimbreMotion");
            }

            foreach (MotionExecutionNode root in EnumerateActionNodes(aspect).OfType<MotionExecutionNode>().Where(IsAudioMotionNode))
            {
                timbreActionNames[root] = GetMotionExecutionName(root);
            }
        }
    }
}
