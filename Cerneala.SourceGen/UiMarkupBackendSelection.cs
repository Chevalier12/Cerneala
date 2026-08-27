using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private const string ApplicationBackendAttributeMetadataName =
        "Cerneala.UI.Hosting.Windowing.ApplicationBackendAttribute";
    private const string ApplicationBackendAttributeUsage =
        "[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(typeof(BackendType))]";
    private const string ApplicationBackendRegistrationSignature =
        "public static void EnsureRegistered()";

    private readonly struct ApplicationBackendSelection
    {
        public ApplicationBackendSelection(INamedTypeSymbol backendType)
        {
            BackendTypeCode = backendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        public string BackendTypeCode { get; }
    }

    private static bool TryResolveApplicationBackend(
        SourceProductionContext context,
        Compilation compilation,
        Location fallbackLocation,
        out ApplicationBackendSelection selection)
    {
        selection = default;
        INamedTypeSymbol? attributeType = compilation.GetTypeByMetadataName(
            ApplicationBackendAttributeMetadataName);
        AttributeData[] attributes = compilation.Assembly.GetAttributes()
            .Where(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            .ToArray();
        if (attributes.Length != 1)
        {
            Location location = attributes.Length > 1
                ? AttributeLocation(attributes[1], fallbackLocation)
                : fallbackLocation;
            ReportApplicationBackendDiagnostic(
                context,
                location,
                $"Exactly one {ApplicationBackendAttributeUsage} declaration is required for generated executable startup; found {attributes.Length}.");
            return false;
        }

        AttributeData attribute = attributes[0];
        Location attributeLocation = AttributeLocation(attribute, fallbackLocation);
        if (attribute.ConstructorArguments.Length != 1 ||
            attribute.ConstructorArguments[0].Kind != TypedConstantKind.Type ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol backendType)
        {
            ReportApplicationBackendDiagnostic(
                context,
                attributeLocation,
                $"{ApplicationBackendAttributeUsage} must select a resolvable backend type.");
            return false;
        }

        string backendTypeCode = backendType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (!IsPublicNonGenericBackendType(backendType) ||
            backendType.TypeKind != TypeKind.Class ||
            (!backendType.IsStatic && backendType.IsAbstract))
        {
            ReportApplicationBackendDiagnostic(
                context,
                attributeLocation,
                $"ApplicationBackendAttribute selected '{backendTypeCode}', but the backend type must be a public, non-generic static or concrete class.");
            return false;
        }

        IMethodSymbol[] registrationMethods = backendType.GetMembers("EnsureRegistered")
            .OfType<IMethodSymbol>()
            .Where(method =>
                method.MethodKind == MethodKind.Ordinary &&
                method.DeclaredAccessibility == Accessibility.Public &&
                method.IsStatic &&
                method.Arity == 0 &&
                method.Parameters.Length == 0 &&
                method.ReturnsVoid)
            .ToArray();
        if (registrationMethods.Length != 1)
        {
            ReportApplicationBackendDiagnostic(
                context,
                attributeLocation,
                $"ApplicationBackendAttribute selected '{backendTypeCode}', which must declare exactly one {ApplicationBackendRegistrationSignature} method with no parameters.");
            return false;
        }

        selection = new ApplicationBackendSelection(backendType);
        return true;
    }

    private static bool IsPublicNonGenericBackendType(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public || current.Arity != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static Location AttributeLocation(AttributeData attribute, Location fallback) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback;

    private static void ReportApplicationBackendDiagnostic(
        SourceProductionContext context,
        Location location,
        string message) =>
        context.ReportDiagnostic(Diagnostic.Create(InvalidApplicationBackendSelection, location, message));

    private static void AppendApplicationBackendRegistration(
        StringBuilder source,
        ApplicationBackendSelection selection,
        string indentation) =>
        source.Append(indentation)
            .Append(selection.BackendTypeCode)
            .AppendLine(".EnsureRegistered();");
}
