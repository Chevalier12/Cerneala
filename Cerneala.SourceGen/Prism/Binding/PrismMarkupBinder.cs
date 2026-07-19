using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Cerneala.SourceGen.Prism;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private const string PrismCatalogResourceName =
        "Cerneala.SourceGen.Prism.prism-catalog.json";

    private static readonly Lazy<PrismCatalogModel> PrismCatalog =
        new(LoadPrismCatalog);

    private static readonly DiagnosticDescriptor PrismUnknownPropertyDiagnostic = new(
        "PRISM2001",
        "Unknown Prism property",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismUnknownSymbolDiagnostic = new(
        "PRISM2002",
        "Unknown Prism symbol",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismDuplicateNameDiagnostic = new(
        "PRISM2003",
        "Duplicate Prism name",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismParameterDiagnostic = new(
        "PRISM2004",
        "Invalid Prism parameter",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismNestingDiagnostic = new(
        "PRISM2005",
        "Invalid Prism nesting",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismBackdropCountDiagnostic = new(
        "PRISM2006",
        "Multiple Prism backdrops",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismBackdropOrderDiagnostic = new(
        "PRISM2007",
        "Invalid Prism backdrop order",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismClipToBelowDiagnostic = new(
        "PRISM2008",
        "Invalid Prism clipping base",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismValueDiagnostic = new(
        "PRISM2009",
        "Invalid Prism value",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static readonly DiagnosticDescriptor PrismStructureDiagnostic = new(
        "PRISM2013",
        "Invalid Prism structure",
        "Prism binding in '{0}' failed: {1}",
        "Cerneala.Prism.Binding",
        DiagnosticSeverity.Error,
        true);

    private static PrismCatalogModel LoadPrismCatalog()
    {
        using Stream stream = typeof(UiMarkupGenerator).Assembly
            .GetManifestResourceStream(PrismCatalogResourceName)
            ?? throw new InvalidOperationException(
                "The normative Prism catalog is not embedded in Cerneala.SourceGen.");
        using StreamReader reader = new(stream);
        PrismCatalogCompilation compilation = PrismCatalogCompiler.Compile(reader.ReadToEnd());
        if (compilation.Model is null || !compilation.Issues.IsEmpty)
        {
            string issues = string.Join(
                "; ",
                compilation.Issues.Select(issue => issue.Id + ": " + issue.Message));
            throw new InvalidOperationException(
                "The embedded Prism catalog is invalid. " + issues);
        }

        return compilation.Model;
    }

    private sealed partial class GenerationScope
    {
        private readonly Dictionary<ResourceScope, Dictionary<string, BoundPrismComposition>>
            boundPrismResources = new();
        private readonly Dictionary<XElement, BoundPrismApplication> boundPrismApplications = new();
        private int prismBindingDiagnosticCount;
        private int nextInlinePrismId;

        private sealed class PrismParameterScope
        {
            private readonly Dictionary<string, BoundPrismParameter> parameters =
                new(StringComparer.Ordinal);

            public PrismParameterScope(PrismParameterScope? parent)
            {
                Parent = parent;
            }

            public PrismParameterScope? Parent { get; }

            public bool TryAdd(BoundPrismParameter parameter)
            {
                if (parameters.ContainsKey(parameter.Name))
                {
                    return false;
                }

                parameters.Add(parameter.Name, parameter);
                return true;
            }

            public bool ContainsLocal(string name) => parameters.ContainsKey(name);

            public bool TryResolve(string name, out BoundPrismParameter parameter)
            {
                for (PrismParameterScope? scope = this; scope is not null; scope = scope.Parent)
                {
                    if (scope.parameters.TryGetValue(name, out parameter))
                    {
                        return true;
                    }
                }

                parameter = null!;
                return false;
            }
        }

        private sealed class PrismBindingState
        {
            public int NextNodeId { get; set; } = 1;

            public Dictionary<string, BoundPrismParameter> ParametersByPath { get; } =
                new(StringComparer.Ordinal);

            public Dictionary<string, BoundPrismNode> NodesByPath { get; } =
                new(StringComparer.Ordinal);
        }

        private sealed class PrismPropertyBindingSpec
        {
            public PrismPropertyBindingSpec(
                PrismCatalogCompiler.CatalogProperty schema,
                string keyExpression,
                string family)
            {
                Schema = schema;
                KeyExpression = keyExpression;
                Family = family;
            }

            public PrismCatalogCompiler.CatalogProperty Schema { get; }

            public string KeyExpression { get; }

            public string Family { get; }
        }

        private void BindPrism()
        {
            foreach (ResourceScope scope in resourceScopes.Values)
            {
                BindPrismResources(scope);
            }

            if (prismBindingDiagnosticCount > 0)
            {
                return;
            }

            foreach (KeyValuePair<XElement, DirectiveParseResult> pair in directiveContent)
            {
                XElement owner = pair.Key;
                DirectiveParseResult result = pair.Value;
                if (result.PrismDiagnostics.Count > 0)
                {
                    continue;
                }

                DirectivePrismNode[] applications =
                    result.Nodes.OfType<DirectivePrismNode>().ToArray();
                if (applications.Length > 1)
                {
                    ReportPrismBinding(
                        PrismStructureDiagnostic,
                        applications[1].Application.Location,
                        "An element may declare only one @prism application.");
                    continue;
                }

                if (applications.Length == 1)
                {
                    BindPrismApplication(owner, applications[0].Application);
                }
            }
        }

        private void BindPrismResources(ResourceScope scope)
        {
            Dictionary<string, BoundPrismComposition> resources =
                new(StringComparer.Ordinal);
            boundPrismResources.Add(scope, resources);
            foreach (PrismCompositionResourceSyntax syntax in scope.PrismCompositions)
            {
                if (resources.ContainsKey(syntax.Name) ||
                    scope.NamedResources.ContainsKey(syntax.Name))
                {
                    ReportPrismBinding(
                        PrismDuplicateNameDiagnostic,
                        syntax.NameLocation,
                        "Resource name '" + syntax.Name + "' is duplicated in the same scope.");
                    continue;
                }

                int diagnosticsBefore = prismBindingDiagnosticCount;
                BoundPrismComposition composition = BindPrismComposition(
                    syntax.Name,
                    syntax.Composition,
                    syntax.Source,
                    isReusable: true);
                if (prismBindingDiagnosticCount == diagnosticsBefore)
                {
                    resources.Add(syntax.Name, composition);
                }
            }
        }

        private void BindPrismApplication(XElement owner, PrismApplicationSyntax syntax)
        {
            int diagnosticsBefore = prismBindingDiagnosticCount;
            BoundPrismApplication? application = syntax switch
            {
                PrismInlineApplicationSyntax inline => BindInlinePrismApplication(owner, inline),
                PrismResourceApplicationSyntax resource => BindResourcePrismApplication(owner, resource),
                _ => null
            };
            if (application is not null &&
                prismBindingDiagnosticCount == diagnosticsBefore)
            {
                boundPrismApplications.Add(owner, application);
            }
        }

        private BoundPrismApplication BindInlinePrismApplication(
            XElement owner,
            PrismInlineApplicationSyntax syntax)
        {
            string name = "InlinePrism" +
                nextInlinePrismId.ToString(CultureInfo.InvariantCulture);
            nextInlinePrismId++;
            BoundPrismComposition composition = BindPrismComposition(
                name,
                syntax.Composition,
                owner,
                isReusable: false);
            return new BoundPrismApplication(
                composition,
                new Dictionary<string, BoundPrismValue>(StringComparer.Ordinal),
                syntax,
                owner);
        }

        private BoundPrismApplication? BindResourcePrismApplication(
            XElement owner,
            PrismResourceApplicationSyntax syntax)
        {
            if (!TryResolvePrismComposition(owner, syntax.ResourceName, out BoundPrismComposition composition))
            {
                ReportPrismBinding(
                    PrismUnknownSymbolDiagnostic,
                    syntax.ResourceLocation,
                    "Unknown PrismComposition resource '$" + syntax.ResourceName + "'.");
                return null;
            }

            Dictionary<string, BoundPrismValue> arguments =
                new(StringComparer.Ordinal);
            foreach (PrismAssignmentSyntax argument in syntax.Arguments)
            {
                if (!composition.ParametersByPath.TryGetValue(
                        argument.Name,
                        out BoundPrismParameter parameter))
                {
                    ReportPrismBinding(
                        PrismParameterDiagnostic,
                        argument.NameLocation,
                        "Unknown Prism parameter path '" + argument.Name + "'.");
                    continue;
                }

                if (arguments.ContainsKey(argument.Name))
                {
                    ReportPrismBinding(
                        PrismDuplicateNameDiagnostic,
                        argument.NameLocation,
                        "Prism parameter '" + argument.Name + "' is assigned more than once.");
                    continue;
                }

                BoundPrismValue? value = BindPrismValue(
                    argument.Value,
                    parameter.Type,
                    scope: null,
                    schema: null,
                    family: "parameter");
                if (value is not null)
                {
                    arguments.Add(argument.Name, value);
                }
            }

            foreach (KeyValuePair<string, BoundPrismParameter> pair in composition.ParametersByPath)
            {
                string path = pair.Key;
                BoundPrismParameter parameter = pair.Value;
                if (arguments.ContainsKey(path))
                {
                    continue;
                }

                if (parameter.DefaultValue is null)
                {
                    ReportPrismBinding(
                        PrismParameterDiagnostic,
                        syntax.ResourceLocation,
                        "Required Prism parameter '" + path + "' has no application value.");
                    break;
                }

                arguments.Add(path, parameter.DefaultValue);
            }

            return new BoundPrismApplication(composition, arguments, syntax, owner);
        }

        private BoundPrismComposition BindPrismComposition(
            string name,
            PrismContainerSyntax syntax,
            XElement source,
            bool isReusable)
        {
            int diagnosticsBefore = prismBindingDiagnosticCount;
            PrismBindingState state = new();
            PrismParameterScope compositionScope = BindPrismParameters(
                syntax.Members,
                parent: null,
                addressPath: string.Empty,
                localIdentity: "composition",
                state,
                out List<BoundPrismParameter> parameters);
            List<BoundPrismProperty> properties = BindPrismAssignments(
                syntax.Members,
                PrismPropertySpecs(
                    PrismCatalog.Value.CompositionProperties,
                    "PrismCompositionPropertyKeys",
                    "composition"),
                compositionScope);

            PrismContainerSyntax[] nodeSyntax =
                syntax.Members.OfType<PrismContainerSyntax>().ToArray();
            ValidateSiblingNames(nodeSyntax);
            ValidateBackdropShape(nodeSyntax);

            List<BoundPrismNode> nodes = [];
            foreach (PrismContainerSyntax node in nodeSyntax)
            {
                BoundPrismNode? bound = BindPrismNode(
                    node,
                    compositionScope,
                    parentAddressPath: string.Empty,
                    state);
                if (bound is not null)
                {
                    nodes.Add(bound);
                }
            }

            if (prismBindingDiagnosticCount == diagnosticsBefore)
            {
                if (nodes.Count == 0)
                {
                    ReportPrismBinding(
                        PrismStructureDiagnostic,
                        syntax.Location,
                        "A Prism composition must contain at least one layer, group, or backdrop.");
                }
                else
                {
                    ValidateClipToBelow(nodes);
                }
            }

            return new BoundPrismComposition(
                name,
                properties,
                parameters,
                nodes,
                state.ParametersByPath,
                state.NodesByPath,
                syntax,
                source,
                isReusable);
        }

        private BoundPrismNode? BindPrismNode(
            PrismContainerSyntax syntax,
            PrismParameterScope parentScope,
            string? parentAddressPath,
            PrismBindingState state)
        {
            int diagnosticsBefore = prismBindingDiagnosticCount;
            int id = state.NextNodeId;
            state.NextNodeId++;
            string? addressPath = syntax.Name is null || parentAddressPath is null
                ? null
                : CombinePrismPath(parentAddressPath, syntax.Name);
            PrismParameterScope scope = BindPrismParameters(
                syntax.Members,
                parentScope,
                addressPath,
                "node" + id.ToString(CultureInfo.InvariantCulture),
                state,
                out List<BoundPrismParameter> parameters);

            IReadOnlyList<PrismCatalogCompiler.CatalogProperty> schemas;
            string keys;
            string family;
            switch (syntax.Kind)
            {
                case PrismContainerKind.Layer:
                    schemas = PrismCatalog.Value.LayerProperties;
                    keys = "PrismLayerPropertyKeys";
                    family = "layer";
                    break;
                case PrismContainerKind.Group:
                    schemas = PrismCatalog.Value.GroupProperties;
                    keys = "PrismGroupPropertyKeys";
                    family = "group";
                    break;
                case PrismContainerKind.Backdrop:
                    schemas = PrismCatalog.Value.BackdropProperties;
                    keys = "PrismBackdropPropertyKeys";
                    family = "backdrop";
                    break;
                default:
                    schemas = Array.Empty<PrismCatalogCompiler.CatalogProperty>();
                    keys = string.Empty;
                    family = "composition";
                    break;
            }
            List<BoundPrismProperty> properties = BindPrismAssignments(
                syntax.Members,
                PrismPropertySpecs(schemas, keys, family),
                scope);

            List<BoundPrismNode> children = [];
            PrismContainerSyntax[] childSyntax =
                syntax.Members.OfType<PrismContainerSyntax>().ToArray();
            if (syntax.Kind == PrismContainerKind.Group)
            {
                ValidateSiblingNames(childSyntax);
            }

            foreach (PrismContainerSyntax child in childSyntax)
            {
                if (syntax.Kind != PrismContainerKind.Group ||
                    child.Kind == PrismContainerKind.Backdrop)
                {
                    ReportPrismBinding(
                        PrismNestingDiagnostic,
                        child.NameLocation ?? child.Location,
                        PrismContainerDisplay(child.Kind) +
                        " cannot be nested inside " +
                        PrismContainerDisplay(syntax.Kind) + ".");
                    continue;
                }

                BoundPrismNode? boundChild =
                    BindPrismNode(child, scope, addressPath, state);
                if (boundChild is not null)
                {
                    children.Add(boundChild);
                }
            }

            List<BoundPrismOperation> filters = [];
            List<BoundPrismOperation> styles = [];
            BoundPrismOperation? mask = null;
            foreach (PrismOperationSyntax operationSyntax in
                syntax.Members.OfType<PrismOperationSyntax>())
            {
                BoundPrismOperation? operation =
                    BindPrismOperation(operationSyntax, scope);
                if (operation is null)
                {
                    continue;
                }

                switch (operation.Kind)
                {
                    case PrismOperationKind.Filter:
                        filters.Add(operation);
                        break;
                    case PrismOperationKind.Style:
                        styles.Add(operation);
                        break;
                    case PrismOperationKind.Mask when mask is null:
                        mask = operation;
                        break;
                    case PrismOperationKind.Mask:
                        ReportPrismBinding(
                            PrismStructureDiagnostic,
                            operation.Syntax.Location,
                            PrismContainerDisplay(syntax.Kind) +
                            " may declare at most one @mask.");
                        break;
                }
            }

            if (prismBindingDiagnosticCount == diagnosticsBefore)
            {
                if (syntax.Kind == PrismContainerKind.Group && children.Count == 0)
                {
                    ReportPrismBinding(
                        PrismStructureDiagnostic,
                        syntax.NameLocation ?? syntax.Location,
                        "A Prism group must contain at least one layer or nested group.");
                }
                else if (syntax.Kind is PrismContainerKind.Layer or PrismContainerKind.Backdrop &&
                    filters.Count == 0 &&
                    styles.Count == 0)
                {
                    ReportPrismBinding(
                        PrismStructureDiagnostic,
                        syntax.NameLocation ?? syntax.Location,
                        PrismContainerDisplay(syntax.Kind) +
                        " must contain at least one filter or style.");
                }
            }

            BoundPrismNode node = new(
                id,
                syntax.Kind,
                syntax.Name,
                addressPath,
                properties,
                parameters,
                children,
                filters,
                styles,
                mask,
                syntax);
            if (addressPath is not null)
            {
                if (!state.NodesByPath.ContainsKey(addressPath))
                {
                    state.NodesByPath.Add(addressPath, node);
                }
            }

            return node;
        }

        private BoundPrismOperation? BindPrismOperation(
            PrismOperationSyntax syntax,
            PrismParameterScope scope)
        {
            PrismCatalogCompiler.CatalogEntry? entry = null;
            List<PrismPropertyBindingSpec> specs = [];
            switch (syntax.Kind)
            {
                case PrismOperationKind.Filter:
                case PrismOperationKind.Style:
                    string kind = syntax.Kind == PrismOperationKind.Filter
                        ? "filter"
                        : "style";
                    entry = PrismCatalog.Value.Entries.FirstOrDefault(candidate =>
                        string.Equals(candidate.Kind, kind, StringComparison.Ordinal) &&
                        string.Equals(candidate.Symbol, syntax.TypeName, StringComparison.Ordinal));
                    if (entry is null)
                    {
                        ReportPrismBinding(
                            PrismUnknownSymbolDiagnostic,
                            syntax.TypeLocation ?? syntax.Location,
                            "Unknown Prism " + kind + " '" + syntax.TypeName + "'.");
                        return null;
                    }

                    string requiredCapability = syntax.Kind == PrismOperationKind.Filter
                        ? "pixel-processing"
                        : "layer-style";
                    if (!entry.Capabilities.Contains(requiredCapability, StringComparer.Ordinal))
                    {
                        ReportPrismBinding(
                            PrismValueDiagnostic,
                            syntax.TypeLocation ?? syntax.Location,
                            "Prism " + kind + " '" + entry.Symbol +
                            "' lacks capability '" + requiredCapability + "'.");
                        return null;
                    }

                    string commonKeys = syntax.Kind == PrismOperationKind.Filter
                        ? "PrismFilterCommonParameterKeys"
                        : "PrismStyleCommonParameterKeys";
                    string entryKeys = syntax.Kind == PrismOperationKind.Filter
                        ? "PrismFilterParameterKeys"
                        : "PrismStyleParameterKeys";
                    string family = syntax.Kind == PrismOperationKind.Filter
                        ? "filter"
                        : "style";
                    IReadOnlyList<PrismCatalogCompiler.CatalogProperty> common =
                        syntax.Kind == PrismOperationKind.Filter
                            ? PrismCatalog.Value.FilterProperties
                            : PrismCatalog.Value.StyleProperties;
                    specs.AddRange(PrismPropertySpecs(common, commonKeys, family));
                    specs.AddRange(PrismPropertySpecs(
                        entry.Properties,
                        entryKeys + "." + entry.Symbol,
                        family));
                    break;
                case PrismOperationKind.Mask:
                    specs.AddRange(PrismPropertySpecs(
                        PrismCatalog.Value.MaskProperties,
                        "PrismMaskPropertyKeys",
                        "mask"));
                    break;
            }

            List<BoundPrismProperty> properties =
                BindPrismAssignments(syntax.Members, specs, scope);
            return new BoundPrismOperation(
                syntax.Kind,
                entry,
                properties,
                syntax);
        }

        private PrismParameterScope BindPrismParameters(
            IReadOnlyList<PrismMemberSyntax> members,
            PrismParameterScope? parent,
            string? addressPath,
            string localIdentity,
            PrismBindingState state,
            out List<BoundPrismParameter> parameters)
        {
            PrismParameterScope scope = new(parent);
            parameters = [];
            foreach (PrismParameterSyntax syntax in members.OfType<PrismParameterSyntax>())
            {
                if (scope.ContainsLocal(syntax.Name))
                {
                    ReportPrismBinding(
                        PrismDuplicateNameDiagnostic,
                        syntax.NameLocation,
                        "Prism parameter '" + syntax.Name + "' is duplicated in the same scope.");
                    continue;
                }

                if (!TryParsePrismParameterType(syntax.TypeName, out BoundPrismValueType type))
                {
                    ReportPrismBinding(
                        PrismParameterDiagnostic,
                        syntax.TypeLocation,
                        "Unknown Prism parameter type '" + syntax.TypeName + "'.");
                    continue;
                }

                BoundPrismValue? defaultValue = syntax.DefaultValue is null
                    ? null
                    : BindPrismValue(
                        syntax.DefaultValue,
                        type,
                        scope,
                        schema: null,
                        family: "parameter");
                string path = addressPath is null
                    ? "<" + localIdentity + ">." + syntax.Name
                    : CombinePrismPath(addressPath, syntax.Name);
                BoundPrismParameter parameter = new(
                    syntax.Name,
                    path,
                    type,
                    defaultValue,
                    syntax);
                scope.TryAdd(parameter);
                parameters.Add(parameter);
                if (addressPath is not null)
                {
                    if (!state.ParametersByPath.ContainsKey(path))
                    {
                        state.ParametersByPath.Add(path, parameter);
                    }
                }
            }

            return scope;
        }

        private List<BoundPrismProperty> BindPrismAssignments(
            IReadOnlyList<PrismMemberSyntax> members,
            IEnumerable<PrismPropertyBindingSpec> propertySpecs,
            PrismParameterScope scope)
        {
            Dictionary<string, PrismPropertyBindingSpec> specs =
                propertySpecs.ToDictionary(spec => spec.Schema.Name, StringComparer.Ordinal);
            Dictionary<string, BoundPrismProperty> properties =
                new(StringComparer.Ordinal);
            int diagnosticsBefore = prismBindingDiagnosticCount;
            foreach (PrismAssignmentSyntax assignment in members.OfType<PrismAssignmentSyntax>())
            {
                if (!specs.TryGetValue(assignment.Name, out PrismPropertyBindingSpec spec))
                {
                    ReportPrismBinding(
                        PrismUnknownPropertyDiagnostic,
                        assignment.NameLocation,
                        "Unknown Prism property '" + assignment.Name + "'.");
                    continue;
                }

                if (properties.ContainsKey(assignment.Name))
                {
                    ReportPrismBinding(
                        PrismDuplicateNameDiagnostic,
                        assignment.NameLocation,
                        "Prism property '" + assignment.Name + "' is assigned more than once.");
                    continue;
                }

                BoundPrismValueType type = CatalogValueType(spec.Schema.ValueType);
                BoundPrismValue? value = BindPrismValue(
                    assignment.Value,
                    type,
                    scope,
                    spec.Schema,
                    spec.Family);
                if (value is not null)
                {
                    properties.Add(
                        assignment.Name,
                        new BoundPrismProperty(
                            spec.Schema,
                            value,
                            assignment,
                            spec.KeyExpression));
                }
            }

            if (prismBindingDiagnosticCount == diagnosticsBefore)
            {
                PrismPropertyBindingSpec? missing = specs.Values.FirstOrDefault(spec =>
                    spec.Schema.Required &&
                    spec.Schema.DefaultValue is null &&
                    !properties.ContainsKey(spec.Schema.Name));
                if (missing is not null)
                {
                    object location = members.FirstOrDefault() is PrismMemberSyntax first
                        ? first.Location
                        : document.Root;
                    ReportPrismBinding(
                        PrismValueDiagnostic,
                        location,
                        "Required Prism property '" + missing.Schema.Name + "' is missing.");
                }
            }

            return properties.Values.ToList();
        }

        private BoundPrismValue? BindPrismValue(
            PrismValueSyntax syntax,
            BoundPrismValueType expectedType,
            PrismParameterScope? scope,
            PrismCatalogCompiler.CatalogProperty? schema,
            string family)
        {
            if (syntax.Kind == PrismValueKind.Identifier &&
                scope is not null &&
                scope.TryResolve(syntax.Text, out BoundPrismParameter parameter))
            {
                if (!CanConvertPrismValue(parameter.Type, expectedType))
                {
                    ReportPrismBinding(
                        PrismValueDiagnostic,
                        syntax.Location,
                        "Prism parameter '" + parameter.Name + "' has type " +
                        DisplayPrismValueType(parameter.Type) + ", not " +
                        DisplayPrismValueType(expectedType) + ".");
                    return null;
                }

                return new BoundPrismValue(expectedType, syntax, parameter);
            }

            bool valid = expectedType switch
            {
                BoundPrismValueType.Boolean =>
                    syntax.Kind == PrismValueKind.BooleanLiteral,
                BoundPrismValueType.Integer =>
                    syntax.Kind == PrismValueKind.NumberLiteral &&
                    int.TryParse(
                        syntax.Text,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out _),
                BoundPrismValueType.Number =>
                    syntax.Kind == PrismValueKind.NumberLiteral &&
                    float.TryParse(
                        syntax.Text,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float number) &&
                    !float.IsNaN(number) &&
                    !float.IsInfinity(number),
                BoundPrismValueType.Color =>
                    syntax.Kind == PrismValueKind.ColorLiteral &&
                    ParseHexColor(syntax.Text) is not null,
                BoundPrismValueType.Vector =>
                    syntax.Kind == PrismValueKind.TupleLiteral &&
                    IsValidPrismVector(syntax.Text),
                BoundPrismValueType.Symbol =>
                    syntax.Kind == PrismValueKind.Identifier &&
                    SyntaxFacts.IsValidIdentifier(syntax.Text),
                BoundPrismValueType.Resource =>
                    syntax.Kind is PrismValueKind.ResourceReference or PrismValueKind.NullLiteral,
                _ => false
            };
            if (!valid)
            {
                ReportPrismBinding(
                    PrismValueDiagnostic,
                    syntax.Location,
                    "Value '" + syntax.Text + "' is not a valid " +
                    DisplayPrismValueType(expectedType) + " Prism value.");
                return null;
            }

            if (schema is not null &&
                !ValidatePrismDomain(schema, syntax, family))
            {
                return null;
            }

            string? resourceName = null;
            if (expectedType == BoundPrismValueType.Resource &&
                syntax.Kind == PrismValueKind.ResourceReference)
            {
                resourceName = syntax.Text.Substring(1);
                if (!TryResolveResource(syntax.Location.Source, resourceName, out NamedSymbol symbol) ||
                    symbol.Source is not BrushResource)
                {
                    ReportPrismBinding(
                        PrismValueDiagnostic,
                        syntax.Location,
                        "Unknown or incompatible typed Prism resource '$" +
                        resourceName + "'.");
                    return null;
                }
            }

            return new BoundPrismValue(expectedType, syntax, resourceName: resourceName);
        }

        private bool ValidatePrismDomain(
            PrismCatalogCompiler.CatalogProperty schema,
            PrismValueSyntax syntax,
            string family)
        {
            if (schema.ValueType is "integer" or "number" &&
                double.TryParse(
                    syntax.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double number))
            {
                if (schema.Domain.Minimum is double minimum && number < minimum ||
                    schema.Domain.Maximum is double maximum && number > maximum)
                {
                    ReportPrismBinding(
                        PrismValueDiagnostic,
                        syntax.Location,
                        "Prism property '" + schema.Name + "' value '" + syntax.Text +
                        "' is outside catalog domain '" + schema.Domain.Canonical + "'.");
                    return false;
                }
            }

            if (schema.ValueType != "symbol")
            {
                return true;
            }

            string? catalogKind = schema.Name switch
            {
                "WorkingColorProfile" => "color-profile",
                "BlendMode" => "blend-mode",
                "Sampling" => "sampling",
                _ => null
            };
            if (catalogKind is not null &&
                !PrismCatalog.Value.Entries.Any(entry =>
                    string.Equals(entry.Kind, catalogKind, StringComparison.Ordinal) &&
                    string.Equals(entry.Symbol, syntax.Text, StringComparison.Ordinal)))
            {
                ReportPrismBinding(
                    PrismValueDiagnostic,
                    syntax.Location,
                    "Unknown Prism " + catalogKind + " symbol '" + syntax.Text + "'.");
                return false;
            }

            if (schema.Name == "BlendMode" &&
                family is not "group" &&
                string.Equals(syntax.Text, "PassThrough", StringComparison.Ordinal))
            {
                ReportPrismBinding(
                    PrismValueDiagnostic,
                    syntax.Location,
                    "PassThrough blending is valid only for Prism groups.");
                return false;
            }

            if (family == "mask" &&
                schema.Name == "Channel" &&
                syntax.Text is not ("Alpha" or "Luminance"))
            {
                ReportPrismBinding(
                    PrismValueDiagnostic,
                    syntax.Location,
                    "Mask Channel must be Alpha or Luminance.");
                return false;
            }

            if (schema.Name == "BlendChannels" &&
                !string.Equals(syntax.Text, "RGBA", StringComparison.Ordinal))
            {
                ReportPrismBinding(
                    PrismValueDiagnostic,
                    syntax.Location,
                    "BlendChannels currently supports only RGBA.");
                return false;
            }

            return true;
        }

        private void ValidateSiblingNames(IReadOnlyList<PrismContainerSyntax> nodes)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (PrismContainerSyntax node in nodes)
            {
                if (node.Name is not null && !names.Add(node.Name))
                {
                    ReportPrismBinding(
                        PrismDuplicateNameDiagnostic,
                        node.NameLocation ?? node.Location,
                        "Prism node name '" + node.Name +
                        "' is duplicated in the same address scope.");
                }
            }
        }

        private void ValidateBackdropShape(IReadOnlyList<PrismContainerSyntax> nodes)
        {
            PrismContainerSyntax[] backdrops =
                nodes.Where(node => node.Kind == PrismContainerKind.Backdrop).ToArray();
            if (backdrops.Length > 1)
            {
                PrismContainerSyntax second = backdrops[1];
                ReportPrismBinding(
                    PrismBackdropCountDiagnostic,
                    second.NameLocation ?? second.Location,
                    "A Prism composition may declare at most one backdrop.");
                return;
            }

            if (backdrops.Length == 0)
            {
                return;
            }

            int backdropIndex = IndexOfReference(nodes, backdrops[0]);
            PrismContainerSyntax? followingNormal = nodes
                .Skip(backdropIndex + 1)
                .FirstOrDefault(node => node.Kind != PrismContainerKind.Backdrop);
            if (followingNormal is not null)
            {
                ReportPrismBinding(
                    PrismBackdropOrderDiagnostic,
                    followingNormal.NameLocation ?? followingNormal.Location,
                    "The Prism backdrop must be the last direct composition child.");
            }
        }

        private void ValidateClipToBelow(IReadOnlyList<BoundPrismNode> nodes)
        {
            ValidateClipToBelowScope(
                nodes.Where(node => node.Kind != PrismContainerKind.Backdrop).ToArray());
            foreach (BoundPrismNode group in EnumeratePrismNodes(nodes)
                .Where(node => node.Kind == PrismContainerKind.Group))
            {
                ValidateClipToBelowScope(group.Children);
            }
        }

        private void ValidateClipToBelowScope(IReadOnlyList<BoundPrismNode> nodes)
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                BoundPrismNode node = nodes[index];
                if (node.Kind != PrismContainerKind.Layer)
                {
                    continue;
                }

                BoundPrismProperty? clip = node.Properties.FirstOrDefault(property =>
                    string.Equals(
                        property.Schema.Name,
                        "ClipToBelow",
                        StringComparison.Ordinal));
                if (clip is null || IsConstantFalse(clip.Value))
                {
                    continue;
                }

                bool hasBase = false;
                for (int lowerIndex = index + 1; lowerIndex < nodes.Count; lowerIndex++)
                {
                    BoundPrismNode lower = nodes[lowerIndex];
                    if (lower.Kind == PrismContainerKind.Group)
                    {
                        hasBase = true;
                        break;
                    }

                    BoundPrismProperty? lowerClip = lower.Properties.FirstOrDefault(property =>
                        string.Equals(
                            property.Schema.Name,
                            "ClipToBelow",
                            StringComparison.Ordinal));
                    if (lowerClip is null || IsConstantFalse(lowerClip.Value))
                    {
                        hasBase = true;
                        break;
                    }
                }

                if (!hasBase)
                {
                    ReportPrismBinding(
                        PrismClipToBelowDiagnostic,
                        clip.Syntax.NameLocation,
                        "ClipToBelow requires an unclipped normal sibling beneath the layer.");
                }
            }
        }

        private bool TryResolvePrismComposition(
            XObject source,
            string name,
            out BoundPrismComposition composition)
        {
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (boundPrismResources.TryGetValue(scope, out Dictionary<string, BoundPrismComposition>? resources) &&
                    resources.TryGetValue(name, out composition))
                {
                    return true;
                }
            }

            if (applicationResources is not null &&
                applicationResources.PrismCompositions.TryGetValue(
                    name,
                    out object? applicationComposition) &&
                applicationComposition is BoundPrismComposition bound)
            {
                composition = bound;
                return true;
            }

            composition = null!;
            return false;
        }

        private void ReportPrismBinding(
            DiagnosticDescriptor descriptor,
            object location,
            string message)
        {
            prismBindingDiagnosticCount++;
            Report(
                descriptor,
                location,
                Path.GetFileName(file.Path),
                message);
        }

        private static IEnumerable<PrismPropertyBindingSpec> PrismPropertySpecs(
            IReadOnlyList<PrismCatalogCompiler.CatalogProperty> schemas,
            string keyContainer,
            string family)
        {
            foreach (PrismCatalogCompiler.CatalogProperty schema in schemas)
            {
                yield return new PrismPropertyBindingSpec(
                    schema,
                    "global::Cerneala.Drawing.Prism.Catalog.PrismCatalogGenerated." +
                    keyContainer + "." + schema.Name + "Key",
                    family);
            }
        }

        private static BoundPrismValueType CatalogValueType(string valueType)
        {
            return valueType switch
            {
                "boolean" => BoundPrismValueType.Boolean,
                "integer" => BoundPrismValueType.Integer,
                "number" => BoundPrismValueType.Number,
                "color" => BoundPrismValueType.Color,
                "vector" => BoundPrismValueType.Vector,
                "symbol" => BoundPrismValueType.Symbol,
                "resource" => BoundPrismValueType.Resource,
                _ => throw new InvalidOperationException(
                    "Unknown Prism catalog value type '" + valueType + "'.")
            };
        }

        private static bool TryParsePrismParameterType(
            string typeName,
            out BoundPrismValueType type)
        {
            switch (typeName)
            {
                case "bool":
                case "boolean":
                    type = BoundPrismValueType.Boolean;
                    return true;
                case "int":
                case "integer":
                    type = BoundPrismValueType.Integer;
                    return true;
                case "float":
                case "number":
                    type = BoundPrismValueType.Number;
                    return true;
                case "color":
                    type = BoundPrismValueType.Color;
                    return true;
                case "vector":
                case "vector4":
                    type = BoundPrismValueType.Vector;
                    return true;
                case "symbol":
                    type = BoundPrismValueType.Symbol;
                    return true;
                case "resource":
                    type = BoundPrismValueType.Resource;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        private static bool CanConvertPrismValue(
            BoundPrismValueType source,
            BoundPrismValueType target) =>
            source == target ||
            source == BoundPrismValueType.Integer &&
            target == BoundPrismValueType.Number;

        private static bool IsValidPrismVector(string text)
        {
            string[] components = text.Substring(1, text.Length - 2)
                .Split(',')
                .Select(component => component.Trim())
                .ToArray();
            return components.Length is >= 2 and <= 4 &&
                components.All(component =>
                    float.TryParse(
                        component,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float value) &&
                    !float.IsNaN(value) &&
                    !float.IsInfinity(value));
        }

        private static bool IsConstantFalse(BoundPrismValue value) =>
            value.Parameter is null &&
            value.Syntax.Kind == PrismValueKind.BooleanLiteral &&
            string.Equals(value.Syntax.Text, "false", StringComparison.OrdinalIgnoreCase);

        private static string DisplayPrismValueType(BoundPrismValueType type) =>
            type.ToString().ToLowerInvariant();

        private static string CombinePrismPath(string prefix, string name) =>
            prefix.Length == 0 ? name : prefix + "." + name;

        private static string PrismContainerDisplay(PrismContainerKind kind) =>
            kind switch
            {
                PrismContainerKind.Layer => "@layer",
                PrismContainerKind.Group => "@group",
                PrismContainerKind.Backdrop => "@backdrop",
                _ => "@prism"
            };

        private static int IndexOfReference<T>(
            IReadOnlyList<T> values,
            T value)
            where T : class
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (ReferenceEquals(values[index], value))
                {
                    return index;
                }
            }

            return -1;
        }

        private static IEnumerable<BoundPrismNode> EnumeratePrismNodes(
            IEnumerable<BoundPrismNode> roots)
        {
            foreach (BoundPrismNode node in roots)
            {
                yield return node;
                foreach (BoundPrismNode child in EnumeratePrismNodes(node.Children))
                {
                    yield return child;
                }
            }
        }
    }
}
