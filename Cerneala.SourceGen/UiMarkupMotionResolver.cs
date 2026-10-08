using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private sealed class MotionClipResource
        {
            public MotionClipResource(
                string name,
                string targetName,
                IReadOnlyList<MotionParameterNode> parameters,
                MotionExecutionNode body,
                MarkupElement source)
            {
                Name = name;
                TargetName = targetName;
                Parameters = parameters;
                Body = body;
                Source = source;
            }

            public string Name { get; }

            public string TargetName { get; }

            public IReadOnlyList<MotionParameterNode> Parameters { get; }

            public MotionExecutionNode Body { get; }

            public MarkupElement Source { get; }
        }

        private sealed class ResolvedMotionParameterValue
        {
            public ResolvedMotionParameterValue(MotionParameterNode parameter, string rawText, string? valueCode, MotionSpecSyntax? spec)
            {
                Parameter = parameter;
                RawText = rawText;
                ValueCode = valueCode;
                Spec = spec;
            }

            public MotionParameterNode Parameter { get; }

            public string RawText { get; }

            public string? ValueCode { get; }

            public MotionSpecSyntax? Spec { get; }
        }

        private sealed class MotionClipInvocationContext
        {
            public MotionClipInvocationContext(IReadOnlyDictionary<string, ResolvedMotionParameterValue> values)
            {
                Values = values;
            }

            public IReadOnlyDictionary<string, ResolvedMotionParameterValue> Values { get; }
        }

        private sealed class ResolvedMotionAspect
        {
            public ResolvedMotionAspect(
                IReadOnlyList<ResolvedMotionAnimation> animations,
                IReadOnlyList<ResolvedMotionSet> sets,
                IReadOnlyList<ResolvedMotionComposition> compositions,
                IReadOnlyList<ResolvedMotionCancelCommand> cancelCommands,
                IReadOnlyList<ResolvedMotionEventTrigger> eventTriggers)
            {
                Animations = animations;
                Sets = sets;
                Compositions = compositions;
                CancelCommands = cancelCommands;
                EventTriggers = eventTriggers;
            }

            public IReadOnlyList<ResolvedMotionAnimation> Animations { get; }

            public IReadOnlyList<ResolvedMotionSet> Sets { get; }

            public IReadOnlyList<ResolvedMotionComposition> Compositions { get; }

            public IReadOnlyList<ResolvedMotionCancelCommand> CancelCommands { get; }

            public IReadOnlyList<ResolvedMotionEventTrigger> EventTriggers { get; }
        }

        private sealed class ResolvedMotionScroll
        {
            public ResolvedMotionScroll(MotionScrollNode syntax, MarkupElement sourceElement, IReadOnlyList<ResolvedMotionScrollProperty> properties)
            {
                Syntax = syntax;
                SourceElement = sourceElement;
                Properties = properties;
            }

            public MotionScrollNode Syntax { get; }

            public MarkupElement SourceElement { get; }

            public IReadOnlyList<ResolvedMotionScrollProperty> Properties { get; }
        }

        private sealed class ResolvedMotionScrollProperty
        {
            public ResolvedMotionScrollProperty(MotionScrollAssignmentSyntax syntax, ResolvedMotionTarget target, PropertySpec property)
            {
                Syntax = syntax;
                Target = target;
                Property = property;
            }

            public MotionScrollAssignmentSyntax Syntax { get; }

            public ResolvedMotionTarget Target { get; }

            public PropertySpec Property { get; }
        }

        private sealed class ResolvedMotionDrag
        {
            public ResolvedMotionDrag(string releaseSpec)
            {
                ReleaseSpec = releaseSpec;
            }

            public string ReleaseSpec { get; }
        }

        private sealed class ResolvedMotionGesturePress
        {
            public ResolvedMotionGesturePress(string spec)
            {
                Spec = spec;
            }

            public string Spec { get; }
        }

        private sealed class ResolvedMotionCancelCommand
        {
            public ResolvedMotionCancelCommand(string actionName, string handleName)
            {
                ActionName = actionName;
                HandleName = handleName;
            }

            public string ActionName { get; }

            public string HandleName { get; }

        }

        private sealed class ResolvedMotionAnimation
        {
            public ResolvedMotionAnimation(
                MotionAnimateNode syntax,
                IReadOnlyList<ResolvedMotionProperty> properties,
                string executionName,
                string factoryName,
                MotionClipInvocationContext? parameters,
                MotionStaggerNode? stagger = null,
                MarkupElement? staggerTarget = null)
            {
                Syntax = syntax;
                Properties = properties;
                ExecutionName = executionName;
                FactoryName = factoryName;
                Parameters = parameters;
                Stagger = stagger;
                StaggerTarget = staggerTarget;
            }

            public MotionAnimateNode Syntax { get; }

            public IReadOnlyList<ResolvedMotionProperty> Properties { get; }

            public string ExecutionName { get; }

            public string FactoryName { get; }

            public MotionClipInvocationContext? Parameters { get; }

            public MotionStaggerNode? Stagger { get; }

            public MarkupElement? StaggerTarget { get; }
        }

        private sealed class ResolvedMotionSet
        {
            public ResolvedMotionSet(
                IReadOnlyList<ResolvedMotionSetProperty> properties,
                string executionName,
                string factoryName,
                MotionClipInvocationContext? parameters)
            {
                Properties = properties;
                ExecutionName = executionName;
                FactoryName = factoryName;
                Parameters = parameters;
            }

            public IReadOnlyList<ResolvedMotionSetProperty> Properties { get; }

            public string ExecutionName { get; }

            public string FactoryName { get; }

            public MotionClipInvocationContext? Parameters { get; }
        }

        private sealed class ResolvedMotionSetProperty
        {
            public ResolvedMotionSetProperty(MotionAssignmentSyntax syntax, ResolvedMotionTarget target, PropertySpec property)
            {
                Syntax = syntax;
                Target = target;
                Property = property;
            }

            public MotionAssignmentSyntax Syntax { get; }

            public ResolvedMotionTarget Target { get; }

            public PropertySpec Property { get; }
        }

        private sealed class ResolvedMotionComposition
        {
            public ResolvedMotionComposition(
                MotionCompositionNode syntax,
                IReadOnlyList<string> childFactoryNames,
                string executionName,
                string factoryName)
            {
                Syntax = syntax;
                ChildFactoryNames = childFactoryNames;
                ExecutionName = executionName;
                FactoryName = factoryName;
            }

            public ResolvedMotionComposition(
                string handleName,
                string childFactoryName,
                string executionName,
                string factoryName)
            {
                HandleName = handleName;
                ChildFactoryNames = [childFactoryName];
                ExecutionName = executionName;
                FactoryName = factoryName;
            }

            public MotionCompositionNode? Syntax { get; }

            public string? HandleName { get; }

            public IReadOnlyList<string> ChildFactoryNames { get; }

            public string ExecutionName { get; }

            public string FactoryName { get; }
        }

        private sealed class ResolvedMotionEventTrigger
        {
            public ResolvedMotionEventTrigger(IEventSymbol eventSymbol, IReadOnlyList<string> executionNames)
            {
                EventSymbol = eventSymbol;
                ExecutionNames = executionNames;
            }

            public IEventSymbol EventSymbol { get; }

            public IReadOnlyList<string> ExecutionNames { get; }
        }

        private sealed class ResolvedMotionPresence
        {
            public ResolvedMotionPresence(string enterSpec, string exitSpec, bool excludeInputWhileExiting)
            {
                EnterSpec = enterSpec;
                ExitSpec = exitSpec;
                ExcludeInputWhileExiting = excludeInputWhileExiting;
            }

            public string EnterSpec { get; }

            public string ExitSpec { get; }

            public bool ExcludeInputWhileExiting { get; }
        }

        private sealed class ResolvedMotionLayout
        {
            public ResolvedMotionLayout(string idExpression, string spec)
            {
                IdExpression = idExpression;
                Spec = spec;
            }

            public string IdExpression { get; }

            public string Spec { get; }
        }

        private sealed class ResolvedMotionProperty
        {
            public ResolvedMotionProperty(
                MotionAssignmentSyntax destination,
                MotionAssignmentSyntax? source,
                ResolvedMotionTarget target,
                PropertySpec property,
                string? specVariable,
                ResolvedMotionKeyframesSpec? keyframes = null)
            {
                Destination = destination;
                Source = source;
                Target = target;
                Property = property;
                SpecVariable = specVariable;
                Keyframes = keyframes;
            }

            public MotionAssignmentSyntax Destination { get; }

            public MotionAssignmentSyntax? Source { get; }

            public ResolvedMotionTarget Target { get; }

            public PropertySpec Property { get; }

            public string? SpecVariable { get; }

            public ResolvedMotionKeyframesSpec? Keyframes { get; }
        }

        private enum ResolvedMotionTargetKind
        {
            Self,
            Named,
            Owner,
            SelfPart,
            NamedPart,
            OwnerPart
        }

        private sealed class ResolvedMotionTarget
        {
            public ResolvedMotionTarget(
                ResolvedMotionTargetKind kind,
                MarkupElement element,
                string? ownerName = null,
                string? partName = null,
                ResolvedPrismMotionTarget? prism = null,
                ResolvedSoundMotionTarget? sound = null)
            {
                Kind = kind;
                Element = element;
                OwnerName = ownerName;
                PartName = partName;
                Prism = prism;
                Sound = sound;
            }

            public ResolvedMotionTargetKind Kind { get; }

            public MarkupElement Element { get; }

            public string? OwnerName { get; }

            public string? PartName { get; }

            public ResolvedPrismMotionTarget? Prism { get; }

            public ResolvedSoundMotionTarget? Sound { get; }
        }

        // `$self.sound.Handle.Parameter`: the Sound handle whose occupant the
        // animation captures and the parameter name it resolves on that
        // occupant's clip, both validated by the bound Language model.
        private sealed class ResolvedSoundMotionTarget
        {
            public ResolvedSoundMotionTarget(string handleName, string parameterName)
            {
                HandleName = handleName;
                ParameterName = parameterName;
            }

            public string HandleName { get; }

            public string ParameterName { get; }
        }

        private sealed class ResolvedMotionKeyframesSpec
        {
            public ResolvedMotionKeyframesSpec(MotionDurationSyntax duration, IReadOnlyList<ResolvedMotionKeyframe> frames)
            {
                Duration = duration;
                Frames = frames;
            }

            public MotionDurationSyntax Duration { get; }

            public IReadOnlyList<ResolvedMotionKeyframe> Frames { get; }
        }

        private sealed class ResolvedMotionKeyframe
        {
            public ResolvedMotionKeyframe(float offset, MotionValueSyntax value, string easingCode, bool hold = false)
            {
                Offset = offset;
                Value = value;
                EasingCode = easingCode;
                Hold = hold;
            }

            public float Offset { get; }

            public MotionValueSyntax Value { get; }

            public string EasingCode { get; }

            public bool Hold { get; }
        }

        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), ResolvedMotionAspect> resolvedMotionAspects = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), ResolvedMotionPresence> resolvedMotionPresences = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), ResolvedMotionLayout> resolvedMotionLayouts = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), IReadOnlyList<ResolvedMotionScroll>> resolvedMotionScrolls = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), ResolvedMotionDrag> resolvedMotionDrags = new();
        private readonly Dictionary<(AspectResource Aspect, MarkupElement Element), ResolvedMotionGesturePress> resolvedMotionGesturePresses = new();
        private readonly Dictionary<MotionAnimateNode, string> motionExecutionNames = new();
        private readonly Dictionary<MotionExecutionNode, string> motionActionNames = new();
        private readonly Dictionary<MotionExecutionNode, string> motionExecutionFactoryNames = new();
        private readonly Dictionary<string, string> specializedMotionSpecs = new(StringComparer.Ordinal);

        private void ReadMotionClip(ResourceScope scope, MarkupElement resource)
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

            string targetName = resource.Attribute("TargetType")?.Value.Trim() ?? string.Empty;
            if (targetName.Length == 0 || ResolveAspectTargetTypeSymbol(targetName, resource) is null)
            {
                Report(InvalidPropertyValue, (object?)resource.Attribute("TargetType") ?? resource, "MotionClip", "TargetType", targetName);
                return;
            }

            DirectiveParseResult parsed = ParseDirectiveContent(
                resource,
                DirectiveContentKind.MotionExecutions | DirectiveContentKind.MotionParameters);
            if (parsed.Error is not null)
            {
                ReportMotion(ClassifyMotionParseError(parsed.Error), parsed.ErrorSource ?? resource, parsed.Error);
                return;
            }

            List<MotionParameterNode> parameters = [];
            MotionExecutionNode? body = null;
            bool bodySeen = false;
            foreach (DirectiveNode node in parsed.Nodes)
            {
                if (node is MotionParameterNode parameter)
                {
                    if (bodySeen)
                    {
                        Report(InvalidDirective, parameter.Location, Path.GetFileName(file.Path), "@parameter declarations must appear before the MotionClip execution body.");
                        return;
                    }

                    if (parameters.Any(existing => existing.Name == parameter.Name))
                    {
                        Report(InvalidDirective, parameter.Location, Path.GetFileName(file.Path), "Duplicate MotionClip parameter '" + parameter.Name + "'.");
                        return;
                    }

                    if (!TryValidateMotionParameter(parameter))
                    {
                        return;
                    }

                    parameters.Add(parameter);
                    continue;
                }

                bodySeen = true;
                if (node is MotionExecutionNode execution && body is null)
                {
                    body = execution;
                }
                else
                {
                    body = null;
                    break;
                }
            }

            if (body is null)
            {
                string forbidden = parsed.Nodes.Any(node => node is DirectiveWhenNode or DirectiveOnNode)
                    ? "MotionClip cannot contain activation directives such as @when or @on."
                    : "MotionClip requires exactly one top-level execution body.";
                Report(InvalidDirective, resource, Path.GetFileName(file.Path), forbidden);
                return;
            }

            if (ContainsMotionRun(body))
            {
                Report(
                    InvalidDirective,
                    body.Source,
                    Path.GetFileName(file.Path),
                    "MotionClip cannot contain @run; recursive clip invocation is not allowed.");
                return;
            }

            if (ContainsMotionCancel(body))
            {
                Report(
                    InvalidDirective,
                    body.Source,
                    Path.GetFileName(file.Path),
                    "MotionClip cannot contain @cancel; handles belong to an Aspect session.");
                return;
            }

            MotionClipResource clip = new(name, targetName, parameters, body, resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.MotionClip, clip));
        }

        private bool TryValidateMotionParameter(MotionParameterNode parameter)
        {
            string typeName = NormalizeMotionParameterType(parameter.TypeName);
            if (IsCSharpMotionSpecParameterType(typeName, out string csharpValueType))
            {
                Report(
                    InvalidDirective,
                    parameter.Location,
                    Path.GetFileName(file.Path),
                    "MotionClip parameter '" + parameter.Name + "' uses C# generic syntax; use 'MotionSpec[" + csharpValueType + "]' in XML markup.");
                return false;
            }

            if (!IsMotionValueParameterType(typeName) && !IsMotionSpecParameterType(typeName, out _))
            {
                Report(
                    InvalidDirective,
                    parameter.Location,
                    Path.GetFileName(file.Path),
                    "MotionClip parameter '" + parameter.Name + "' has unsupported type '" + parameter.TypeName + "'.");
                return false;
            }

            if (parameter.DefaultValue is null)
            {
                return true;
            }

            return TryCreateMotionParameterValue(parameter, parameter.DefaultValue, parameter.Location, out _);
        }

        private static string NormalizeMotionParameterType(string typeName)
        {
            return typeName.Replace("global::", string.Empty).Replace(" ", string.Empty);
        }

        private static bool IsMotionValueParameterType(string typeName)
        {
            return typeName is "float" or "System.Single" or "double" or "System.Double" or
                "int" or "System.Int32" or "bool" or "System.Boolean" or "string" or "System.String";
        }

        private static bool IsMotionSpecParameterType(string typeName, out string valueType)
        {
            typeName = NormalizeMotionParameterType(typeName);
            const string prefix = "Cerneala.UI.Motion.Specs.MotionSpec[";
            const string shortPrefix = "MotionSpec[";
            string? argument = typeName.StartsWith(prefix, StringComparison.Ordinal) && typeName.EndsWith("]", StringComparison.Ordinal)
                ? typeName.Substring(prefix.Length, typeName.Length - prefix.Length - 1)
                : typeName.StartsWith(shortPrefix, StringComparison.Ordinal) && typeName.EndsWith("]", StringComparison.Ordinal)
                    ? typeName.Substring(shortPrefix.Length, typeName.Length - shortPrefix.Length - 1)
                    : null;
            return TryNormalizeMotionSpecValueType(argument, out valueType);
        }

        private static bool IsCSharpMotionSpecParameterType(string typeName, out string valueType)
        {
            typeName = NormalizeMotionParameterType(typeName);
            const string prefix = "Cerneala.UI.Motion.Specs.MotionSpec<";
            const string shortPrefix = "MotionSpec<";
            string? argument = typeName.StartsWith(prefix, StringComparison.Ordinal) && typeName.EndsWith(">", StringComparison.Ordinal)
                ? typeName.Substring(prefix.Length, typeName.Length - prefix.Length - 1)
                : typeName.StartsWith(shortPrefix, StringComparison.Ordinal) && typeName.EndsWith(">", StringComparison.Ordinal)
                    ? typeName.Substring(shortPrefix.Length, typeName.Length - shortPrefix.Length - 1)
                    : null;
            return TryNormalizeMotionSpecValueType(argument, out valueType);
        }

        private static bool TryNormalizeMotionSpecValueType(string? argument, out string valueType)
        {
            valueType = argument switch
            {
                "float" or "System.Single" => "float",
                "double" or "System.Double" => "double",
                _ => string.Empty
            };
            return valueType.Length > 0;
        }

        private static bool ContainsMotionRun(MotionExecutionNode execution)
        {
            return execution is MotionRunNode ||
                execution is MotionCompositionNode composition && composition.Children.Any(ContainsMotionRun);
        }

        private static bool ContainsMotionCancel(MotionExecutionNode execution)
        {
            return execution is MotionCancelNode ||
                execution is MotionCompositionNode composition && composition.Children.Any(ContainsMotionCancel);
        }

        private bool ResolveMotionAspect(MarkupElement applicationElement, string applicationVariable, AspectResource aspect)
        {
            if (resolvedMotionAspects.ContainsKey((aspect, applicationElement)))
            {
                return true;
            }

            INamedTypeSymbol? targetType = ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source);
            INamedTypeSymbol? applicationType = ResolvePropertyOwnerType(
                applicationElement.Name.LocalName,
                ReferenceEquals(applicationElement, document.Root));
            if (targetType is null)
            {
                ReportMotion(MotionDiagnosticKind.Target, aspect.Source, "Motion TargetType '" + aspect.TargetName + "' could not be resolved.");
                return false;
            }

            if (applicationType is null || !IsOrDerivesFrom(applicationType, targetType))
            {
                ReportMotion(
                    MotionDiagnosticKind.Target,
                    (object?)applicationElement.Attribute("Aspect") ?? applicationElement,
                    "Aspect TargetType '" + aspect.TargetName + "' is not assignable from '" + applicationElement.Name.LocalName + "'.");
                return false;
            }

            if (aspect.Presence is MotionPresenceNode presence)
            {
                ITypeSymbol floatType = compilation.GetSpecialType(SpecialType.System_Single);
                if (!TryResolveConcreteMotionSpec(presence.Enter, floatType, "Presence enter", out string? enterSpec) ||
                    !TryResolveConcreteMotionSpec(presence.Exit, floatType, "Presence exit", out string? exitSpec))
                {
                    return false;
                }

                resolvedMotionPresences[(aspect, applicationElement)] = new ResolvedMotionPresence(
                    enterSpec!,
                    exitSpec!,
                    presence.ExcludeInputWhileExiting);
            }

            if (aspect.Layout is MotionLayoutNode layout)
            {
                INamedTypeSymbol? transformType = compilation.GetTypeByMetadataName("Cerneala.UI.Media.Transform");
                if (transformType is null ||
                    !TryResolveLayoutId(applicationElement, applicationVariable, layout.Id, out string? idExpression) ||
                    !TryResolveConcreteMotionSpec(layout.Spec, transformType, "Layout correction", out string? correctionSpec))
                {
                    return false;
                }

                resolvedMotionLayouts[(aspect, applicationElement)] = new ResolvedMotionLayout(idExpression!, correctionSpec!);
            }

            List<ResolvedMotionScroll> scrolls = [];
            foreach (MotionScrollNode scroll in aspect.Scrolls)
            {
                if (!TryResolveMotionScroll(applicationElement, aspect, scroll, out ResolvedMotionScroll? resolvedScroll))
                {
                    return false;
                }

                scrolls.Add(resolvedScroll!);
            }

            if (scrolls.Count > 0)
            {
                resolvedMotionScrolls[(aspect, applicationElement)] = scrolls;
            }

            if (aspect.Drag is MotionDragNode drag)
            {
                if (drag.Spec is MotionResourceSpecSyntax resourceSyntax &&
                    TryResolveResource(resourceSyntax.Location.Source, resourceSyntax.Name, out NamedSymbol dragSpecSymbol) &&
                    dragSpecSymbol.Source is MotionSpecResource { Kind: "Decay" })
                {
                    Report(InvalidDirective, drag.Spec.Location, Path.GetFileName(file.Path), "@drag does not support a Decay release spec.");
                    return false;
                }

                ITypeSymbol floatType = compilation.GetSpecialType(SpecialType.System_Single);
                if (!TryResolveConcreteMotionSpec(drag.Spec, floatType, "Drag release", out string? releaseSpec))
                {
                    return false;
                }

                resolvedMotionDrags[(aspect, applicationElement)] = new ResolvedMotionDrag(releaseSpec!);
            }

            if (aspect.GesturePress is MotionGesturePressNode gesturePress)
            {
                ITypeSymbol floatType = compilation.GetSpecialType(SpecialType.System_Single);
                if (!TryResolveConcreteMotionSpec(gesturePress.Spec, floatType, "Gesture press", out string? gestureSpec))
                {
                    return false;
                }

                resolvedMotionGesturePresses[(aspect, applicationElement)] = new ResolvedMotionGesturePress(gestureSpec!);
            }

            List<MotionExecutionNode> syntaxExecutions = [];
            foreach (DirectiveWhenNode condition in aspect.Conditions)
            {
                CollectMotionExecutionRoots(condition, syntaxExecutions);
            }

            foreach (DirectiveOnNode trigger in aspect.EventTriggers)
            {
                syntaxExecutions.AddRange(trigger.Body);
            }

            // @cancel of a Sound handle is a Sound action, not a Motion command.
            syntaxExecutions.RemoveAll(IsSoundNode);

            List<ResolvedMotionAnimation> animations = [];
            List<ResolvedMotionSet> sets = [];
            List<ResolvedMotionComposition> compositions = [];
            List<ResolvedMotionCancelCommand> cancelCommands = [];
            foreach (MotionExecutionNode execution in syntaxExecutions)
            {
                if (!TryResolveMotionExecution(applicationElement, aspect, execution, animations, sets, compositions, cancelCommands))
                {
                    return false;
                }
            }

            List<ResolvedMotionEventTrigger> eventTriggers = [];
            List<(DirectiveOnNode Trigger, IEventSymbol Event)> soundTriggers = [];
            foreach (DirectiveOnNode trigger in aspect.EventTriggers)
            {
                IEventSymbol? eventSymbol = FindMotionEvent(targetType, trigger.EventName);
                if (eventSymbol is null || !IsAccessibleFromGeneratedCode(eventSymbol))
                {
                    IEventSymbol? concreteEvent = applicationType is null
                        ? null
                        : FindMotionEvent(applicationType, trigger.EventName);
                    string suggestion = concreteEvent is not null && IsAccessibleFromGeneratedCode(concreteEvent)
                        ? " The event exists on concrete type '" + applicationType!.ToDisplayString() +
                            "'; use TargetType=\"" + applicationType.ToDisplayString() + "\"."
                        : string.Empty;
                    ReportMotion(
                        MotionDiagnosticKind.Event,
                        trigger.Location,
                        "Motion event '" + trigger.EventName + "' was not found or is not accessible on TargetType '" +
                        aspect.TargetName + "'." + suggestion);
                    return false;
                }

                // A body with Sound actions or audio Motion is one handler owned
                // by the Sound session, so its actions keep source order and
                // survive hiding.
                if (trigger.Actions.Any(IsSoundOwnedNode))
                {
                    soundTriggers.Add((trigger, eventSymbol));
                    continue;
                }

                string[] executionNames = trigger.Body
                    .Select(execution => GetMotionExecutionName(execution))
                    .ToArray();
                eventTriggers.Add(new ResolvedMotionEventTrigger(eventSymbol, executionNames));
            }

            resolvedMotionAspects.Add(
                (aspect, applicationElement),
                new ResolvedMotionAspect(animations, sets, compositions, cancelCommands, eventTriggers));
            resolvedSoundTriggers[(aspect, applicationElement)] = soundTriggers;
            return true;
        }

        private bool TryResolveMotionScroll(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionScrollNode scroll,
            out ResolvedMotionScroll? resolved)
        {
            string sourceName = scroll.SourceReference.Substring(1);
            MarkupElement? sourceElement = FindMotionNamedElement(applicationElement, sourceName);

            INamedTypeSymbol? sourceType = sourceElement is null
                ? null
                : ResolvePropertyOwnerType(sourceElement.Name.LocalName, ReferenceEquals(sourceElement, document.Root));
            INamedTypeSymbol? scrollViewerType = compilation.GetTypeByMetadataName("Cerneala.UI.Controls.ScrollViewer");
            if (sourceElement is null || sourceType is null || scrollViewerType is null || !IsOrDerivesFrom(sourceType, scrollViewerType))
            {
                Report(InvalidDirective, scroll.Source, Path.GetFileName(file.Path), "@scroll source '" + scroll.SourceReference + "' must resolve to an attached named ScrollViewer.");
                resolved = null;
                return false;
            }

            List<ResolvedMotionScrollProperty> properties = [];
            foreach (MotionScrollAssignmentSyntax assignment in scroll.Assignments)
            {
                MotionAssignmentSyntax target = new(
                    assignment.Target,
                    new MotionAtomValueSyntax("0", assignment.Location),
                    null,
                    assignment.Location);
                if (!TryResolveMotionTarget(applicationElement, aspect, target, out ResolvedMotionTarget? resolvedTarget, out PropertySpec? property))
                {
                    resolved = null;
                    return false;
                }

                if (resolvedTarget!.Prism is not null ||
                    !property!.Assignable ||
                    property.ValueType.SpecialType != SpecialType.System_Single)
                {
                    Report(InvalidDirective, assignment.Location, Path.GetFileName(file.Path), "@scroll target '" + assignment.Target + "' must be an assignable float UiProperty.");
                    resolved = null;
                    return false;
                }

                properties.Add(new ResolvedMotionScrollProperty(assignment, resolvedTarget!, property));
            }

            resolved = new ResolvedMotionScroll(scroll, sourceElement, properties);
            return true;
        }

        private static IEventSymbol? FindMotionEvent(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                ISymbol[] members = current.GetMembers(name).ToArray();
                if (members.Length > 0)
                {
                    return members.OfType<IEventSymbol>().FirstOrDefault();
                }
            }

            return null;
        }

        private static void CollectMotionExecutionRoots(DirectiveWhenNode condition, ICollection<MotionExecutionNode> executions)
        {
            if (condition.BooleanBody is not null)
            {
                CollectMotionExecutionRoots(condition.BooleanBody, executions);
            }

            foreach (DirectiveIfNode branch in condition.Branches)
            {
                CollectMotionExecutionRoots(branch.Body, executions);
            }
        }

        private static void CollectMotionExecutionRoots(IReadOnlyList<DirectiveNode> nodes, ICollection<MotionExecutionNode> executions)
        {
            foreach (MotionExecutionNode execution in nodes.OfType<MotionExecutionNode>())
            {
                executions.Add(execution);
            }

            foreach (DirectiveWhenNode nested in nodes.OfType<DirectiveWhenNode>())
            {
                CollectMotionExecutionRoots(nested, executions);
            }
        }

        private bool TryResolveMotionExecution(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionExecutionNode execution,
            ICollection<ResolvedMotionAnimation> animations,
            ICollection<ResolvedMotionSet> sets,
            ICollection<ResolvedMotionComposition> compositions,
            ICollection<ResolvedMotionCancelCommand> cancelCommands,
            MotionClipInvocationContext? parameters = null)
        {
            if (execution is MotionAnimateNode animation)
            {
                if (!TryResolveMotionAnimation(applicationElement, aspect, animation, parameters, out ResolvedMotionAnimation? resolved))
                {
                    return false;
                }

                animations.Add(resolved!);
                return true;
            }

            if (execution is MotionSetNode set)
            {
                List<ResolvedMotionSetProperty> properties = [];
                foreach (MotionAssignmentSyntax assignment in set.Assignments)
                {
                    if (!TryResolveMotionTarget(applicationElement, aspect, assignment, out ResolvedMotionTarget? target, out PropertySpec? property))
                    {
                        return false;
                    }

                    if (!property!.Assignable)
                    {
                        ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "Motion property '" + assignment.Target + "' is not assignable.");
                        return false;
                    }

                    if (!ValidateMotionValue(assignment.Value, property, assignment.Target, parameters, target!, allowExplicitBinding: false))
                    {
                        return false;
                    }

                    properties.Add(new ResolvedMotionSetProperty(assignment, target!, property));
                }

                (string setExecutionName, string setFactoryName) = CreateMotionExecutionNames();
                motionExecutionFactoryNames[set] = setFactoryName;
                sets.Add(new ResolvedMotionSet(properties, setExecutionName, setFactoryName, parameters));
                return true;
            }

            if (execution is MotionKeyframesNode keyframes)
            {
                return TryResolveMotionKeyframes(
                    applicationElement,
                    aspect,
                    keyframes,
                    animations,
                    parameters);
            }

            if (execution is MotionStaggerNode stagger)
            {
                if (!TryResolveMotionStagger(applicationElement, aspect, stagger, parameters, out ResolvedMotionAnimation? resolved))
                {
                    return false;
                }

                animations.Add(resolved!);
                return true;
            }

            if (execution is MotionRunNode run)
            {
                if (!TryResolveResource(run.Source, run.ClipName, out NamedSymbol symbol) ||
                    symbol.Source is not MotionClipResource clip)
                {
                    Report(InvalidDirective, run.Source, Path.GetFileName(file.Path), "Unknown MotionClip resource '$" + run.ClipName + "'.");
                    return false;
                }

                INamedTypeSymbol? applicationType = ResolvePropertyOwnerType(
                    applicationElement.Name.LocalName,
                    ReferenceEquals(applicationElement, document.Root));
                INamedTypeSymbol? clipTargetType = ResolveAspectTargetTypeSymbol(clip.TargetName, clip.Source);
                if (applicationType is null || clipTargetType is null || !IsOrDerivesFrom(applicationType, clipTargetType))
                {
                    Report(
                        InvalidDirective,
                        run.Source,
                        Path.GetFileName(file.Path),
                        "MotionClip '$" + run.ClipName + "' TargetType '" + clip.TargetName +
                        "' is not assignable from '" + applicationElement.Name.LocalName + "'.");
                    return false;
                }

                if (!TryResolveMotionClipArguments(run, clip, out MotionClipInvocationContext? invocation) ||
                    !TryResolveMotionExecution(applicationElement, aspect, clip.Body, animations, sets, compositions, cancelCommands, invocation))
                {
                    return false;
                }

                string clipFactoryName = motionExecutionFactoryNames[clip.Body];
                if (run.HandleName is null)
                {
                    motionExecutionFactoryNames[run] = clipFactoryName;
                }
                else
                {
                    (string runExecutionName, string runFactoryName) = CreateMotionExecutionNames();
                    motionExecutionFactoryNames[run] = runFactoryName;
                    compositions.Add(new ResolvedMotionComposition(
                        run.HandleName,
                        clipFactoryName,
                        runExecutionName,
                        runFactoryName));
                }

                return true;
            }

            if (execution is MotionCancelNode cancel)
            {
                string actionName = CreateMotionActionName();
                motionActionNames[cancel] = actionName;
                cancelCommands.Add(new ResolvedMotionCancelCommand(actionName, cancel.HandleName));
                return true;
            }

            MotionCompositionNode composition = (MotionCompositionNode)execution;
            foreach (MotionExecutionNode child in composition.Children)
            {
                if (!TryResolveMotionExecution(applicationElement, aspect, child, animations, sets, compositions, cancelCommands, parameters))
                {
                    return false;
                }
            }

            (string executionName, string factoryName) = CreateMotionExecutionNames();
            motionExecutionFactoryNames[composition] = factoryName;
            compositions.Add(new ResolvedMotionComposition(
                composition,
                composition.Children.Select(child => motionExecutionFactoryNames[child]).ToArray(),
                executionName,
                factoryName));
            return true;
        }

        private bool TryResolveMotionStagger(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionStaggerNode stagger,
            MotionClipInvocationContext? parameters,
            out ResolvedMotionAnimation? resolved)
        {
            resolved = null;
            MarkupElement? collectionElement = FindMotionNamedElement(applicationElement, stagger.TargetName);

            if (collectionElement is null)
            {
                Report(InvalidDirective, stagger.Source, Path.GetFileName(file.Path), "Stagger target named element '" + stagger.TargetName + "' is not available at this application site.");
                return false;
            }

            MotionAnimateNode animation = stagger.Animation;
            if (!ValidateMotionOptions(animation.Options, parameters) || !IsTweenMotionSpec(animation.DefaultSpec))
            {
                Report(InvalidDirective, animation.Source, Path.GetFileName(file.Path), "@stagger supports exactly one Tween-based @animate.");
                return false;
            }

            INamedTypeSymbol? itemType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            if (itemType is null)
            {
                Report(InvalidDirective, stagger.Source, Path.GetFileName(file.Path), "Stagger item type UIElement could not be resolved.");
                return false;
            }

            Dictionary<string, MotionAssignmentSyntax> sources = animation.From
                .GroupBy(item => item.Target, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            List<ResolvedMotionProperty> properties = [];
            foreach (MotionAssignmentSyntax destination in animation.To)
            {
                if (destination.Target.StartsWith("$", StringComparison.Ordinal) || destination.Spec is not null && !IsTweenMotionSpec(destination.Spec))
                {
                    Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Stagger assignments must target the current item and use Tween specs.");
                    return false;
                }

                PropertySpec? property = FindPropertySpec(itemType, destination.Target);
                if (property is null || !property.Assignable || !HasBuiltInMixer(property.ValueType))
                {
                    Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Stagger property '" + destination.Target + "' is not animatable on UIElement.");
                    return false;
                }

                MotionAssignmentSyntax? source = sources.TryGetValue(destination.Target, out MotionAssignmentSyntax? matched) ? matched : null;
                ResolvedMotionTarget target = new(ResolvedMotionTargetKind.Self, collectionElement);
                if (!ValidateMotionValue(destination.Value, property, destination.Target, parameters, target, allowExplicitBinding: true) ||
                    source is not null && !ValidateMotionValue(source.Value, property, source.Target, parameters, target, allowExplicitBinding: false))
                {
                    return false;
                }

                MotionSpecSyntax spec = destination.Spec ?? animation.DefaultSpec!;
                if (!TryResolveMotionSpec(applicationElement, spec, property, destination.Target, parameters, out string? specVariable))
                {
                    return false;
                }

                properties.Add(new ResolvedMotionProperty(destination, source, target, property, specVariable));
            }

            (string executionName, string factoryName) = CreateMotionExecutionNames();
            motionExecutionFactoryNames[stagger] = factoryName;
            resolved = new ResolvedMotionAnimation(animation, properties, executionName, factoryName, parameters, stagger, collectionElement);
            return true;
        }

        private bool IsTweenMotionSpec(MotionSpecSyntax? syntax)
        {
            if (syntax is MotionInlineSpecSyntax inline)
            {
                return inline.Kind == "Tween";
            }

            return syntax is MotionResourceSpecSyntax resourceReference &&
                TryResolveResource(resourceReference.Location.Source, resourceReference.Name, out NamedSymbol symbol) &&
                symbol.Source is MotionSpecResource { Kind: "Tween" };
        }

        private bool TryResolveMotionKeyframes(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionKeyframesNode timeline,
            ICollection<ResolvedMotionAnimation> animations,
            MotionClipInvocationContext? parameters)
        {
            List<(MotionKeyframeSegmentSyntax Segment, MotionAssignmentSyntax Source, MotionAssignmentSyntax Destination)> assignments = [];
            foreach (MotionKeyframeSegmentSyntax segment in timeline.Segments)
            {
                MotionAnimateNode animation = segment.Animation;
                if (animation.Options.Count > 0)
                {
                    Report(InvalidDirective, animation.Options[0].Location, Path.GetFileName(file.Path), "Ranged @animate options are not supported; keyframe persistence belongs to the timeline execution.");
                    return false;
                }

                if (animation.From.Count == 0)
                {
                    Report(InvalidDirective, animation.Source, Path.GetFileName(file.Path), "Ranged @animate requires an explicit @from block.");
                    return false;
                }

                Dictionary<string, MotionAssignmentSyntax> sources = animation.From
                    .GroupBy(item => item.Target, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                if (sources.Count != animation.From.Count || animation.To.GroupBy(item => item.Target, StringComparer.Ordinal).Any(group => group.Count() > 1))
                {
                    Report(InvalidDirective, animation.Source, Path.GetFileName(file.Path), "Ranged @animate contains a duplicate target.");
                    return false;
                }

                foreach (MotionAssignmentSyntax destination in animation.To)
                {
                    if (!sources.TryGetValue(destination.Target, out MotionAssignmentSyntax? source))
                    {
                        Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Every ranged @animate target requires matching @from and @to assignments.");
                        return false;
                    }

                    if (destination.Spec is not null)
                    {
                        Report(InvalidDirective, destination.Spec.Location, Path.GetFileName(file.Path), "Keyframe easing belongs on the ranged @animate header, not a property assignment.");
                        return false;
                    }

                    assignments.Add((segment, source, destination));
                }
            }

            List<ResolvedMotionProperty> properties = [];
            foreach (IGrouping<string, (MotionKeyframeSegmentSyntax Segment, MotionAssignmentSyntax Source, MotionAssignmentSyntax Destination)> group in
                assignments.GroupBy(item => item.Destination.Target, StringComparer.Ordinal))
            {
                var ordered = group.OrderBy(item => item.Segment.Start).ThenBy(item => item.Segment.End).ToArray();
                for (int index = 1; index < ordered.Length; index++)
                {
                    if (ordered[index].Segment.Start < ordered[index - 1].Segment.End)
                    {
                        Report(InvalidDirective, ordered[index].Destination.Location, Path.GetFileName(file.Path), "Keyframe ranges for property '" + group.Key + "' overlap.");
                        return false;
                    }
                }

                if (!TryResolveMotionTarget(applicationElement, aspect, ordered[0].Destination, out ResolvedMotionTarget? target, out PropertySpec? property) ||
                    !property!.Assignable ||
                    !(target!.Prism?.IsDiscrete == true ||
                        HasBuiltInMixer(property.ValueType)))
                {
                    if (property is not null &&
                        (!property.Assignable ||
                            !(target?.Prism?.IsDiscrete == true ||
                                HasBuiltInMixer(property.ValueType))))
                    {
                        Report(InvalidDirective, ordered[0].Destination.Location, Path.GetFileName(file.Path), "Motion property '" + group.Key + "' is not animatable.");
                    }

                    return false;
                }

                List<ResolvedMotionKeyframe> frames = [];
                MotionValueSyntax? previousValue = null;
                foreach (var item in ordered)
                {
                    if (!ValidateMotionValue(item.Source.Value, property, group.Key, parameters, target!, allowExplicitBinding: false) ||
                        !ValidateMotionValue(item.Destination.Value, property, group.Key, parameters, target!, allowExplicitBinding: false) ||
                        item.Source.Value is MotionCurrentValueSyntax || item.Destination.Value is MotionCurrentValueSyntax)
                    {
                        if (item.Source.Value is MotionCurrentValueSyntax || item.Destination.Value is MotionCurrentValueSyntax)
                        {
                            Report(InvalidDirective, item.Source.Location, Path.GetFileName(file.Path), "Keyframe values must be deterministic; 'current' is not allowed.");
                        }

                        return false;
                    }

                    if (!TryResolveKeyframeEasing(item.Segment.Animation.DefaultSpec, out string easingCode))
                    {
                        return false;
                    }

                    if (frames.Count == 0 && item.Segment.Start > 0)
                    {
                        frames.Add(new ResolvedMotionKeyframe(0, item.Source.Value, "global::Cerneala.UI.Motion.Specs.Easings.Linear"));
                    }
                    else if (frames.Count > 0 && item.Segment.Start > frames[frames.Count - 1].Offset)
                    {
                        frames.Add(new ResolvedMotionKeyframe(item.Segment.Start, previousValue!, "global::Cerneala.UI.Motion.Specs.Easings.Linear"));
                    }

                    frames.Add(new ResolvedMotionKeyframe(item.Segment.Start, item.Source.Value, easingCode, item.Segment.Hold));
                    frames.Add(new ResolvedMotionKeyframe(item.Segment.End, item.Destination.Value, "global::Cerneala.UI.Motion.Specs.Easings.Linear"));
                    previousValue = item.Destination.Value;
                }

                if (frames[frames.Count - 1].Offset < 1)
                {
                    frames.Add(new ResolvedMotionKeyframe(1, previousValue!, "global::Cerneala.UI.Motion.Specs.Easings.Linear"));
                }

                properties.Add(new ResolvedMotionProperty(
                    ordered[ordered.Length - 1].Destination,
                    ordered[0].Source,
                    target!,
                    property,
                    null,
                    new ResolvedMotionKeyframesSpec(timeline.Duration, frames)));
            }

            MotionAnimateNode synthetic = new(
                null,
                [],
                properties.Select(item => item.Source!).ToArray(),
                properties.Select(item => item.Destination).ToArray(),
                timeline.Source);
            (string executionName, string factoryName) = CreateMotionExecutionNames();
            motionExecutionFactoryNames[timeline] = factoryName;
            animations.Add(new ResolvedMotionAnimation(synthetic, properties, executionName, factoryName, parameters));
            return true;
        }

        private bool TryResolveKeyframeEasing(MotionSpecSyntax? syntax, out string easingCode)
        {
            easingCode = "global::Cerneala.UI.Motion.Specs.Easings.Linear";
            if (syntax is null)
            {
                return true;
            }

            string? easing = null;
            if (syntax is MotionParameterSpecSyntax named)
            {
                easing = named.Name;
            }
            else if (syntax is MotionResourceSpecSyntax resourceReference &&
                TryResolveResource(resourceReference.Location.Source, resourceReference.Name, out NamedSymbol symbol) &&
                symbol.Source is MotionSpecResource resource && resource.Kind == "Tween")
            {
                easingCode = resource.Arguments[1];
                return true;
            }
            else if (syntax is MotionInlineSpecSyntax inline && inline.Kind == "Tween")
            {
                easing = inline.Arguments.Count == 2 ? inline.Arguments[1].Text : "Standard";
            }
            else if (syntax is MotionInlineSpecSyntax step && step.Kind == "Step")
            {
                string position = step.Arguments.Count == 2 ? step.Arguments[1].Text : "JumpEnd";
                easingCode = "new global::Cerneala.UI.Motion.Specs.StepEasing(" + step.Arguments[0].Text +
                    ", global::Cerneala.UI.Motion.Specs.StepPosition." + position + ")";
                return true;
            }
            else
            {
                string kind = syntax is MotionInlineSpecSyntax rejected ? rejected.Kind : "resource";
                Report(InvalidDirective, syntax.Location, Path.GetFileName(file.Path), kind + " is not supported inside @keyframes; use a Tween easing.");
                return false;
            }

            if (!IsKnownEasing(easing!))
            {
                Report(InvalidDirective, syntax.Location, Path.GetFileName(file.Path), "Unknown keyframe easing '" + easing + "'.");
                return false;
            }

            easingCode = "global::Cerneala.UI.Motion.Specs.Easings." + easing;
            return true;
        }

        private bool TryResolveMotionClipArguments(
            MotionRunNode run,
            MotionClipResource clip,
            out MotionClipInvocationContext? context)
        {
            context = null;
            Dictionary<string, MotionRunArgumentSyntax> arguments = new(StringComparer.Ordinal);
            foreach (MotionRunArgumentSyntax argument in run.Arguments)
            {
                if (arguments.ContainsKey(argument.Name))
                {
                    Report(InvalidDirective, argument.Location, Path.GetFileName(file.Path), "Duplicate MotionClip argument '" + argument.Name + "'.");
                    return false;
                }

                arguments.Add(argument.Name, argument);

                if (!clip.Parameters.Any(parameter => parameter.Name == argument.Name))
                {
                    Report(InvalidDirective, argument.Location, Path.GetFileName(file.Path), "Unknown parameter '" + argument.Name + "' for MotionClip '$" + clip.Name + "'.");
                    return false;
                }
            }

            Dictionary<string, ResolvedMotionParameterValue> values = new(StringComparer.Ordinal);
            foreach (MotionParameterNode parameter in clip.Parameters)
            {
                string? rawText;
                DirectiveExpressionLocation location;
                if (arguments.TryGetValue(parameter.Name, out MotionRunArgumentSyntax? argument))
                {
                    rawText = argument.Value;
                    location = argument.Location;
                }
                else
                {
                    rawText = parameter.DefaultValue;
                    location = parameter.Location;
                }

                if (rawText is null)
                {
                    Report(InvalidDirective, run.Source, Path.GetFileName(file.Path), "MotionClip '$" + clip.Name + "' requires argument '" + parameter.Name + "'.");
                    return false;
                }

                if (!TryCreateMotionParameterValue(parameter, rawText, location, out ResolvedMotionParameterValue? value))
                {
                    return false;
                }

                values.Add(parameter.Name, value!);
            }

            context = new MotionClipInvocationContext(values);
            return true;
        }

        private bool TryCreateMotionParameterValue(
            MotionParameterNode parameter,
            string rawText,
            DirectiveExpressionLocation location,
            out ResolvedMotionParameterValue? value)
        {
            value = null;
            string typeName = NormalizeMotionParameterType(parameter.TypeName);
            string text = rawText.Trim();
            string? code = null;
            MotionSpecSyntax? spec = null;

            if (IsMotionSpecParameterType(typeName, out _))
            {
                try
                {
                    spec = DirectiveCursor.ParseMotionSpec(text, location);
                }
                catch (DirectiveParseException exception)
                {
                    Report(InvalidDirective, location, Path.GetFileName(file.Path), "MotionClip parameter '" + parameter.Name + "' requires a Motion spec: " + exception.Message);
                    return false;
                }

                if (spec is MotionParameterSpecSyntax)
                {
                    Report(InvalidDirective, location, Path.GetFileName(file.Path), "MotionClip argument '" + parameter.Name + "' cannot reference another parameter.");
                    return false;
                }
            }
            else if (typeName is "float" or "System.Single" &&
                float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue) &&
                !float.IsNaN(floatValue) && !float.IsInfinity(floatValue))
            {
                code = floatValue.ToString("R", CultureInfo.InvariantCulture) + "f";
            }
            else if (typeName is "double" or "System.Double" &&
                double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue) &&
                !double.IsNaN(doubleValue) && !double.IsInfinity(doubleValue))
            {
                code = doubleValue.ToString("R", CultureInfo.InvariantCulture) + "d";
            }
            else if (typeName is "int" or "System.Int32" &&
                int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                code = intValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (typeName is "bool" or "System.Boolean" && bool.TryParse(text, out bool boolValue))
            {
                code = boolValue ? "true" : "false";
            }
            else if (typeName is "string" or "System.String" &&
                text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"')
            {
                code = Literal(text.Substring(1, text.Length - 2));
            }
            else
            {
                Report(
                    InvalidDirective,
                    location,
                    Path.GetFileName(file.Path),
                    "MotionClip argument '" + parameter.Name + "' is not compatible with type '" + parameter.TypeName + "'.");
                return false;
            }

            value = new ResolvedMotionParameterValue(parameter, text, code, spec);
            return true;
        }

        private string GetMotionExecutionName(MotionExecutionNode execution)
        {
            if (motionActionNames.TryGetValue(execution, out string? actionName))
            {
                return actionName;
            }

            if (execution is MotionAnimateNode animation)
            {
                return motionExecutionNames[animation];
            }

            string factoryName = motionExecutionFactoryNames[execution];
            return "motionExecution" + factoryName.Substring("motionExecutionFactory".Length);
        }

        private string CreateMotionActionName()
        {
            string name = "motionExecution" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            return name;
        }

        private (string ExecutionName, string FactoryName) CreateMotionExecutionNames()
        {
            string suffix = nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            return ("motionExecution" + suffix, "motionExecutionFactory" + suffix);
        }

        private bool TryResolveMotionAnimation(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionAnimateNode animation,
            MotionClipInvocationContext? parameters,
            out ResolvedMotionAnimation? resolved)
        {
            resolved = null;
            if (!ValidateMotionOptions(animation.Options, parameters))
            {
                return false;
            }

            Dictionary<string, MotionAssignmentSyntax> destinations = animation.To
                .GroupBy(assignment => assignment.Target, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            if (destinations.Count != animation.To.Count)
            {
                MotionAssignmentSyntax duplicate = animation.To
                    .GroupBy(assignment => assignment.Target, StringComparer.Ordinal)
                    .First(group => group.Count() > 1)
                    .Skip(1)
                    .First();
                Report(InvalidDirective, duplicate.Location, Path.GetFileName(file.Path), "@to contains duplicate target '" + duplicate.Target + "'.");
                return false;
            }

            foreach (MotionAssignmentSyntax source in animation.From)
            {
                if (!destinations.ContainsKey(source.Target))
                {
                    Report(InvalidDirective, source.Location, Path.GetFileName(file.Path), "Property '" + source.Target + "' from @from must also appear in @to.");
                    return false;
                }
            }

            List<ResolvedMotionProperty> properties = [];
            foreach (MotionAssignmentSyntax destination in animation.To)
            {
                MotionAssignmentSyntax? source = animation.From.FirstOrDefault(candidate => candidate.Target == destination.Target);
                if (!TryResolveMotionTarget(applicationElement, aspect, destination, out ResolvedMotionTarget? target, out PropertySpec? property))
                {
                    return false;
                }

                if (!property!.Assignable)
                {
                    Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Motion property '" + destination.Target + "' is read-only or inaccessible.");
                    return false;
                }

                if (!(target!.Prism?.IsDiscrete == true ||
                    HasBuiltInMixer(property.ValueType)))
                {
                    Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Motion property '" + destination.Target + "' has no compatible mixer.");
                    return false;
                }

                if (!ValidateMotionValue(destination.Value, property, destination.Target, parameters, target!, allowExplicitBinding: true) ||
                    (source is not null && !ValidateMotionValue(source.Value, property, source.Target, parameters, target!, allowExplicitBinding: false)))
                {
                    return false;
                }

                MotionSpecSyntax? spec = destination.Spec ?? animation.DefaultSpec;
                string? specVariable = null;
                if ((target.Prism is null && target.Sound is null || spec is not null) &&
                    !TryResolveMotionSpec(
                        applicationElement,
                        spec,
                        property,
                        destination.Target,
                        parameters,
                        out specVariable))
                {
                    return false;
                }

                properties.Add(new ResolvedMotionProperty(destination, source, target!, property, specVariable));
            }

            (string executionName, string factoryName) = CreateMotionExecutionNames();
            motionExecutionNames[animation] = executionName;
            motionExecutionFactoryNames[animation] = factoryName;
            resolved = new ResolvedMotionAnimation(animation, properties, executionName, factoryName, parameters);
            return true;
        }

        private bool TryResolveLayoutId(
            MarkupElement element,
            string variable,
            DirectiveSourceExpression source,
            out string? expression)
        {
            BindingResolutionContext context = new(
                variable,
                element.Name.LocalName,
                ReferenceEquals(element, document.Root),
                templateEmissionContexts.Count == 0 ? null : templateEmissionContexts.Peek());
            BindingSourceDescriptor? descriptor = ResolveBindingSource(context, source.Text, source.Location.Source, source.Location);
            expression = null;
            if (descriptor is null)
            {
                return false;
            }

            string valueType = descriptor.ValueType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            string valueCode;
            if (descriptor.Kind == BindingSourceKind.DataPath)
            {
                valueCode = "((" + DataTypeCode + ")dataContext!)";
                foreach (DataPathSegmentDescriptor segment in descriptor.DataSegments)
                {
                    valueCode += "." + segment.Property.Name;
                }
            }
            else if (descriptor.Kind is BindingSourceKind.UiProperty or BindingSourceKind.TemplatePartProperty)
            {
                valueCode = descriptor.OwnerCode + ".GetValue(" + descriptor.Property!.PropertyCode + ")";
            }
            else
            {
                Report(InvalidBindingSource, source.Location, source.Text, "@layout id requires a String or LayoutMotionId reactive property source.");
                return false;
            }

            if (valueType == "string")
            {
                expression = "new global::Cerneala.UI.Motion.Layout.LayoutMotionId(" + valueCode +
                    " ?? throw new global::System.InvalidOperationException(\"@layout id cannot be null.\"))";
                return true;
            }

            if (valueType == "Cerneala.UI.Motion.Layout.LayoutMotionId")
            {
                expression = valueCode;
                return true;
            }

            Report(InvalidBindingSource, source.Location, source.Text, "@layout id must resolve to String or LayoutMotionId.");
            return false;
        }

        private bool TryResolveMotionTarget(
            MarkupElement applicationElement,
            AspectResource aspect,
            MotionAssignmentSyntax assignment,
            out ResolvedMotionTarget? target,
            out PropertySpec? property)
        {
            if (assignment.Target.IndexOf(
                    ".prism.",
                    StringComparison.Ordinal) >= 0)
            {
                return TryResolvePrismMotionTarget(
                    applicationElement,
                    assignment,
                    out target,
                    out property);
            }

            if (IsSoundMotionTarget(assignment.Target))
            {
                return TryResolveSoundMotionTarget(applicationElement, aspect, assignment, out target, out property);
            }

            target = null;
            string propertyName = assignment.Target;
            INamedTypeSymbol? targetType = ResolvePropertyOwnerType(
                applicationElement.Name.LocalName,
                ReferenceEquals(applicationElement, document.Root));
            ResolvedMotionTargetKind targetKind = ResolvedMotionTargetKind.Self;
            MarkupElement targetElement = applicationElement;
            string? ownerName = null;
            string? partName = null;

            if (assignment.Target.StartsWith("$", StringComparison.Ordinal))
            {
                string[] parts = assignment.Target.Split('.');
                ownerName = parts[0].Substring(1);
                bool targetsPart = parts.Length == 4;
                propertyName = targetsPart ? parts[3] : parts[1];

                if (ownerName == "self")
                {
                    targetKind = targetsPart ? ResolvedMotionTargetKind.SelfPart : ResolvedMotionTargetKind.Self;
                }
                else if (ownerName == "owner")
                {
                    if (templateEmissionContexts.Count == 0)
                    {
                        ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "Motion target '$owner' is available only inside a component template.");
                        property = null;
                        return false;
                    }

                    TemplateEmissionContext templateContext = templateEmissionContexts.Peek();
                    targetKind = targetsPart ? ResolvedMotionTargetKind.OwnerPart : ResolvedMotionTargetKind.Owner;
                    targetType = templateContext.OwnerType;
                }
                else
                {
                    MarkupElement? namedElement = FindMotionNamedElement(applicationElement, ownerName);
                    if (namedElement is null)
                    {
                        ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "Motion target named element '" + ownerName + "' is not available at this Aspect application site.");
                        property = null;
                        return false;
                    }

                    targetKind = targetsPart ? ResolvedMotionTargetKind.NamedPart : ResolvedMotionTargetKind.Named;
                    targetElement = namedElement;
                    targetType = ResolvePropertyOwnerType(namedElement.Name.LocalName, ReferenceEquals(namedElement, document.Root));
                }

                if (targetsPart)
                {
                    if (!IsControlType(targetType))
                    {
                        ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "Motion template parts can be targeted only through a Control.");
                        property = null;
                        return false;
                    }

                    partName = parts[2].Substring(1);
                    MarkupElement? templateRoot = targetKind switch
                    {
                        ResolvedMotionTargetKind.SelfPart => ResolveMotionTemplate(applicationElement, aspect)?.Root,
                        ResolvedMotionTargetKind.OwnerPart => applicationElement.AncestorsAndSelf().LastOrDefault(),
                        _ => ResolveMotionTemplate(targetElement, null)?.Root
                    };
                    MarkupElement[] matchingParts = templateRoot?.DescendantsAndSelf()
                        .Where(element => string.Equals(element.Attribute("Name")?.Value?.Trim(), partName, StringComparison.Ordinal))
                        .ToArray() ?? [];
                    if (matchingParts.Length != 1)
                    {
                        ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "The Motion target control template has no unique part named '" + partName + "'.");
                        property = null;
                        return false;
                    }

                    targetElement = matchingParts[0];
                    targetType = ResolveElementTypeSymbol(targetElement.Name.LocalName);
                }
            }

            if (targetType is null)
            {
                ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "Motion target type could not be resolved.");
                property = null;
                return false;
            }

            property = FindPropertySpec(targetType, propertyName);
            if (property is null)
            {
                ReportMotion(MotionDiagnosticKind.Target, assignment.Location, "Motion property '" + assignment.Target + "' does not exist on target type '" + targetType.Name + "'.");
                return false;
            }

            target = new ResolvedMotionTarget(targetKind, targetElement, ownerName, partName);
            return true;
        }

        private DirectiveTemplateNode? ResolveMotionTemplate(MarkupElement control, AspectResource? preferredAspect)
        {
            DirectiveParseResult content = GetDirectiveContent(
                control,
                DirectiveContentKind.Elements | DirectiveContentKind.Templates);
            return content.Nodes.OfType<DirectiveTemplateNode>().SingleOrDefault() ??
                preferredAspect?.Template ??
                ResolveAspects(control).Select(candidate => candidate.Template).LastOrDefault(candidate => candidate is not null);
        }

        private MarkupElement? FindMotionNamedElement(MarkupElement applicationElement, string name)
        {
            return applicationElement.DescendantsAndSelf()
                .FirstOrDefault(element =>
                    string.Equals(element.Attribute("Name")?.Value?.Trim(), name, StringComparison.Ordinal) &&
                    !IsResourceElement(element) &&
                    !IsTemplatePartElement(element));
        }

        private bool ValidateMotionOptions(
            IReadOnlyList<MotionOptionSyntax> options,
            MotionClipInvocationContext? parameters)
        {
            HashSet<string> seen = new(StringComparer.Ordinal);
            foreach (MotionOptionSyntax option in options)
            {
                if (!seen.Add(option.Name))
                {
                    Report(InvalidDirective, option.Location, Path.GetFileName(file.Path), "Duplicate Motion option '" + option.Name + "'.");
                    return false;
                }

                if (option.Value is not MotionAtomValueSyntax atom)
                {
                    Report(InvalidDirective, option.Location, Path.GetFileName(file.Path), "Motion option '" + option.Name + "' requires a literal value.");
                    return false;
                }

                string text = atom.Text.Trim();
                ResolvedMotionParameterValue? parameter = null;
                parameters?.Values.TryGetValue(text, out parameter);
                string parameterType = parameter is null
                    ? string.Empty
                    : NormalizeMotionParameterType(parameter.Parameter.TypeName);
                bool valid = option.Name switch
                {
                    "retarget" => parameter is null
                        ? text is "Restart" or "PreserveProgress"
                        : parameterType is "string" or "System.String" && parameter.RawText.Trim('"') is "Restart" or "PreserveProgress",
                    "holdOnComplete" => parameter is null
                        ? bool.TryParse(text, out _)
                        : parameterType is "bool" or "System.Boolean",
                    "debugName" => parameter is null
                        ? text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"'
                        : parameterType is "string" or "System.String",
                    _ => false
                };
                if (!valid)
                {
                    Report(
                        InvalidDirective,
                        option.Location,
                        Path.GetFileName(file.Path),
                        "Unsupported or invalid Motion option '" + option.Name + "'. Supported options are retarget, holdOnComplete and debugName.");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateMotionValue(
            MotionValueSyntax value,
            PropertySpec property,
            string target,
            MotionClipInvocationContext? parameters,
            ResolvedMotionTarget resolvedTarget,
            bool allowExplicitBinding)
        {
            if (value is MotionCurrentValueSyntax)
            {
                return true;
            }

            if (value is MotionConditionalValueSyntax conditional)
            {
                return ValidateMotionValue(conditional.WhenTrue, property, target, parameters, resolvedTarget, allowExplicitBinding) &&
                    ValidateMotionValue(conditional.WhenFalse, property, target, parameters, resolvedTarget, allowExplicitBinding);
            }

            string text = ((MotionAtomValueSyntax)value).Text.Trim();
            if (parameters is not null && parameters.Values.TryGetValue(text, out ResolvedMotionParameterValue? parameter))
            {
                bool compatible = IsMotionParameterCompatible(parameter.Parameter.TypeName, property.ValueType);
                if (!compatible)
                {
                    Report(InvalidDirective, value.Location, Path.GetFileName(file.Path), "MotionClip parameter '" + text + "' is not compatible with property type '" + property.ValueType.ToDisplayString() + "'.");
                }

                return compatible;
            }

            if (text.StartsWith("$", StringComparison.Ordinal) && LooksLikeBindingPath(text))
            {
                ParsedMarkupValue? parsed = ParseMarkupBindingValue(
                    text,
                    assignment: true,
                    stringTarget: false,
                    value.Location);
                if (parsed is null || parsed.Kind == ParsedMarkupValueKind.Invalid)
                {
                    return false;
                }

                MarkupBindingToken token = parsed.Binding!;
                if (token.ModeOffset >= 0 && !allowExplicitBinding)
                {
                    Report(
                        InvalidBindingSource,
                        value.Location,
                        text,
                        "An explicit Motion binding is allowed only as an @animate destination. @from, @set and keyframe values are snapshots.");
                    return false;
                }

                BindingResolutionContext context = new(
                    "motionTarget",
                    resolvedTarget.Element.Name.LocalName,
                    ReferenceEquals(resolvedTarget.Element, document.Root),
                    templateEmissionContexts.Count == 0 ? null : templateEmissionContexts.Peek(),
                    validateClrObservability: token.ModeOffset >= 0);
                return ResolveMarkupValue(
                    context,
                    property,
                    parsed,
                    value.Location.Source,
                    value.Location) is not null;
            }

            bool valid = property.ValueKind switch
            {
                MarkupValueKind.Float or MarkupValueKind.NonNegativeFloat or MarkupValueKind.PositiveFloat =>
                    float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                MarkupValueKind.Double => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                MarkupValueKind.Integer => IsMotionIntegerLiteral(text, property.LiteralType.SpecialType),
                MarkupValueKind.Bool => bool.TryParse(text, out _),
                MarkupValueKind.Color =>
                    ParseHexColor(text) is not null ||
                    text.StartsWith("$", StringComparison.Ordinal),
                MarkupValueKind.String => text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"',
                MarkupValueKind.Enum => property.ValueType.GetMembers(text).OfType<IFieldSymbol>().Any(field => field.HasConstantValue),
                _ => text.StartsWith("$", StringComparison.Ordinal)
            };
            if (!valid)
            {
                ReportMotion(MotionDiagnosticKind.Type, value.Location, "Motion value for property '" + target + "' is not compatible with type '" + property.ValueType.ToDisplayString() + "'.");
            }

            return valid;
        }

        private static bool IsMotionIntegerLiteral(string text, SpecialType type)
        {
            return type switch
            {
                SpecialType.System_Byte => byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_SByte => sbyte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int16 => short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt16 => ushort.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int32 => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt32 => uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_Int64 => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                SpecialType.System_UInt64 => ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                _ => false
            };
        }

        private static bool IsMotionParameterCompatible(string parameterTypeName, ITypeSymbol propertyType)
        {
            string parameterType = NormalizeMotionParameterType(parameterTypeName);
            string type = propertyType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            return parameterType switch
            {
                "float" or "System.Single" => type == "float",
                "double" or "System.Double" => type == "double",
                "int" or "System.Int32" => type == "int",
                "bool" or "System.Boolean" => type == "bool",
                "string" or "System.String" => type == "string",
                _ => false
            };
        }

        private static bool HasBuiltInMixer(ITypeSymbol type)
        {
            string name = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            return name is "float" or "double" or
                "Cerneala.Drawing.Color" or
                "Cerneala.UI.Media.Brush" or
                "Cerneala.UI.Layout.Thickness" or
                "Cerneala.Drawing.DrawPoint" or
                "Cerneala.Drawing.DrawSize" or
                "Cerneala.Drawing.DrawRect" or
                "Cerneala.UI.Media.Transform";
        }

        private static string GetMotionTypeCode(ITypeSymbol type)
        {
            SymbolDisplayFormat format = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            return type.ToDisplayString(format);
        }

    }
}
