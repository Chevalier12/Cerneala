using System.Numerics;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogFilterMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogProceduralMath;
using static Cerneala.Drawing.Prism.Filters.PrismCatalogQuantizationMath;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismCatalogTextureMath
{
    internal static Vector4 PlasticWrap(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float highlightStrength = MathF.Max(
            0,
            Option(plan, "HighlightStrength", 15));
        if (center.W <= 0 || highlightStrength <= 0)
        {
            return center;
        }

        float detail = Math.Clamp(
            Option(plan, "Detail", 9) / 15,
            0,
            1);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 7) / 15,
            0,
            1);
        float radius = MathF.Max(
            1,
            MathF.Max(pass.RadiusX, pass.RadiusY));
        float left = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x - radius,
            y));
        float right = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x + radius,
            y));
        float top = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x,
            y - radius));
        float bottom = Luminance(SamplePixelBilinear(
            source,
            width,
            height,
            x,
            y + radius));
        float heightScale = 6 * detail;
        Vector3 normal = Vector3.Normalize(
            new Vector3(
                -(right - left) * heightScale,
                -(bottom - top) * heightScale,
                1));
        Vector3 view = Vector3.UnitZ;
        Vector3 surfaceToLight = Vector3.Normalize(
            new Vector3(-0.45f, -0.55f, 1));
        float roughness = MathF.Max(
            0.045f,
            0.4f - (0.3f * smoothness));
        float normalDotLight = MathF.Max(
            Vector3.Dot(normal, surfaceToLight),
            0);
        Vector3 specular = CookTorranceGgxSpecular(
            new Vector3(0.04f),
            normal,
            view,
            surfaceToLight,
            roughness);
        float effectAmount = Math.Clamp(
            highlightStrength / 20,
            0,
            1);
        float diffuseShade = 1 +
            (((0.55f + (0.45f * normalDotLight)) - 1) *
                effectAmount);
        float specularGain =
            highlightStrength *
            (0.65f + (0.35f * smoothness));
        Vector3 result =
            (Unpremultiply(center) * diffuseShade) +
            (specular * normalDotLight * specularGain);
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 AngledStrokes(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness: Math.Clamp(
                Option(plan, "Sharpness", 3),
                0.5f,
                12),
            widthScale: 1.65f,
            minorScale: 0.42f,
            roughness: 0,
            luminancePreference: 0,
            diagonalBias: 1,
            diagonalBalance: Math.Clamp(
                Option(plan, "DirectionBalance", 0.5f),
                0,
                1),
            balanceDiagonalsByLuminance: true,
            jitterSeed: unchecked(93u * 0x9e3779b9u));
    }

    internal static Vector4 PaintDaubs(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        int brushType = Math.Clamp(
            (int)MathF.Round(plan.Options1.X),
            0,
            5);
        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        float sharpness = Math.Clamp(
            Option(plan, "Sharpness", 5),
            0.5f,
            10);
        float widthScale = 1;
        float minorScale = 1;
        float roughness = 0;
        float luminancePreference = 0;
        switch (brushType)
        {
            case 1:
                roughness = 0.55f;
                luminancePreference = 0.65f;
                break;
            case 2:
                roughness = 0.55f;
                luminancePreference = -0.65f;
                break;
            case 3:
                widthScale = 1.45f;
                minorScale = 0.7f;
                sharpness *= 1.35f;
                break;
            case 4:
                widthScale = 1.55f;
                minorScale = 1.05f;
                sharpness *= 0.55f;
                break;
            case 5:
                widthScale = 1.1f;
                minorScale = 0.75f;
                roughness = 0.85f;
                luminancePreference = 1.1f;
                sharpness *= 1.6f;
                break;
        }
        sharpness = Math.Clamp(sharpness, 0.5f, 12);

        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness,
            widthScale,
            minorScale,
            roughness,
            luminancePreference,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(83u * 0x9e3779b9u));
    }

    internal static Vector4 PaletteKnife(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float detail = MathF.Max(
            Option(plan, "StrokeDetail", 1),
            0);
        float softness = MathF.Max(
            Option(plan, "Softness", 0),
            0);
        float sharpness = Math.Clamp(
            (2 + (2 * detail)) /
                (1 + (0.5f * softness)),
            0.5f,
            12);
        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness,
            widthScale: 1,
            minorScale: 1,
            roughness: 0,
            luminancePreference: 0,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(84u * 0x9e3779b9u));
    }

    internal static Vector4 SmudgeStick(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        float amount = Math.Clamp(
            Option(plan, "Intensity", 10) / 10,
            0,
            1);
        if (center.W <= 0 || amount <= 0)
        {
            return center.W <= 0 ? Vector4.Zero : center;
        }

        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        Vector4 filteredSample = PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            sharpness: 3 + (4 * amount),
            widthScale: 1.65f,
            minorScale: 0.42f,
            roughness: 0,
            luminancePreference: -0.65f * amount,
            diagonalBias: 0.8f,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(88u * 0x9e3779b9u));
        Vector3 straight = Unpremultiply(center);
        Vector3 filtered = Unpremultiply(filteredSample);
        float darkness = 1 - StraightLuminance(straight);
        float smudgeMix = amount * (0.55f + (0.45f * darkness));
        Vector3 result = Vector3.Lerp(
            straight,
            filtered,
            smudgeMix);

        float highlightArea = Math.Clamp(
            Option(plan, "HighlightArea", 0),
            0,
            20) / 20;
        if (highlightArea > 0)
        {
            float threshold = 1 - (0.75f * highlightArea);
            float highlightMask = Math.Clamp(
                (StraightLuminance(result) - threshold) / 0.2f,
                0,
                1);
            highlightMask =
                highlightMask *
                highlightMask *
                (3 - (2 * highlightMask));
            float highlightGain =
                highlightMask *
                amount *
                (0.15f + (0.2f * highlightArea));
            result = Vector3.Lerp(
                result,
                Vector3.One,
                highlightGain);
        }

        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 Sponge(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        float definition = Math.Clamp(
            Option(plan, "Definition", 12),
            0,
            24);
        float smoothness = Math.Clamp(
            Option(plan, "Smoothness", 5) / 15,
            0,
            1);
        float sharpness = Math.Clamp(
            (1 + (0.45f * definition)) *
                (1.25f - (0.5f * smoothness)),
            0.5f,
            12);
        float minorScale = 0.38f + (0.52f * smoothness);
        return PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            MathF.Max(
                MathF.Max(pass.RadiusX, pass.RadiusY),
                1),
            sharpness,
            widthScale: 1.2f,
            minorScale,
            roughness: 0,
            luminancePreference: 0,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: unchecked(89u * 0x9e3779b9u));
    }

    internal static Vector4 RoughPastels(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float radius = MathF.Max(
            MathF.Max(pass.RadiusX, pass.RadiusY),
            1);
        float detail = Math.Clamp(
            Option(plan, "StrokeDetail", 4),
            0,
            16);
        bool coarsePass = pass.Iteration == 0;
        Vector4 filteredSample = PolynomialAnisotropicKuwahara(
            source,
            width,
            height,
            x,
            y,
            radius,
            coarsePass
                ? 2 + (detail * 0.45f)
                : 4 + (detail * 0.5f),
            widthScale: coarsePass ? 1.35f : 1.1f,
            minorScale: coarsePass ? 0.55f : 0.72f,
            roughness: coarsePass ? 0.22f : 0.08f,
            luminancePreference: 0,
            diagonalBias: 0,
            diagonalBalance: 0,
            balanceDiagonalsByLuminance: false,
            jitterSeed: coarsePass
                ? unchecked(87u * 0x9e3779b9u)
                : unchecked(87u * 0x85ebca6bu));
        if (coarsePass)
        {
            return filteredSample;
        }
        Vector3 filtered = Unpremultiply(filteredSample);

        float scaling = MathF.Max(
            Option(plan, "Scaling", 1),
            0.125f);
        int texture = Math.Clamp(
            (int)MathF.Round(Option(plan, "Texture", 0)),
            0,
            3);
        bool invert = Option(plan, "Invert", 0) >= 0.5f;
        float paper = ProceduralTextureHeight(
            x,
            y,
            texture,
            scaling,
            unchecked(87u * 0xc2b2ae35u),
            unchecked(87u * 0x27d4eb2du));
        float heightValue = invert ? 1 - paper : paper;

        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(source, width, height, x, y);
        float angle = 0.5f * MathF.Atan2(
            2 * tensorCross,
            tensorX - tensorY) +
            (MathF.PI * 0.5f);
        float fiber = 0.5f +
            (0.5f * MathF.Cos(
                (((x * MathF.Cos(angle)) +
                    (y * MathF.Sin(angle))) /
                    MathF.Max(scaling * 0.75f, 0.125f)) *
                MathF.PI));

        float relief = Math.Clamp(
            Option(plan, "Relief", 0.2f),
            0,
            2);
        int lightDirection = Math.Clamp(
            (int)MathF.Round(Option(plan, "LightDirection", 0)),
            0,
            7);
        float lightAngle =
            (-MathF.PI * 0.5f) +
            (lightDirection * MathF.PI * 0.25f);
        float lightX = MathF.Cos(lightAngle);
        float lightY = MathF.Sin(lightAngle);
        float ahead = ProceduralTextureHeight(
            x + lightX,
            y + lightY,
            texture,
            scaling,
            unchecked(87u * 0xc2b2ae35u),
            unchecked(87u * 0x27d4eb2du));
        float behind = ProceduralTextureHeight(
            x - lightX,
            y - lightY,
            texture,
            scaling,
            unchecked(87u * 0xc2b2ae35u),
            unchecked(87u * 0x27d4eb2du));
        if (invert)
        {
            ahead = 1 - ahead;
            behind = 1 - behind;
        }

        float coverageGap =
            (0.55f * heightValue) +
            (0.45f * (1 - fiber));
        float coverage = Math.Clamp(
            1 - (coverageGap * (0.12f + (0.18f * relief))),
            0.55f,
            1);
        float shade = Math.Clamp(
            1 + ((ahead - behind) * relief * 1.25f),
            0.55f,
            1.45f);
        Vector3 result =
            ((filtered * coverage) +
                (Vector3.One * (1 - coverage) * 0.65f)) *
            shade;
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 Underpainting(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float radius = MathF.Max(pass.RadiusX, pass.RadiusY);
        Vector4 filteredSample = radius <= 0.000001f
            ? center
            : PolynomialAnisotropicKuwahara(
                source,
                width,
                height,
                x,
                y,
                radius,
                sharpness: 3 + (0.35f * MathF.Min(radius, 12)),
                widthScale: 1.35f,
                minorScale: 0.55f,
                roughness: 0.08f,
                luminancePreference: 0,
                diagonalBias: 0,
                diagonalBalance: 0,
                balanceDiagonalsByLuminance: false,
                jitterSeed: unchecked(90u * 0x9e3779b9u));
        Vector3 filtered = Unpremultiply(filteredSample);
        float scaling = MathF.Max(
            Option(plan, "Scaling", 1),
            0.125f);
        int texture = Math.Clamp(
            (int)MathF.Round(Option(plan, "Texture", 0)),
            0,
            3);
        bool invert = Option(plan, "Invert", 0) >= 0.5f;
        const uint fineSeed = 0x7584a42du;
        const uint coarseSeed = 0x1f123bb5u;
        float heightValue = ProceduralTextureHeight(
            x,
            y,
            texture,
            scaling,
            fineSeed,
            coarseSeed);
        if (invert)
        {
            heightValue = 1 - heightValue;
        }

        float relief = Math.Clamp(
            Option(plan, "Relief", 0.04f),
            0,
            2);
        int lightDirection = Math.Clamp(
            (int)MathF.Round(Option(plan, "LightDirection", 0)),
            0,
            7);
        float lightAngle =
            (-MathF.PI * 0.5f) +
            (lightDirection * MathF.PI * 0.25f);
        float lightX = MathF.Cos(lightAngle);
        float lightY = MathF.Sin(lightAngle);
        float ahead = ProceduralTextureHeight(
            x + lightX,
            y + lightY,
            texture,
            scaling,
            fineSeed,
            coarseSeed);
        float behind = ProceduralTextureHeight(
            x - lightX,
            y - lightY,
            texture,
            scaling,
            fineSeed,
            coarseSeed);
        if (invert)
        {
            ahead = 1 - ahead;
            behind = 1 - behind;
        }

        float coverage = Math.Clamp(
            Option(plan, "TextureCoverage", 0.2f),
            0,
            1);
        float textureTone = 1 +
            (coverage * ((0.82f + (0.3f * heightValue)) - 1));
        float shade = Math.Clamp(
            1 + ((ahead - behind) * relief * 1.5f),
            0.55f,
            1.45f);
        return Associated(
            Vector3.Clamp(
                filtered * textureTone * shade,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }

    private static float ProceduralTextureHeight(
        float x,
        float y,
        int texture,
        float scaling,
        uint fineSeed,
        uint coarseSeed)
    {
        float qx = x / scaling;
        float qy = y / scaling;
        float fineNoise = Hash(
            (int)MathF.Floor(qx),
            (int)MathF.Floor(qy),
            fineSeed);
        if (texture == 1)
        {
            float row = MathF.Floor(qy / 4);
            float localX = Fraction(
                (qx / 8) + ((row % 2 + 2) % 2 * 0.5f));
            float localY = Fraction(qy / 4);
            float edge = MathF.Min(
                MathF.Min(localX, 1 - localX),
                MathF.Min(localY, 1 - localY));
            float mortar = edge < 0.08f ? 1 : 0;
            return Math.Clamp(
                0.25f + (0.5f * fineNoise) + (0.25f * mortar),
                0,
                1);
        }
        if (texture == 2)
        {
            float warp = 0.5f +
                (0.5f * MathF.Cos(qx * MathF.PI * 0.5f));
            float weft = 0.5f +
                (0.5f * MathF.Cos(qy * MathF.PI * 0.5f));
            return Math.Clamp(
                0.25f +
                    (0.3f * warp) +
                    (0.3f * weft) +
                    (0.15f * fineNoise),
                0,
                1);
        }
        if (texture == 3)
        {
            float coarseNoise = Hash(
                (int)MathF.Floor(qx / 4),
                (int)MathF.Floor(qy / 4),
                coarseSeed);
            return (0.6f * coarseNoise) +
                (0.4f * fineNoise);
        }

        float canvasX = 0.5f +
            (0.5f * MathF.Cos(qx * MathF.PI));
        float canvasY = 0.5f +
            (0.5f * MathF.Cos(qy * MathF.PI));
        return Math.Clamp(
            (0.35f * canvasX) +
                (0.35f * canvasY) +
                (0.3f * fineNoise),
            0,
            1);
    }

    private static float Fraction(float value) =>
        value - MathF.Floor(value);

    internal static Vector4 PolynomialAnisotropicKuwahara(
        Vector4[] source,
        int width,
        int height,
        int x,
        int y,
        float radius,
        float sharpness,
        float widthScale,
        float minorScale,
        float roughness,
        float luminancePreference,
        float diagonalBias,
        float diagonalBalance,
        bool balanceDiagonalsByLuminance,
        uint jitterSeed)
    {
        const int latticeRadius = 4;
        const float zeta = 2f / latticeRadius;
        const float gamma = 3 * MathF.PI / 16;
        const float diagonal = 0.7071067811865476f;
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(
                source,
                width,
                height,
                x,
                y);
        float discriminant = MathF.Sqrt(
            MathF.Max(
                0,
                ((tensorX - tensorY) *
                    (tensorX - tensorY)) +
                (4 * tensorCross * tensorCross)));
        float lambda1 =
            0.5f * (tensorX + tensorY + discriminant);
        float lambda2 =
            0.5f * (tensorX + tensorY - discriminant);
        float tensorEnergy = lambda1 + lambda2;
        float anisotropy = tensorEnergy <= 0.000001f
            ? 0
            : Math.Clamp(
                (lambda1 - lambda2) / tensorEnergy,
                0,
                1);
        float angle = tensorEnergy <= 0.000001f
            ? 0
            : (0.5f * MathF.Atan2(
                2 * tensorCross,
                tensorX - tensorY)) +
                (MathF.PI * 0.5f);
        if (roughness > 0)
        {
            float blockSize = MathF.Max(radius * 2, 1);
            float jitter = Hash(
                (int)MathF.Floor(x / blockSize),
                (int)MathF.Floor(y / blockSize),
                jitterSeed) -
                0.5f;
            angle += jitter * roughness * MathF.PI;
        }
        if (diagonalBias > 0)
        {
            float diagonalAngle;
            if (balanceDiagonalsByLuminance)
            {
                float threshold =
                    1 - Math.Clamp(diagonalBalance, 0, 1);
                diagonalAngle = StraightLuminance(
                    Unpremultiply(center)) >= threshold
                    ? MathF.PI * 0.25f
                    : -MathF.PI * 0.25f;
            }
            else
            {
                diagonalAngle = MathF.Sin(2 * angle) >= 0
                    ? MathF.PI * 0.25f
                    : -MathF.PI * 0.25f;
            }
            Vector2 tangent = new(
                MathF.Cos(angle),
                MathF.Sin(angle));
            Vector2 diagonalTangent = new(
                MathF.Cos(diagonalAngle),
                MathF.Sin(diagonalAngle));
            if (Vector2.Dot(tangent, diagonalTangent) < 0)
            {
                diagonalTangent = -diagonalTangent;
            }
            float effectiveBias = Math.Clamp(
                diagonalBias * (1 - (0.35f * anisotropy)),
                0,
                1);
            tangent = Vector2.Normalize(Vector2.Lerp(
                tangent,
                diagonalTangent,
                effectiveBias));
            angle = MathF.Atan2(tangent.Y, tangent.X);
        }

        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        float majorRadius =
            radius * widthScale * (1 + anisotropy);
        float minorRadius =
            radius * minorScale / (1 + anisotropy);
        float eta =
            (zeta + MathF.Cos(gamma)) /
            MathF.Pow(MathF.Sin(gamma), 2);
        Span<Vector3> colorSums = stackalloc Vector3[8];
        Span<Vector3> squareSums = stackalloc Vector3[8];
        Span<float> weightSums = stackalloc float[8];
        Span<float> sectorWeights = stackalloc float[8];
        for (int offsetY = -latticeRadius;
            offsetY <= latticeRadius;
            offsetY++)
        {
            for (int offsetX = -latticeRadius;
                offsetX <= latticeRadius;
                offsetX++)
            {
                float localX = offsetX / (float)latticeRadius;
                float localY = offsetY / (float)latticeRadius;
                float radiusSquared =
                    (localX * localX) +
                    (localY * localY);
                if (radiusSquared > 1)
                {
                    continue;
                }

                FacetSectorWeights(
                    localX,
                    localY,
                    zeta,
                    eta,
                    diagonal,
                    sectorWeights);
                float sectorTotal = 0;
                for (int sector = 0; sector < 8; sector++)
                {
                    sectorTotal += sectorWeights[sector];
                }
                if (sectorTotal <= 0.000001f)
                {
                    continue;
                }

                float sampleX = x +
                    (cosine * localX * majorRadius) -
                    (sine * localY * minorRadius);
                float sampleY = y +
                    (sine * localX * majorRadius) +
                    (cosine * localY * minorRadius);
                Vector4 sample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    sampleX,
                    sampleY);
                if (sample.W <= 0)
                {
                    continue;
                }
                Vector3 straight = Vector3.Clamp(
                    Unpremultiply(sample),
                    Vector3.Zero,
                    Vector3.One);
                float gaussian =
                    MathF.Exp(-3.125f * radiusSquared) /
                    sectorTotal;
                for (int sector = 0; sector < 8; sector++)
                {
                    float weight =
                        sectorWeights[sector] *
                        gaussian *
                        sample.W;
                    colorSums[sector] += straight * weight;
                    squareSums[sector] +=
                        straight * straight * weight;
                    weightSums[sector] += weight;
                }
            }
        }

        Vector3 result = Vector3.Zero;
        float resultWeight = 0;
        for (int sector = 0; sector < 8; sector++)
        {
            if (weightSums[sector] <= 0.000001f)
            {
                continue;
            }
            Vector3 mean = colorSums[sector] / weightSums[sector];
            Vector3 variance = Vector3.Max(
                Vector3.Zero,
                (squareSums[sector] / weightSums[sector]) -
                    (mean * mean));
            float varianceSum = variance.X + variance.Y + variance.Z;
            float confidence = 1 /
                (1 + MathF.Pow(
                    MathF.Max(varianceSum * 100, 0),
                    sharpness));
            float meanLuminance = Vector3.Dot(
                mean,
                new Vector3(0.2126f, 0.7152f, 0.0722f));
            confidence *= MathF.Max(
                0.05f,
                1 +
                    (luminancePreference *
                        (meanLuminance - 0.5f)));
            result += mean * confidence;
            resultWeight += confidence;
        }

        if (resultWeight <= 0.000001f)
        {
            result = Unpremultiply(center);
        }
        else
        {
            result /= resultWeight;
        }
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    internal static Vector4 FilmGrain(
        PrismCatalogFilterPlan plan,
        Vector4 center,
        int x,
        int y)
    {
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float intensity = Math.Clamp(
            Option(plan, "Intensity", 10),
            0,
            10) * 0.01f;
        if (intensity <= 0)
        {
            return center;
        }

        float grain = Math.Clamp(
            Option(plan, "Grain", 4),
            0,
            20);
        float grainScale = 1 + (grain * 0.25f);
        float sigma = grainScale * 0.55f;
        float inverseTwoSigmaSquared =
            0.5f / (sigma * sigma);
        float pixelX = x + 0.5f;
        float pixelY = y + 0.5f;
        int cellX = (int)MathF.Floor(pixelX / grainScale);
        int cellY = (int)MathF.Floor(pixelY / grainScale);
        uint seed = Seed(plan, "Seed");
        float weightedNoise = 0;
        float squaredWeightTotal = 0;
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int nodeX = cellX + offsetX;
                int nodeY = cellY + offsetY;
                float nodePositionX =
                    (nodeX + 0.5f) * grainScale;
                float nodePositionY =
                    (nodeY + 0.5f) * grainScale;
                float deltaX = pixelX - nodePositionX;
                float deltaY = pixelY - nodePositionY;
                float weight = MathF.Exp(
                    -((deltaX * deltaX) +
                        (deltaY * deltaY)) *
                    inverseTwoSigmaSquared);
                weightedNoise +=
                    FilmGrainGaussian(nodeX, nodeY, seed) *
                    weight;
                squaredWeightTotal += weight * weight;
            }
        }

        float correlatedNoise = weightedNoise /
            MathF.Sqrt(MathF.Max(
                squaredWeightTotal,
                0.000001f));
        Vector3 straight = Unpremultiply(center);
        float luminance = Math.Clamp(
            StraightLuminance(straight),
            0,
            1);
        float highlightArea = Math.Clamp(
            Option(plan, "HighlightArea", 0),
            0,
            20) / 20;
        float variancePeak = 0.5f +
            (highlightArea * 0.4f);
        float booleanLevel = luminance <= variancePeak
            ? 0.5f * luminance / variancePeak
            : 0.5f +
                (0.5f *
                    (luminance - variancePeak) /
                    (1 - variancePeak));
        float signalDeviation = 2 * MathF.Sqrt(
            MathF.Max(
                booleanLevel * (1 - booleanLevel),
                0));
        Vector3 result = straight +
            new Vector3(
                correlatedNoise *
                intensity *
                signalDeviation);
        return Associated(
            Vector3.Clamp(result, Vector3.Zero, Vector3.One),
            center.W);
    }

    private static float FilmGrainGaussian(
        int x,
        int y,
        uint seed) =>
        ((Hash(x, y, seed ^ 0xa511e9b3u) +
            Hash(x, y, seed ^ 0x63d83595u) +
            Hash(x, y, seed ^ 0xb8d26d4du) +
            Hash(x, y, seed ^ 0x9e3779b9u)) -
            2) *
        1.7320508075688772f;

    internal static Vector4 DryBrush(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(
            source,
            width,
            height,
            x,
            y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }

        float radius = MathF.Max(
            1,
            MathF.Max(pass.RadiusX, pass.RadiusY));
        float detail = Math.Clamp(
            Option(plan, "BrushDetail", 8),
            0,
            32);
        float textureStrength = Math.Clamp(
            Option(plan, "Texture", 1) / 4,
            0,
            1);
        uint seed = unchecked(
            (uint)(int)PrismFilterId.DryBrush *
            0x9e3779b9u);
        (float tensorX, float tensorCross, float tensorY) =
            FacetStructureTensor(
                source,
                width,
                height,
                x,
                y);
        float discriminant = MathF.Sqrt(
            MathF.Max(
                0,
                ((tensorX - tensorY) *
                    (tensorX - tensorY)) +
                (4 * tensorCross * tensorCross)));
        float lambda1 =
            0.5f * (tensorX + tensorY + discriminant);
        float lambda2 =
            0.5f * (tensorX + tensorY - discriminant);
        float tensorEnergy = lambda1 + lambda2;
        Vector2 tangent;
        if (tensorEnergy > 0.000001f)
        {
            float angle =
                (0.5f * MathF.Atan2(
                    2 * tensorCross,
                    tensorX - tensorY)) +
                (MathF.PI * 0.5f);
            tangent = new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle));
        }
        else
        {
            int blockSize = Math.Max(
                1,
                (int)MathF.Round(radius * 2));
            float angle = Hash(
                (int)MathF.Floor(x / (float)blockSize),
                (int)MathF.Floor(y / (float)blockSize),
                seed) * MathF.Tau;
            tangent = new Vector2(
                MathF.Cos(angle),
                MathF.Sin(angle));
        }

        Vector2 normal = new(-tangent.Y, tangent.X);
        float coherence =
            tensorEnergy <= 0.000001f
                ? 0
                : Math.Clamp(
                    (lambda1 - lambda2) /
                        tensorEnergy,
                    0,
                    1);
        float majorScale = 1 + (1.25f * coherence);
        float minorScale = 1 - (0.5f * coherence);
        float sharpness =
            1 + (5 * Math.Clamp(detail / 16, 0, 1));
        Vector3 accumulated = Vector3.Zero;
        float totalConfidence = 0;
        for (int sector = 0; sector < 8; sector++)
        {
            float angle = sector * (MathF.PI / 4);
            Vector2 direction =
                (tangent * (MathF.Cos(angle) * majorScale)) +
                (normal * (MathF.Sin(angle) * minorScale));
            direction = Vector2.Normalize(direction);
            DryBrushSector(
                source,
                width,
                height,
                x,
                y,
                direction,
                radius,
                center.W,
                out Vector3 mean,
                out float variance);
            float confidence = 1 /
                (1 + MathF.Pow(
                    MathF.Max(variance * 24, 0),
                    sharpness));
            accumulated += mean * confidence;
            totalConfidence += confidence;
        }

        Vector3 filtered = accumulated /
            MathF.Max(totalConfidence, 0.000001f);
        Vector2 pixel = new(x, y);
        float tangentCoordinate =
            Vector2.Dot(pixel, tangent);
        float normalCoordinate =
            Vector2.Dot(pixel, normal);
        float phaseScale = MathF.Max(radius * 4, 1);
        float normalPhaseScale = MathF.Max(radius * 2, 1);
        float phase = Hash(
            (int)MathF.Floor(
                tangentCoordinate / phaseScale),
            (int)MathF.Floor(
                normalCoordinate / normalPhaseScale),
            seed ^ 0x68bc21ebu);
        float fiberCoordinate =
            normalCoordinate /
            MathF.Max(radius * 0.32f, 0.75f);
        float fiber =
            0.5f +
            (0.5f * MathF.Cos(
                (fiberCoordinate * MathF.Tau) +
                (phase * MathF.Tau)));
        float grain = Hash(
            x,
            y,
            seed ^ 0x02e5be93u);
        float dryPattern = MathF.Pow(
            Math.Clamp(
                (fiber * 0.82f) +
                (grain * 0.18f),
                0,
                1),
            1.4f);
        Vector3 paperGap = Vector3.Lerp(
            filtered,
            Vector3.One,
            0.3f);
        Vector3 result = Vector3.Lerp(
            filtered,
            paperGap,
            textureStrength * dryPattern);
        return Associated(
            Vector3.Clamp(
                result,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }

    private static void DryBrushSector(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        Vector2 direction,
        float radius,
        float centerAlpha,
        out Vector3 mean,
        out float variance)
    {
        Vector3 sum = Vector3.Zero;
        Vector3 squareSum = Vector3.Zero;
        float totalWeight = 0;
        DryBrushAccumulateSample(
            source,
            width,
            height,
            x,
            y,
            Vector2.Zero,
            1,
            centerAlpha,
            ref sum,
            ref squareSum,
            ref totalWeight);
        for (int step = 1; step <= 3; step++)
        {
            float fraction = step / 3f;
            float spatialWeight = MathF.Exp(
                -2 * fraction * fraction);
            Vector2 offset =
                direction * (radius * fraction);
            DryBrushAccumulateSample(
                source,
                width,
                height,
                x,
                y,
                offset,
                spatialWeight,
                centerAlpha,
                ref sum,
                ref squareSum,
                ref totalWeight);
        }

        mean = sum / MathF.Max(totalWeight, 0.000001f);
        Vector3 colorVariance = Vector3.Max(
            (squareSum /
                MathF.Max(totalWeight, 0.000001f)) -
                (mean * mean),
            Vector3.Zero);
        variance =
            (colorVariance.X +
                colorVariance.Y +
                colorVariance.Z) /
            3;
    }

    private static void DryBrushAccumulateSample(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y,
        Vector2 offset,
        float spatialWeight,
        float centerAlpha,
        ref Vector3 sum,
        ref Vector3 squareSum,
        ref float totalWeight)
    {
        Vector4 sample = SamplePixelBilinear(
            source,
            width,
            height,
            x + offset.X,
            y + offset.Y);
        if (sample.W <= 0)
        {
            return;
        }

        float weight =
            spatialWeight *
            MathF.Exp(
                -MathF.Abs(sample.W - centerAlpha) * 8);
        Vector3 color = Unpremultiply(sample);
        sum += color * weight;
        squareSum += color * color * weight;
        totalWeight += weight;
    }

    internal static Vector4 Cutout(
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass,
        Vector4[] source,
        int width,
        int height,
        int x,
        int y)
    {
        Vector4 center = SamplePixel(source, width, height, x, y);
        if (center.W <= 0)
        {
            return Vector4.Zero;
        }
        if (pass.Kind == PrismCatalogFilterPassKind.Direct)
        {
            int levels = Math.Clamp(
                (int)MathF.Round(Option(plan, "Levels", 8)),
                2,
                32);
            return Associated(
                Quantize(
                    Vector3.Clamp(
                        Unpremultiply(center),
                        Vector3.Zero,
                        Vector3.One),
                    levels),
                center.W);
        }

        float radius = MathF.Max(
            1,
            MathF.Max(pass.RadiusX, pass.RadiusY));
        float fidelity = Math.Clamp(
            Option(plan, "EdgeFidelity", 3) / 10,
            0,
            1);
        float rangeSigma = 0.42f - (0.36f * fidelity);
        float rangeDivisor =
            2 * rangeSigma * rangeSigma;
        const float SpatialDivisor = 3.125f;
        Vector3 centerColor = Unpremultiply(center);
        Vector3 accumulated = Vector3.Zero;
        float totalWeight = 0;
        for (int offsetY = -2; offsetY <= 2; offsetY++)
        {
            for (int offsetX = -2; offsetX <= 2; offsetX++)
            {
                Vector4 sample = SamplePixelBilinear(
                    source,
                    width,
                    height,
                    x + (offsetX * radius * 0.5f),
                    y + (offsetY * radius * 0.5f));
                if (sample.W <= 0)
                {
                    continue;
                }

                Vector3 sampleColor = Unpremultiply(sample);
                float spatialDistance =
                    (offsetX * offsetX) +
                    (offsetY * offsetY);
                float rangeDistance =
                    Vector3.DistanceSquared(
                        sampleColor,
                        centerColor);
                float weight =
                    MathF.Exp(
                        -spatialDistance /
                        SpatialDivisor) *
                    MathF.Exp(
                        -rangeDistance /
                        rangeDivisor) *
                    MathF.Exp(
                        -MathF.Abs(sample.W - center.W) * 8);
                accumulated += sampleColor * weight;
                totalWeight += weight;
            }
        }

        Vector3 shifted = totalWeight > 0
            ? accumulated / totalWeight
            : centerColor;
        return Associated(
            Vector3.Clamp(
                shifted,
                Vector3.Zero,
                Vector3.One),
            center.W);
    }
}
