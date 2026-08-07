using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;



internal static class PrismWindFilter
{
    private const int IntegrationSteps = 8;
    private const float MinimumLength = 0.0001f;
    private const float Pi = MathF.PI;

    public static Vector4[] Apply(
        PrismCatalogFilterPlan plan,
        Vector4[] source,
        int width,
        int height)
    {
        float strength = MathF.Max(
            0,
            plan.GetOption("Strength").X);
        if (strength <= MinimumLength)
        {
            return (Vector4[])source.Clone();
        }

        Vector4[] firstLic = Integrate(
            plan,
            source,
            source,
            width,
            height,
            strength);
        Vector4[] enhanced = EnhanceContrast(
            firstLic,
            width,
            height);
        return Integrate(
            plan,
            enhanced,
            source,
            width,
            height,
            strength);
    }

    private static Vector4[] Integrate(
        PrismCatalogFilterPlan plan,
        Vector4[] signal,
        Vector4[] original,
        int width,
        int height,
        float strength)
    {
        int method = (int)MathF.Round(plan.GetOption("Method").X);
        int direction = (int)MathF.Round(
            plan.GetOption("Direction").X);
        uint seed = UnpackInteger(plan.GetOption("Seed"));
        float lineLength = Math.Clamp(
            strength * MethodLengthScale(method),
            0,
            64);
        float stepLength = lineLength / IntegrationSteps;
        float reverseBias = method switch
        {
            1 => 0.16f,
            2 => 0.42f,
            _ => 0.3f
        };
        Vector4[] result = new Vector4[signal.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 origin = new(x, y);
                Vector2 forward = origin;
                Vector2 backward = origin;
                Vector2 guidance = FlowDirection(
                    origin,
                    direction,
                    method,
                    seed,
                    original,
                    width,
                    height);
                Vector4 total = SampleBilinear(
                    signal,
                    width,
                    height,
                    x,
                    y);
                float weightTotal = 1;

                for (int step = 1; step <= IntegrationSteps; step++)
                {
                    forward = Advance(
                        forward,
                        1,
                        stepLength,
                        direction,
                        method,
                        seed,
                        guidance);
                    backward = Advance(
                        backward,
                        -1,
                        stepLength,
                        direction,
                        method,
                        seed,
                        guidance);
                    float phase = step / (IntegrationSteps + 1f);
                    float window = 0.5f +
                        (0.5f * MathF.Cos(Pi * phase));
                    float forwardWeight = window *
                        StaggerWeight(method, forward, seed, step);
                    float backwardWeight = window * reverseBias;
                    total += SampleBilinear(
                            signal,
                            width,
                            height,
                            forward.X,
                            forward.Y) *
                        forwardWeight;
                    total += SampleBilinear(
                            signal,
                            width,
                            height,
                            backward.X,
                            backward.Y) *
                        backwardWeight;
                    weightTotal += forwardWeight + backwardWeight;
                }

                Vector4 integrated = total / weightTotal;
                float directionSign = direction == 1 ? -1 : 1;
                Vector4 rampSample = SampleBilinear(
                    signal,
                    width,
                    height,
                    x + (directionSign * lineLength * 0.5f),
                    y);
                integrated = Vector4.Lerp(
                    integrated,
                    rampSample,
                    0.18f);
                integrated.W = SampleBilinear(
                    original,
                    width,
                    height,
                    x,
                    y).W;
                result[(y * width) + x] =
                    ClampAssociated(integrated);
            }
        }

        return result;
    }

    private static Vector2 Advance(
        Vector2 position,
        float sign,
        float stepLength,
        int direction,
        int method,
        uint seed,
        Vector2 guidance)
    {
        Vector2 local = ProceduralDirection(
            position,
            direction,
            method,
            seed);
        Vector2 flow = Vector2.Lerp(local, guidance, 0.35f);
        if (flow.LengthSquared() > MinimumLength)
        {
            flow = Vector2.Normalize(flow);
        }
        return position + (flow * (sign * stepLength));
    }

    private static Vector2 FlowDirection(
        Vector2 position,
        int direction,
        int method,
        uint seed,
        Vector4[] original,
        int width,
        int height)
    {
        Vector2 baseDirection = direction == 1
            ? -Vector2.UnitX
            : Vector2.UnitX;
        Vector2 flow = ProceduralDirection(
            position,
            direction,
            method,
            seed);

        float horizontal = Luminance(SampleBilinear(
                original,
                width,
                height,
                position.X + 1,
                position.Y)) -
            Luminance(SampleBilinear(
                original,
                width,
                height,
                position.X - 1,
                position.Y));
        float vertical = Luminance(SampleBilinear(
                original,
                width,
                height,
                position.X,
                position.Y + 1)) -
            Luminance(SampleBilinear(
                original,
                width,
                height,
                position.X,
                position.Y - 1));
        Vector2 tangent = new(-vertical, horizontal);
        float edge = Math.Clamp(tangent.Length() * 1.5f, 0, 1);
        if (tangent.LengthSquared() > MinimumLength)
        {
            tangent = Vector2.Normalize(tangent);
            if (Vector2.Dot(tangent, baseDirection) < 0)
            {
                tangent = -tangent;
            }
            flow = Vector2.Lerp(flow, tangent, edge * 0.18f);
        }

        return flow.LengthSquared() <= MinimumLength
            ? baseDirection
            : Vector2.Normalize(flow);
    }

    private static Vector2 ProceduralDirection(
        Vector2 position,
        int direction,
        int method,
        uint seed)
    {
        Vector2 baseDirection = direction == 1
            ? -Vector2.UnitX
            : Vector2.UnitX;
        float scale = method == 2 ? 7 : 11;
        float noise = ValueNoise(
                position.X / scale,
                position.Y / scale,
                seed + 0x68bc21ebu) -
            0.5f;
        float turbulence = method switch
        {
            1 => 0.16f,
            2 => 0.68f,
            _ => 0.38f
        };
        float angle = noise * turbulence;
        float cosine = MathF.Cos(angle);
        float sine = MathF.Sin(angle);
        return new Vector2(
            (baseDirection.X * cosine) - (baseDirection.Y * sine),
            (baseDirection.X * sine) + (baseDirection.Y * cosine));
    }

    private static Vector4[] EnhanceContrast(
        Vector4[] source,
        int width,
        int height)
    {
        Vector4[] result = new Vector4[source.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector4 center = SampleBilinear(
                    source,
                    width,
                    height,
                    x,
                    y);
                Vector4 neighbors =
                    SampleBilinear(source, width, height, x - 1, y) +
                    SampleBilinear(source, width, height, x + 1, y) +
                    SampleBilinear(source, width, height, x, y - 1) +
                    SampleBilinear(source, width, height, x, y + 1);
                Vector3 highPass = new(
                    (center.X * 4) - neighbors.X,
                    (center.Y * 4) - neighbors.Y,
                    (center.Z * 4) - neighbors.Z);
                Vector3 enhanced = new(
                    center.X + (highPass.X * 0.32f),
                    center.Y + (highPass.Y * 0.32f),
                    center.Z + (highPass.Z * 0.32f));
                float alpha = Math.Clamp(center.W, 0, 1);
                enhanced = Vector3.Clamp(
                    enhanced,
                    Vector3.Zero,
                    new Vector3(alpha));
                result[(y * width) + x] = new Vector4(enhanced, alpha);
            }
        }
        return result;
    }

    private static float StaggerWeight(
        int method,
        Vector2 position,
        uint seed,
        int step)
    {
        if (method != 2)
        {
            return 1;
        }

        float lane = ValueNoise(
            position.X / 5,
            position.Y / 3,
            seed + unchecked((uint)step * 0x9e3779b9u));
        return 0.35f + (lane * 0.65f);
    }

    private static float MethodLengthScale(int method) =>
        method switch
        {
            1 => 5.5f,
            2 => 4.5f,
            _ => 4f
        };

    private static float ValueNoise(float x, float y, uint seed)
    {
        int cellX = (int)MathF.Floor(x);
        int cellY = (int)MathF.Floor(y);
        float horizontal = SmoothCurve(x - cellX);
        float vertical = SmoothCurve(y - cellY);
        float top = float.Lerp(
            Hash(cellX, cellY, seed),
            Hash(cellX + 1, cellY, seed),
            horizontal);
        float bottom = float.Lerp(
            Hash(cellX, cellY + 1, seed),
            Hash(cellX + 1, cellY + 1, seed),
            horizontal);
        return float.Lerp(top, bottom, vertical);
    }

    private static float Hash(int x, int y, uint seed)
    {
        uint value = unchecked((uint)x * 0x9e3779b9u) ^
            unchecked((uint)y * 0x85ebca6bu) ^
            (seed * 0xc2b2ae35u);
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return (value & 0x00ffffffu) / 16777215f;
    }

    private static Vector4 SampleBilinear(
        Vector4[] source,
        int width,
        int height,
        float x,
        float y)
    {
        float clampedX = Math.Clamp(x, 0, width - 1);
        float clampedY = Math.Clamp(y, 0, height - 1);
        int left = (int)MathF.Floor(clampedX);
        int top = (int)MathF.Floor(clampedY);
        int right = Math.Min(left + 1, width - 1);
        int bottom = Math.Min(top + 1, height - 1);
        float horizontal = clampedX - left;
        float vertical = clampedY - top;
        Vector4 upper = Vector4.Lerp(
            source[(top * width) + left],
            source[(top * width) + right],
            horizontal);
        Vector4 lower = Vector4.Lerp(
            source[(bottom * width) + left],
            source[(bottom * width) + right],
            horizontal);
        return Vector4.Lerp(upper, lower, vertical);
    }

    private static float Luminance(Vector4 color)
    {
        if (color.W <= MinimumLength)
        {
            return 0;
        }
        Vector3 straight =
            new Vector3(color.X, color.Y, color.Z) / color.W;
        return Vector3.Dot(
            straight,
            new Vector3(0.2126f, 0.7152f, 0.0722f));
    }

    private static Vector4 ClampAssociated(Vector4 color)
    {
        float alpha = Math.Clamp(color.W, 0, 1);
        return new Vector4(
            Math.Clamp(color.X, 0, alpha),
            Math.Clamp(color.Y, 0, alpha),
            Math.Clamp(color.Z, 0, alpha),
            alpha);
    }

    private static uint UnpackInteger(Vector4 value) =>
        ((uint)value.Y << 16) |
        ((uint)value.X & 0xffffu);

    private static float SmoothCurve(float value) =>
        value * value * (3 - (2 * value));
}
