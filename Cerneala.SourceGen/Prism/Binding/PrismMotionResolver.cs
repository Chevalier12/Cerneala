using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Cerneala.SourceGen.Prism;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private static readonly DiagnosticDescriptor PrismMotionTargetDiagnostic = new(
        "PRISM2010",
        "Invalid Prism Motion target",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismMotionNodeDiagnostic = new(
        "PRISM2011",
        "Unknown Prism Motion node",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismMotionPropertyDiagnostic = new(
        "PRISM2012",
        "Unknown Prism Motion property",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private sealed partial class GenerationScope
    {
        private enum PrismMotionAccessorKind
        {
            Node,
            Filter,
            Style,
            Mask
        }

        private sealed class PrismMotionAccessor
        {
            public PrismMotionAccessor(
                PrismMotionAccessorKind kind,
                BoundPrismNode node,
                PrismCatalogCompiler.CatalogProperty schema,
                BoundPrismValueType storageType,
                int operationIndex = -1,
                int entryStableId = 0,
                int slot = -1,
                bool usesCatalogKey = false)
            {
                Kind = kind;
                Node = node;
                Schema = schema;
                StorageType = storageType;
                OperationIndex = operationIndex;
                EntryStableId = entryStableId;
                Slot = slot;
                UsesCatalogKey = usesCatalogKey;
            }

            public PrismMotionAccessorKind Kind { get; }

            public BoundPrismNode Node { get; }

            public PrismCatalogCompiler.CatalogProperty Schema { get; }

            public BoundPrismValueType StorageType { get; }

            public int OperationIndex { get; }

            public int EntryStableId { get; }

            public int Slot { get; }

            public bool UsesCatalogKey { get; }
        }

        private sealed class ResolvedPrismMotionTarget
        {
            public ResolvedPrismMotionTarget(
                BoundPrismApplication application,
                BoundPrismNode node,
                BoundPrismValueType valueType,
                IReadOnlyList<PrismMotionAccessor> accessors,
                int propertyId,
                string? elementCode)
            {
                Application = application;
                Node = node;
                ValueType = valueType;
                Accessors = accessors;
                PropertyId = propertyId;
                ElementCode = elementCode;
            }

            public BoundPrismApplication Application { get; }

            public BoundPrismNode Node { get; }

            public BoundPrismValueType ValueType { get; }

            public IReadOnlyList<PrismMotionAccessor> Accessors { get; }

            public int PropertyId { get; }

            public string? ElementCode { get; }

            public bool IsDiscrete =>
                ValueType is BoundPrismValueType.Boolean or
                    BoundPrismValueType.Integer or
                    BoundPrismValueType.Symbol;
        }

        private bool TryResolvePrismMotionTarget(
            XElement applicationElement,
            AspectResource aspect,
            MotionAssignmentSyntax assignment,
            out ResolvedMotionTarget? target,
            out PropertySpec? property)
        {
            target = null;
            property = null;
            string[] segments = assignment.Target.Split('.');
            if (segments.Length < 4 ||
                !string.Equals(segments[1], "prism", StringComparison.Ordinal))
            {
                ReportPrismBinding(
                    PrismMotionPropertyDiagnostic,
                    assignment.Location,
                    "Prism Motion paths require an element, a named node and a property.");
                return false;
            }

            if (!TryResolvePrismMotionOwner(
                    applicationElement,
                    aspect,
                    assignment,
                    segments[0],
                    out ResolvedMotionTargetKind targetKind,
                    out XElement? targetElement,
                    out string? ownerName,
                    out string? elementCode))
            {
                return false;
            }

            if (!boundPrismApplications.TryGetValue(
                    targetElement!,
                    out BoundPrismApplication prismApplication))
            {
                ReportPrismBinding(
                    PrismMotionTargetDiagnostic,
                    PrismMotionSegmentLocation(assignment, 0),
                    "Motion target '" + segments[0] +
                    "' has no statically attached Prism composition.");
                return false;
            }

            string nodePath = string.Empty;
            BoundPrismNode? node = null;
            for (int index = 2; index < segments.Length - 1; index++)
            {
                nodePath = nodePath.Length == 0
                    ? segments[index]
                    : nodePath + "." + segments[index];
                if (!prismApplication.Composition.NodesByPath.TryGetValue(
                        nodePath,
                        out node))
                {
                    ReportPrismBinding(
                        PrismMotionNodeDiagnostic,
                        PrismMotionSegmentLocation(assignment, index),
                        "Prism node path '" + nodePath + "' does not exist.");
                    return false;
                }
            }

            string propertyName = segments[segments.Length - 1];
            string propertyPath = nodePath + "." + propertyName;
            BoundPrismValueType valueType;
            IReadOnlyList<PrismMotionAccessor> accessors;
            if (prismApplication.Composition.ParametersByPath.TryGetValue(
                    propertyPath,
                    out BoundPrismParameter parameter))
            {
                valueType = parameter.Type;
                accessors = FindPrismParameterAccessors(prismApplication, parameter);
            }
            else
            {
                PrismCatalogCompiler.CatalogProperty? schema =
                    PrismNodePropertySchemas(node!.Kind).FirstOrDefault(candidate =>
                        string.Equals(
                            candidate.Name,
                            propertyName,
                            StringComparison.Ordinal));
                if (schema is null)
                {
                    ReportPrismBinding(
                        PrismMotionPropertyDiagnostic,
                        PrismMotionSegmentLocation(
                            assignment,
                            segments.Length - 1),
                        "Prism node '" + nodePath +
                        "' has no property or scoped parameter named '" +
                        propertyName + "'.");
                    return false;
                }

                valueType = CatalogValueType(schema.ValueType);
                accessors =
                [
                    new PrismMotionAccessor(
                        PrismMotionAccessorKind.Node,
                        node,
                        schema,
                        valueType)
                ];
            }

            if (accessors.Count == 0 ||
                !TryCreatePrismMotionPropertySpec(
                    valueType,
                    propertyName,
                    node!.Id,
                    out property))
            {
                ReportPrismBinding(
                    PrismMotionPropertyDiagnostic,
                    PrismMotionSegmentLocation(
                        assignment,
                        segments.Length - 1),
                    "Prism property or parameter '" + propertyPath +
                    "' is not animatable.");
                return false;
            }

            int propertyId = unchecked((int)Fnv1a32(
                prismApplication.Composition.Name + "|" + propertyPath));
            ResolvedPrismMotionTarget prismTarget = new(
                prismApplication,
                node,
                valueType,
                accessors,
                propertyId,
                elementCode);
            target = new ResolvedMotionTarget(
                targetKind,
                targetElement!,
                ownerName,
                prism: prismTarget);
            return true;
        }

        private bool TryResolvePrismMotionOwner(
            XElement applicationElement,
            AspectResource aspect,
            MotionAssignmentSyntax assignment,
            string ownerSegment,
            out ResolvedMotionTargetKind targetKind,
            out XElement? targetElement,
            out string? ownerName,
            out string? elementCode)
        {
            targetKind = ResolvedMotionTargetKind.Self;
            targetElement = applicationElement;
            ownerName = ownerSegment.Length > 1
                ? ownerSegment.Substring(1)
                : string.Empty;
            elementCode = null;
            if (!ownerSegment.StartsWith("$", StringComparison.Ordinal))
            {
                ReportPrismBinding(
                    PrismMotionTargetDiagnostic,
                    PrismMotionSegmentLocation(assignment, 0),
                    "A Prism Motion path must start with $self, $owner or a named element.");
                return false;
            }

            if (ownerName == "self")
            {
                return true;
            }

            if (ownerName == "owner")
            {
                if (templateEmissionContexts.Count == 0)
                {
                    ReportPrismBinding(
                        PrismMotionTargetDiagnostic,
                        PrismMotionSegmentLocation(assignment, 0),
                        "$owner is available only inside a component template.");
                    return false;
                }

                targetKind = ResolvedMotionTargetKind.Owner;
                targetElement = templateEmissionContexts.Peek().OwnerElement;
                if (targetElement is null ||
                    !boundPrismApplications.ContainsKey(targetElement))
                {
                    ReportPrismBinding(
                        PrismMotionTargetDiagnostic,
                        PrismMotionSegmentLocation(assignment, 0),
                        "The component template owner has no statically known Prism.");
                    return false;
                }

                return true;
            }

            if (!TryResolveNamedElement(
                    assignment.Location.Source,
                    ownerName,
                    out NamedElementReference named) ||
                !IsInSameBindingNameScope(
                    assignment.Location.Source,
                    named.Element))
            {
                ReportPrismBinding(
                    PrismMotionTargetDiagnostic,
                    PrismMotionSegmentLocation(assignment, 0),
                    "Prism Motion target named element '" + ownerName +
                    "' is not available at this application site.");
                return false;
            }

            targetKind = ResolvedMotionTargetKind.Named;
            targetElement = named.Element;
            elementCode = named.Code;
            return true;
        }

        private bool TryCreatePrismMotionPropertySpec(
            BoundPrismValueType valueType,
            string propertyName,
            int nodeId,
            out PropertySpec? property)
        {
            ITypeSymbol? valueSymbol;
            MarkupValueKind valueKind;
            switch (valueType)
            {
                case BoundPrismValueType.Boolean:
                    valueSymbol = compilation.GetSpecialType(
                        SpecialType.System_Boolean);
                    valueKind = MarkupValueKind.Bool;
                    break;
                case BoundPrismValueType.Integer:
                    valueSymbol = compilation.GetSpecialType(
                        SpecialType.System_Int32);
                    valueKind = MarkupValueKind.Integer;
                    break;
                case BoundPrismValueType.Number:
                    valueSymbol = compilation.GetSpecialType(
                        SpecialType.System_Single);
                    valueKind = MarkupValueKind.Float;
                    break;
                case BoundPrismValueType.Color:
                    valueSymbol = compilation.GetTypeByMetadataName(
                        "Cerneala.Drawing.Color");
                    valueKind = MarkupValueKind.Color;
                    break;
                case BoundPrismValueType.Symbol:
                    valueSymbol = PrismMotionSymbolType(propertyName);
                    valueKind = MarkupValueKind.Enum;
                    break;
                default:
                    property = null;
                    return false;
            }

            if (valueSymbol is null)
            {
                property = null;
                return false;
            }

            property = new PropertySpec(
                propertyName,
                valueKind,
                "__prism_" + nodeId.ToString(CultureInfo.InvariantCulture) +
                    "_" + propertyName,
                valueSymbol);
            return true;
        }

        private ITypeSymbol? PrismMotionSymbolType(string propertyName)
        {
            string? metadataName = propertyName switch
            {
                "BlendMode" =>
                    "Cerneala.Drawing.Prism.Catalog.PrismBlendMode",
                "BlendChannels" =>
                    "Cerneala.UI.Prism.Runtime.PrismBlendChannels",
                "Knockout" =>
                    "Cerneala.UI.Prism.Runtime.PrismKnockout",
                "BlendIfChannel" =>
                    "Cerneala.UI.Prism.Runtime.PrismBlendIfChannel",
                _ => null
            };
            return metadataName is null
                ? null
                : compilation.GetTypeByMetadataName(metadataName);
        }

        private IReadOnlyList<PrismMotionAccessor> FindPrismParameterAccessors(
            BoundPrismApplication application,
            BoundPrismParameter parameter)
        {
            List<PrismMotionAccessor> accessors = [];
            foreach (BoundPrismNode node in application.Composition.Nodes)
            {
                CollectPrismParameterAccessors(
                    application,
                    parameter,
                    node,
                    accessors);
            }

            return accessors;
        }

        private void CollectPrismParameterAccessors(
            BoundPrismApplication application,
            BoundPrismParameter parameter,
            BoundPrismNode node,
            ICollection<PrismMotionAccessor> accessors)
        {
            foreach (BoundPrismProperty property in node.Properties)
            {
                if (PrismValueDependsOnParameter(
                        property.Value,
                        application,
                        parameter))
                {
                    accessors.Add(new PrismMotionAccessor(
                        PrismMotionAccessorKind.Node,
                        node,
                        property.Schema,
                        property.Value.Type));
                }
            }

            for (int index = 0; index < node.Filters.Count; index++)
            {
                CollectPrismOperationParameterAccessors(
                    application,
                    parameter,
                    node,
                    node.Filters[index],
                    PrismMotionAccessorKind.Filter,
                    index,
                    accessors);
            }

            for (int index = 0; index < node.Styles.Count; index++)
            {
                CollectPrismOperationParameterAccessors(
                    application,
                    parameter,
                    node,
                    node.Styles[index],
                    PrismMotionAccessorKind.Style,
                    index,
                    accessors);
            }

            if (node.Mask is not null)
            {
                foreach (BoundPrismProperty property in node.Mask.Properties)
                {
                    if (PrismValueDependsOnParameter(
                            property.Value,
                            application,
                            parameter))
                    {
                        accessors.Add(new PrismMotionAccessor(
                            PrismMotionAccessorKind.Mask,
                            node,
                            property.Schema,
                            property.Value.Type));
                    }
                }
            }

            foreach (BoundPrismNode child in node.Children)
            {
                CollectPrismParameterAccessors(
                    application,
                    parameter,
                    child,
                    accessors);
            }
        }

        private void CollectPrismOperationParameterAccessors(
            BoundPrismApplication application,
            BoundPrismParameter parameter,
            BoundPrismNode node,
            BoundPrismOperation operation,
            PrismMotionAccessorKind kind,
            int operationIndex,
            ICollection<PrismMotionAccessor> accessors)
        {
            PrismCatalogCompiler.CatalogEntry entry = operation.CatalogEntry
                ?? throw new InvalidOperationException(
                    "A Prism Motion operation has no catalog entry.");
            foreach (BoundPrismProperty property in operation.Properties)
            {
                if (!PrismValueDependsOnParameter(
                        property.Value,
                        application,
                        parameter))
                {
                    continue;
                }

                bool usesCatalogKey = entry.Properties.Any(candidate =>
                    ReferenceEquals(candidate, property.Schema));
                accessors.Add(new PrismMotionAccessor(
                    kind,
                    node,
                    property.Schema,
                    property.Value.Type,
                    operationIndex,
                    entry.StableId,
                    usesCatalogKey
                        ? GetPrismParameterSlot(entry, property.Schema)
                        : -1,
                    usesCatalogKey));
            }
        }

        private static bool PrismValueDependsOnParameter(
            BoundPrismValue value,
            BoundPrismApplication application,
            BoundPrismParameter target)
        {
            HashSet<BoundPrismParameter> visited = new();
            BoundPrismValue current = value;
            while (current.Parameter is BoundPrismParameter parameter)
            {
                if (ReferenceEquals(parameter, target))
                {
                    return true;
                }

                if (!visited.Add(parameter))
                {
                    return false;
                }

                if (!application.Arguments.TryGetValue(
                        parameter.Path,
                        out BoundPrismValue? next))
                {
                    next = parameter.DefaultValue;
                }

                if (next is null)
                {
                    return false;
                }

                current = next;
            }

            return false;
        }

        private static IReadOnlyList<PrismCatalogCompiler.CatalogProperty>
            PrismNodePropertySchemas(PrismContainerKind kind)
        {
            PrismCatalogModel catalog = PrismCatalog.Value;
            return kind switch
            {
                PrismContainerKind.Layer => catalog.LayerProperties,
                PrismContainerKind.Group => catalog.GroupProperties,
                PrismContainerKind.Backdrop => catalog.BackdropProperties,
                _ => Array.Empty<PrismCatalogCompiler.CatalogProperty>()
            };
        }

        private static DirectiveExpressionLocation PrismMotionSegmentLocation(
            MotionAssignmentSyntax assignment,
            int segmentIndex)
        {
            int offset = 0;
            for (int index = 0; index < segmentIndex; index++)
            {
                int separator = assignment.Target.IndexOf('.', offset);
                offset = separator < 0 ? assignment.Target.Length : separator + 1;
            }

            int end = assignment.Target.IndexOf('.', offset);
            int length = (end < 0 ? assignment.Target.Length : end) - offset;
            return new DirectiveExpressionLocation(
                assignment.Location.Source,
                assignment.Location.Offset + offset,
                Math.Max(1, length));
        }
    }
}
