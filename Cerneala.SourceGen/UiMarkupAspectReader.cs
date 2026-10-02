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

        private sealed class AspectResource
        {
            public AspectResource(
                string? name,
                string targetName,
                IReadOnlyList<AspectPropertyAssignment> assignments,
                IReadOnlyList<DirectiveWhenNode> conditions,
                IReadOnlyList<DirectiveOnNode> eventTriggers,
                MotionPresenceNode? presence,
                MotionLayoutNode? layout,
                IReadOnlyList<MotionScrollNode> scrolls,
                MotionDragNode? drag,
                MotionGesturePressNode? gesturePress,
                DirectiveTemplateNode? template,
                MarkupElement source,
                bool isInline = false)
            {
                Name = name;
                TargetName = targetName;
                Assignments = assignments;
                Conditions = conditions;
                EventTriggers = eventTriggers;
                Presence = presence;
                Layout = layout;
                Scrolls = scrolls;
                Drag = drag;
                GesturePress = gesturePress;
                Template = template;
                Source = source;
                IsInline = isInline;
            }

            public string? Name { get; }

            public string TargetName { get; }

            public IReadOnlyList<AspectPropertyAssignment> Assignments { get; }

            public IReadOnlyList<DirectiveWhenNode> Conditions { get; }

            public IReadOnlyList<DirectiveOnNode> EventTriggers { get; }

            public MotionPresenceNode? Presence { get; }

            public MotionLayoutNode? Layout { get; }

            public IReadOnlyList<MotionScrollNode> Scrolls { get; }

            public MotionDragNode? Drag { get; }

            public MotionGesturePressNode? GesturePress { get; }

            public DirectiveTemplateNode? Template { get; }

            public MarkupElement Source { get; }

            public bool IsInline { get; }

            public string? RuntimeVariable { get; set; }

            public string? RuntimeResourceVariable { get; set; }

            public bool BehaviorOwnedByPackage { get; set; }

            public ReactivePlan? ReactivePlan { get; set; }

            public List<string> ConditionKeyVariables { get; } = [];

            public bool ConditionKeysEmitted { get; set; }

            public IReadOnlyList<string>? BehaviorLines { get; set; }

            public IReadOnlyList<string>? BehaviorPostLines { get; set; }

            public IReadOnlyList<string> BehaviorLifetimeVariables { get; set; } = [];

            public string? TemplateVariable { get; set; }
        }

        private sealed class AspectPropertyAssignment
        {
            public AspectPropertyAssignment(string propertyName, string rawValue, bool isReference, MarkupObject source)
            {
                PropertyName = propertyName;
                RawValue = rawValue;
                IsReference = isReference;
                Source = source;
            }

            public string PropertyName { get; }

            public string RawValue { get; }

            public bool IsReference { get; }

            public MarkupObject Source { get; }
        }

        private void ImportApplicationAspects()
        {
            if (applicationResources is null)
            {
                return;
            }

            IEnumerable<AspectResource> applicationAspects = applicationResources.NamedResources.Values
                .OfType<NamedSymbol>()
                .Select(symbol => symbol.Source)
                .OfType<AspectResource>();
            foreach (AspectResource aspect in applicationAspects)
            {
                if (!allAspects.Contains(aspect))
                {
                    allAspects.Add(aspect);
                }
            }
        }

        private void ReadAspect(ResourceScope scope, MarkupElement resource)
        {
            MarkupAttribute? targetAttribute = resource.Attribute("TargetType");
            string targetName = targetAttribute?.Value ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetName))
            {
                Report(InvalidPropertyValue, resource, "Aspect", "TargetType", targetName);
                return;
            }

            targetName = targetName.Trim();
            if (ResolveAspectTargetType(targetName, resource) is null)
            {
                Report(UnsupportedElement, resource, targetName);
                return;
            }

            if (!TryParseAspectBody(
                resource,
                out List<AspectPropertyAssignment> assignments,
                out List<DirectiveWhenNode> conditions,
                out List<DirectiveOnNode> eventTriggers,
                out MotionPresenceNode? presence,
                out MotionLayoutNode? layout,
                out List<MotionScrollNode> scrolls,
                out MotionDragNode? drag,
                out MotionGesturePressNode? gesturePress,
                out DirectiveTemplateNode? template))
            {
                return;
            }

            string? name = resource.Attribute("Name")?.Value;
            string? trimmedName = string.IsNullOrWhiteSpace(name) ? null : name!.Trim();
            if (trimmedName is not null && IsReservedTemplateReference(trimmedName))
            {
                Report(
                    InvalidDocumentShape,
                    (MarkupObject?)resource.Attribute("Name") ?? resource,
                    Path.GetFileName(file.Path),
                    "Resource Name '" + trimmedName + "' is reserved by component templates.");
                return;
            }

            AspectResource aspect = new(
                trimmedName,
                targetName,
                assignments,
                conditions,
                eventTriggers,
                presence,
                layout,
                scrolls,
                drag,
                gesturePress,
                template,
                resource);
            allAspects.Add(aspect);
            if (aspect.Name is null)
            {
                if (scope.DefaultAspectsByTarget.ContainsKey(targetName))
                {
                    Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate unnamed Aspect for target '" + targetName + "' in the same resource scope.");
                    return;
                }

                scope.DefaultAspectsByTarget.Add(targetName, aspect);
                scope.RuntimeResources.Add(aspect);
                return;
            }

            if (scope.NamedResources.ContainsKey(aspect.Name))
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate resource Name '" + aspect.Name + "' in the same scope.");
                return;
            }

            scope.NamedResources.Add(aspect.Name, new NamedSymbol(aspect.Name, NamedSymbolKind.Aspect, aspect));
            scope.RuntimeResources.Add(aspect);
        }

        private void ReadInlineAspects()
        {
            MarkupElement[] owners = document.Root.DescendantsAndSelf().ToArray();
            INamedTypeSymbol? uiElementType = compilation.GetTypeByMetadataName("Cerneala.UI.Elements.UIElement");
            foreach (MarkupElement owner in owners.Where(element => !element.Name.LocalName.EndsWith(".Aspect", StringComparison.Ordinal)))
            {
                string expectedName = owner.Name.LocalName + ".Aspect";
                MarkupElement[] propertyElements = owner.Elements()
                    .Where(element => element.Name.LocalName.EndsWith(".Aspect", StringComparison.Ordinal))
                    .ToArray();
                MarkupElement[] matching = propertyElements
                    .Where(element => string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                    .ToArray();

                foreach (MarkupElement invalid in propertyElements.Where(element => !matching.Contains(element)))
                {
                    Report(
                        InvalidDocumentShape,
                        invalid,
                        Path.GetFileName(file.Path),
                        "Aspect property element '" + invalid.Name.LocalName + "' must match its owner tag '" + expectedName + "'.");
                    invalid.Remove();
                }

                if (matching.Length > 1)
                {
                    Report(
                        InvalidDocumentShape,
                        matching[1],
                        Path.GetFileName(file.Path),
                        "Element '" + owner.Name.LocalName + "' may declare only one Aspect property element.");
                    foreach (MarkupElement duplicate in matching.Skip(1))
                    {
                        duplicate.Remove();
                    }
                }

                if (matching.Length == 0)
                {
                    continue;
                }

                MarkupElement inline = matching[0];
                if (owner.Attribute("Aspect") is MarkupAttribute aspectAttribute)
                {
                    Report(
                        InvalidDocumentShape,
                        aspectAttribute,
                        Path.GetFileName(file.Path),
                        "Element '" + owner.Name.LocalName + "' cannot combine an Aspect attribute with an inline Aspect property element.");
                }

                INamedTypeSymbol? ownerType = ResolvePropertyOwnerType(owner.Name.LocalName, ReferenceEquals(owner, document.Root));
                if (uiElementType is null || ownerType is null || !IsOrDerivesFrom(ownerType, uiElementType))
                {
                    Report(UnsupportedProperty, inline, owner.Name.LocalName, "Aspect");
                    inline.Remove();
                    continue;
                }

                if (inline.HasAttributes)
                {
                    Report(
                        InvalidDocumentShape,
                        inline,
                        Path.GetFileName(file.Path),
                        "An inline Aspect property element does not accept attributes.");
                }

                if (TryParseAspectBody(
                    inline,
                    out List<AspectPropertyAssignment> assignments,
                    out List<DirectiveWhenNode> conditions,
                    out List<DirectiveOnNode> eventTriggers,
                    out MotionPresenceNode? presence,
                    out MotionLayoutNode? layout,
                    out List<MotionScrollNode> scrolls,
                    out MotionDragNode? drag,
                    out MotionGesturePressNode? gesturePress,
                    out DirectiveTemplateNode? template))
                {
                    AspectResource aspect = new(
                        null,
                        owner.Name.LocalName,
                        assignments,
                        conditions,
                        eventTriggers,
                        presence,
                        layout,
                        scrolls,
                        drag,
                        gesturePress,
                        template,
                        inline,
                        isInline: true);
                    inlineAspects.Add(owner, aspect);
                    allAspects.Add(aspect);
                }

                inline.Remove();
            }
        }

        private bool TryParseAspectBody(
            MarkupElement source,
            out List<AspectPropertyAssignment> assignments,
            out List<DirectiveWhenNode> conditions,
            out List<DirectiveOnNode> eventTriggers,
            out MotionPresenceNode? presence,
            out MotionLayoutNode? layout,
            out List<MotionScrollNode> scrolls,
            out MotionDragNode? drag,
            out MotionGesturePressNode? gesturePress,
            out DirectiveTemplateNode? template)
        {
            assignments = [];
            conditions = [];
            eventTriggers = [];
            presence = null;
            layout = null;
            scrolls = [];
            drag = null;
            gesturePress = null;
            template = null;
            DirectiveParseResult parsed = ParseDirectiveContent(
                source,
                DirectiveContentKind.Assignments | DirectiveContentKind.Templates |
                DirectiveContentKind.MotionTriggers | DirectiveContentKind.MotionHandles |
                DirectiveContentKind.MotionPresence | DirectiveContentKind.MotionLayout | DirectiveContentKind.MotionScroll |
                DirectiveContentKind.MotionDrag | DirectiveContentKind.MotionGesture);
            if (parsed.Error is not null)
            {
                ReportMotion(ClassifyMotionParseError(parsed.Error), parsed.ErrorSource ?? source, parsed.Error);
                return false;
            }

            HashSet<string> motionHandles = new(StringComparer.Ordinal);
            foreach (DirectiveNode node in parsed.Nodes)
            {
                if (node is MotionHandleNode handle)
                {
                    if (!motionHandles.Add(handle.Name))
                    {
                        ReportMotion(MotionDiagnosticKind.Composition, handle.Source, "Duplicate Motion handle '" + handle.Name + "'.");
                        return false;
                    }

                    continue;
                }

                if (!ValidateMotionHandleUses(node, motionHandles))
                {
                    return false;
                }

                if (node is DirectiveDefaultNode defaults)
                {
                    foreach (DirectiveNode child in defaults.Body)
                    {
                        if (child is DirectiveAssignmentNode assignment)
                        {
                            assignments.Add(ToAspectAssignment(assignment));
                        }
                        else if (child is DirectiveWhenNode nestedWhen)
                        {
                            conditions.Add(nestedWhen);
                        }
                        else
                        {
                            Report(InvalidDirective, child.Source, Path.GetFileName(file.Path), "@default may contain only property assignments or @when blocks.");
                            return false;
                        }
                    }
                }
                else if (node is DirectiveWhenNode when)
                {
                    conditions.Add(when);
                }
                else if (node is DirectiveOnNode on)
                {
                    eventTriggers.Add(on);
                }
                else if (node is MotionPresenceNode declaredPresence)
                {
                    if (presence is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredPresence.Source, "An Aspect may declare only one @presence block.");
                        return false;
                    }

                    presence = declaredPresence;
                }
                else if (node is MotionLayoutNode declaredLayout)
                {
                    if (layout is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredLayout.Source, "An Aspect may declare only one @layout statement.");
                        return false;
                    }

                    layout = declaredLayout;
                }
                else if (node is MotionScrollNode scroll)
                {
                    scrolls.Add(scroll);
                }
                else if (node is MotionDragNode declaredDrag)
                {
                    if (drag is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredDrag.Source, "An Aspect may declare only one @drag statement.");
                        return false;
                    }

                    drag = declaredDrag;
                }
                else if (node is MotionGesturePressNode declaredGesturePress)
                {
                    if (gesturePress is not null)
                    {
                        ReportMotion(MotionDiagnosticKind.Lifecycle, declaredGesturePress.Source, "An Aspect may declare only one @gesture press statement.");
                        return false;
                    }

                    gesturePress = declaredGesturePress;
                }
                else if (node is DirectiveTemplateNode declaredTemplate)
                {
                    if (template is not null)
                    {
                        Report(
                            InvalidComponentTemplate,
                            declaredTemplate.Source,
                            Path.GetFileName(file.Path),
                            "An Aspect may declare only one @template block.");
                        return false;
                    }

                    template = declaredTemplate;
                }
                else
                {
                    Report(InvalidDirective, node.Source, Path.GetFileName(file.Path), "Aspect bodies may contain only @default, @when, @on, @presence, @layout, @scroll, @drag, @gesture press and @template blocks.");
                    return false;
                }
            }

            IGrouping<string, AspectPropertyAssignment>? duplicate = assignments
                .GroupBy(assignment => assignment.PropertyName, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                Report(
                    InvalidDocumentShape,
                    duplicate.Skip(1).First().Source,
                    Path.GetFileName(file.Path),
                    "Aspect assigns property '" + duplicate.Key + "' more than once in @default.");
                return false;
            }

            return !HasErrors;
        }

        private bool ValidateMotionHandleUses(DirectiveNode node, ISet<string> declaredHandles)
        {
            foreach (MotionExecutionNode execution in EnumerateMotionExecutions(node))
            {
                string? handleName = execution switch
                {
                    MotionRunNode run => run.HandleName,
                    MotionCancelNode cancel => cancel.HandleName,
                    _ => null
                };
                if (handleName is not null && !declaredHandles.Contains(handleName))
                {
                    Report(
                        InvalidDirective,
                        execution.Source,
                        Path.GetFileName(file.Path),
                        "Motion handle '" + handleName + "' is undeclared or used before its @handle declaration.");
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<MotionExecutionNode> EnumerateMotionExecutions(DirectiveNode node)
        {
            if (node is MotionExecutionNode execution)
            {
                yield return execution;
                if (execution is MotionCompositionNode composition)
                {
                    foreach (MotionExecutionNode child in composition.Children.SelectMany(EnumerateMotionExecutions))
                    {
                        yield return child;
                    }
                }

                yield break;
            }

            IEnumerable<DirectiveNode> children = node switch
            {
                DirectiveOnNode on => on.Body,
                DirectiveWhenNode condition when condition.BooleanBody is not null => condition.BooleanBody,
                DirectiveWhenNode condition => condition.Branches.SelectMany(branch => branch.Body),
                DirectiveIfNode branch => branch.Body,
                DirectiveDefaultNode defaults => defaults.Body,
                _ => []
            };
            foreach (MotionExecutionNode child in children.SelectMany(EnumerateMotionExecutions))
            {
                yield return child;
            }
        }

        private static AspectPropertyAssignment ToAspectAssignment(DirectiveAssignmentNode assignment)
        {
            string value = assignment.Value.Trim();
            bool isReference = value.StartsWith("$", StringComparison.Ordinal);
            if (isReference)
            {
                value = value.Substring(1);
            }

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            return new AspectPropertyAssignment(assignment.PropertyName, value, isReference, assignment.Source);
        }
    }
}
