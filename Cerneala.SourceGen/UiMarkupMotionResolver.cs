using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private sealed class MotionSpecResource
        {
            public MotionSpecResource(string name, string kind, IReadOnlyList<string> arguments, XElement source)
            {
                Name = name;
                Kind = kind;
                Arguments = arguments;
                Source = source;
            }

            public string Name { get; }

            public string Kind { get; }

            public IReadOnlyList<string> Arguments { get; }

            public XElement Source { get; }
        }

        private sealed class ResolvedMotionAspect
        {
            public ResolvedMotionAspect(
                IReadOnlyList<ResolvedMotionAnimation> animations,
                IReadOnlyList<ResolvedMotionEventTrigger> eventTriggers)
            {
                Animations = animations;
                EventTriggers = eventTriggers;
            }

            public IReadOnlyList<ResolvedMotionAnimation> Animations { get; }

            public IReadOnlyList<ResolvedMotionEventTrigger> EventTriggers { get; }
        }

        private sealed class ResolvedMotionAnimation
        {
            public ResolvedMotionAnimation(
                MotionAnimateNode syntax,
                IReadOnlyList<ResolvedMotionProperty> properties,
                string executionName)
            {
                Syntax = syntax;
                Properties = properties;
                ExecutionName = executionName;
            }

            public MotionAnimateNode Syntax { get; }

            public IReadOnlyList<ResolvedMotionProperty> Properties { get; }

            public string ExecutionName { get; }
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

        private sealed class ResolvedMotionProperty
        {
            public ResolvedMotionProperty(
                MotionAssignmentSyntax destination,
                MotionAssignmentSyntax? source,
                XElement targetElement,
                PropertySpec property,
                string? specVariable)
            {
                Destination = destination;
                Source = source;
                TargetElement = targetElement;
                Property = property;
                SpecVariable = specVariable;
            }

            public MotionAssignmentSyntax Destination { get; }

            public MotionAssignmentSyntax? Source { get; }

            public XElement TargetElement { get; }

            public PropertySpec Property { get; }

            public string? SpecVariable { get; }
        }

        private readonly Dictionary<(AspectResource Aspect, XElement Element), ResolvedMotionAspect> resolvedMotionAspects = new();
        private readonly Dictionary<MotionAnimateNode, string> motionExecutionNames = new();
        private readonly Dictionary<string, string> specializedMotionSpecs = new(StringComparer.Ordinal);

        private void ReadMotionSpecResource(ResourceScope scope, XElement resource)
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

            List<string> arguments = [];
            if (resource.Name.LocalName == "Tween")
            {
                string duration = resource.Attribute("Duration")?.Value.Trim() ?? string.Empty;
                if (!TryBuildDurationExpression(duration, out string durationCode))
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("Duration") ?? resource, "Tween", "Duration", duration);
                    return;
                }

                string easing = resource.Attribute("Easing")?.Value.Trim() ?? "Standard";
                if (!IsKnownEasing(easing))
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("Easing") ?? resource, "Tween", "Easing", easing);
                    return;
                }

                arguments.Add(durationCode);
                arguments.Add("global::Cerneala.UI.Motion.Specs.Easings." + easing);
            }
            else
            {
                foreach ((string attributeName, float defaultValue, bool allowZero) in new[]
                {
                    ("Stiffness", 520f, false),
                    ("Damping", 38f, true),
                    ("Mass", 1f, false)
                })
                {
                    string text = resource.Attribute(attributeName)?.Value.Trim() ?? defaultValue.ToString("R", CultureInfo.InvariantCulture);
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
                        float.IsNaN(value) || float.IsInfinity(value) ||
                        (allowZero ? value < 0 : value <= 0))
                    {
                        Report(InvalidPropertyValue, (object?)resource.Attribute(attributeName) ?? resource, "Spring", attributeName, text);
                        return;
                    }

                    arguments.Add(value.ToString("R", CultureInfo.InvariantCulture) + "f");
                }
            }

            MotionSpecResource spec = new(name, resource.Name.LocalName, arguments, resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.MotionSpec, spec));
        }

        private bool ResolveMotionAspect(XElement applicationElement, AspectResource aspect)
        {
            if (resolvedMotionAspects.ContainsKey((aspect, applicationElement)))
            {
                return true;
            }

            INamedTypeSymbol? targetType = ResolveAspectTargetTypeSymbol(aspect.TargetName);
            INamedTypeSymbol? applicationType = ResolvePropertyOwnerType(
                applicationElement.Name.LocalName,
                ReferenceEquals(applicationElement, document.Root));
            if (targetType is null)
            {
                Report(InvalidDirective, aspect.Source, Path.GetFileName(file.Path), "Motion TargetType '" + aspect.TargetName + "' could not be resolved.");
                return false;
            }

            if (applicationType is null || !IsOrDerivesFrom(applicationType, targetType))
            {
                Report(
                    InvalidDirective,
                    (object?)applicationElement.Attribute("Aspect") ?? applicationElement,
                    Path.GetFileName(file.Path),
                    "Aspect TargetType '" + aspect.TargetName + "' is not assignable from '" + applicationElement.Name.LocalName + "'.");
                return false;
            }

            List<MotionAnimateNode> syntaxAnimations = [];
            foreach (DirectiveWhenNode condition in aspect.Conditions)
            {
                CollectMotionAnimations(condition, syntaxAnimations);
            }

            foreach (DirectiveOnNode trigger in aspect.EventTriggers)
            {
                syntaxAnimations.AddRange(trigger.Body.OfType<MotionAnimateNode>());
            }

            List<ResolvedMotionAnimation> animations = [];
            foreach (MotionAnimateNode animation in syntaxAnimations)
            {
                if (!TryResolveMotionAnimation(applicationElement, aspect, animation, out ResolvedMotionAnimation? resolved))
                {
                    return false;
                }

                animations.Add(resolved!);
            }

            List<ResolvedMotionEventTrigger> eventTriggers = [];
            foreach (DirectiveOnNode trigger in aspect.EventTriggers)
            {
                IEventSymbol? eventSymbol = FindMotionEvent(targetType, trigger.EventName);
                if (eventSymbol is null || !IsAccessibleFromGeneratedCode(eventSymbol))
                {
                    Report(
                        InvalidDirective,
                        trigger.Source,
                        Path.GetFileName(file.Path),
                        "Motion event '" + trigger.EventName + "' was not found or is not accessible on TargetType '" + aspect.TargetName + "'.");
                    return false;
                }

                string[] executionNames = trigger.Body
                    .OfType<MotionAnimateNode>()
                    .Select(animation => motionExecutionNames[animation])
                    .ToArray();
                eventTriggers.Add(new ResolvedMotionEventTrigger(eventSymbol, executionNames));
            }

            resolvedMotionAspects.Add((aspect, applicationElement), new ResolvedMotionAspect(animations, eventTriggers));
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

        private static void CollectMotionAnimations(DirectiveWhenNode condition, ICollection<MotionAnimateNode> animations)
        {
            if (condition.BooleanBody is not null)
            {
                CollectMotionAnimations(condition.BooleanBody, animations);
            }

            foreach (DirectiveIfNode branch in condition.Branches)
            {
                CollectMotionAnimations(branch.Body, animations);
            }
        }

        private static void CollectMotionAnimations(IReadOnlyList<DirectiveNode> nodes, ICollection<MotionAnimateNode> animations)
        {
            foreach (MotionAnimateNode animation in nodes.OfType<MotionAnimateNode>())
            {
                animations.Add(animation);
            }

            foreach (DirectiveWhenNode nested in nodes.OfType<DirectiveWhenNode>())
            {
                CollectMotionAnimations(nested, animations);
            }
        }

        private bool TryResolveMotionAnimation(
            XElement applicationElement,
            AspectResource aspect,
            MotionAnimateNode animation,
            out ResolvedMotionAnimation? resolved)
        {
            resolved = null;
            if (!ValidateMotionOptions(animation.Options))
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
                if (!TryResolveMotionTarget(applicationElement, aspect, destination, out XElement? targetElement, out PropertySpec? property))
                {
                    return false;
                }

                if (!property!.Assignable)
                {
                    Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Motion property '" + destination.Target + "' is read-only or inaccessible.");
                    return false;
                }

                if (!HasBuiltInMixer(property.ValueType))
                {
                    Report(InvalidDirective, destination.Location, Path.GetFileName(file.Path), "Motion property '" + destination.Target + "' has no compatible mixer.");
                    return false;
                }

                if (!ValidateMotionValue(destination.Value, property, destination.Target) ||
                    (source is not null && !ValidateMotionValue(source.Value, property, source.Target)))
                {
                    return false;
                }

                MotionSpecSyntax? spec = destination.Spec ?? animation.DefaultSpec;
                if (!TryResolveMotionSpec(applicationElement, spec, property, destination.Target, out string? specVariable))
                {
                    return false;
                }

                properties.Add(new ResolvedMotionProperty(destination, source, targetElement!, property, specVariable));
            }

            string executionName = "motionExecution" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            motionExecutionNames[animation] = executionName;
            resolved = new ResolvedMotionAnimation(animation, properties, executionName);
            return true;
        }

        private void EmitMotionActivations(XElement element, string variable, AspectResource aspect)
        {
            if (!resolvedMotionAspects.TryGetValue((aspect, element), out ResolvedMotionAspect? resolved))
            {
                return;
            }

            string sessionName = "motionSession" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            currentPostLines.Add(
                "global::System.IDisposable " + sessionName +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.AttachMotionSession(" + variable + ");");
            if (templateEmissionContexts.Count > 0)
            {
                currentPostLines.Add(templateEmissionContexts.Peek().ContextVariable + ".RegisterLifetime(" + sessionName + ");");
            }

            foreach (ResolvedMotionAnimation animation in resolved.Animations)
            {
                List<string> starts = [];
                foreach (ResolvedMotionProperty property in animation.Properties)
                {
                    string targetCode = ReferenceEquals(property.TargetElement, element)
                        ? variable
                        : CreateIdentifier(property.TargetElement.Attribute("Name")!.Value);
                    string typeCode = property.Property.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    bool hasFrom = property.Source is not null && property.Source.Value is not MotionCurrentValueSyntax;
                    string fromCode = hasFrom
                        ? EmitMotionValue(property.Source!.Value, property.Property, targetCode)
                        : "default(" + typeCode + ")!";
                    bool toCurrent = property.Destination.Value is MotionCurrentValueSyntax;
                    string toCode = toCurrent
                        ? "default(" + typeCode + ")!"
                        : EmitMotionValue(property.Destination.Value, property.Property, targetCode);
                    string specCode = property.SpecVariable ?? "null";
                    string optionsCode = EmitMotionOptions(animation.Syntax.Options);
                    starts.Add(
                        "global::Cerneala.UI.Markup.GeneratedMarkup.StartMotionProperty(" + sessionName + ", " +
                        targetCode + ", " + property.Property.PropertyCode + ", " +
                        (hasFrom ? "true" : "false") + ", " + fromCode + ", " +
                        (toCurrent ? "true" : "false") + ", " + toCode + ", " + specCode + ", " + optionsCode + ")");
                }

                currentPostLines.Add(
                    "global::System.Action " + animation.ExecutionName +
                    " = () => global::Cerneala.UI.Markup.GeneratedMarkup.StartMotion(" + sessionName +
                    ", () => new global::Cerneala.UI.Motion.Core.MotionHandle[] { " + string.Join(", ", starts) + " });");
            }

            foreach (ResolvedMotionEventTrigger trigger in resolved.EventTriggers)
            {
                string handlerName = "motionEventHandler" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                INamedTypeSymbol delegateType = (INamedTypeSymbol)trigger.EventSymbol.Type;
                IMethodSymbol invoke = delegateType.DelegateInvokeMethod!;
                string parameters = string.Join(", ", invoke.Parameters.Select((_, index) => "eventArg" + index.ToString(CultureInfo.InvariantCulture)));
                string calls = string.Join(" ", trigger.ExecutionNames.Select(name => name + "();"));
                currentPostLines.Add(
                    delegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " " + handlerName +
                    " = (" + parameters + ") => { " + calls + " };");

                currentPostLines.Add(
                    "global::Cerneala.UI.Markup.GeneratedMarkup.AddMotionTrigger(" + sessionName +
                    ", () => " + variable + "." + trigger.EventSymbol.Name + " += " + handlerName +
                    ", () => " + variable + "." + trigger.EventSymbol.Name + " -= " + handlerName + ");");
            }
        }

        private string EmitMotionValue(MotionValueSyntax value, PropertySpec property, string targetCode)
        {
            if (value is MotionConditionalValueSyntax conditional)
            {
                return "(" + EmitMotionCondition(conditional.Condition, targetCode) + " ? " +
                    EmitMotionValue(conditional.WhenTrue, property, targetCode) + " : " +
                    EmitMotionValue(conditional.WhenFalse, property, targetCode) + ")";
            }

            if (value is MotionCurrentValueSyntax)
            {
                return "default(" + property.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")!";
            }

            MotionAtomValueSyntax atom = (MotionAtomValueSyntax)value;
            GeneratedExpression? expression = ParseDirectiveValue(
                null,
                property.Name,
                atom.Text,
                property,
                atom.Location.Source);
            return expression?.Code ?? "default(" + property.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")!";
        }

        private static string EmitMotionCondition(DirectiveExpression expression, string targetCode)
        {
            return expression switch
            {
                DirectiveSourceExpression source => source.Text.StartsWith("$self.", StringComparison.Ordinal)
                    ? targetCode + "." + source.Text.Substring(6)
                    : targetCode + "." + source.Text,
                DirectiveLiteralExpression literal => literal.Text,
                DirectiveGroupExpression group => "(" + EmitMotionCondition(group.Inner, targetCode) + ")",
                DirectiveComparisonExpression comparison =>
                    EmitMotionCondition(comparison.Left, targetCode) + " " + comparison.Comparator + " " +
                    EmitMotionCondition(comparison.Right, targetCode),
                DirectiveLogicalExpression logical =>
                    "(" + EmitMotionCondition(logical.Left, targetCode) +
                    (logical.Operator == DirectiveLogicalOperator.And ? " && " : " || ") +
                    EmitMotionCondition(logical.Right, targetCode) + ")",
                _ => "false"
            };
        }

        private static string EmitMotionOptions(IReadOnlyList<MotionOptionSyntax> options)
        {
            string retarget = options.FirstOrDefault(option => option.Name == "retarget")?.Value is MotionAtomValueSyntax retargetValue
                ? retargetValue.Text
                : "Restart";
            string hold = options.FirstOrDefault(option => option.Name == "holdOnComplete")?.Value is MotionAtomValueSyntax holdValue
                ? holdValue.Text.ToLowerInvariant()
                : "true";
            string debugName = options.FirstOrDefault(option => option.Name == "debugName")?.Value is MotionAtomValueSyntax debugValue
                ? debugValue.Text
                : "null";
            return "new global::Cerneala.UI.Motion.Properties.MotionPropertyStartOptions { RetargetMode = " +
                "global::Cerneala.UI.Motion.Specs.RetargetMode." + retarget +
                ", HoldOnComplete = " + hold + ", DebugName = " + debugName + " }";
        }

        private bool TryResolveMotionTarget(
            XElement applicationElement,
            AspectResource aspect,
            MotionAssignmentSyntax assignment,
            out XElement? targetElement,
            out PropertySpec? property)
        {
            targetElement = applicationElement;
            string propertyName = assignment.Target;
            INamedTypeSymbol? targetType;
            if (assignment.Target.StartsWith("$part.", StringComparison.Ordinal))
            {
                string[] parts = assignment.Target.Split('.');
                string partName = parts[1];
                propertyName = parts[2];
                targetElement = applicationElement.DescendantsAndSelf()
                    .FirstOrDefault(element => string.Equals(element.Attribute("Name")?.Value, partName, StringComparison.Ordinal));
                if (targetElement is null && aspect.Template is not null &&
                    templateParts.TryGetValue(aspect.Template, out IReadOnlyDictionary<string, XElement>? declaredParts))
                {
                    declaredParts.TryGetValue(partName, out targetElement);
                }

                if (targetElement is null)
                {
                    Report(InvalidDirective, assignment.Location, Path.GetFileName(file.Path), "Motion target part '" + partName + "' is not available at this Aspect application site.");
                    property = null;
                    return false;
                }

                targetType = ResolvePropertyOwnerType(targetElement.Name.LocalName, ReferenceEquals(targetElement, document.Root));
            }
            else
            {
                targetType = ResolvePropertyOwnerType(applicationElement.Name.LocalName, ReferenceEquals(applicationElement, document.Root));
            }

            if (targetType is null)
            {
                Report(InvalidDirective, assignment.Location, Path.GetFileName(file.Path), "Motion target type could not be resolved.");
                property = null;
                return false;
            }

            property = FindPropertySpec(targetType, propertyName);
            if (property is null)
            {
                Report(InvalidDirective, assignment.Location, Path.GetFileName(file.Path), "Motion property '" + assignment.Target + "' does not exist on target type '" + targetType.Name + "'.");
                return false;
            }

            return true;
        }

        private bool ValidateMotionOptions(IReadOnlyList<MotionOptionSyntax> options)
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

                bool valid = option.Name switch
                {
                    "retarget" => atom.Text is "Restart" or "PreserveProgress",
                    "holdOnComplete" => bool.TryParse(atom.Text, out _),
                    "debugName" => atom.Text.Length >= 2 && atom.Text[0] == '"' && atom.Text[atom.Text.Length - 1] == '"',
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

        private bool ValidateMotionValue(MotionValueSyntax value, PropertySpec property, string target)
        {
            if (value is MotionCurrentValueSyntax)
            {
                return true;
            }

            if (value is MotionConditionalValueSyntax conditional)
            {
                return ValidateMotionValue(conditional.WhenTrue, property, target) &&
                    ValidateMotionValue(conditional.WhenFalse, property, target);
            }

            string text = ((MotionAtomValueSyntax)value).Text.Trim();
            bool valid = property.ValueKind switch
            {
                MarkupValueKind.Float or MarkupValueKind.NonNegativeFloat or MarkupValueKind.PositiveFloat =>
                    float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                MarkupValueKind.Double => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                MarkupValueKind.Integer => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                MarkupValueKind.Bool => bool.TryParse(text, out _),
                MarkupValueKind.String => text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"',
                MarkupValueKind.Enum => property.ValueType.GetMembers(text).OfType<IFieldSymbol>().Any(field => field.HasConstantValue),
                _ => text.StartsWith("$", StringComparison.Ordinal)
            };
            if (!valid)
            {
                Report(InvalidDirective, value.Location, Path.GetFileName(file.Path), "Motion value for property '" + target + "' is not compatible with type '" + property.ValueType.ToDisplayString() + "'.");
            }

            return valid;
        }

        private bool TryResolveMotionSpec(
            XElement applicationElement,
            MotionSpecSyntax? syntax,
            PropertySpec property,
            string target,
            out string? variable)
        {
            variable = null;
            if (syntax is null)
            {
                if (!IsBuiltInAnimatableProperty(property.PropertyCode))
                {
                    Report(InvalidDirective, property.PropertyCode, Path.GetFileName(file.Path), "Motion property '" + target + "' has no implicit spec because it is not registered as animatable.");
                    return false;
                }

                return true;
            }

            string kind;
            IReadOnlyList<string> arguments;
            if (syntax is MotionResourceSpecSyntax resourceReference)
            {
                if (!TryResolveResource(resourceReference.Location.Source, resourceReference.Name, out NamedSymbol symbol) ||
                    symbol.Source is not MotionSpecResource resource)
                {
                    Report(InvalidDirective, resourceReference.Location, Path.GetFileName(file.Path), "Unknown Motion resource '$" + resourceReference.Name + "'.");
                    return false;
                }

                kind = resource.Kind;
                arguments = resource.Arguments;
            }
            else
            {
                MotionInlineSpecSyntax inline = (MotionInlineSpecSyntax)syntax;
                kind = inline.Kind;
                if (!TryBuildInlineSpecArguments(inline, out arguments))
                {
                    return false;
                }
            }

            if (kind == "Spring" && !HasVectorMixer(property.ValueType))
            {
                Report(InvalidDirective, syntax.Location, Path.GetFileName(file.Path), "Spring is not a valid spec type for property '" + target + "' because its mixer has no vector operations.");
                return false;
            }

            string typeCode = property.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string expression = "new global::Cerneala.UI.Motion.Specs." + kind + "Spec<" + typeCode + ">(" + string.Join(", ", arguments) + ")";
            string key = typeCode + "|" + expression;
            if (!specializedMotionSpecs.TryGetValue(key, out variable))
            {
                variable = "motionSpec" + specializedMotionSpecs.Count.ToString(CultureInfo.InvariantCulture);
                specializedMotionSpecs.Add(key, variable);
                currentLines.Add("global::Cerneala.UI.Motion.Specs.MotionSpec<" + typeCode + "> " + variable + " = " + expression + ";");
            }

            return true;
        }

        private bool TryBuildInlineSpecArguments(MotionInlineSpecSyntax inline, out IReadOnlyList<string> arguments)
        {
            List<string> result = [];
            if (inline.Kind == "Tween")
            {
                MotionDurationSyntax duration = inline.Arguments[0].Duration!;
                result.Add(BuildDurationExpression(duration));
                string easing = inline.Arguments.Count == 2 ? inline.Arguments[1].Text : "Standard";
                if (!IsKnownEasing(easing))
                {
                    Report(InvalidDirective, inline.Arguments[Math.Min(1, inline.Arguments.Count - 1)].Location, Path.GetFileName(file.Path), "Unknown easing '" + easing + "'.");
                    arguments = [];
                    return false;
                }

                result.Add("global::Cerneala.UI.Motion.Specs.Easings." + easing);
            }
            else
            {
                if (inline.Arguments.Count is < 1 or > 3)
                {
                    Report(InvalidDirective, inline.Location, Path.GetFileName(file.Path), "Spring accepts stiffness, damping and mass.");
                    arguments = [];
                    return false;
                }

                foreach (MotionSpecArgumentSyntax argument in inline.Arguments)
                {
                    if (!float.TryParse(argument.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
                        float.IsNaN(value) || float.IsInfinity(value))
                    {
                        Report(InvalidDirective, argument.Location, Path.GetFileName(file.Path), "Spring argument '" + argument.Text + "' must be numeric.");
                        arguments = [];
                        return false;
                    }

                    result.Add(value.ToString("R", CultureInfo.InvariantCulture) + "f");
                }
            }

            arguments = result;
            return true;
        }

        private static bool TryBuildDurationExpression(string text, out string expression)
        {
            expression = string.Empty;
            string unit;
            string number;
            if (text.EndsWith("ms", StringComparison.Ordinal))
            {
                unit = "FromMilliseconds";
                number = text.Substring(0, text.Length - 2);
            }
            else if (text.EndsWith("s", StringComparison.Ordinal))
            {
                unit = "FromSeconds";
                number = text.Substring(0, text.Length - 1);
            }
            else
            {
                return false;
            }

            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
            {
                return false;
            }

            expression = "global::System.TimeSpan." + unit + "(" + value.ToString("R", CultureInfo.InvariantCulture) + ")";
            return true;
        }

        private static string BuildDurationExpression(MotionDurationSyntax duration)
        {
            string factory = duration.Unit == "ms" ? "FromMilliseconds" : "FromSeconds";
            return "global::System.TimeSpan." + factory + "(" + duration.Value.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private static bool IsKnownEasing(string name)
        {
            return name is "Linear" or "Standard" or "Emphasized" or "EaseIn" or "EaseOut" or "EaseInOut" or "Sharp";
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

        private static bool HasVectorMixer(ITypeSymbol type)
        {
            string name = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
            return name is "float" or "double" or
                "Cerneala.UI.Layout.Thickness" or
                "Cerneala.Drawing.DrawPoint" or
                "Cerneala.Drawing.DrawSize";
        }

        private static bool IsBuiltInAnimatableProperty(string propertyCode)
        {
            return propertyCode.EndsWith(".BackgroundProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".ForegroundProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".BorderBrushProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".BorderThicknessProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".PaddingProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".MarginProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".OpacityProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".RenderTransformProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".TranslateXProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".TranslateYProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".ScaleProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".ScaleXProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".ScaleYProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".RotationProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".SkewXProperty", StringComparison.Ordinal) ||
                propertyCode.EndsWith(".SkewYProperty", StringComparison.Ordinal);
        }
    }
}
