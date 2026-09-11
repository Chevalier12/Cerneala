// Cropping physical render targets must not change the composition's paint,
// noise, dissolve, or analysis coordinates. Other backends use one raster.
float2 PrismReferencePosition(float2 position)
{
#ifdef CERNEALA_SDL_GPU
    return position + float2(PrismFrame.w, PrismFilterControl.w) - PrismReferenceRaster.xy;
#else
    return position;
#endif
}

float2 PrismReferenceUv(float2 position)
{
#ifdef CERNEALA_SDL_GPU
    return PrismReferencePosition(position) / PrismReferenceRaster.zw;
#else
    return position * PixelSize;
#endif
}

float2 PrismReferenceUvToSource(float2 uv)
{
#ifdef CERNEALA_SDL_GPU
    return (PrismReferenceRaster.xy + uv * PrismReferenceRaster.zw -
        float2(PrismFrame.w, PrismFilterControl.w)) * PixelSize;
#else
    return uv;
#endif
}

float2 PrismSourceUvDeltaToReference(float2 delta)
{
#ifdef CERNEALA_SDL_GPU
    return delta / (PixelSize * PrismReferenceRaster.zw);
#else
    return delta;
#endif
}
