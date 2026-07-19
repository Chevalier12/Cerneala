using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private enum PrismValueKind
    {
        Identifier,
        ResourceReference,
        StringLiteral,
        NumberLiteral,
        BooleanLiteral,
        ColorLiteral,
        TupleLiteral,
        NullLiteral
    }

    private abstract class PrismSyntaxNode
    {
        protected PrismSyntaxNode(DirectiveExpressionLocation location)
        {
            Location = location;
        }

        public DirectiveExpressionLocation Location { get; }
    }

    private abstract class PrismMemberSyntax : PrismSyntaxNode
    {
        protected PrismMemberSyntax(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class PrismValueSyntax : PrismSyntaxNode
    {
        public PrismValueSyntax(string text, PrismValueKind kind, DirectiveExpressionLocation location) : base(location)
        {
            Text = text;
            Kind = kind;
        }

        public string Text { get; }

        public PrismValueKind Kind { get; }
    }

    private sealed class PrismAssignmentSyntax : PrismMemberSyntax
    {
        public PrismAssignmentSyntax(
            string name,
            DirectiveExpressionLocation nameLocation,
            PrismValueSyntax value,
            DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
            NameLocation = nameLocation;
            Value = value;
        }

        public string Name { get; }

        public DirectiveExpressionLocation NameLocation { get; }

        public PrismValueSyntax Value { get; }
    }

    private sealed class PrismParameterSyntax : PrismMemberSyntax
    {
        public PrismParameterSyntax(
            string name,
            DirectiveExpressionLocation nameLocation,
            string typeName,
            DirectiveExpressionLocation typeLocation,
            PrismValueSyntax? defaultValue,
            DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
            NameLocation = nameLocation;
            TypeName = typeName;
            TypeLocation = typeLocation;
            DefaultValue = defaultValue;
        }

        public string Name { get; }

        public DirectiveExpressionLocation NameLocation { get; }

        public string TypeName { get; }

        public DirectiveExpressionLocation TypeLocation { get; }

        public PrismValueSyntax? DefaultValue { get; }
    }

    private enum PrismContainerKind
    {
        Composition,
        Layer,
        Group,
        Backdrop
    }

    private sealed class PrismContainerSyntax : PrismMemberSyntax
    {
        public PrismContainerSyntax(
            PrismContainerKind kind,
            string? name,
            DirectiveExpressionLocation? nameLocation,
            IReadOnlyList<PrismMemberSyntax> members,
            DirectiveExpressionLocation location) : base(location)
        {
            Kind = kind;
            Name = name;
            NameLocation = nameLocation;
            Members = members;
        }

        public PrismContainerKind Kind { get; }

        public string? Name { get; }

        public DirectiveExpressionLocation? NameLocation { get; }

        public IReadOnlyList<PrismMemberSyntax> Members { get; }
    }

    private enum PrismOperationKind
    {
        Filter,
        Style,
        Mask
    }

    private sealed class PrismOperationSyntax : PrismMemberSyntax
    {
        public PrismOperationSyntax(
            PrismOperationKind kind,
            string? typeName,
            DirectiveExpressionLocation? typeLocation,
            IReadOnlyList<PrismMemberSyntax> members,
            DirectiveExpressionLocation location) : base(location)
        {
            Kind = kind;
            TypeName = typeName;
            TypeLocation = typeLocation;
            Members = members;
        }

        public PrismOperationKind Kind { get; }

        public string? TypeName { get; }

        public DirectiveExpressionLocation? TypeLocation { get; }

        public IReadOnlyList<PrismMemberSyntax> Members { get; }
    }

    private abstract class PrismApplicationSyntax : PrismSyntaxNode
    {
        protected PrismApplicationSyntax(DirectiveExpressionLocation location) : base(location)
        {
        }
    }

    private sealed class PrismInlineApplicationSyntax : PrismApplicationSyntax
    {
        public PrismInlineApplicationSyntax(
            PrismContainerSyntax composition,
            DirectiveExpressionLocation location) : base(location)
        {
            Composition = composition;
        }

        public PrismContainerSyntax Composition { get; }
    }

    private sealed class PrismResourceApplicationSyntax : PrismApplicationSyntax
    {
        public PrismResourceApplicationSyntax(
            string resourceName,
            DirectiveExpressionLocation resourceLocation,
            IReadOnlyList<PrismAssignmentSyntax> arguments,
            DirectiveExpressionLocation location) : base(location)
        {
            ResourceName = resourceName;
            ResourceLocation = resourceLocation;
            Arguments = arguments;
        }

        public string ResourceName { get; }

        public DirectiveExpressionLocation ResourceLocation { get; }

        public IReadOnlyList<PrismAssignmentSyntax> Arguments { get; }
    }

    private sealed class PrismCompositionResourceSyntax : PrismSyntaxNode
    {
        public PrismCompositionResourceSyntax(
            string name,
            DirectiveExpressionLocation nameLocation,
            PrismContainerSyntax composition,
            XElement source,
            DirectiveExpressionLocation location) : base(location)
        {
            Name = name;
            NameLocation = nameLocation;
            Composition = composition;
            Source = source;
        }

        public string Name { get; }

        public DirectiveExpressionLocation NameLocation { get; }

        public PrismContainerSyntax Composition { get; }

        public XElement Source { get; }
    }

    private sealed class PrismSyntaxDiagnostic
    {
        public PrismSyntaxDiagnostic(DiagnosticDescriptor descriptor, string message, object locationSource)
        {
            Descriptor = descriptor;
            Message = message;
            LocationSource = locationSource;
        }

        public DiagnosticDescriptor Descriptor { get; }

        public string Message { get; }

        public object LocationSource { get; }
    }

    private sealed class PrismSyntaxParseException : Exception
    {
        public PrismSyntaxParseException(
            DiagnosticDescriptor descriptor,
            string message,
            object? locationSource) : base(message)
        {
            Descriptor = descriptor;
            LocationSource = locationSource;
        }

        public DiagnosticDescriptor Descriptor { get; }

        public object? LocationSource { get; }
    }
}
