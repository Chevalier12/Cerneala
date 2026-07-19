using Cerneala.Drawing.MonoGame.Prism.Shaders;
using Cerneala.Drawing.Prism.Catalog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Kernels;

internal enum PrismKernelKind
{
    Copy,
    NormalComposite,
    MaskAlpha,
    ClipAlpha,
    SrgbToLinear,
    LinearToSrgb,
    Present
}

internal readonly record struct PrismKernelParameters(
    Texture2D SecondaryTexture,
    float Opacity,
    Vector2 PixelSize,
    Vector2 UvScale,
    Vector2 UvOffset);

internal sealed class PrismKernelRegistry : IDisposable
{
    private const string CatalogOwnerPrefix =
        "planned:PrismKernelRegistry/";

    private readonly Effect effect;
    private readonly EffectParameter secondaryTextureParameter;
    private readonly EffectParameter opacityParameter;
    private readonly EffectParameter pixelSizeParameter;
    private readonly EffectParameter uvScaleParameter;
    private readonly EffectParameter uvOffsetParameter;
    private readonly PrismKernel copy;
    private readonly PrismKernel normalComposite;
    private readonly PrismKernel maskAlpha;
    private readonly PrismKernel clipAlpha;
    private readonly PrismKernel srgbToLinear;
    private readonly PrismKernel linearToSrgb;
    private readonly PrismKernel present;
    private bool disposed;

    public PrismKernelRegistry(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ObjectDisposedException.ThrowIf(graphicsDevice.IsDisposed, graphicsDevice);

        ValidateFundamentalCatalogBindings();

        try
        {
            effect = PrismShaderResources.CreateEffect(
                graphicsDevice,
                PrismShaderId.CopyComposite);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                ArgumentException or
                BadImageFormatException)
        {
            throw new PrismShaderUnavailableException(
                "The embedded Prism shader package could not be loaded.",
                exception);
        }

        secondaryTextureParameter = GetParameter("SecondaryTexture");
        opacityParameter = GetParameter("Opacity");
        pixelSizeParameter = GetParameter("PixelSize");
        uvScaleParameter = GetParameter("UvScale");
        uvOffsetParameter = GetParameter("UvOffset");

        copy = CreateKernel(
            PrismKernelKind.Copy,
            "CopyComposite");
        normalComposite = CreateKernel(
            PrismKernelKind.NormalComposite,
            "NormalComposite");
        maskAlpha = CreateKernel(
            PrismKernelKind.MaskAlpha,
            "MaskAlpha");
        clipAlpha = CreateKernel(
            PrismKernelKind.ClipAlpha,
            "ClipAlpha");
        srgbToLinear = CreateKernel(
            PrismKernelKind.SrgbToLinear,
            "SrgbToLinear");
        linearToSrgb = CreateKernel(
            PrismKernelKind.LinearToSrgb,
            "LinearToSrgb");
        present = CreateKernel(
            PrismKernelKind.Present,
            "Present");
    }

    public Effect Effect => effect;

    public PrismKernel Copy => copy;

    public PrismKernel MaskAlpha => maskAlpha;

    public PrismKernel ClipAlpha => clipAlpha;

    public PrismKernel Present => present;

    public PrismKernel LinearToSrgb => linearToSrgb;

    public bool TryGetBlendKernel(
        PrismBlendMode blendMode,
        out PrismKernel kernel)
    {
        if (blendMode is PrismBlendMode.Normal or
            PrismBlendMode.PassThrough)
        {
            kernel = normalComposite;
            return true;
        }

        kernel = default;
        return false;
    }

    public bool TryGetColorConversionKernel(
        PrismColorProfile targetProfile,
        out PrismKernel kernel)
    {
        switch (targetProfile)
        {
            case PrismColorProfile.LinearSrgb:
                kernel = srgbToLinear;
                return true;
            case PrismColorProfile.Srgb:
                kernel = linearToSrgb;
                return true;
            default:
                kernel = default;
                return false;
        }
    }

    public bool IsFundamentalCatalogEntryRegistered(
        string kind,
        string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return kind switch
        {
            "blend-mode" =>
                symbol is "Normal" or "PassThrough",
            "color-profile" =>
                symbol is "LinearSrgb" or "Srgb",
            "sampling" =>
                symbol == "Linear",
            _ => false
        };
    }

    public void Bind(
        PrismKernel kernel,
        in PrismKernelParameters parameters)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(parameters.SecondaryTexture);

        effect.CurrentTechnique = kernel.Technique;
        secondaryTextureParameter.SetValue(parameters.SecondaryTexture);
        opacityParameter.SetValue(parameters.Opacity);
        pixelSizeParameter.SetValue(parameters.PixelSize);
        uvScaleParameter.SetValue(parameters.UvScale);
        uvOffsetParameter.SetValue(parameters.UvOffset);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        effect.Dispose();
        disposed = true;
    }

    private PrismKernel CreateKernel(
        PrismKernelKind kind,
        string techniqueName)
    {
        EffectTechnique? technique = effect.Techniques[techniqueName];
        return technique is null
            ? throw new PrismShaderUnavailableException(
                $"The Prism shader package does not contain technique '{techniqueName}'.")
            : new PrismKernel(kind, technique);
    }

    private EffectParameter GetParameter(string name)
    {
        EffectParameter? parameter = effect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The Prism shader package does not contain parameter '{name}'.");
    }

    private static void ValidateFundamentalCatalogBindings()
    {
        ValidateCatalogBinding("blend-mode", "Normal");
        ValidateCatalogBinding("blend-mode", "PassThrough");
        ValidateCatalogBinding("color-profile", "LinearSrgb");
        ValidateCatalogBinding("color-profile", "Srgb");
        ValidateCatalogBinding("sampling", "Linear");
    }

    private static void ValidateCatalogBinding(
        string kind,
        string symbol)
    {
        string expectedOwner = CatalogOwnerPrefix + symbol;
        foreach (PrismCatalogEntryDescriptor entry in
            PrismCatalogGenerated.Entries)
        {
            if (entry.Kind != kind || entry.Symbol != symbol)
            {
                continue;
            }

            if (!string.Equals(
                entry.Coverage.Kernel,
                expectedOwner,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Catalog entry '{entry.Id}' is assigned to " +
                    $"'{entry.Coverage.Kernel}', not '{expectedOwner}'.");
            }

            return;
        }

        throw new InvalidOperationException(
            $"The Prism catalog does not contain fundamental " +
            $"kernel '{kind}:{symbol}'.");
    }
}

internal readonly record struct PrismKernel(
    PrismKernelKind Kind,
    EffectTechnique Technique);

internal sealed class PrismShaderUnavailableException :
    InvalidOperationException
{
    public PrismShaderUnavailableException(string message)
        : base(message)
    {
    }

    public PrismShaderUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
