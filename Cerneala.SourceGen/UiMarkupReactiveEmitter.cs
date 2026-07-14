using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private sealed class ReactivePlan
        {
            public ReactivePlan(
                string ownerVariable,
                string elementName,
                bool isRoot = false,
                TemplateEmissionContext? templateContext = null)
            {
                OwnerVariable = ownerVariable;
                ElementName = elementName;
                IsRoot = isRoot;
                TemplateContext = templateContext;
            }

            public string OwnerVariable { get; }

            public string ElementName { get; }

            public bool IsRoot { get; }

            public TemplateEmissionContext? TemplateContext { get; }

            public List<string> ObservationLines { get; } = [];

            public List<string> ObservationNames { get; } = [];

            public Dictionary<string, ObservationEmission> Observations { get; } = new(StringComparer.Ordinal);

            public List<ReactiveRule> Rules { get; } = [];

            public int NextOrder { get; set; }

            public bool HasConditionalContent => Rules.Any(rule => rule.Elements.Count > 0);
        }

        private sealed class ReactiveRule
        {
            public ReactiveRule(
                int order,
                string predicate,
                IReadOnlyList<DirectiveAssignmentNode> assignments,
                IReadOnlyList<DirectiveElementNode> elements,
                string valueSource)
            {
                Order = order;
                Predicate = predicate;
                Assignments = assignments;
                Elements = elements;
                ValueSource = valueSource;
            }

            public int Order { get; }

            public string Predicate { get; }

            public IReadOnlyList<DirectiveAssignmentNode> Assignments { get; }

            public IReadOnlyList<DirectiveElementNode> Elements { get; }

            public string ValueSource { get; }

            public string? StaticContentExpression { get; set; }
        }

        private sealed class ObservationEmission
        {
            public ObservationEmission(
                string name,
                string valueCode,
                MarkupValueKind? markupKind,
                ITypeSymbol? valueType,
                string? valueGuard = null,
                string? rawValueCode = null)
            {
                Name = name;
                ValueCode = valueCode;
                MarkupKind = markupKind;
                ValueType = valueType;
                ValueGuard = valueGuard;
                RawValueCode = rawValueCode ?? name + ".Value";
            }

            public string Name { get; }

            public string ValueCode { get; }

            public MarkupValueKind? MarkupKind { get; }

            public ITypeSymbol? ValueType { get; }

            public string? ValueGuard { get; }

            public string RawValueCode { get; }
        }

        private sealed class BoundDirectiveExpression
        {
            public BoundDirectiveExpression(
                string code,
                string rawValueCode,
                ITypeSymbol type,
                MarkupValueKind? markupKind = null,
                string? valueGuard = null)
            {
                Code = code;
                RawValueCode = rawValueCode;
                Type = type;
                MarkupKind = markupKind;
                ValueGuard = valueGuard;
            }

            public string Code { get; }

            public string RawValueCode { get; }

            public ITypeSymbol Type { get; }

            public MarkupValueKind? MarkupKind { get; }

            public string? ValueGuard { get; }
        }

        private DirectiveParseResult GetDirectiveContent(XElement element, DirectiveContentKind allowedContent)
        {
            if (!directiveContent.TryGetValue(element, out DirectiveParseResult? parsed))
            {
                parsed = ParseDirectiveContent(element, allowedContent);
                directiveContent.Add(element, parsed);
            }

            return parsed;
        }

        private void EmitReactiveContent(
            XElement element,
            string variable,
            DirectiveParseResult parsed,
            IReadOnlyList<AspectResource> aspects)
        {
            ReactivePlan plan = new(
                variable,
                element.Name.LocalName,
                ReferenceEquals(element, document.Root),
                templateEmissionContexts.Count == 0 ? null : templateEmissionContexts.Peek());
            foreach (AspectResource aspect in aspects)
            {
                string valueSource = aspect.IsInline
                    ? "global::Cerneala.UI.Core.UiPropertyValueSource.LocalAspectConditional"
                    : "global::Cerneala.UI.Core.UiPropertyValueSource.AspectVisualState";
                string inheritedPredicate = "true";
                if (aspect.IsInline && aspect.Conditions.Count > 0)
                {
                    string observationName = "observation" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                    nextReactiveId++;
                    plan.ObservationLines.Add(
                        "global::Cerneala.UI.Markup.MarkupObservation " + observationName +
                        " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveProperty(" + plan.OwnerVariable +
                        ", global::Cerneala.UI.Elements.UIElement.AspectProperty);");
                    plan.ObservationNames.Add(observationName);
                    inheritedPredicate =
                        "global::System.Object.ReferenceEquals(" + plan.OwnerVariable + ".Aspect, " + aspect.RuntimeVariable + ")";
                }

                foreach (DirectiveWhenNode when in aspect.Conditions)
                {
                    CollectWhen(plan, when, inheritedPredicate, valueSource);
                }
            }

            foreach (DirectiveNode node in parsed.Nodes)
            {
                switch (node)
                {
                    case DirectiveWhenNode directiveWhen:
                        CollectWhen(plan, directiveWhen, "true", "global::Cerneala.UI.Core.UiPropertyValueSource.MarkupConditional");
                        break;
                    case DirectiveDefaultNode defaults:
                        Report(InvalidDirective, defaults.Source, Path.GetFileName(file.Path), "@default is valid only inside Aspect resources.");
                        break;
                    case DirectiveTextNode text:
                        EmitTextContent(element, variable, text.Text);
                        break;
                    case DirectiveElementNode child:
                        {
                            if (IsNonContentPropertyElement(element, child.Element))
                            {
                                break;
                            }

                            string childVariable = EmitElement(child.Element);
                            ReactiveRule staticRule = new(
                                plan.NextOrder++,
                                "true",
                                [],
                                [],
                                "global::Cerneala.UI.Core.UiPropertyValueSource.MarkupConditional")
                            {
                                StaticContentExpression = "new global::Cerneala.UI.Elements.UIElement[] { " + childVariable + " }"
                            };
                            plan.Rules.Add(staticRule);
                            break;
                        }
                    case DirectiveTemplateNode _:
                        break;
                    case DirectiveAssignmentNode assignment:
                        Report(InvalidDirective, assignment.Source, Path.GetFileName(file.Path), "Property assignments must be inside an @if block.");
                        break;
                }
            }

            if (plan.HasConditionalContent)
            {
                EmitReactivePlan(plan, controlsContent: true);
            }
            else
            {
                foreach (ReactiveRule staticRule in plan.Rules.Where(rule => rule.StaticContentExpression is not null))
                {
                    string childVariable = staticRule.StaticContentExpression!
                        .Replace("new global::Cerneala.UI.Elements.UIElement[] { ", string.Empty)
                        .Replace(" }", string.Empty);
                    EmitChild(element, variable, childVariable);
                }

                plan.Rules.RemoveAll(rule => rule.StaticContentExpression is not null);
                EmitReactivePlan(plan, controlsContent: false);
            }
        }

        private void CollectWhen(ReactivePlan plan, DirectiveWhenNode when, string inheritedPredicate, string valueSource)
        {
            if (!IsWhenExpression(when.Expression))
            {
                Report(
                    InvalidBindingSource,
                    when.Expression.Location,
                    DescribeExpression(when.Expression),
                    "@when accepts source expressions combined only with 'and', 'or' and parentheses.");
                return;
            }

            BoundDirectiveExpression? whenExpression = BindDirectiveExpression(plan, when.Expression, null);
            if (whenExpression is null)
            {
                return;
            }

            if (when.BooleanBody is not null)
            {
                string? booleanPredicate = RequireBooleanPredicate(whenExpression, when.Expression.Location, DescribeExpression(when.Expression));
                if (booleanPredicate is null)
                {
                    return;
                }

                string predicate = CombinePredicates(inheritedPredicate, booleanPredicate);
                CollectRuleBody(plan, when.BooleanBody, predicate, valueSource);
                return;
            }

            foreach (DirectiveIfNode branch in when.Branches)
            {
                if (!IsIfExpression(branch.Expression))
                {
                    Report(
                        InvalidBindingSource,
                        branch.Expression.Location,
                        DescribeExpression(branch.Expression),
                        "@if requires a comparison or comparisons combined with 'and' and 'or'.");
                    continue;
                }

                BoundDirectiveExpression? branchExpression = BindDirectiveExpression(plan, branch.Expression, whenExpression);
                if (branchExpression is null)
                {
                    continue;
                }

                string? comparison = RequireBooleanPredicate(
                    branchExpression,
                    branch.Expression.Location,
                    DescribeExpression(branch.Expression));
                if (comparison is null)
                {
                    continue;
                }

                string predicate = CombinePredicates(inheritedPredicate, comparison);
                CollectRuleBody(plan, branch.Body, predicate, valueSource);
            }
        }

        private BoundDirectiveExpression? BindDirectiveExpression(
            ReactivePlan plan,
            DirectiveExpression expression,
            BoundDirectiveExpression? valueExpression)
        {
            switch (expression)
            {
                case DirectiveSourceExpression source:
                    {
                        ObservationEmission? observation = EmitObservation(plan, source.Text, source.Location.Source, source.Location);
                        return observation is null
                            ? null
                            : new BoundDirectiveExpression(
                                observation.ValueCode,
                                observation.RawValueCode,
                                observation.ValueType!,
                                observation.MarkupKind,
                                observation.ValueGuard);
                    }
                case DirectiveValueExpression value:
                    if (valueExpression is null)
                    {
                        Report(InvalidBindingSource, value.Location, "value", "'value' is available only inside @if.");
                    }

                    return valueExpression;
                case DirectiveLiteralExpression literal:
                    Report(
                        InvalidBindingSource,
                        literal.Location,
                        literal.Text,
                        "A literal must be used as a comparison operand.");
                    return null;
                case DirectiveGroupExpression group:
                    return BindDirectiveExpression(plan, group.Inner, valueExpression);
                case DirectiveComparisonExpression comparison:
                    return BindComparison(plan, comparison, valueExpression);
                case DirectiveLogicalExpression logical:
                    return BindLogicalExpression(plan, logical, valueExpression);
                default:
                    throw new InvalidOperationException("Unsupported directive expression node.");
            }
        }

        private BoundDirectiveExpression? BindLogicalExpression(
            ReactivePlan plan,
            DirectiveLogicalExpression logical,
            BoundDirectiveExpression? valueExpression)
        {
            BoundDirectiveExpression? left = BindDirectiveExpression(plan, logical.Left, valueExpression);
            BoundDirectiveExpression? right = BindDirectiveExpression(plan, logical.Right, valueExpression);
            if (left is null || right is null)
            {
                return null;
            }

            string? leftPredicate = RequireBooleanPredicate(left, logical.Left.Location, DescribeExpression(logical.Left));
            string? rightPredicate = RequireBooleanPredicate(right, logical.Right.Location, DescribeExpression(logical.Right));
            if (leftPredicate is null || rightPredicate is null)
            {
                return null;
            }

            string logicalOperator = logical.Operator == DirectiveLogicalOperator.And ? "&&" : "||";
            string code = "(" + leftPredicate + " " + logicalOperator + " " + rightPredicate + ")";
            return new BoundDirectiveExpression(
                code,
                code,
                compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        private BoundDirectiveExpression? BindComparison(
            ReactivePlan plan,
            DirectiveComparisonExpression comparison,
            BoundDirectiveExpression? valueExpression)
        {
            BoundDirectiveExpression? left = BindDirectiveExpression(plan, comparison.Left, valueExpression);
            if (left is null)
            {
                return null;
            }

            if (comparison.Right is DirectiveLiteralExpression literal)
            {
                return BindLiteralComparison(left, comparison.Comparator, literal.Text, comparison.Location);
            }

            if (comparison.Right is DirectiveSourceExpression enumMember &&
                left.Type.TypeKind == TypeKind.Enum &&
                !enumMember.Text.StartsWith("$", StringComparison.Ordinal))
            {
                return BindLiteralComparison(left, comparison.Comparator, enumMember.Text, enumMember.Location);
            }

            BoundDirectiveExpression? right = BindDirectiveExpression(plan, comparison.Right, valueExpression);
            if (right is null)
            {
                return null;
            }

            if (!SymbolEqualityComparer.Default.Equals(UnwrapNullable(left.Type), UnwrapNullable(right.Type)))
            {
                Report(
                    InvalidBindingSource,
                    comparison.Right.Location,
                    DescribeExpression(comparison.Right),
                    "Both expressions must have the same type.");
                return null;
            }

            if ((comparison.Comparator is "<" or "<=" or ">" or ">=") && !SupportsOrdering(left))
            {
                Report(
                    InvalidBindingSource,
                    comparison.Location,
                    comparison.Comparator,
                    "The observed value type does not support ordering comparisons.");
                return null;
            }

            string comparisonCode = left.Type.SpecialType == SpecialType.System_String &&
                comparison.Comparator is "<" or "<=" or ">" or ">="
                ? "global::System.String.CompareOrdinal(" + left.Code + ", " + right.Code + ") " + comparison.Comparator + " 0"
                : left.Code + " " + comparison.Comparator + " " + right.Code;
            return BooleanExpression(GuardComparison(left, right, comparisonCode));
        }

        private BoundDirectiveExpression? BindLiteralComparison(
            BoundDirectiveExpression left,
            string comparator,
            string operand,
            DirectiveExpressionLocation location)
        {
            if (string.Equals(operand, "Null", StringComparison.OrdinalIgnoreCase))
            {
                if (comparator is not "==" and not "!=")
                {
                    Report(InvalidBindingSource, location, operand, "Null supports only == and !=.");
                    return null;
                }

                return BooleanExpression("(" + left.RawValueCode + " " + comparator + " null)");
            }

            string? operandCode = ParseSymbolLiteral(left.Type, operand, location);
            if (operandCode is null)
            {
                return null;
            }

            if ((comparator is "<" or "<=" or ">" or ">=") && !SupportsOrdering(left))
            {
                Report(
                    InvalidBindingSource,
                    location,
                    comparator,
                    "The observed value type does not support ordering comparisons.");
                return null;
            }

            string comparisonCode = left.Type.SpecialType == SpecialType.System_String &&
                comparator is "<" or "<=" or ">" or ">="
                ? "global::System.String.CompareOrdinal(" + left.Code + ", " + operandCode + ") " + comparator + " 0"
                : left.Code + " " + comparator + " " + operandCode;
            return BooleanExpression(GuardComparison(left, null, comparisonCode));
        }

        private BoundDirectiveExpression BooleanExpression(string code)
        {
            return new BoundDirectiveExpression(
                code,
                code,
                compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        private string? RequireBooleanPredicate(
            BoundDirectiveExpression expression,
            object location,
            string display)
        {
            if (UnwrapNullable(expression.Type).SpecialType != SpecialType.System_Boolean)
            {
                Report(InvalidBindingSource, location, display, "Logical expression leaves must be Boolean.");
                return null;
            }

            return expression.ValueGuard is null
                ? "(" + expression.Code + ")"
                : "((" + expression.ValueGuard + ") && (" + expression.Code + "))";
        }

        private static string GuardComparison(
            BoundDirectiveExpression left,
            BoundDirectiveExpression? right,
            string comparison)
        {
            List<string> terms = [];
            if (left.ValueGuard is not null)
            {
                terms.Add("(" + left.ValueGuard + ")");
            }

            if (right?.ValueGuard is not null)
            {
                terms.Add("(" + right.ValueGuard + ")");
            }

            terms.Add("(" + comparison + ")");
            return "(" + string.Join(" && ", terms) + ")";
        }

        private static string CombinePredicates(string inheritedPredicate, string predicate)
        {
            return inheritedPredicate == "true"
                ? "(" + predicate + ")"
                : "((" + inheritedPredicate + ") && (" + predicate + "))";
        }

        private static bool IsWhenExpression(DirectiveExpression expression)
        {
            return expression switch
            {
                DirectiveSourceExpression => true,
                DirectiveGroupExpression group => IsWhenExpression(group.Inner),
                DirectiveLogicalExpression logical =>
                    IsWhenExpression(logical.Left) && IsWhenExpression(logical.Right),
                _ => false
            };
        }

        private static bool IsIfExpression(DirectiveExpression expression)
        {
            return expression switch
            {
                DirectiveComparisonExpression => true,
                DirectiveGroupExpression group => IsIfExpression(group.Inner),
                DirectiveLogicalExpression logical =>
                    IsIfExpression(logical.Left) && IsIfExpression(logical.Right),
                _ => false
            };
        }

        private static string DescribeExpression(DirectiveExpression expression)
        {
            return expression switch
            {
                DirectiveSourceExpression source => source.Text,
                DirectiveValueExpression => "value",
                DirectiveLiteralExpression literal => literal.Text,
                DirectiveComparisonExpression comparison =>
                    DescribeExpression(comparison.Left) + " " + comparison.Comparator + " " + DescribeExpression(comparison.Right),
                DirectiveLogicalExpression logical =>
                    DescribeExpression(logical.Left) +
                    (logical.Operator == DirectiveLogicalOperator.And ? " and " : " or ") +
                    DescribeExpression(logical.Right),
                DirectiveGroupExpression group => "(" + DescribeExpression(group.Inner) + ")",
                _ => "expression"
            };
        }

        private void CollectRuleBody(
            ReactivePlan plan,
            IReadOnlyList<DirectiveNode> body,
            string predicate,
            string valueSource)
        {
            List<DirectiveAssignmentNode> assignments = body.OfType<DirectiveAssignmentNode>().ToList();
            List<DirectiveElementNode> elements = body.OfType<DirectiveElementNode>().ToList();
            if (assignments.Count > 0 || elements.Count > 0)
            {
                plan.Rules.Add(new ReactiveRule(plan.NextOrder++, predicate, assignments, elements, valueSource));
            }

            foreach (DirectiveWhenNode nested in body.OfType<DirectiveWhenNode>())
            {
                CollectWhen(plan, nested, predicate, valueSource);
            }

            foreach (DirectiveDefaultNode invalid in body.OfType<DirectiveDefaultNode>())
            {
                Report(InvalidDirective, invalid.Source, Path.GetFileName(file.Path), "@default cannot appear inside @when or @if.");
            }

            foreach (DirectiveTextNode text in body.OfType<DirectiveTextNode>())
            {
                Report(InvalidDirective, text.Source, Path.GetFileName(file.Path), "Raw text is not supported inside @when or @if; use a property assignment or XML element.");
            }
        }

        private ObservationEmission? EmitObservation(
            ReactivePlan plan,
            string expression,
            XObject source,
            object diagnosticSource)
        {
            BindingResolutionContext resolutionContext = new(
                plan.OwnerVariable,
                plan.ElementName,
                plan.IsRoot,
                plan.TemplateContext);
            BindingSourceDescriptor? descriptor = ResolveBindingSource(
                resolutionContext,
                expression,
                source,
                diagnosticSource);
            if (descriptor is null)
            {
                return null;
            }

            if (plan.Observations.TryGetValue(descriptor.CanonicalExpression, out ObservationEmission? existing))
            {
                return existing;
            }

            string name = "observation" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            ObservationEmission emission = EmitObservationDescriptor(
                plan.ObservationLines,
                plan.ObservationNames,
                name,
                descriptor);
            plan.Observations.Add(descriptor.CanonicalExpression, emission);

            return emission;
        }

        private ObservationEmission EmitObservationDescriptor(
            ICollection<string> observationLines,
            ICollection<string>? observationNames,
            string name,
            BindingSourceDescriptor descriptor)
        {
            switch (descriptor.Kind)
            {
                case BindingSourceKind.DataPath:
                {
                    List<string> segments = [];
                    for (int index = 0; index < descriptor.DataSegments.Count; index++)
                    {
                        DataPathSegmentDescriptor segment = descriptor.DataSegments[index];
                        string ownerType = segment.OwnerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        string propertyType = segment.Property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        string setter = index == descriptor.DataSegments.Count - 1 && descriptor.CanWrite
                            ? ", (owner, value) => ((" + ownerType + ")owner!)." + segment.Property.Name +
                                " = (" + propertyType + ")value!"
                            : string.Empty;
                        segments.Add(
                            "new global::Cerneala.UI.Markup.MarkupDataPathSegment(" + Literal(segment.Property.Name) +
                            ", value => ((" + ownerType + ")value!)." + segment.Property.Name + setter + ")");
                    }

                    observationLines.Add(
                        "global::Cerneala.UI.Markup.MarkupObservation " + name +
                        " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveDataPath(" + descriptor.OwnerCode +
                        (segments.Count == 0 ? ")" : ", " + string.Join(", ", segments) + ")") + ";");

                    ITypeSymbol comparisonType = UnwrapNullable(descriptor.ValueType);
                    string typeCode = comparisonType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    observationNames?.Add(name);
                    return new ObservationEmission(
                        name,
                        "(" + typeCode + ")" + name + ".Value!",
                        null,
                        comparisonType,
                        name + ".Value is " + typeCode);
                }
                case BindingSourceKind.UiProperty:
                {
                    PropertySpec spec = descriptor.Property!;
                    observationLines.Add(
                        "global::Cerneala.UI.Markup.MarkupObservation " + name +
                        " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveProperty(" + descriptor.OwnerCode +
                        ", " + spec.PropertyCode + ");");
                    observationNames?.Add(name);
                    return new ObservationEmission(
                        name,
                        "(" + spec.ValueTypeCode + ")" + name + ".Value!",
                        spec.ValueKind,
                        spec.ValueType);
                }
                case BindingSourceKind.TemplatePartProperty:
                {
                    PropertySpec spec = descriptor.Property!;
                    observationLines.Add(
                        "global::Cerneala.UI.Markup.MarkupObservation " + name +
                        " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveTemplatePartProperty(" + descriptor.OwnerCode +
                        ", " + Literal(descriptor.PartName!) + ", " + spec.PropertyCode + ");");
                    observationNames?.Add(name);
                    return new ObservationEmission(
                        name,
                        "(" + spec.ValueTypeCode + ")" + name + ".Value!",
                        spec.ValueKind,
                        spec.ValueType);
                }
                case BindingSourceKind.Object:
                {
                    observationLines.Add(
                        "global::Cerneala.UI.Markup.MarkupObservation " + name +
                        " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveObject(() => (object?)" + descriptor.OwnerCode + ");");
                    observationNames?.Add(name);
                    return new ObservationEmission(
                        name,
                        name + ".Value",
                        null,
                        compilation.GetSpecialType(SpecialType.System_Object));
                }
                default:
                    throw new InvalidOperationException("Unsupported binding source descriptor.");
            }
        }

        private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
        {
            return type is INamedTypeSymbol named &&
                named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? named.TypeArguments[0]
                : type;
        }

        private GeneratedExpression? ParseDirectiveValue(
            string? planElementName,
            string propertyName,
            string rawValue,
            PropertySpec spec,
            XObject source)
        {
            string value = rawValue.Trim();
            if (value.StartsWith("$", StringComparison.Ordinal))
            {
                return ResolveReferenceValue(planElementName ?? "value", propertyName, value.Substring(1), spec.ValueKind, source);
            }

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            if (spec.ValueType.SpecialType == SpecialType.System_String)
            {
                value = UnescapeMarkupDollar(value);
            }

            XAttribute synthetic = new(propertyName, value);
            return ParseLiteralValue(planElementName ?? "value", propertyName, synthetic, value, spec);
        }

        private string? ParseSymbolLiteral(ITypeSymbol type, string rawValue, object source)
        {
            string value = rawValue.Trim();
            if (type.SpecialType == SpecialType.System_String)
            {
                if (value.Length < 2 || value[0] != '"' || value[value.Length - 1] != '"')
                {
                    Report(InvalidBindingSource, source, rawValue, "String operands must be quoted.");
                    return null;
                }

                return Literal(value.Substring(1, value.Length - 2));
            }

            if (type.SpecialType == SpecialType.System_Boolean && bool.TryParse(value, out bool boolean))
            {
                return boolean ? "true" : "false";
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                IFieldSymbol? member = type.GetMembers(value).OfType<IFieldSymbol>().FirstOrDefault(field => field.HasConstantValue);
                if (member is not null)
                {
                    return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + member.Name;
                }
            }

            if (IsNumeric(type) && decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                return NumericLiteral(type.SpecialType, value);
            }

            Report(InvalidBindingSource, source, rawValue, "Operand is not valid for type '" + type.ToDisplayString() + "'.");
            return null;
        }

        private static string NumericLiteral(SpecialType type, string value)
        {
            return type switch
            {
                SpecialType.System_Single => value + "f",
                SpecialType.System_Double => value + "d",
                SpecialType.System_Decimal => value + "m",
                _ => value
            };
        }

        private static bool SupportsOrdering(BoundDirectiveExpression expression)
        {
            if (expression.MarkupKind is MarkupValueKind.Float or MarkupValueKind.NonNegativeFloat or MarkupValueKind.PositiveFloat)
            {
                return true;
            }

            return IsNumeric(expression.Type) || expression.Type.SpecialType == SpecialType.System_String;
        }

        private static bool IsNumeric(ITypeSymbol type)
        {
            return type.SpecialType is
                SpecialType.System_Byte or
                SpecialType.System_SByte or
                SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Decimal;
        }

        private void EmitReactivePlan(ReactivePlan plan, bool controlsContent)
        {
            if (plan.Rules.Count == 0)
            {
                return;
            }

            foreach (string observationLine in plan.ObservationLines)
            {
                currentPostLines.Add(observationLine);
            }

            List<string> ruleExpressions = [];
            foreach (ReactiveRule rule in plan.Rules.OrderBy(rule => rule.Order))
            {
                List<string> values = [];
                foreach (DirectiveAssignmentNode assignment in rule.Assignments)
                {
                    PropertySpec? spec = FindPropertySpec(plan.ElementName, assignment.PropertyName, plan.IsRoot);
                    if (spec is null || !spec.Assignable)
                    {
                        Report(UnsupportedProperty, assignment.Source, plan.ElementName, assignment.PropertyName);
                        continue;
                    }

                    ParsedMarkupValue? parsedMarkup = ParseMarkupBindingValue(
                        assignment.Value,
                        assignment: true,
                        stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                        assignment.ValueLocation);
                    if (parsedMarkup?.Kind == ParsedMarkupValueKind.Invalid)
                    {
                        continue;
                    }

                    if (parsedMarkup is not null)
                    {
                        BindingResolutionContext bindingContext = new(
                            plan.OwnerVariable,
                            plan.ElementName,
                            plan.IsRoot,
                            plan.TemplateContext,
                            validateClrObservability: true);
                        ResolvedMarkupValue? resolvedMarkup = ResolveMarkupValue(
                            bindingContext,
                            spec,
                            parsedMarkup,
                            assignment.Source,
                            assignment.ValueLocation);
                        if (resolvedMarkup is null)
                        {
                            continue;
                        }

                        values.Add(EmitConditionalMarkupBinding(
                            bindingContext,
                            spec,
                            resolvedMarkup,
                            plan.ElementName + "." + assignment.PropertyName + " <- " + assignment.Value));
                        continue;
                    }

                    GeneratedExpression? expression = ParseDirectiveValue(plan.ElementName, assignment.PropertyName, assignment.Value, spec, assignment.Source);
                    if (expression is null)
                    {
                        continue;
                    }

                    values.Add(
                        "new global::Cerneala.UI.Markup.MarkupConditionalValue(" + plan.OwnerVariable + ", " +
                        spec.PropertyCode + ", " + expression.Code + ", " + rule.ValueSource + ")");
                }

                string valuesCode = values.Count == 0
                    ? "global::System.Array.Empty<global::Cerneala.UI.Markup.MarkupConditionalValue>()"
                    : "new global::Cerneala.UI.Markup.MarkupConditionalValue[] { " + string.Join(", ", values) + " }";
                string contentCode = "null";
                if (rule.StaticContentExpression is not null)
                {
                    contentCode = "new global::Cerneala.UI.Markup.MarkupConditionalContent(" + rule.Order + ", () => " + rule.StaticContentExpression + ")";
                }
                else if (rule.Elements.Count > 0)
                {
                    string factoryName = EmitConditionalFactory(rule.Elements);
                    contentCode = EmitConditionalContentExpression(rule.Order, factoryName);
                }

                ruleExpressions.Add(
                    "new global::Cerneala.UI.Markup.MarkupConditionRule(" + rule.Order + ", () => " + rule.Predicate +
                    ", " + valuesCode + ", " + contentCode + ")");
            }

            string observations = plan.ObservationLines.Count == 0
                ? "global::System.Array.Empty<global::Cerneala.UI.Markup.MarkupObservation>()"
                : "new global::Cerneala.UI.Markup.MarkupObservation[] { " +
                    string.Join(", ", plan.ObservationNames) + " }";
            string attachExpression =
                "global::Cerneala.UI.Markup.GeneratedMarkup.AttachConditions(" + plan.OwnerVariable + ", " + observations +
                ", new global::Cerneala.UI.Markup.MarkupConditionRule[] { " + string.Join(", ", ruleExpressions) + " })";
            currentPostLines.Add(plan.TemplateContext is null
                ? attachExpression + ";"
                : plan.TemplateContext.ContextVariable + ".RegisterLifetime(" + attachExpression + ");");
        }

        private string EmitConditionalFactory(IReadOnlyList<DirectiveElementNode> elements)
        {
            string factoryName = "CreateConditionalContent" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            List<string> functionLines = [];
            List<string> functionPostLines = [];
            List<string> variables = [];
            List<NamedElementMember> conditionalMembers = [];
            WithEmissionBuffers(functionLines, functionPostLines, () =>
            {
                conditionalMemberScopes.Push(conditionalMembers);
                try
                {
                    foreach (DirectiveElementNode element in elements)
                    {
                        variables.Add(EmitElement(element.Element));
                    }
                }
                finally
                {
                    conditionalMemberScopes.Pop();
                }
            });

            conditionalFactoryMembers[factoryName] = conditionalMembers;

            currentPostLines.Add("global::System.Collections.Generic.IReadOnlyList<global::Cerneala.UI.Elements.UIElement> " + factoryName + "()");
            currentPostLines.Add("{");
            foreach (string line in functionLines)
            {
                currentPostLines.Add("    " + line);
            }

            foreach (string line in functionPostLines)
            {
                currentPostLines.Add("    " + line);
            }

            currentPostLines.Add("    return new global::Cerneala.UI.Elements.UIElement[] { " + string.Join(", ", variables) + " };");
            currentPostLines.Add("}");
            return factoryName;
        }
    }
}
