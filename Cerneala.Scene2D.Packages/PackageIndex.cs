using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

// These headers contain selection information and byte ranges only. Never add
// model instances, palettes, entities or opaque metadata to the resident index.
internal sealed record PackageBlock(long Offset, int Length, byte[] Hash);
internal sealed record PackageMap(TileMapCatalog2D Catalog, PackageBlock Metadata, PackageBlock[] Chunks);
internal sealed record PackageEntity(Scene2DPackageEntityInfo Info, PackageBlock Data)
{
    internal string Id => Info.Id;
}
internal sealed record PackagePromotion(TileCellKey2D Cell, PackageBlock Data);
internal sealed record PackageLevel(string Id, DrawPoint WorldOffset, DrawSize? TileSize, TileMapBounds2D? Bounds,
    PackageBlock Metadata, PackageMap[] Maps, PackageEntity[] Entities, PackagePromotion[] Promotions);
internal sealed record PackageIndex(Scene2DAsset[] Assets, string[] Files, PackageBlock Metadata,
    PackageLevel[] Levels, long DataLength);
