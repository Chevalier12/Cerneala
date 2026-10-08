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

        // The typed audio target comes from the Aspect's bound Timbre schema;
        // validity was decided by Cerneala.Language, so a missing schema is an
        // internal mismatch, not a fallback to element properties.
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
            if (aspect.Timbre is not BoundTimbreAspect sound ||
                segments.Length != 4 ||
                segments[0] != "$self" ||
                !sound.Handles.TryGetValue(segments[2], out TimbreHandleKind kind) ||
                kind != TimbreHandleKind.Timbre ||
                segments[3] != "Volume" && sound.FindHandleParameter(segments[2], segments[3]) is null)
            {
                ReportMotion(
                    MotionDiagnosticKind.Target,
                    assignment.Location,
                    "Timbre Motion target '" + assignment.Target + "' has no bound Timbre handle schema.");
                return false;
            }

            property = new PropertySpec(
                segments[3],
                MarkupValueKind.Float,
                "__sound_" + segments[2] + "_" + segments[3],
                compilation.GetSpecialType(SpecialType.System_Single));
            target = new ResolvedMotionTarget(
                ResolvedMotionTargetKind.Self,
                applicationElement,
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
