float4 CatalogMosaicTiles(float2 uv, int profile)
{
    float tileSize = max(FilterOptions2.x, 1.0);
    float groutWidth = clamp(FilterOptions0.x, 0.0, tileSize);
    float lightenGrout = saturate(FilterOptions1.x);
    float2 pixel = uv / PixelSize;
    float2 cell = floor(pixel / tileSize);
    float2 local = pixel - (cell * tileSize);
    float2 edgeDistance = min(local, tileSize - local);
    float2 samplePixel = (cell + 0.5) * tileSize;
    float4 tile = CatalogLinearSample(
        samplePixel * PixelSize,
        profile);

    if (min(edgeDistance.x, edgeDistance.y) >= groutWidth * 0.5)
    {
        return tile;
    }

    float3 straight = saturate(Unpremultiply(tile));
    straight = lerp(straight, 1.0, lightenGrout);
    return float4(straight * tile.a, tile.a);
}
