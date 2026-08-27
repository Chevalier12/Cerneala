float3 ApplyPhotoFilter(float3 color, VertexShaderOutput input)
{
    return lerp(color, FilterOptions0.rgb, FilterOptions1.x);
}
