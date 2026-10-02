using System;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private void EmitProperty(
            MarkupElement element,
            string variable,
            MarkupAttribute attribute,
            PropertySpec? explicitSpec = null,
            string? explicitPropertyName = null,
            bool forceUiPropertyAssignment = false)
        {
            string elementName = element.Name.LocalName;
            string propertyName = explicitPropertyName ?? attribute.Name.LocalName;
            string value = attribute.Value;
            string trimmedValue = value.Trim();

            bool isRoot = ReferenceEquals(element, document.Root);
            PropertySpec? spec = explicitSpec ??
                FindPropertySpec(elementName, propertyName, isRoot) ??
                FindClrPropertySpec(elementName, propertyName, isRoot);
            if (spec is null)
            {
                if (!HasErrors)
                {
                    Report(UnsupportedProperty, attribute, elementName, propertyName);
                }

                return;
            }

            ParsedMarkupValue? parsedMarkup = ParseMarkupBindingValue(
                value,
                assignment: false,
                stringTarget: spec.ValueType.SpecialType == SpecialType.System_String,
                attribute);
            if (parsedMarkup?.Kind == ParsedMarkupValueKind.Invalid)
            {
                return;
            }

            if (!spec.Assignable)
            {
                if (templateEmissionContexts.Count > 0 && trimmedValue.StartsWith("$owner.", StringComparison.Ordinal))
                {
                    Report(
                        InvalidComponentTemplate,
                        attribute,
                        Path.GetFileName(file.Path),
                        "Template binding target '" + elementName + "." + propertyName + "' is read-only.");
                }
                else if (parsedMarkup is not null)
                {
                    Report(
                        InvalidBindingSource,
                        attribute,
                        parsedMarkup.Binding?.Path ?? trimmedValue,
                        "The target UI property is read-only.");
                }
                else if (!HasErrors)
                {
                    Report(UnsupportedProperty, attribute, elementName, propertyName);
                }

                return;
            }

            MarkupBindingToken? directReference = parsedMarkup?.Binding;
            if (directReference is not null &&
                directReference.ModeOffset < 0 &&
                LooksLikeBindingPath(trimmedValue))
            {
                GeneratedExpression? referenceExpression = ResolveDirectiveReferenceValue(
                    elementName,
                    propertyName,
                    directReference,
                    spec,
                    attribute,
                    variable);
                if (referenceExpression is null)
                {
                    return;
                }

                currentPostLines.Add((reactiveDocument || forceUiPropertyAssignment) && spec.IsUiProperty
                    ? variable + ".SetValue(" + spec.PropertyCode + ", " + referenceExpression.Code +
                        ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                    : variable + "." + spec.Name + " = " + referenceExpression.Code + ";");
                return;
            }

            if (parsedMarkup is not null)
            {
                if (!spec.IsUiProperty)
                {
                    Report(
                        InvalidBindingSource,
                        attribute,
                        trimmedValue,
                        "Bindings require a UiProperty-backed target; ordinary CLR properties support literal and resource values only.");
                    return;
                }

                BindingResolutionContext bindingContext = CreateBindingResolutionContext(
                    variable,
                    elementName,
                    ReferenceEquals(element, document.Root));

                MarkupBindingToken? direct = parsedMarkup.Binding;
                if (direct is not null && direct.Path.StartsWith("$owner.", StringComparison.Ordinal))
                {
                    BindingSourceDescriptor? sourceDescriptor = ResolveBindingSource(
                        bindingContext,
                        direct.Path,
                        attribute,
                        attribute);
                    if (sourceDescriptor is null)
                    {
                        return;
                    }

                    if (direct.Mode == MarkupBindingMode.TwoWay)
                    {
                        Report(InvalidBindingSource, attribute, trimmedValue, "$owner template bindings support OneWay only.");
                        return;
                    }

                    if (!SymbolEqualityComparer.Default.Equals(sourceDescriptor.ValueType, spec.ValueType))
                    {
                        Report(
                            InvalidComponentTemplate,
                            attribute,
                            Path.GetFileName(file.Path),
                            "Template binding '" + trimmedValue + "' has type '" + sourceDescriptor.ValueType.ToDisplayString() +
                            "', but '" + elementName + "." + propertyName + "' expects '" + spec.ValueType.ToDisplayString() + "'.");
                        return;
                    }

                    TemplateEmissionContext templateContext = templateEmissionContexts.Peek();
                    currentLines.Add(
                        templateContext.ContextVariable + ".Bind(" + sourceDescriptor.Property!.PropertyCode + ", " +
                        variable + ", " + spec.PropertyCode + ");");
                    return;
                }

                ResolvedMarkupValue? resolvedMarkup = ResolveMarkupValue(
                    bindingContext,
                    spec,
                    parsedMarkup,
                    attribute,
                    attribute);
                if (resolvedMarkup is null)
                {
                    return;
                }

                EmitMarkupBinding(
                    bindingContext,
                    spec,
                    resolvedMarkup,
                    elementName + "." + propertyName + " <- " + trimmedValue);
                return;
            }

            if (trimmedValue.StartsWith("$", StringComparison.Ordinal))
            {
                string resourceName = trimmedValue.EndsWith(":OneWay", StringComparison.Ordinal)
                    ? trimmedValue.Substring(1, trimmedValue.Length - ":OneWay".Length - 1)
                    : trimmedValue.Substring(1);
                GeneratedExpression? resourceExpression = ResolveReferenceValue(
                    elementName,
                    propertyName,
                    resourceName,
                    spec.ValueKind,
                    attribute);
                if (resourceExpression is not null)
                {
                    if (resourceExpression.ApplicationResourceName is not null && spec.IsUiProperty)
                    {
                        EmitApplicationResourceBinding(
                            variable,
                            spec,
                            resourceExpression.ApplicationResourceName,
                            "global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase");
                        return;
                    }

                    currentLines.Add((reactiveDocument || forceUiPropertyAssignment) && spec.IsUiProperty
                        ? variable + ".SetValue(" + spec.PropertyCode + ", " + resourceExpression.Code +
                            ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                        : variable + "." + spec.Name + " = " + resourceExpression.Code + ";");
                }

                return;
            }

            string literalValue = spec.ValueType.SpecialType == SpecialType.System_String
                ? UnescapeMarkupDollar(value)
                : value;
            GeneratedExpression? expression = ParseLiteralValue(elementName, propertyName, attribute, literalValue, spec);
            if (expression is null)
            {
                return;
            }

            currentLines.Add((reactiveDocument || forceUiPropertyAssignment) && spec.IsUiProperty
                ? variable + ".SetValue(" + spec.PropertyCode + ", " + expression.Code +
                    ", global::Cerneala.UI.Core.UiPropertyValueSource.MarkupBase);"
                : variable + "." + spec.Name + " = " + expression.Code + ";");
        }

        private void EmitApplicationResourceBinding(
            string variable,
            PropertySpec spec,
            string resourceName,
            string valueSource)
        {
            currentLines.Add(
                "global::Cerneala.UI.Markup.GeneratedMarkup.AttachResource(" +
                variable + ", " + variable + ", " + spec.PropertyCode + ", " +
                Literal(resourceName) + ", " + valueSource + ");");
        }
    }
}
