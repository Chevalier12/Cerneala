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

        private static bool HasRuntimeBehavior(AspectResource aspect) =>
            aspect.Conditions.Count > 0 ||
            aspect.EventTriggers.Count > 0 ||
            aspect.Presence is not null ||
            aspect.Layout is not null ||
            aspect.Scrolls.Count > 0 ||
            aspect.Drag is not null ||
            aspect.GesturePress is not null;

        private static bool SupportsPackageBehavior(AspectResource aspect) =>
            aspect.Presence is null &&
            aspect.Layout is null &&
            aspect.Scrolls.Count == 0 &&
            aspect.Drag is null &&
            aspect.GesturePress is null &&
            aspect.EventTriggers.Count == 0 &&
            !aspect.Conditions.Any(ContainsMotionExecution);

        private static bool HasMotionBehavior(AspectResource aspect) =>
            aspect.EventTriggers.Count > 0 ||
            aspect.Presence is not null ||
            aspect.Layout is not null ||
            aspect.Scrolls.Count > 0 ||
            aspect.Drag is not null ||
            aspect.GesturePress is not null ||
            aspect.Conditions.Any(ContainsMotionExecution);

        private bool PrepareAspectBehavior(string targetType, AspectResource aspect, bool includeMotion)
        {
            if (aspect.BehaviorLines is not null)
            {
                return true;
            }

            List<string> behaviorLines = [];
            List<string> behaviorPostLines = [];
            List<string> lifetimeVariables = [];
            bool resolved = true;
            MarkupElement targetElement = new(ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source)!.Name);
            WithEmissionBuffers(behaviorLines, behaviorPostLines, () =>
            {
                if (includeMotion && !ResolveMotionAspect(targetElement, "target", aspect))
                {
                    resolved = false;
                    return;
                }

                if (includeMotion)
                {
                    EmitMotionPresence(targetElement, "target", aspect);
                    EmitMotionLayout(targetElement, "target", aspect);
                    EmitMotionActivations(targetElement, "target", aspect, bindToElementAspect: false);
                }
                ReactivePlan plan = BuildAspectReactivePlan(aspect, "target", targetElement.Name.LocalName);
                if (!includeMotion)
                {
                    foreach (ReactiveRule rule in plan.Rules)
                    {
                        rule.Activations = [];
                    }
                }

                aspect.ReactivePlan = plan;
                while (aspect.ConditionKeyVariables.Count < plan.Rules.Count)
                {
                    string keyVariable = "aspectConditionKey" + nextResourceId.ToString(CultureInfo.InvariantCulture);
                    nextResourceId++;
                    aspect.ConditionKeyVariables.Add(keyVariable);
                }

                if (plan.Rules.Count > 0)
                {
                    string conditionBehavior = "aspectConditionBehavior" + nextResourceId.ToString(CultureInfo.InvariantCulture);
                    nextResourceId++;
                    currentLines.Add("global::System.IDisposable? " + conditionBehavior + " = null;");
                    lifetimeVariables.Add(conditionBehavior);
                    EmitReactivePlan(
                        plan,
                        controlsContent: plan.HasConditionalContent,
                        aspect.ConditionKeyVariables,
                        assignmentTarget: conditionBehavior);
                }
            });

            if (!resolved)
            {
                return false;
            }

            foreach (string line in behaviorLines.Concat(behaviorPostLines))
            {
                const string prefix = "global::System.IDisposable ";
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                int end = line.IndexOf(' ', prefix.Length);
                if (end > prefix.Length)
                {
                    lifetimeVariables.Add(line.Substring(prefix.Length, end - prefix.Length));
                }
            }

            aspect.BehaviorLines = behaviorLines;
            aspect.BehaviorPostLines = behaviorPostLines;
            aspect.BehaviorLifetimeVariables = lifetimeVariables.Distinct(StringComparer.Ordinal).ToArray();
            return true;
        }

        private void EmitAspectConditionKeys(AspectResource aspect, string diagnosticPrefix)
        {
            if (aspect.ConditionKeysEmitted)
            {
                return;
            }

            for (int index = 0; index < aspect.ConditionKeyVariables.Count; index++)
            {
                currentLines.Add(
                    "global::Cerneala.UI.Aspect.AspectConditionKey " + aspect.ConditionKeyVariables[index] +
                    " = new(" + Literal(diagnosticPrefix + ".condition." + index.ToString(CultureInfo.InvariantCulture)) + ");");
            }

            aspect.ConditionKeysEmitted = true;
        }

        private static bool ContainsMotionExecution(DirectiveWhenNode when) =>
            (when.BooleanBody is not null && ContainsMotionExecution(when.BooleanBody)) ||
            when.Branches.Any(branch => ContainsMotionExecution(branch.Body));

        private static bool ContainsMotionExecution(IReadOnlyList<DirectiveNode> nodes) =>
            nodes.Any(node =>
                node is MotionExecutionNode ||
                (node is DirectiveWhenNode when && ContainsMotionExecution(when)) ||
                (node is DirectiveIfNode branch && ContainsMotionExecution(branch.Body)));

        private void EmitAspectPackageResource(
            string ownerVariable,
            string key,
            string targetType,
            AspectResource aspect)
        {
            INamedTypeSymbol targetSymbol = ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source)!;
            bool packageOwnsBehavior = HasRuntimeBehavior(aspect) && SupportsPackageBehavior(aspect);
            if (HasRuntimeBehavior(aspect) && !PrepareAspectBehavior(
                targetType,
                aspect,
                includeMotion: packageOwnsBehavior))
            {
                return;
            }

            string resourceVariable = "aspectPackage" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            string packageName = "Markup." + Path.GetFileNameWithoutExtension(file.Path) + "." +
                targetSymbol.Name + "." + resourceVariable;
            EmitAspectConditionKeys(aspect, packageName);
            List<string>? declarations = BuildAspectDeclarations(targetSymbol.Name, aspect);
            if (declarations is null)
            {
                return;
            }

            string declarationsCode = declarations.Count == 0
                ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.AspectDeclaration>()"
                : "new global::Cerneala.UI.Aspect.AspectDeclaration[] { " + string.Join(", ", declarations) + " }";
            aspect.RuntimeResourceVariable = resourceVariable;
            currentLines.Add("global::Cerneala.UI.Aspect.AspectPackage " + resourceVariable + " =");
            currentLines.Add("    global::Cerneala.UI.Aspect.AspectPackage.Create(" + Literal(packageName) + ")");
            currentLines.Add(
                "    .Origin(new global::Cerneala.UI.Aspect.AspectOrigin(" +
                "global::Cerneala.UI.Aspect.AspectAuthoringKind.MarkupDefault, " +
                Literal(Path.GetFileName(file.Path)) + ", " + Literal(targetSymbol.Name) + "))");
            currentLines.Add("    .Components(components =>");
            currentLines.Add("    {");
            if (declarations.Count > 0)
            {
                currentLines.Add(
                    "        components.AddRule(new global::Cerneala.UI.Aspect.AspectRuleSet(" +
                    Literal(packageName + ".default") + ", global::Cerneala.UI.Aspect.AspectLayer.App, " +
                    "new global::Cerneala.UI.Aspect.AspectTarget(typeof(" + targetType + ")), " +
                    declarationsCode + ", 0));");
            }

            if (aspect.ReactivePlan is not null)
            {
                ReactiveRule[] rules = aspect.ReactivePlan.Rules.OrderBy(rule => rule.Order).ToArray();
                for (int index = 0; index < rules.Length; index++)
                {
                    List<string>? conditionalDeclarations = BuildConditionalAspectDeclarations(
                        targetSymbol.Name,
                        rules[index].Assignments);
                    if (conditionalDeclarations is null)
                    {
                        return;
                    }

                    string conditionalCode = conditionalDeclarations.Count == 0
                        ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.AspectDeclaration>()"
                        : "new global::Cerneala.UI.Aspect.AspectDeclaration[] { " +
                            string.Join(", ", conditionalDeclarations) + " }";
                    currentLines.Add(
                        "        components.AddRule(new global::Cerneala.UI.Aspect.AspectRuleSet(" +
                        Literal(packageName + ".condition." + rules[index].Order.ToString(CultureInfo.InvariantCulture)) +
                        ", global::Cerneala.UI.Aspect.AspectLayer.App, new global::Cerneala.UI.Aspect.AspectTarget(" +
                        "typeof(" + targetType + "), conditions: new global::Cerneala.UI.Aspect.AspectCondition[] { " +
                        "global::Cerneala.UI.Aspect.AspectCondition.Signal(" + aspect.ConditionKeyVariables[index] + ") }), " +
                        conditionalCode + ", " + (rules[index].Order + 1).ToString(CultureInfo.InvariantCulture) + "));");
                }
            }

            if (packageOwnsBehavior && aspect.BehaviorLines is not null)
            {
                currentLines.Add("        components.AddBehavior(new global::Cerneala.UI.Aspect.AspectBehavior(");
                currentLines.Add("            typeof(" + targetType + "), element =>");
                currentLines.Add("            {");
                currentLines.Add("                if (element is not " + targetType + " target)");
                currentLines.Add("                {");
                currentLines.Add("                    return null;");
                currentLines.Add("                }");
                foreach (string line in aspect.BehaviorLines.Concat(aspect.BehaviorPostLines!))
                {
                    currentLines.Add("                " + line);
                }

                string lifetimes = aspect.BehaviorLifetimeVariables.Count == 0
                    ? "global::System.Array.Empty<global::System.IDisposable?>()"
                    : "new global::System.IDisposable?[] { " + string.Join(", ", aspect.BehaviorLifetimeVariables) + " }";
                currentLines.Add("                return global::Cerneala.UI.Markup.GeneratedMarkup.CombineLifetimes(" + lifetimes + ");");
                currentLines.Add("            }));");
                aspect.BehaviorOwnedByPackage = true;
            }

            currentLines.Add("    })");
            currentLines.Add("    .Build();");
            currentLines.Add(ownerVariable + ".Resources[" + key + "] = " + resourceVariable + ";");
        }

        private void EmitNamedElementAspectResource(
            string ownerVariable,
            string key,
            string targetType,
            AspectResource aspect)
        {
            INamedTypeSymbol targetSymbol = ResolveAspectTargetTypeSymbol(aspect.TargetName, aspect.Source)!;
            if (HasRuntimeBehavior(aspect) && !PrepareAspectBehavior(targetType, aspect, includeMotion: false))
            {
                return;
            }

            string diagnosticName = aspect.Name!;
            EmitAspectConditionKeys(aspect, diagnosticName);
            string? valuesCode = BuildElementAspectValues(targetSymbol.Name, aspect);
            if (valuesCode is null)
            {
                return;
            }

            string resourceVariable = "elementAspectResource" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            aspect.RuntimeResourceVariable = resourceVariable;
            aspect.RuntimeVariable = resourceVariable;
            EmitElementAspectConstruction(resourceVariable, aspect.Name, targetType, targetSymbol.Name, valuesCode, aspect);
            currentLines.Add(ownerVariable + ".Resources[" + key + "] = " + resourceVariable + ";");
        }

        private void EmitElementAspectConstruction(
            string variable,
            string? name,
            string targetType,
            string elementName,
            string valuesCode,
            AspectResource aspect)
        {
            string? conditionsCode = BuildElementAspectConditionsCode(elementName, aspect);
            if (conditionsCode is null)
            {
                return;
            }

            currentLines.Add("global::Cerneala.UI.Aspect.ElementAspect " + variable + " = new(");
            currentLines.Add("    " + (name is null ? "null" : Literal(name)) + ", typeof(" + targetType + "), " + valuesCode + ",");
            currentLines.Add("    " + conditionsCode + ",");
            if (aspect.BehaviorLines is null)
            {
                currentLines.Add("    behaviorFactory: null,");
            }
            else
            {
                currentLines.Add("    element =>");
                currentLines.Add("    {");
                currentLines.Add("        if (element is not " + targetType + " target)");
                currentLines.Add("        {");
                currentLines.Add("            return null;");
                currentLines.Add("        }");
                foreach (string line in aspect.BehaviorLines.Concat(aspect.BehaviorPostLines!))
                {
                    currentLines.Add("        " + line);
                }

                string lifetimes = aspect.BehaviorLifetimeVariables.Count == 0
                    ? "global::System.Array.Empty<global::System.IDisposable?>()"
                    : "new global::System.IDisposable?[] { " + string.Join(", ", aspect.BehaviorLifetimeVariables) + " }";
                currentLines.Add("        return global::Cerneala.UI.Markup.GeneratedMarkup.CombineLifetimes(" + lifetimes + ");");
                currentLines.Add("    },");
            }

            currentLines.Add("    isConditional: " + (aspect.Conditions.Count > 0 ? "true" : "false") + ",");
            string authoringKind = aspect.Name is null ? "MarkupInline" : "MarkupNamed";
            currentLines.Add(
                "    origin: new global::Cerneala.UI.Aspect.AspectOrigin(" +
                "global::Cerneala.UI.Aspect.AspectAuthoringKind." + authoringKind + ", " +
                Literal(Path.GetFileName(file.Path)) + ", " +
                (aspect.Name is null ? "null" : Literal(aspect.Name)) + "));");
        }

        private string? BuildElementAspectConditionsCode(string elementName, AspectResource aspect)
        {
            if (aspect.ReactivePlan is null || aspect.ReactivePlan.Rules.Count == 0)
            {
                return "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectCondition>()";
            }

            List<string> conditions = [];
            ReactiveRule[] rules = aspect.ReactivePlan.Rules.OrderBy(rule => rule.Order).ToArray();
            for (int index = 0; index < rules.Length; index++)
            {
                List<string>? values = BuildConditionalElementAspectValues(elementName, rules[index].Assignments);
                if (values is null)
                {
                    return null;
                }

                string valuesCode = values.Count == 0
                    ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectValue>()"
                    : "new global::Cerneala.UI.Aspect.ElementAspectValue[] { " + string.Join(", ", values) + " }";
                conditions.Add(
                    "new global::Cerneala.UI.Aspect.ElementAspectCondition(" +
                    aspect.ConditionKeyVariables[index] + ", " + valuesCode + ", " +
                    rules[index].Order.ToString(CultureInfo.InvariantCulture) + ")");
            }

            return "new global::Cerneala.UI.Aspect.ElementAspectCondition[] { " + string.Join(", ", conditions) + " }";
        }

        private IReadOnlyList<AspectResource> ResolveAspects(MarkupElement element)
        {
            string elementName = element.Name.LocalName;
            List<AspectResource> resolved = [];
            if (TryResolveDefaultAspect(element, elementName, out AspectResource defaultAspect))
            {
                resolved.Add(defaultAspect);
            }

            if (inlineAspects.TryGetValue(element, out AspectResource? inlineAspect))
            {
                resolved.Add(inlineAspect);
                return resolved;
            }

            MarkupAttribute? aspectAttribute = element.Attribute("Aspect");
            if (aspectAttribute is null)
            {
                return resolved;
            }

            string referenceName = ReadReferenceName(elementName, "Aspect", aspectAttribute);
            if (referenceName.Length == 0)
            {
                return resolved;
            }

            if (!TryResolveResource(aspectAttribute, referenceName, out NamedSymbol symbol) ||
                symbol.Source is not AspectResource namedAspect)
            {
                Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
                return resolved;
            }

            INamedTypeSymbol? namedTargetType = ResolveAspectTargetTypeSymbol(namedAspect.TargetName, namedAspect.Source);
            INamedTypeSymbol? appliedElementType = ResolvePropertyOwnerType(elementName, ReferenceEquals(element, document.Root));
            if (namedTargetType is null || appliedElementType is null || !IsOrDerivesFrom(appliedElementType, namedTargetType))
            {
                Report(InvalidPropertyValue, aspectAttribute, elementName, "Aspect", aspectAttribute.Value);
                return resolved;
            }

            resolved.Add(namedAspect.RuntimeResourceVariable is null
                ? CloneAspectForApplication(namedAspect)
                : namedAspect);
            return resolved;
        }

        private static AspectResource CloneAspectForApplication(AspectResource aspect)
        {
            return new AspectResource(
                aspect.Name,
                aspect.TargetName,
                aspect.Assignments,
                aspect.Conditions,
                aspect.EventTriggers,
                aspect.Presence,
                aspect.Layout,
                aspect.Scrolls,
                aspect.Drag,
                aspect.GesturePress,
                aspect.Template,
                aspect.Source,
                isInline: true)
            {
                TemplateVariable = aspect.TemplateVariable
            };
        }

        private void ApplyAspects(MarkupElement element, string variable, IReadOnlyList<AspectResource> aspects)
        {
            foreach (AspectResource aspect in aspects)
            {
                if (aspect.Name is null && !aspect.IsInline)
                {
                    if (!aspect.BehaviorOwnedByPackage && HasRuntimeBehavior(aspect))
                    {
                        if (!ResolveMotionAspect(element, variable, aspect))
                        {
                            continue;
                        }

                        EmitMotionPresence(element, variable, aspect);
                        EmitMotionLayout(element, variable, aspect);
                        EmitMotionActivations(element, variable, aspect, bindToElementAspect: false);
                        if (aspect.Conditions.Count > 0)
                        {
                            if (aspect.ConditionKeyVariables.Count > 0)
                            {
                                ReactivePlan signalPlan = BuildAspectReactivePlan(aspect, variable, element.Name.LocalName);
                                foreach (ReactiveRule rule in signalPlan.Rules)
                                {
                                    rule.Activations = [];
                                }

                                signalPlan.Rules.RemoveAll(rule => rule.Assignments.Count == 0 && rule.Elements.Count == 0);
                                EmitReactivePlan(
                                    signalPlan,
                                    controlsContent: signalPlan.HasConditionalContent,
                                    aspectConditionKeys: aspect.ConditionKeyVariables,
                                    suppressValues: true);
                            }

                            ReactivePlan motionPlan = BuildAspectReactivePlan(aspect, variable, element.Name.LocalName);
                            motionPlan.Rules.RemoveAll(rule => rule.Activations.Count == 0 && rule.Elements.Count == 0);
                            EmitReactivePlan(
                                motionPlan,
                                controlsContent: motionPlan.HasConditionalContent,
                                suppressValues: true);
                        }
                    }

                    continue;
                }

                bool hasMotionBehavior = HasMotionBehavior(aspect);
                if (hasMotionBehavior)
                {
                    if (!ResolveMotionAspect(element, variable, aspect))
                    {
                        continue;
                    }

                    EmitMotionPresence(element, variable, aspect);
                    EmitMotionLayout(element, variable, aspect);
                    EmitMotionActivations(element, variable, aspect, bindToElementAspect: true);
                }

                if (aspect.RuntimeResourceVariable is not null)
                {
                    if (IsApplicationNamedAspect(aspect))
                    {
                        currentLines.Add(
                            "global::Cerneala.UI.Markup.GeneratedMarkup.AttachResource(" +
                            variable + ", " + variable + ", global::Cerneala.UI.Elements.UIElement.AspectProperty, " +
                            Literal(aspect.Name!) + ", global::Cerneala.UI.Core.UiPropertyValueSource.Local);");
                    }
                    else
                    {
                        aspect.RuntimeVariable = aspect.RuntimeResourceVariable;
                        currentLines.Add(variable + ".Aspect = " + aspect.RuntimeResourceVariable + ";");
                    }
                }
                else if (IsLocalAspect(aspect))
                {
                    EmitLocalAspect(element, variable, aspect);
                }
                else
                {
                    EmitAspectAssignments(element, variable, aspect);
                }

                if (hasMotionBehavior && aspect.Conditions.Count > 0)
                {
                    ReactivePlan motionPlan = BuildAspectReactivePlan(aspect, variable, element.Name.LocalName);
                    motionPlan.Rules.RemoveAll(rule => rule.Activations.Count == 0 && rule.Elements.Count == 0);
                    EmitReactivePlan(
                        motionPlan,
                        controlsContent: motionPlan.HasConditionalContent,
                        suppressValues: true);
                }

            }
        }

        private static bool IsLocalAspect(AspectResource aspect) => aspect.IsInline || aspect.Name is not null;

        private bool IsApplicationNamedAspect(AspectResource aspect) =>
            applicationResources is not null &&
            applicationResources.NamedResources.Values
                .OfType<NamedSymbol>()
                .Any(symbol => ReferenceEquals(symbol.Source, aspect));

        private void EmitLocalAspect(MarkupElement element, string variable, AspectResource aspect)
        {
            string elementName = element.Name.LocalName;
            string targetType = ResolveAspectTargetType(aspect.TargetName, aspect.Source)!;
            if (HasRuntimeBehavior(aspect) && !PrepareAspectBehavior(targetType, aspect, includeMotion: false))
            {
                return;
            }

            EmitAspectConditionKeys(aspect, (aspect.Name ?? "Inline") + "." + elementName);
            string? valuesCode = BuildElementAspectValues(elementName, aspect);
            if (valuesCode is null)
            {
                return;
            }

            string aspectVariable = "localAspect" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            aspect.RuntimeVariable = aspectVariable;
            EmitElementAspectConstruction(aspectVariable, aspect.Name, targetType, elementName, valuesCode, aspect);
            currentLines.Add(variable + ".Aspect = " + aspectVariable + ";");
        }

        private string? BuildElementAspectValues(string elementName, AspectResource aspect)
        {
            List<string> values = [];
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);
                if (expression is null)
                {
                    return null;
                }

                values.Add(BuildElementAspectValueCode(
                    spec,
                    expression,
                    resourceName: null));
            }

            if (aspect.TemplateVariable is not null)
            {
                values.Add(
                    "new global::Cerneala.UI.Aspect.ElementAspectValue(" +
                    "global::Cerneala.UI.Controls.Control.ComponentTemplateProperty, " + aspect.TemplateVariable + ")");
            }

            return values.Count == 0
                ? "global::System.Array.Empty<global::Cerneala.UI.Aspect.ElementAspectValue>()"
                : "new global::Cerneala.UI.Aspect.ElementAspectValue[] { " + string.Join(", ", values) + " }";
        }

        private List<string>? BuildAspectDeclarations(string elementName, AspectResource aspect)
        {
            List<string> declarations = [];
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable || !spec.IsUiProperty)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);
                if (expression is null)
                {
                    return null;
                }

                string valueCode = BuildAspectValueCode(
                    spec,
                    expression,
                    assignment.IsReference && spec.ValueKind == MarkupValueKind.Brush
                        ? assignment.RawValue
                        : expression.ApplicationResourceName);
                declarations.Add(
                    "new global::Cerneala.UI.Aspect.AspectDeclaration(" + spec.PropertyCode + ", " + valueCode +
                    ", diagnosticName: " + Literal(assignment.PropertyName) + ")");
            }

            if (aspect.TemplateVariable is not null)
            {
                declarations.Add(
                    "new global::Cerneala.UI.Aspect.AspectDeclaration(" +
                    "global::Cerneala.UI.Controls.Control.ComponentTemplateProperty, " +
                    "global::Cerneala.UI.Aspect.AspectValue<global::Cerneala.UI.Controls.Templates.ComponentTemplate?>.Literal(" +
                    aspect.TemplateVariable + "), diagnosticName: \"ComponentTemplate\")");
            }

            return declarations;
        }

        private List<string>? BuildConditionalAspectDeclarations(
            string elementName,
            IReadOnlyList<DirectiveAssignmentNode> assignments)
        {
            List<string> declarations = [];
            foreach (DirectiveAssignmentNode assignment in assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable || !spec.IsUiProperty)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                ParsedMarkupValue? binding = ParseMarkupBindingValue(
                    assignment.Value,
                    assignment: true,
                    stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                    assignment.ValueLocation,
                    requireExplicitMode: true);
                if (binding is not null)
                {
                    Report(
                        InvalidBindingSource,
                        assignment.ValueLocation,
                        assignment.Value,
                        "Conditional Aspect assignment bindings must be lowered as computed Aspect values.");
                    return null;
                }

                GeneratedExpression? expression = ParseDirectiveValue(
                    elementName,
                    assignment.PropertyName,
                    assignment.Value,
                    spec,
                    assignment.Source,
                    "target");
                if (expression is null)
                {
                    return null;
                }

                declarations.Add(
                    "new global::Cerneala.UI.Aspect.AspectDeclaration(" + spec.PropertyCode + ", " +
                    BuildAspectValueCode(spec, expression, ConditionalResourceName(assignment, spec)) + ", diagnosticName: " +
                    Literal(assignment.PropertyName) + ")");
            }

            return declarations;
        }

        private List<string>? BuildConditionalElementAspectValues(
            string elementName,
            IReadOnlyList<DirectiveAssignmentNode> assignments)
        {
            List<string> values = [];
            foreach (DirectiveAssignmentNode assignment in assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, isRoot: false);
                if (spec is null || !spec.Assignable || !spec.IsUiProperty)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return null;
                }

                ParsedMarkupValue? binding = ParseMarkupBindingValue(
                    assignment.Value,
                    assignment: true,
                    stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                    assignment.ValueLocation,
                    requireExplicitMode: true);
                if (binding is not null)
                {
                    Report(
                        InvalidBindingSource,
                        assignment.ValueLocation,
                        assignment.Value,
                        "Conditional ElementAspect assignment bindings must be lowered as computed Aspect values.");
                    return null;
                }

                GeneratedExpression? expression = ParseDirectiveValue(
                    elementName,
                    assignment.PropertyName,
                    assignment.Value,
                    spec,
                    assignment.Source,
                    "target");
                if (expression is null)
                {
                    return null;
                }

                values.Add(BuildElementAspectValueCode(
                    spec,
                    expression,
                    resourceName: null));
            }

            return values;
        }

        private static string BuildAspectValueCode(
            PropertySpec spec,
            GeneratedExpression expression,
            string? resourceName)
        {
            string valueType = spec.ValueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return resourceName is null
                ? "global::Cerneala.UI.Aspect.AspectValue<" + valueType + ">.Literal(" + expression.Code + ")"
                : "global::Cerneala.UI.Aspect.AspectValue<" + valueType + ">.Computed(" +
                    "context => context.Element.FindResource<" + valueType + ">(" +
                    Literal(resourceName) + "), " +
                    "global::System.Array.Empty<global::Cerneala.UI.Aspect.AspectToken>())";
        }

        private static string BuildElementAspectValueCode(
            PropertySpec spec,
            GeneratedExpression expression,
            string? resourceName)
        {
            string valueCode = resourceName is null
                ? expression.Code
                : BuildAspectValueCode(spec, expression, resourceName);
            return "new global::Cerneala.UI.Aspect.ElementAspectValue(" + spec.PropertyCode + ", " + valueCode + ")";
        }

        private static string? ConditionalResourceName(
            DirectiveAssignmentNode assignment,
            PropertySpec spec)
        {
            string value = assignment.Value.Trim();
            return spec.ValueKind == MarkupValueKind.Brush &&
                value.StartsWith("$", StringComparison.Ordinal) &&
                !LooksLikeBindingPath(value)
                ? value.Substring(1)
                : null;
        }

        private void EmitAspectAssignments(
            MarkupElement element,
            string variable,
            AspectResource aspect,
            string valueSource = "global::Cerneala.UI.Core.UiPropertyValueSource.AspectBase")
        {
            string elementName = element.Name.LocalName;
            foreach (AspectPropertyAssignment assignment in aspect.Assignments)
            {
                PropertySpec? spec = FindPropertySpec(elementName, assignment.PropertyName, ReferenceEquals(element, document.Root));
                if (spec is null)
                {
                    Report(UnsupportedProperty, assignment.Source, elementName, assignment.PropertyName);
                    return;
                }

                GeneratedExpression? expression = assignment.IsReference
                    ? ResolveReferenceValue(elementName, assignment.PropertyName, assignment.RawValue, spec.ValueKind, assignment.Source)
                    : ParseAspectLiteralValue(elementName, assignment.PropertyName, assignment.RawValue, spec, assignment.Source);

                if (expression is null)
                {
                    return;
                }

                if (expression.ApplicationResourceName is not null && spec.IsUiProperty)
                {
                    EmitApplicationResourceBinding(
                        variable,
                        spec,
                        expression.ApplicationResourceName,
                        valueSource);
                    continue;
                }

                currentLines.Add(spec.IsUiProperty
                    ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                        ", " + valueSource + ");"
                    : variable + "." + spec.Name + " = " + expression.Code + ";");
            }

            if (aspect.TemplateVariable is not null)
            {
                currentLines.Add(
                    variable + ".SetValue(global::Cerneala.UI.Controls.Control.ComponentTemplateProperty, " +
                    aspect.TemplateVariable + ", " + valueSource + ");");
            }
        }

        private GeneratedExpression? ParseAspectLiteralValue(string elementName, string propertyName, string value, PropertySpec spec, MarkupObject source)
        {
            MarkupAttribute synthetic = new(propertyName, value);
            return ParseLiteralValue(elementName, propertyName, synthetic, value, spec);
        }

        private bool TryResolveDefaultAspect(MarkupObject source, string targetName, out AspectResource aspect)
        {
            bool isRoot = source is MarkupElement element && ReferenceEquals(element, document.Root);
            INamedTypeSymbol? appliedElementType = ResolvePropertyOwnerType(targetName, isRoot);
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (scope.DefaultAspectsByTarget.TryGetValue(targetName, out aspect))
                {
                    return true;
                }

                if (appliedElementType is not null && TryResolveNearestDefaultAspect(
                    scope.DefaultAspectsByTarget.Values,
                    appliedElementType,
                    out aspect))
                {
                    return true;
                }
            }

            aspect = null!;
            return false;
        }

        private bool TryResolveNearestDefaultAspect(
            IEnumerable<AspectResource> candidates,
            INamedTypeSymbol appliedElementType,
            out AspectResource aspect)
        {
            AspectResource? nearest = null;
            int nearestDistance = int.MaxValue;
            foreach (AspectResource candidate in candidates)
            {
                INamedTypeSymbol? candidateType = ResolveAspectTargetTypeSymbol(candidate.TargetName, candidate.Source);
                int distance = candidateType is null
                    ? -1
                    : GetBaseTypeDistance(appliedElementType, candidateType);
                if (distance < 0 || distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            aspect = nearest!;
            return nearest is not null;
        }

        private static int GetBaseTypeDistance(INamedTypeSymbol type, INamedTypeSymbol candidateBaseType)
        {
            int distance = 0;
            for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, candidateBaseType))
                {
                    return distance;
                }

                distance++;
            }

            return -1;
        }
    }
}
