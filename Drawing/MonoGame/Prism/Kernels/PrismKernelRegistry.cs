using Cerneala.Drawing.MonoGame.Prism.Shaders;
using Cerneala.Drawing.Prism.Catalog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Kernels;

internal enum PrismKernelKind
{
    Copy,
    Blend,
    MaskExtract,
    MaskFeather,
    MaskAlpha,
    ClipAlpha,
    LayerStyle,
    InputColorConversion,
    OutputColorConversion
}

internal readonly record struct PrismKernelParameters(
    Texture2D SecondaryTexture,
    float Opacity,
    Vector2 PixelSize,
    Vector2 UvScale,
    Vector2 UvOffset)
{
    public Vector4 BlendChannels { get; init; } =
        Vector4.One;

    public float KnockoutMode { get; init; }

    public float BlendIfChannel { get; init; }

    public Vector4 ThisLayerRange { get; init; } =
        new(0, 0, 1, 1);

    public Vector4 UnderlyingRange { get; init; } =
        new(0, 0, 1, 1);

    public float DissolveSeed { get; init; }

    public float BackgroundAvailable { get; init; } = 1;

    public float MaskChannel { get; init; }

    public float MaskDensity { get; init; } = 1;

    public float MaskInvert { get; init; }

    public Vector3 MaskUvRowX { get; init; } =
        new(1, 0, 0);

    public Vector3 MaskUvRowY { get; init; } =
        new(0, 1, 0);

    public Vector2 MaskFeatherStep { get; init; }

    public Texture2D? StyleTexture { get; init; }

    public Vector4 StyleColor { get; init; } =
        Vector4.One;

    public Vector4 StyleSecondaryColor { get; init; } =
        Vector4.One;

    public Vector4 StyleGeometry0 { get; init; }

    public Vector4 StyleGeometry1 { get; init; }

    public Vector4 StyleOptions0 { get; init; }

    public Vector4 StyleOptions1 { get; init; }

    public Vector4 StyleModes0 { get; init; }

    public Vector4 StyleModes1 { get; init; }

    public Vector4 StyleModes2 { get; init; }

    public Vector4 StyleModes3 { get; init; }

    public float StyleResourceAvailable { get; init; }
}

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
    private readonly EffectParameter blendChannelsParameter;
    private readonly EffectParameter knockoutModeParameter;
    private readonly EffectParameter blendIfChannelParameter;
    private readonly EffectParameter thisLayerRangeParameter;
    private readonly EffectParameter underlyingRangeParameter;
    private readonly EffectParameter dissolveSeedParameter;
    private readonly EffectParameter backgroundAvailableParameter;
    private readonly EffectParameter maskChannelParameter;
    private readonly EffectParameter maskDensityParameter;
    private readonly EffectParameter maskInvertParameter;
    private readonly EffectParameter maskUvRowXParameter;
    private readonly EffectParameter maskUvRowYParameter;
    private readonly EffectParameter maskFeatherStepParameter;
    private readonly EffectParameter styleTextureParameter;
    private readonly EffectParameter styleColorParameter;
    private readonly EffectParameter styleSecondaryColorParameter;
    private readonly EffectParameter styleGeometry0Parameter;
    private readonly EffectParameter styleGeometry1Parameter;
    private readonly EffectParameter styleOptions0Parameter;
    private readonly EffectParameter styleOptions1Parameter;
    private readonly EffectParameter styleModes0Parameter;
    private readonly EffectParameter styleModes1Parameter;
    private readonly EffectParameter styleModes2Parameter;
    private readonly EffectParameter styleModes3Parameter;
    private readonly EffectParameter styleResourceAvailableParameter;
    private readonly PrismKernel copy;
    private readonly PrismKernel maskExtract;
    private readonly PrismKernel maskFeather;
    private readonly PrismKernel maskAlpha;
    private readonly PrismKernel clipAlpha;
    private readonly PrismKernel layerStyle;
    private readonly Dictionary<PrismBlendMode, PrismKernel>
        blendKernels = [];
    private readonly Dictionary<PrismColorProfile, PrismKernel>
        inputColorConversions = [];
    private readonly Dictionary<PrismColorProfile, PrismKernel>
        outputColorConversions = [];
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
        blendChannelsParameter = GetParameter("BlendChannels");
        knockoutModeParameter = GetParameter("KnockoutMode");
        blendIfChannelParameter = GetParameter("BlendIfChannel");
        thisLayerRangeParameter = GetParameter("ThisLayerRange");
        underlyingRangeParameter = GetParameter("UnderlyingRange");
        dissolveSeedParameter = GetParameter("DissolveSeed");
        backgroundAvailableParameter =
            GetParameter("BackgroundAvailable");
        maskChannelParameter = GetParameter("MaskChannel");
        maskDensityParameter = GetParameter("MaskDensity");
        maskInvertParameter = GetParameter("MaskInvert");
        maskUvRowXParameter = GetParameter("MaskUvRowX");
        maskUvRowYParameter = GetParameter("MaskUvRowY");
        maskFeatherStepParameter =
            GetParameter("MaskFeatherStep");
        styleTextureParameter = GetParameter("StyleTexture");
        styleColorParameter = GetParameter("StyleColor");
        styleSecondaryColorParameter =
            GetParameter("StyleSecondaryColor");
        styleGeometry0Parameter =
            GetParameter("StyleGeometry0");
        styleGeometry1Parameter =
            GetParameter("StyleGeometry1");
        styleOptions0Parameter =
            GetParameter("StyleOptions0");
        styleOptions1Parameter =
            GetParameter("StyleOptions1");
        styleModes0Parameter = GetParameter("StyleModes0");
        styleModes1Parameter = GetParameter("StyleModes1");
        styleModes2Parameter = GetParameter("StyleModes2");
        styleModes3Parameter = GetParameter("StyleModes3");
        styleResourceAvailableParameter =
            GetParameter("StyleResourceAvailable");

        copy = CreateKernel(
            PrismKernelKind.Copy,
            "CopyComposite");
        maskExtract = CreateKernel(
            PrismKernelKind.MaskExtract,
            "MaskExtract");
        maskFeather = CreateKernel(
            PrismKernelKind.MaskFeather,
            "MaskFeather");
        maskAlpha = CreateKernel(
            PrismKernelKind.MaskAlpha,
            "MaskAlpha");
        clipAlpha = CreateKernel(
            PrismKernelKind.ClipAlpha,
            "ClipAlpha");
        layerStyle = CreateKernel(
            PrismKernelKind.LayerStyle,
            "LayerStyle");
        foreach (PrismBlendMode blendMode in
            Enum.GetValues<PrismBlendMode>())
        {
            blendKernels.Add(
                blendMode,
                CreateKernel(
                    PrismKernelKind.Blend,
                    $"{blendMode}Blend"));
        }
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            string symbol = profile.ToString();
            inputColorConversions.Add(
                profile,
                CreateKernel(
                    PrismKernelKind.InputColorConversion,
                    $"InputTo{symbol}"));
            outputColorConversions.Add(
                profile,
                CreateKernel(
                    PrismKernelKind.OutputColorConversion,
                    $"{symbol}ToOutput"));
        }
    }

    public Effect Effect => effect;

    public PrismKernel Copy => copy;

    public PrismKernel MaskExtract => maskExtract;

    public PrismKernel MaskFeather => maskFeather;

    public PrismKernel MaskAlpha => maskAlpha;

    public PrismKernel ClipAlpha => clipAlpha;

    public PrismKernel LayerStyle => layerStyle;

    public PrismKernel Present =>
        outputColorConversions[PrismColorProfile.Srgb];

    public bool TryGetBlendKernel(
        PrismBlendMode blendMode,
        out PrismKernel kernel)
    {
        return blendKernels.TryGetValue(blendMode, out kernel);
    }

    public bool TryGetColorConversionKernel(
        PrismColorProfile targetProfile,
        out PrismKernel kernel)
    {
        return inputColorConversions.TryGetValue(
            targetProfile,
            out kernel);
    }

    public bool TryGetPresentKernel(
        PrismColorProfile sourceProfile,
        out PrismKernel kernel)
    {
        return outputColorConversions.TryGetValue(
            sourceProfile,
            out kernel);
    }

    public bool TryGetStyleKernel(
        PrismStyleId style,
        out PrismKernel kernel)
    {
        if (Enum.IsDefined(style))
        {
            kernel = layerStyle;
            return true;
        }

        kernel = default;
        return false;
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
                IsRegisteredBlendMode(symbol),
            "color-profile" =>
                IsRegisteredColorProfile(symbol),
            "sampling" =>
                symbol == "Linear",
            "style" =>
                IsRegisteredStyle(symbol),
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
        blendChannelsParameter.SetValue(parameters.BlendChannels);
        knockoutModeParameter.SetValue(parameters.KnockoutMode);
        blendIfChannelParameter.SetValue(parameters.BlendIfChannel);
        thisLayerRangeParameter.SetValue(parameters.ThisLayerRange);
        underlyingRangeParameter.SetValue(
            parameters.UnderlyingRange);
        dissolveSeedParameter.SetValue(parameters.DissolveSeed);
        backgroundAvailableParameter.SetValue(
            parameters.BackgroundAvailable);
        maskChannelParameter.SetValue(parameters.MaskChannel);
        maskDensityParameter.SetValue(parameters.MaskDensity);
        maskInvertParameter.SetValue(parameters.MaskInvert);
        maskUvRowXParameter.SetValue(parameters.MaskUvRowX);
        maskUvRowYParameter.SetValue(parameters.MaskUvRowY);
        maskFeatherStepParameter.SetValue(
            parameters.MaskFeatherStep);
        styleTextureParameter.SetValue(
            parameters.StyleTexture ??
            parameters.SecondaryTexture);
        styleColorParameter.SetValue(parameters.StyleColor);
        styleSecondaryColorParameter.SetValue(
            parameters.StyleSecondaryColor);
        styleGeometry0Parameter.SetValue(
            parameters.StyleGeometry0);
        styleGeometry1Parameter.SetValue(
            parameters.StyleGeometry1);
        styleOptions0Parameter.SetValue(
            parameters.StyleOptions0);
        styleOptions1Parameter.SetValue(
            parameters.StyleOptions1);
        styleModes0Parameter.SetValue(parameters.StyleModes0);
        styleModes1Parameter.SetValue(parameters.StyleModes1);
        styleModes2Parameter.SetValue(parameters.StyleModes2);
        styleModes3Parameter.SetValue(parameters.StyleModes3);
        styleResourceAvailableParameter.SetValue(
            parameters.StyleResourceAvailable);
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
        foreach (PrismBlendMode blendMode in
            Enum.GetValues<PrismBlendMode>())
        {
            ValidateCatalogBinding(
                "blend-mode",
                blendMode.ToString());
        }
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            ValidateCatalogBinding(
                "color-profile",
                profile.ToString());
        }
        ValidateCatalogBinding("sampling", "Linear");
        foreach (PrismStyleId style in
            Enum.GetValues<PrismStyleId>())
        {
            ValidateCatalogBinding("style", style.ToString());
        }
    }

    private bool IsRegisteredColorProfile(string symbol)
    {
        return Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismColorProfile profile) &&
            string.Equals(
                profile.ToString(),
                symbol,
                StringComparison.Ordinal) &&
            inputColorConversions.ContainsKey(profile) &&
            outputColorConversions.ContainsKey(profile);
    }

    private bool IsRegisteredBlendMode(string symbol)
    {
        return Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismBlendMode blendMode) &&
            string.Equals(
                blendMode.ToString(),
                symbol,
                StringComparison.Ordinal) &&
            blendKernels.ContainsKey(blendMode);
    }

    private bool IsRegisteredStyle(string symbol)
    {
        return Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismStyleId style) &&
            string.Equals(
                style.ToString(),
                symbol,
                StringComparison.Ordinal) &&
            TryGetStyleKernel(style, out PrismKernel kernel) &&
            kernel == layerStyle;
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
