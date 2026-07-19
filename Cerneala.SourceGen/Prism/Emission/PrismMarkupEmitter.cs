using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Cerneala.SourceGen.Prism;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {
        private readonly Dictionary<BoundPrismComposition, string> prismDefinitionNames = new();
        private int nextPrismDefinitionId;
        private int nextPrismFactoryId;

        public List<string> PrismDeclarationLines { get; } = new();

        private void EmitPrismApplication(XElement element, string elementVariable)
        {
            if (!boundPrismApplications.TryGetValue(element, out BoundPrismApplication application))
            {
                return;
            }

            string definitionName = GetOrEmitPrismDefinition(application.Composition);
            string factoryName = "__CernealaCreatePrism" +
                nextPrismFactoryId.ToString(CultureInfo.InvariantCulture);
            nextPrismFactoryId++;
            EmitPrismFactory(factoryName, definitionName, application);
            currentLines.Add(
                "_ = global::Cerneala.UI.Markup.GeneratedMarkup.AttachPrism(" +
                elementVariable + ", " + factoryName + ");");
        }

        private string GetOrEmitPrismDefinition(BoundPrismComposition composition)
        {
            if (prismDefinitionNames.TryGetValue(composition, out string? existing))
            {
                return existing;
            }

            string name = "__CernealaPrismDefinition" +
                nextPrismDefinitionId.ToString(CultureInfo.InvariantCulture);
            nextPrismDefinitionId++;
            prismDefinitionNames.Add(composition, name);
            PrismDeclarationLines.Add(
                "private static readonly global::Cerneala.UI.Prism.Definitions.PrismCompositionDefinition " +
                name + " = " + EmitPrismCompositionDefinition(composition) + ";");
            return name;
        }

        private string EmitPrismCompositionDefinition(BoundPrismComposition composition)
        {
            List<string> arguments =
            [
                Literal(composition.Name),
                EmitPrismArray(
                    "global::Cerneala.UI.Prism.Definitions.PrismNodeDefinition",
                    composition.Nodes.Select(EmitPrismNodeDefinition))
            ];
            AddPrismDefinitionArgument(
                arguments,
                composition.Properties,
                "WorkingColorProfile",
                "workingColorProfile");
            AddPrismDefinitionArgument(
                arguments,
                composition.Properties,
                "GlobalLightAngle",
                "globalLightAngle");
            AddPrismDefinitionArgument(
                arguments,
                composition.Properties,
                "GlobalLightAltitude",
                "globalLightAltitude");
            return "new global::Cerneala.UI.Prism.Definitions.PrismCompositionDefinition(" +
                string.Join(", ", arguments) + ")";
        }

        private string EmitPrismNodeDefinition(BoundPrismNode node)
        {
            List<string> arguments =
            [
                "new global::Cerneala.UI.Prism.Definitions.PrismNodeId(" +
                    node.Id.ToString(CultureInfo.InvariantCulture) + ")",
                node.Name is null ? "null" : Literal(node.Name)
            ];

            string typeName;
            switch (node.Kind)
            {
                case PrismContainerKind.Layer:
                    typeName = "global::Cerneala.UI.Prism.Definitions.PrismLayerDefinition";
                    arguments.Add(
                        "filters: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismFilterDefinition",
                            node.Filters.Select(EmitPrismFilterDefinition)));
                    arguments.Add(
                        "styles: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismStyleDefinition",
                            node.Styles.Select(EmitPrismStyleDefinition)));
                    arguments.Add("mask: " + EmitPrismMaskDefinition(node.Mask));
                    AddPrismDefinitionArgument(arguments, node.Properties, "Visible", "visible");
                    AddPrismDefinitionArgument(arguments, node.Properties, "Opacity", "opacity");
                    AddPrismDefinitionArgument(arguments, node.Properties, "Fill", "fill");
                    AddPrismDefinitionArgument(arguments, node.Properties, "BlendMode", "blendMode");
                    AddPrismDefinitionArgument(arguments, node.Properties, "ClipToBelow", "clipToBelow");
                    break;
                case PrismContainerKind.Group:
                    typeName = "global::Cerneala.UI.Prism.Definitions.PrismGroupDefinition";
                    arguments.Add(
                        "children: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismNodeDefinition",
                            node.Children.Select(EmitPrismNodeDefinition)));
                    arguments.Add(
                        "filters: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismFilterDefinition",
                            node.Filters.Select(EmitPrismFilterDefinition)));
                    arguments.Add(
                        "styles: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismStyleDefinition",
                            node.Styles.Select(EmitPrismStyleDefinition)));
                    arguments.Add("mask: " + EmitPrismMaskDefinition(node.Mask));
                    AddPrismDefinitionArgument(arguments, node.Properties, "Visible", "visible");
                    AddPrismDefinitionArgument(arguments, node.Properties, "Opacity", "opacity");
                    AddPrismDefinitionArgument(arguments, node.Properties, "BlendMode", "blendMode");
                    break;
                case PrismContainerKind.Backdrop:
                    typeName = "global::Cerneala.UI.Prism.Definitions.PrismBackdropDefinition";
                    arguments.Add(
                        "filters: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismFilterDefinition",
                            node.Filters.Select(EmitPrismFilterDefinition)));
                    arguments.Add(
                        "styles: " + EmitPrismArray(
                            "global::Cerneala.UI.Prism.Definitions.PrismStyleDefinition",
                            node.Styles.Select(EmitPrismStyleDefinition)));
                    arguments.Add("mask: " + EmitPrismMaskDefinition(node.Mask));
                    AddPrismDefinitionArgument(arguments, node.Properties, "Visible", "visible");
                    AddPrismDefinitionArgument(arguments, node.Properties, "Opacity", "opacity");
                    break;
                default:
                    throw new InvalidOperationException("A Prism composition cannot be emitted as a node.");
            }

            return "new " + typeName + "(" + string.Join(", ", arguments) + ")";
        }

        private string EmitPrismFilterDefinition(BoundPrismOperation operation)
        {
            PrismCatalogCompiler.CatalogEntry entry = operation.CatalogEntry
                ?? throw new InvalidOperationException("A Prism filter has no catalog entry.");
            List<string> arguments =
            [
                "global::Cerneala.Drawing.Prism.Catalog.PrismFilterId." + entry.Symbol
            ];
            AddPrismDefinitionArgument(arguments, operation.Properties, "Visible", "visible");
            AddPrismDefinitionArgument(arguments, operation.Properties, "Opacity", "opacity");
            AddPrismDefinitionArgument(arguments, operation.Properties, "BlendMode", "blendMode");
            return "new global::Cerneala.UI.Prism.Definitions.PrismFilterDefinition(" +
                string.Join(", ", arguments) + ")";
        }

        private string EmitPrismStyleDefinition(BoundPrismOperation operation)
        {
            PrismCatalogCompiler.CatalogEntry entry = operation.CatalogEntry
                ?? throw new InvalidOperationException("A Prism style has no catalog entry.");
            List<string> arguments =
            [
                "global::Cerneala.Drawing.Prism.Catalog.PrismStyleId." + entry.Symbol
            ];
            AddPrismDefinitionArgument(arguments, operation.Properties, "Visible", "visible");
            return "new global::Cerneala.UI.Prism.Definitions.PrismStyleDefinition(" +
                string.Join(", ", arguments) + ")";
        }

        private string EmitPrismMaskDefinition(BoundPrismOperation? operation)
        {
            if (operation is null)
            {
                return "null";
            }

            BoundPrismProperty? image = FindPrismProperty(operation.Properties, "Image");
            string imageExpression = image is null
                ? PrismPlaceholder(BoundPrismValueType.Resource, "Image")
                : EmitPrismDefinitionValue(image.Value, image.Schema.Name);
            List<string> arguments = [imageExpression];
            AddPrismDefinitionArgument(arguments, operation.Properties, "Channel", "channel");
            AddPrismDefinitionArgument(arguments, operation.Properties, "Feather", "feather");
            AddPrismDefinitionArgument(arguments, operation.Properties, "Density", "density");
            AddPrismDefinitionArgument(arguments, operation.Properties, "Invert", "invert");
            return "new global::Cerneala.UI.Prism.Definitions.PrismMaskDefinition(" +
                string.Join(", ", arguments) + ")";
        }

        private void AddPrismDefinitionArgument(
            List<string> arguments,
            IReadOnlyList<BoundPrismProperty> properties,
            string propertyName,
            string argumentName)
        {
            BoundPrismProperty? property = FindPrismProperty(properties, propertyName);
            if (property is not null)
            {
                arguments.Add(
                    argumentName + ": " +
                    EmitPrismDefinitionValue(property.Value, property.Schema.Name));
            }
        }

        private string EmitPrismDefinitionValue(BoundPrismValue value, string propertyName)
        {
            BoundPrismValue? resolved = ResolvePrismDefinitionValue(value, new HashSet<BoundPrismParameter>());
            return resolved is null
                ? PrismPlaceholder(value.Type, propertyName)
                : EmitPrismStateValue(resolved, propertyName);
        }

        private static BoundPrismValue? ResolvePrismDefinitionValue(
            BoundPrismValue value,
            HashSet<BoundPrismParameter> visited)
        {
            if (value.Parameter is null)
            {
                return value;
            }

            return visited.Add(value.Parameter) && value.Parameter.DefaultValue is not null
                ? ResolvePrismDefinitionValue(value.Parameter.DefaultValue, visited)
                : null;
        }

        private void EmitPrismFactory(
            string factoryName,
            string definitionName,
            BoundPrismApplication application)
        {
            PrismDeclarationLines.Add(
                "private static global::Cerneala.UI.Prism.Runtime.PrismInstance " +
                factoryName + "()");
            PrismDeclarationLines.Add("{");
            PrismDeclarationLines.Add(
                "    global::Cerneala.UI.Prism.Runtime.PrismInstance instance = new(" +
                definitionName + ");");

            foreach (BoundPrismProperty property in application.Composition.Properties)
            {
                PrismDeclarationLines.Add(
                    "    instance.Composition." + property.Schema.Name + " = " +
                    EmitPrismApplicationValue(property.Value, property.Schema.Name, application) + ";");
            }

            foreach (BoundPrismNode node in application.Composition.Nodes)
            {
                EmitPrismNodeAssignments(node, application);
            }

            PrismDeclarationLines.Add("    return instance;");
            PrismDeclarationLines.Add("}");
        }

        private void EmitPrismNodeAssignments(
            BoundPrismNode node,
            BoundPrismApplication application)
        {
            string nodeVariable = "__prismNode" +
                node.Id.ToString(CultureInfo.InvariantCulture);
            string nodeId = "new global::Cerneala.UI.Prism.Definitions.PrismNodeId(" +
                node.Id.ToString(CultureInfo.InvariantCulture) + ")";
            string stateType;
            string getter;
            switch (node.Kind)
            {
                case PrismContainerKind.Layer:
                    stateType = "global::Cerneala.UI.Prism.Runtime.PrismLayerState";
                    getter = "GetLayerState";
                    break;
                case PrismContainerKind.Group:
                    stateType = "global::Cerneala.UI.Prism.Runtime.PrismGroupState";
                    getter = "GetGroupState";
                    break;
                case PrismContainerKind.Backdrop:
                    stateType = "global::Cerneala.UI.Prism.Runtime.PrismBackdropState";
                    getter = "GetBackdropState";
                    break;
                default:
                    throw new InvalidOperationException("A Prism composition cannot be assigned as a node.");
            }

            PrismDeclarationLines.Add(
                "    " + stateType + " " + nodeVariable + " = instance." +
                getter + "(" + nodeId + ");");
            foreach (BoundPrismProperty property in node.Properties)
            {
                PrismDeclarationLines.Add(
                    "    " + nodeVariable + "." + property.Schema.Name + " = " +
                    EmitPrismApplicationValue(property.Value, property.Schema.Name, application) + ";");
            }

            for (int index = 0; index < node.Filters.Count; index++)
            {
                EmitPrismOperationAssignments(
                    node.Filters[index],
                    nodeVariable + ".Filters[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    "Filter",
                    node.Id,
                    index,
                    application);
            }

            for (int index = 0; index < node.Styles.Count; index++)
            {
                EmitPrismOperationAssignments(
                    node.Styles[index],
                    nodeVariable + ".Styles[" + index.ToString(CultureInfo.InvariantCulture) + "]",
                    "Style",
                    node.Id,
                    index,
                    application);
            }

            if (node.Mask is not null)
            {
                string maskVariable = "__prismMask" +
                    node.Id.ToString(CultureInfo.InvariantCulture);
                PrismDeclarationLines.Add(
                    "    global::Cerneala.UI.Prism.Runtime.PrismMaskState " +
                    maskVariable + " = " + nodeVariable + ".Mask!;");
                foreach (BoundPrismProperty property in node.Mask.Properties)
                {
                    PrismDeclarationLines.Add(
                        "    " + maskVariable + "." + property.Schema.Name + " = " +
                        EmitPrismApplicationValue(
                            property.Value,
                            property.Schema.Name,
                            application) + ";");
                }
            }

            foreach (BoundPrismNode child in node.Children)
            {
                EmitPrismNodeAssignments(child, application);
            }
        }

        private void EmitPrismOperationAssignments(
            BoundPrismOperation operation,
            string stateExpression,
            string operationKind,
            int nodeId,
            int operationIndex,
            BoundPrismApplication application)
        {
            PrismCatalogCompiler.CatalogEntry entry = operation.CatalogEntry
                ?? throw new InvalidOperationException("A Prism catalog operation has no entry.");
            string variable = "__prism" + operationKind +
                nodeId.ToString(CultureInfo.InvariantCulture) + "_" +
                operationIndex.ToString(CultureInfo.InvariantCulture);
            PrismDeclarationLines.Add(
                "    global::Cerneala.UI.Prism.Runtime.Prism" + operationKind +
                "State " + variable + " = " + stateExpression + ";");

            foreach (BoundPrismProperty property in operation.Properties)
            {
                if (!entry.Properties.Any(candidate => ReferenceEquals(candidate, property.Schema)))
                {
                    PrismDeclarationLines.Add(
                        "    " + variable + "." + property.Schema.Name + " = " +
                        EmitPrismApplicationValue(
                            property.Value,
                            property.Schema.Name,
                            application) + ";");
                    continue;
                }

                int slot = GetPrismParameterSlot(entry, property.Schema);
                string valueKind = property.Value.Type switch
                {
                    BoundPrismValueType.Boolean => "Boolean",
                    BoundPrismValueType.Integer => "Integer",
                    BoundPrismValueType.Number => "Number",
                    BoundPrismValueType.Color => "Color",
                    BoundPrismValueType.Vector => "Vector",
                    BoundPrismValueType.Symbol => "Integer",
                    BoundPrismValueType.Resource => "Resource",
                    _ => throw new InvalidOperationException("Unknown Prism parameter type.")
                };
                PrismDeclarationLines.Add(
                    "    global::Cerneala.UI.Markup.GeneratedMarkup.SetPrism" +
                    operationKind + valueKind + "(" + variable + ", " +
                    entry.StableId.ToString(CultureInfo.InvariantCulture) + ", " +
                    slot.ToString(CultureInfo.InvariantCulture) + ", " +
                    EmitPrismApplicationParameterValue(
                        property.Value,
                        property.Schema.Name,
                        application) + ");");
            }
        }

        private string EmitPrismApplicationValue(
            BoundPrismValue value,
            string propertyName,
            BoundPrismApplication application)
        {
            BoundPrismValue resolved = ResolvePrismApplicationValue(value, application);
            return EmitPrismStateValue(resolved, propertyName);
        }

        private string EmitPrismApplicationParameterValue(
            BoundPrismValue value,
            string propertyName,
            BoundPrismApplication application)
        {
            BoundPrismValue resolved = ResolvePrismApplicationValue(value, application);
            return resolved.Type == BoundPrismValueType.Symbol
                ? EmitPrismParameterSymbol(resolved.Syntax.Text, propertyName)
                : EmitPrismValue(resolved);
        }

        private static BoundPrismValue ResolvePrismApplicationValue(
            BoundPrismValue value,
            BoundPrismApplication application)
        {
            HashSet<BoundPrismParameter> visited = new();
            BoundPrismValue current = value;
            while (current.Parameter is BoundPrismParameter parameter)
            {
                if (!visited.Add(parameter))
                {
                    throw new InvalidOperationException(
                        "A Prism parameter default contains a reference cycle.");
                }

                if (!application.Arguments.TryGetValue(parameter.Path, out BoundPrismValue? next))
                {
                    next = parameter.DefaultValue
                        ?? throw new InvalidOperationException(
                            "A bound Prism application is missing a required parameter.");
                }

                current = next;
            }

            return current;
        }

        private static int GetPrismParameterSlot(
            PrismCatalogCompiler.CatalogEntry entry,
            PrismCatalogCompiler.CatalogProperty property)
        {
            string storageType = PrismStorageType(property.ValueType);
            int slot = 0;
            foreach (PrismCatalogCompiler.CatalogProperty candidate in
                entry.Properties.OrderBy(candidate => candidate.Id, StringComparer.Ordinal))
            {
                if (!string.Equals(
                        PrismStorageType(candidate.ValueType),
                        storageType,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (ReferenceEquals(candidate, property) ||
                    string.Equals(candidate.Id, property.Id, StringComparison.Ordinal))
                {
                    return slot;
                }

                slot++;
            }

            throw new InvalidOperationException(
                "A bound Prism property is missing from its catalog entry.");
        }

        private static string PrismStorageType(string valueType) =>
            valueType is "integer" or "symbol" ? "integer" : valueType;

        private string EmitPrismStateValue(BoundPrismValue value, string propertyName)
        {
            if (value.Type == BoundPrismValueType.Symbol)
            {
                return propertyName switch
                {
                    "WorkingColorProfile" =>
                        "global::Cerneala.Drawing.Prism.Catalog.PrismColorProfile." +
                        value.Syntax.Text,
                    "BlendMode" =>
                        "global::Cerneala.Drawing.Prism.Catalog.PrismBlendMode." +
                        value.Syntax.Text,
                    "Channel" =>
                        "global::Cerneala.UI.Prism.Definitions.PrismMaskChannel." +
                        value.Syntax.Text,
                    "BlendChannels" =>
                        "global::Cerneala.UI.Prism.Runtime.PrismBlendChannels.Rgba",
                    "Knockout" =>
                        "global::Cerneala.UI.Prism.Runtime.PrismKnockout." +
                        value.Syntax.Text,
                    "BlendIfChannel" =>
                        "global::Cerneala.UI.Prism.Runtime.PrismBlendIfChannel." +
                        value.Syntax.Text,
                    _ => EmitPrismValue(value)
                };
            }

            if (value.Type == BoundPrismValueType.Vector &&
                propertyName is "ThisLayerRange" or "UnderlyingRange")
            {
                string[] parts = ParsePrismVector(value.Syntax.Text);
                return "new global::Cerneala.UI.Prism.Runtime.PrismBlendRange(" +
                    string.Join(", ", parts) + ")";
            }

            return EmitPrismValue(value);
        }

        private string EmitPrismValue(BoundPrismValue value)
        {
            switch (value.Type)
            {
                case BoundPrismValueType.Boolean:
                    return string.Equals(value.Syntax.Text, "true", StringComparison.OrdinalIgnoreCase)
                        ? "true"
                        : "false";
                case BoundPrismValueType.Integer:
                    return int.Parse(
                        value.Syntax.Text,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                case BoundPrismValueType.Number:
                    return float.Parse(
                        value.Syntax.Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f";
                case BoundPrismValueType.Color:
                    return ParseHexColor(value.Syntax.Text)?.ToExpression()
                        ?? throw new InvalidOperationException("A bound Prism color is invalid.");
                case BoundPrismValueType.Vector:
                    return "new global::System.Numerics.Vector4(" +
                        string.Join(", ", ParsePrismVector(value.Syntax.Text)) + ")";
                case BoundPrismValueType.Symbol:
                    return unchecked((int)Fnv1a32(value.Syntax.Text))
                        .ToString(CultureInfo.InvariantCulture);
                case BoundPrismValueType.Resource:
                    if (value.Syntax.Kind == PrismValueKind.NullLiteral)
                    {
                        return "default(global::Cerneala.UI.Prism.Definitions.PrismResourceId)";
                    }

                    uint hash = Fnv1a32(value.ResourceName ?? value.Syntax.Text);
                    int id = (int)(hash & 0x7fffffff);
                    return "new global::Cerneala.UI.Prism.Definitions.PrismResourceId(" +
                        (id == 0 ? 1 : id).ToString(CultureInfo.InvariantCulture) + ")";
                default:
                    throw new InvalidOperationException("Unknown bound Prism value type.");
            }
        }

        private static string EmitPrismParameterSymbol(string symbol, string propertyName)
        {
            if (propertyName == "BlendMode")
            {
                return "(int)global::Cerneala.Drawing.Prism.Catalog.PrismBlendMode." + symbol;
            }

            if (propertyName == "WorkingColorProfile")
            {
                return "(int)global::Cerneala.Drawing.Prism.Catalog.PrismColorProfile." + symbol;
            }

            if (propertyName == "Channel" && symbol is "Alpha" or "Luminance")
            {
                return "(int)global::Cerneala.UI.Prism.Definitions.PrismMaskChannel." + symbol;
            }

            if (propertyName == "BlendChannels" &&
                string.Equals(symbol, "RGBA", StringComparison.Ordinal))
            {
                return "(int)global::Cerneala.UI.Prism.Runtime.PrismBlendChannels.Rgba";
            }

            if (propertyName == "Knockout" && symbol is "None" or "Shallow" or "Deep")
            {
                return "(int)global::Cerneala.UI.Prism.Runtime.PrismKnockout." + symbol;
            }

            if (propertyName == "BlendIfChannel" &&
                symbol is "Gray" or "Red" or "Green" or "Blue")
            {
                return "(int)global::Cerneala.UI.Prism.Runtime.PrismBlendIfChannel." + symbol;
            }

            return unchecked((int)Fnv1a32(symbol)).ToString(CultureInfo.InvariantCulture);
        }

        private static string[] ParsePrismVector(string text)
        {
            string body = text.Trim();
            if (body.Length >= 2 && body[0] == '(' && body[body.Length - 1] == ')')
            {
                body = body.Substring(1, body.Length - 2);
            }

            return body.Split(',')
                .Select(part => float.Parse(
                    part.Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture) + "f")
                .ToArray();
        }

        private static string PrismPlaceholder(
            BoundPrismValueType type,
            string propertyName)
        {
            return type switch
            {
                BoundPrismValueType.Boolean => "false",
                BoundPrismValueType.Integer => "0",
                BoundPrismValueType.Number => "0f",
                BoundPrismValueType.Color => "global::Cerneala.Drawing.Color.Transparent",
                BoundPrismValueType.Vector => propertyName is "ThisLayerRange" or "UnderlyingRange"
                    ? "new global::Cerneala.UI.Prism.Runtime.PrismBlendRange(0f, 0f, 1f, 1f)"
                    : "global::System.Numerics.Vector4.Zero",
                BoundPrismValueType.Symbol => propertyName switch
                {
                    "WorkingColorProfile" =>
                        "global::Cerneala.Drawing.Prism.Catalog.PrismColorProfile.LinearSrgb",
                    "BlendMode" =>
                        "global::Cerneala.Drawing.Prism.Catalog.PrismBlendMode.Normal",
                    "Channel" =>
                        "global::Cerneala.UI.Prism.Definitions.PrismMaskChannel.Alpha",
                    "BlendChannels" =>
                        "global::Cerneala.UI.Prism.Runtime.PrismBlendChannels.Rgba",
                    "Knockout" =>
                        "global::Cerneala.UI.Prism.Runtime.PrismKnockout.None",
                    "BlendIfChannel" =>
                        "global::Cerneala.UI.Prism.Runtime.PrismBlendIfChannel.Gray",
                    _ => "0"
                },
                BoundPrismValueType.Resource =>
                    "new global::Cerneala.UI.Prism.Definitions.PrismResourceId(1)",
                _ => throw new InvalidOperationException("Unknown Prism placeholder type.")
            };
        }

        private static BoundPrismProperty? FindPrismProperty(
            IReadOnlyList<BoundPrismProperty> properties,
            string name)
        {
            return properties.FirstOrDefault(property =>
                string.Equals(property.Schema.Name, name, StringComparison.Ordinal));
        }

        private static string EmitPrismArray(
            string elementType,
            IEnumerable<string> values)
        {
            string[] items = values.ToArray();
            return items.Length == 0
                ? "global::System.Array.Empty<" + elementType + ">()"
                : "new " + elementType + "[] { " + string.Join(", ", items) + " }";
        }
    }
}
