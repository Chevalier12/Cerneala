using System;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.SourceGen.Prism;

[Generator]
internal sealed class PrismCatalogGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor DuplicateCatalog = new(
        "PRISM3000",
        "Multiple Prism catalog inputs",
        "At most one prism-catalog.json AdditionalFile is allowed, but {0} were supplied",
        "Prism.Catalog",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<CatalogInput>> catalogs = context.AdditionalTextsProvider
            .Where(static text => string.Equals(
                Path.GetFileName(text.Path),
                "prism-catalog.json",
                StringComparison.OrdinalIgnoreCase))
            .Select(static (text, cancellationToken) => new CatalogInput(
                text.Path,
                text.GetText(cancellationToken)?.ToString() ?? string.Empty))
            .Collect();

        context.RegisterSourceOutput(catalogs, static (productionContext, inputs) =>
        {
            if (inputs.Length == 0)
            {
                return;
            }
            if (inputs.Length > 1)
            {
                productionContext.ReportDiagnostic(Diagnostic.Create(DuplicateCatalog, Location.None, inputs.Length));
                return;
            }

            CatalogInput input = inputs[0];
            PrismCatalogCompilation compilation = PrismCatalogCompiler.Compile(input.Text);
            Location location = Location.Create(
                input.Path,
                new TextSpan(0, 0),
                new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 0)));

            foreach (PrismCatalogIssue issue in compilation.Issues)
            {
                DiagnosticDescriptor descriptor = new(
                    issue.Id,
                    "Invalid Prism catalog",
                    issue.Message,
                    "Prism.Catalog",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);
                productionContext.ReportDiagnostic(Diagnostic.Create(descriptor, location));
            }

            if (compilation.GeneratedSource is not null)
            {
                productionContext.AddSource(
                    "PrismCatalog.Generated.g.cs",
                    SourceText.From(compilation.GeneratedSource, Encoding.UTF8));
            }
        });
    }

    private readonly struct CatalogInput
    {
        public CatalogInput(string path, string text)
        {
            Path = path;
            Text = text;
        }

        public string Path { get; }

        public string Text { get; }
    }
}
