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
            public ReactivePlan(string ownerVariable, string elementName)
            {
                OwnerVariable = ownerVariable;
                ElementName = elementName;
            }

            public string OwnerVariable { get; }

            public string ElementName { get; }

            public List<string> ObservationLines { get; } = [];

            public List<string> ObservationNames { get; } = [];

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
                string? valueGuard = null)
            {
                Name = name;
                ValueCode = valueCode;
                MarkupKind = markupKind;
                ValueType = valueType;
                ValueGuard = valueGuard;
            }

            public string Name { get; }

            public string ValueCode { get; }

            public MarkupValueKind? MarkupKind { get; }

            public ITypeSymbol? ValueType { get; }

            public string? ValueGuard { get; }
        }

        private DirectiveParseResult GetDirectiveContent(XElement element, bool allowAssignments, bool allowElements)
        {
            if (!directiveContent.TryGetValue(element, out DirectiveParseResult? parsed))
            {
                parsed = ParseDirectiveContent(element, allowAssignments, allowElements);
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
            ReactivePlan plan = new(variable, element.Name.LocalName);
            foreach (AspectResource aspect in aspects)
            {
                foreach (DirectiveWhenNode when in aspect.Conditions)
                {
                    CollectWhen(plan, when, "true", "global::Cerneala.UI.Core.UiPropertyValueSource.AspectVisualState");
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
            ObservationEmission? observation = EmitObservation(plan, when.SourceExpression, when.Source);
            if (observation is null)
            {
                return;
            }

            foreach (DirectiveIfNode branch in when.Branches)
            {
                string? comparison = EmitComparison(plan, observation, branch.Comparator, branch.Operand, branch.Source);
                if (comparison is null)
                {
                    continue;
                }

                string predicate = inheritedPredicate == "true" ? comparison : "(" + inheritedPredicate + ") && (" + comparison + ")";
                List<DirectiveAssignmentNode> assignments = branch.Body.OfType<DirectiveAssignmentNode>().ToList();
                List<DirectiveElementNode> elements = branch.Body.OfType<DirectiveElementNode>().ToList();
                if (assignments.Count > 0 || elements.Count > 0)
                {
                    plan.Rules.Add(new ReactiveRule(plan.NextOrder++, predicate, assignments, elements, valueSource));
                }

                foreach (DirectiveWhenNode nested in branch.Body.OfType<DirectiveWhenNode>())
                {
                    CollectWhen(plan, nested, predicate, valueSource);
                }

                foreach (DirectiveDefaultNode invalid in branch.Body.OfType<DirectiveDefaultNode>())
                {
                    Report(InvalidDirective, invalid.Source, Path.GetFileName(file.Path), "@default cannot appear inside @if.");
                }

                foreach (DirectiveTextNode text in branch.Body.OfType<DirectiveTextNode>())
                {
                    Report(InvalidDirective, text.Source, Path.GetFileName(file.Path), "Raw text is not supported inside @if; use a property assignment or XML element.");
                }
            }
        }

        private ObservationEmission? EmitObservation(ReactivePlan plan, string expression, XObject source)
        {
            string name = "observation" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            if (expression.StartsWith("$DataContext", StringComparison.Ordinal))
            {
                return EmitDataObservation(plan, name, expression, source);
            }

            if (expression.StartsWith("$", StringComparison.Ordinal))
            {
                string referenceName = expression.Substring(1);
                if (!TryResolveObjectSymbol(source, referenceName, out NamedSymbol symbol))
                {
                    Report(InvalidBindingSource, source, expression, "Unknown local reference.");
                    return null;
                }

                string? objectCode = symbol.Source switch
                {
                    string variable => variable,
                    SolidColorBrushResource brush => brush.Variable,
                    _ => null
                };
                if (objectCode is null)
                {
                    Report(InvalidBindingSource, source, expression, "The referenced symbol is not an observable object.");
                    return null;
                }

                plan.ObservationLines.Add(
                    "global::Cerneala.UI.Markup.MarkupObservation " + name +
                    " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveObject(() => (object?)" + objectCode + ");");
                plan.ObservationNames.Add(name);
                return new ObservationEmission(name, name + ".Value", null, compilation.GetSpecialType(SpecialType.System_Object));
            }

            PropertySpec? spec = FindPropertySpec(plan.ElementName, expression);
            if (spec is null)
            {
                Report(InvalidBindingSource, source, expression, "No supported UI property with this name exists on the current element.");
                return null;
            }

            plan.ObservationLines.Add(
                "global::Cerneala.UI.Markup.MarkupObservation " + name +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveProperty(" + plan.OwnerVariable + ", " + spec.PropertyCode + ");");
            plan.ObservationNames.Add(name);
            return new ObservationEmission(name, "(" + TypeCode(spec.ValueKind) + ")" + name + ".Value!", spec.ValueKind, null);
        }

        private ObservationEmission? EmitDataObservation(ReactivePlan plan, string name, string expression, XObject source)
        {
            if (dataType is null)
            {
                Report(InvalidBindingSource, source, expression, "DataType is required on the root element.");
                return null;
            }

            string suffix = expression.Substring("$DataContext".Length);
            string[] names = suffix.Length == 0
                ? []
                : suffix.TrimStart('.').Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            ITypeSymbol currentType = dataType;
            List<string> segments = [];
            foreach (string propertyName in names)
            {
                IPropertySymbol? property = currentType.GetMembers(propertyName)
                    .OfType<IPropertySymbol>()
                    .FirstOrDefault(candidate => !candidate.IsStatic && candidate.GetMethod is not null &&
                        candidate.GetMethod.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal);
                if (property is null)
                {
                    Report(InvalidBindingSource, source, expression, "Public property '" + propertyName + "' was not found on '" + currentType.ToDisplayString() + "'.");
                    return null;
                }

                string ownerType = currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                segments.Add(
                    "new global::Cerneala.UI.Markup.MarkupDataPathSegment(" + Literal(propertyName) +
                    ", value => ((" + ownerType + ")value!)." + propertyName + ")");
                currentType = property.Type;
            }

            plan.ObservationLines.Add(
                "global::Cerneala.UI.Markup.MarkupObservation " + name +
                " = global::Cerneala.UI.Markup.GeneratedMarkup.ObserveDataPath(" + plan.OwnerVariable +
                (segments.Count == 0 ? ")" : ", " + string.Join(", ", segments) + ")") + ";");
            plan.ObservationNames.Add(name);
            ITypeSymbol comparisonType = UnwrapNullable(currentType);
            string typeCode = comparisonType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string capturedValue = name + "Value";
            return new ObservationEmission(
                name,
                capturedValue,
                null,
                comparisonType,
                name + ".Value is " + typeCode + " " + capturedValue);
        }

        private string? EmitComparison(
            ReactivePlan plan,
            ObservationEmission observation,
            string comparator,
            string operand,
            XObject source)
        {
            if (string.Equals(operand, "Null", StringComparison.OrdinalIgnoreCase))
            {
                if (comparator is not "==" and not "!=")
                {
                    Report(InvalidBindingSource, source, operand, "Null supports only == and !=.");
                    return null;
                }

                return observation.Name + ".Value " + comparator + " null";
            }

            if (operand.StartsWith("$DataContext", StringComparison.Ordinal))
            {
                string operandName = "observation" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
                nextReactiveId++;
                ObservationEmission? operandObservation = EmitDataObservation(plan, operandName, operand, source);
                if (operandObservation is null)
                {
                    return null;
                }

                if (observation.ValueType is null || operandObservation.ValueType is null ||
                    !SymbolEqualityComparer.Default.Equals(observation.ValueType, operandObservation.ValueType))
                {
                    Report(InvalidBindingSource, source, operand, "Both DataContext expressions must have the same type.");
                    return null;
                }

                if ((comparator is "<" or "<=" or ">" or ">=") && !SupportsOrdering(observation))
                {
                    Report(InvalidBindingSource, source, comparator, "The observed value type does not support ordering comparisons.");
                    return null;
                }

                string comparison = observation.ValueType.SpecialType == SpecialType.System_String &&
                    comparator is "<" or "<=" or ">" or ">="
                    ? "global::System.String.CompareOrdinal(" + observation.ValueCode + ", " + operandObservation.ValueCode + ") " + comparator + " 0"
                    : observation.ValueCode + " " + comparator + " " + operandObservation.ValueCode;
                string left = observation.ValueGuard ?? "true";
                string right = operandObservation.ValueGuard ?? "true";
                return left + " && " + right + " && " + comparison;
            }

            if (operand.StartsWith("$", StringComparison.Ordinal) &&
                observation.ValueType?.SpecialType == SpecialType.System_Object)
            {
                if (comparator is not "==" and not "!=")
                {
                    Report(InvalidBindingSource, source, comparator, "Object references support only == and !=.");
                    return null;
                }

                string referenceName = operand.Substring(1);
                if (!TryResolveObjectSymbol(source, referenceName, out NamedSymbol symbol))
                {
                    Report(InvalidBindingSource, source, operand, "Unknown local reference.");
                    return null;
                }

                string? objectCode = symbol.Source switch
                {
                    string variable => variable,
                    SolidColorBrushResource brush => brush.Variable,
                    _ => null
                };
                if (objectCode is null)
                {
                    Report(InvalidBindingSource, source, operand, "The referenced symbol is not an object value.");
                    return null;
                }

                return observation.ValueCode + " " + comparator + " (object?)" + objectCode;
            }

            string? operandCode;
            bool isString;
            if (observation.MarkupKind is MarkupValueKind kind)
            {
                GeneratedExpression? generated = ParseDirectiveValue(null, "value", operand, kind, source);
                operandCode = generated?.Code;
                isString = kind == MarkupValueKind.String;
            }
            else
            {
                operandCode = ParseSymbolLiteral(observation.ValueType!, operand, source);
                isString = observation.ValueType?.SpecialType == SpecialType.System_String;
            }

            if (operandCode is null)
            {
                return null;
            }

            if (isString && comparator is "<" or "<=" or ">" or ">=")
            {
                string comparison = "global::System.String.CompareOrdinal(" + observation.ValueCode + ", " + operandCode + ")";
                return GuardComparison(observation, comparison + " " + comparator + " 0");
            }

            if ((comparator is "<" or "<=" or ">" or ">=") && !SupportsOrdering(observation))
            {
                Report(InvalidBindingSource, source, comparator, "The observed value type does not support ordering comparisons.");
                return null;
            }

            return GuardComparison(observation, observation.ValueCode + " " + comparator + " " + operandCode);
        }

        private static string GuardComparison(ObservationEmission observation, string comparison)
        {
            return observation.ValueGuard is null
                ? comparison
                : observation.ValueGuard + " && " + comparison;
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
            MarkupValueKind kind,
            XObject source)
        {
            string value = rawValue.Trim();
            if (value.StartsWith("$", StringComparison.Ordinal))
            {
                return ResolveReferenceValue(planElementName ?? "value", propertyName, value.Substring(1), kind, source);
            }

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2);
            }

            XAttribute synthetic = new(propertyName, value);
            return ParseLiteralValue(planElementName ?? "value", propertyName, synthetic, value, kind);
        }

        private string? ParseSymbolLiteral(ITypeSymbol type, string rawValue, XObject source)
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

        private static bool SupportsOrdering(ObservationEmission observation)
        {
            if (observation.MarkupKind is MarkupValueKind.Float or MarkupValueKind.NonNegativeFloat or MarkupValueKind.PositiveFloat)
            {
                return true;
            }

            return observation.ValueType is not null &&
                (IsNumeric(observation.ValueType) || observation.ValueType.SpecialType == SpecialType.System_String);
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

        private static string TypeCode(MarkupValueKind kind)
        {
            return kind switch
            {
                MarkupValueKind.String => "string",
                MarkupValueKind.Bool => "bool",
                MarkupValueKind.Float or MarkupValueKind.NonNegativeFloat or MarkupValueKind.PositiveFloat => "float",
                MarkupValueKind.Thickness or MarkupValueKind.NonNegativeThickness => "global::Cerneala.UI.Layout.Thickness",
                MarkupValueKind.DrawColor => "global::Cerneala.Drawing.DrawColor",
                MarkupValueKind.WindowState => "global::Cerneala.UI.Controls.WindowState",
                MarkupValueKind.ResizeMode => "global::Cerneala.UI.Controls.ResizeMode",
                MarkupValueKind.WindowStartupLocation => "global::Cerneala.UI.Controls.WindowStartupLocation",
                _ => "object"
            };
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
                    PropertySpec? spec = FindPropertySpec(plan.ElementName, assignment.PropertyName);
                    if (spec is null || !spec.Assignable)
                    {
                        Report(UnsupportedProperty, assignment.Source, plan.ElementName, assignment.PropertyName);
                        continue;
                    }

                    GeneratedExpression? expression = ParseDirectiveValue(plan.ElementName, assignment.PropertyName, assignment.Value, spec.ValueKind, assignment.Source);
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
            currentPostLines.Add(
                "global::Cerneala.UI.Markup.GeneratedMarkup.AttachConditions(" + plan.OwnerVariable + ", " + observations +
                ", new global::Cerneala.UI.Markup.MarkupConditionRule[] { " + string.Join(", ", ruleExpressions) + " });");
        }

        private string EmitConditionalFactory(IReadOnlyList<DirectiveElementNode> elements)
        {
            string factoryName = "CreateConditionalContent" + nextReactiveId.ToString(CultureInfo.InvariantCulture);
            nextReactiveId++;
            List<string> functionLines = [];
            List<string> functionPostLines = [];
            List<string> variables = [];
            List<string> previousLines = currentLines;
            List<string> previousPostLines = currentPostLines;
            List<NamedElementMember> conditionalMembers = [];
            currentLines = functionLines;
            currentPostLines = functionPostLines;
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
                currentLines = previousLines;
                currentPostLines = previousPostLines;
            }

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
