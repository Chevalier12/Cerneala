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
        private sealed class MotionSpecResource
        {
            public MotionSpecResource(string kind, IReadOnlyList<string> arguments)
            {
                Kind = kind;
                Arguments = arguments;
            }

            public string Kind { get; }

            public IReadOnlyList<string> Arguments { get; }
        }

        private void ReadMotionSpecResource(ResourceScope scope, MarkupElement resource)
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

                string delay = resource.Attribute("Delay")?.Value.Trim() ?? "0ms";
                if (!TryBuildNonNegativeDurationExpression(delay, out string delayCode))
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("Delay") ?? resource, "Tween", "Delay", delay);
                    return;
                }

                string fillMode = resource.Attribute("FillMode")?.Value.Trim() ?? "Both";
                if (fillMode is not ("None" or "Backwards" or "Forwards" or "Both"))
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("FillMode") ?? resource, "Tween", "FillMode", fillMode);
                    return;
                }

                arguments.Add(delayCode);
                arguments.Add("global::Cerneala.UI.Motion.Specs.FillMode." + fillMode);
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

                foreach ((string attributeName, float defaultValue) in new[]
                {
                    ("RestSpeed", 0.01f),
                    ("RestDelta", 0.01f)
                })
                {
                    string text = resource.Attribute(attributeName)?.Value.Trim() ?? defaultValue.ToString("R", CultureInfo.InvariantCulture);
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
                        float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                    {
                        Report(InvalidPropertyValue, (object?)resource.Attribute(attributeName) ?? resource, "Spring", attributeName, text);
                        return;
                    }

                    arguments.Add(value.ToString("R", CultureInfo.InvariantCulture) + "f");
                }

                string velocityMode = resource.Attribute("VelocityMode")?.Value.Trim() ?? "Preserve";
                if (velocityMode is not ("Preserve" or "Reset"))
                {
                    Report(InvalidPropertyValue, (object?)resource.Attribute("VelocityMode") ?? resource, "Spring", "VelocityMode", velocityMode);
                    return;
                }

                arguments.Add("global::Cerneala.UI.Motion.Specs.SpringVelocityMode." + velocityMode);
            }

            MotionSpecResource spec = new(resource.Name.LocalName, arguments);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.MotionSpec, spec));
        }

        private bool TryResolveMotionSpec(
            MarkupElement applicationElement,
            MotionSpecSyntax? syntax,
            PropertySpec property,
            string target,
            MotionClipInvocationContext? parameters,
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

            if (syntax is MotionParameterSpecSyntax parameterReference)
            {
                if (parameters is null ||
                    !parameters.Values.TryGetValue(parameterReference.Name, out ResolvedMotionParameterValue? parameter) ||
                    parameter.Spec is null)
                {
                    Report(InvalidDirective, parameterReference.Location, Path.GetFileName(file.Path), "Unknown MotionClip spec parameter '" + parameterReference.Name + "'.");
                    return false;
                }

                IsMotionSpecParameterType(parameter.Parameter.TypeName, out string parameterValueType);
                string propertyType = property.ValueType.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
                if (parameterValueType != propertyType)
                {
                    Report(InvalidDirective, parameterReference.Location, Path.GetFileName(file.Path), "MotionClip spec parameter '" + parameterReference.Name + "' is not compatible with property type '" + propertyType + "'.");
                    return false;
                }

                return TryResolveMotionSpec(applicationElement, parameter.Spec, property, target, null, out variable);
            }

            return TryResolveConcreteMotionSpec(syntax, property.ValueType, target, out variable);
        }

        private bool TryResolveConcreteMotionSpec(
            MotionSpecSyntax syntax,
            ITypeSymbol valueType,
            string target,
            out string? variable)
        {
            variable = null;
            string kind;
            IReadOnlyList<string> arguments;
            if (syntax is MotionResourceSpecSyntax resourceReference)
            {
                if (!TryResolveResource(resourceReference.Location.Source, resourceReference.Name, out NamedSymbol symbol) ||
                    symbol.Source is not MotionSpecResource resource)
                {
                    ReportMotion(MotionDiagnosticKind.Type, resourceReference.Location, "Unknown Motion resource '$" + resourceReference.Name + "'.");
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

            if (kind == "Spring" && !HasVectorMixer(valueType))
            {
                ReportMotion(MotionDiagnosticKind.Type, syntax.Location, "Spring is not a valid spec type for property '" + target + "' because its mixer has no vector operations.");
                return false;
            }

            string typeCode = GetMotionTypeCode(valueType);
            string expression = kind is "Repeat" or "PingPong"
                ? "new global::Cerneala.UI.Motion.Specs." + kind + "Spec<" + typeCode + ">(" +
                    "new global::Cerneala.UI.Motion.Specs.TweenSpec<" + typeCode + ">(" + arguments[0] + ", " + arguments[1] + "), " + arguments[2] + ")"
                : "new global::Cerneala.UI.Motion.Specs." + kind + "Spec<" + typeCode + ">(" + string.Join(", ", arguments) + ")";
            string key = typeCode + "|" + expression;
            if (!specializedMotionSpecs.TryGetValue(key, out variable))
            {
                variable = "motionSpec" + nextMotionSpecId.ToString(CultureInfo.InvariantCulture);
                nextMotionSpecId++;
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
            else if (inline.Kind is "Repeat" or "PingPong")
            {
                MotionInlineSpecSyntax inner = (MotionInlineSpecSyntax)DirectiveCursor.ParseMotionSpec(
                    inline.Arguments[0].Text,
                    inline.Arguments[0].Location);
                MotionDurationSyntax duration = inner.Arguments[0].Duration!;
                string easing = inner.Arguments.Count == 2 ? inner.Arguments[1].Text : "Standard";
                if (!IsKnownEasing(easing))
                {
                    Report(InvalidDirective, inner.Location, Path.GetFileName(file.Path), "Unknown easing '" + easing + "'.");
                    arguments = [];
                    return false;
                }

                result.Add(BuildDurationExpression(duration));
                result.Add("global::Cerneala.UI.Motion.Specs.Easings." + easing);
                result.Add(inline.Arguments[1].Text == "forever" ? "null" : inline.Arguments[1].Text);
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
            return TryBuildDurationExpression(text, allowZero: false, out expression);
        }

        private static bool TryBuildNonNegativeDurationExpression(string text, out string expression)
        {
            return TryBuildDurationExpression(text, allowZero: true, out expression);
        }

        private static bool TryBuildDurationExpression(string text, bool allowZero, out string expression)
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
                double.IsNaN(value) || double.IsInfinity(value) || (allowZero ? value < 0 : value <= 0))
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
