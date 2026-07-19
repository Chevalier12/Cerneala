using System.Collections.Generic;
using System.Xml.Linq;
using Cerneala.SourceGen.Prism;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private enum BoundPrismValueType
    {
        Boolean,
        Integer,
        Number,
        Color,
        Vector,
        Symbol,
        Resource
    }

    private sealed class BoundPrismValue
    {
        public BoundPrismValue(
            BoundPrismValueType type,
            PrismValueSyntax syntax,
            BoundPrismParameter? parameter = null,
            string? resourceName = null)
        {
            Type = type;
            Syntax = syntax;
            Parameter = parameter;
            ResourceName = resourceName;
        }

        public BoundPrismValueType Type { get; }

        public PrismValueSyntax Syntax { get; }

        public BoundPrismParameter? Parameter { get; }

        public string? ResourceName { get; }

        public bool IsConstant => Parameter is null;
    }

    private sealed class BoundPrismProperty
    {
        public BoundPrismProperty(
            PrismCatalogCompiler.CatalogProperty schema,
            BoundPrismValue value,
            PrismAssignmentSyntax syntax,
            string keyExpression)
        {
            Schema = schema;
            Value = value;
            Syntax = syntax;
            KeyExpression = keyExpression;
        }

        public PrismCatalogCompiler.CatalogProperty Schema { get; }

        public BoundPrismValue Value { get; }

        public PrismAssignmentSyntax Syntax { get; }

        public string KeyExpression { get; }
    }

    private sealed class BoundPrismParameter
    {
        public BoundPrismParameter(
            string name,
            string path,
            BoundPrismValueType type,
            BoundPrismValue? defaultValue,
            PrismParameterSyntax syntax)
        {
            Name = name;
            Path = path;
            Type = type;
            DefaultValue = defaultValue;
            Syntax = syntax;
        }

        public string Name { get; }

        public string Path { get; }

        public BoundPrismValueType Type { get; }

        public BoundPrismValue? DefaultValue { get; }

        public PrismParameterSyntax Syntax { get; }
    }

    private sealed class BoundPrismOperation
    {
        public BoundPrismOperation(
            PrismOperationKind kind,
            PrismCatalogCompiler.CatalogEntry? catalogEntry,
            IReadOnlyList<BoundPrismProperty> properties,
            PrismOperationSyntax syntax)
        {
            Kind = kind;
            CatalogEntry = catalogEntry;
            Properties = properties;
            Syntax = syntax;
        }

        public PrismOperationKind Kind { get; }

        public PrismCatalogCompiler.CatalogEntry? CatalogEntry { get; }

        public IReadOnlyList<BoundPrismProperty> Properties { get; }

        public PrismOperationSyntax Syntax { get; }
    }

    private sealed class BoundPrismNode
    {
        public BoundPrismNode(
            int id,
            PrismContainerKind kind,
            string? name,
            string? path,
            IReadOnlyList<BoundPrismProperty> properties,
            IReadOnlyList<BoundPrismParameter> parameters,
            IReadOnlyList<BoundPrismNode> children,
            IReadOnlyList<BoundPrismOperation> filters,
            IReadOnlyList<BoundPrismOperation> styles,
            BoundPrismOperation? mask,
            PrismContainerSyntax syntax)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Path = path;
            Properties = properties;
            Parameters = parameters;
            Children = children;
            Filters = filters;
            Styles = styles;
            Mask = mask;
            Syntax = syntax;
        }

        public int Id { get; }

        public PrismContainerKind Kind { get; }

        public string? Name { get; }

        public string? Path { get; }

        public IReadOnlyList<BoundPrismProperty> Properties { get; }

        public IReadOnlyList<BoundPrismParameter> Parameters { get; }

        public IReadOnlyList<BoundPrismNode> Children { get; }

        public IReadOnlyList<BoundPrismOperation> Filters { get; }

        public IReadOnlyList<BoundPrismOperation> Styles { get; }

        public BoundPrismOperation? Mask { get; }

        public PrismContainerSyntax Syntax { get; }
    }

    private sealed class BoundPrismComposition
    {
        public BoundPrismComposition(
            string name,
            IReadOnlyList<BoundPrismProperty> properties,
            IReadOnlyList<BoundPrismParameter> parameters,
            IReadOnlyList<BoundPrismNode> nodes,
            IReadOnlyDictionary<string, BoundPrismParameter> parametersByPath,
            IReadOnlyDictionary<string, BoundPrismNode> nodesByPath,
            PrismContainerSyntax syntax,
            XElement source,
            bool isReusable)
        {
            Name = name;
            Properties = properties;
            Parameters = parameters;
            Nodes = nodes;
            ParametersByPath = parametersByPath;
            NodesByPath = nodesByPath;
            Syntax = syntax;
            Source = source;
            IsReusable = isReusable;
        }

        public string Name { get; }

        public IReadOnlyList<BoundPrismProperty> Properties { get; }

        public IReadOnlyList<BoundPrismParameter> Parameters { get; }

        public IReadOnlyList<BoundPrismNode> Nodes { get; }

        public IReadOnlyDictionary<string, BoundPrismParameter> ParametersByPath { get; }

        public IReadOnlyDictionary<string, BoundPrismNode> NodesByPath { get; }

        public PrismContainerSyntax Syntax { get; }

        public XElement Source { get; }

        public bool IsReusable { get; }
    }

    private sealed class BoundPrismApplication
    {
        public BoundPrismApplication(
            BoundPrismComposition composition,
            IReadOnlyDictionary<string, BoundPrismValue> arguments,
            PrismApplicationSyntax syntax,
            XElement owner)
        {
            Composition = composition;
            Arguments = arguments;
            Syntax = syntax;
            Owner = owner;
        }

        public BoundPrismComposition Composition { get; }

        public IReadOnlyDictionary<string, BoundPrismValue> Arguments { get; }

        public PrismApplicationSyntax Syntax { get; }

        public XElement Owner { get; }
    }
}
