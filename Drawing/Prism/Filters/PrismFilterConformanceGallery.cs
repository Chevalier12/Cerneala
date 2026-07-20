using System.Collections.Immutable;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Filters;

internal readonly record struct PrismFilterConformanceGalleryEntry(
    PrismFilterId Filter,
    string Symbol,
    string Category,
    PrismCompositionDefinition Composition);

internal static class PrismFilterConformanceGallery
{
    private static readonly Lazy<
        ImmutableArray<PrismFilterConformanceGalleryEntry>> entries =
        new(CreateEntries);

    public static ImmutableArray<PrismFilterConformanceGalleryEntry> Entries =>
        entries.Value;

    private static ImmutableArray<
        PrismFilterConformanceGalleryEntry> CreateEntries()
    {
        ImmutableArray<PrismFilterConformanceGalleryEntry>.Builder gallery =
        ImmutableArray.CreateBuilder<
                PrismFilterConformanceGalleryEntry>();
        foreach (PrismCatalogEntryDescriptor entry in
            PrismCatalogGenerated.Entries)
        {
            if (entry.Kind != "filter")
            {
                continue;
            }

            PrismFilterId filter = (PrismFilterId)entry.StableId;
            if (!PrismCatalogFilterPlanner.IsSupported(filter))
            {
                continue;
            }

            PrismLayerDefinition layer = new(
                new PrismNodeId(entry.StableId),
                entry.Symbol,
                filters:
                [
                    new PrismFilterDefinition(filter)
                ]);
            PrismCompositionDefinition composition = new(
                $"PrismFilterConformance.{entry.Symbol}",
                [layer]);
            gallery.Add(
                new PrismFilterConformanceGalleryEntry(
                    filter,
                    entry.Symbol,
                    entry.Category,
                    composition));
        }
        return gallery.ToImmutable();
    }
}
