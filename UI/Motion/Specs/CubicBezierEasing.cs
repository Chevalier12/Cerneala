namespace Cerneala.UI.Motion.Specs;

public sealed class CubicBezierEasing : IEasing
{
    private const float Epsilon = 0.000001f;
    private readonly float x1;
    private readonly float y1;
    private readonly float x2;
    private readonly float y2;

    public CubicBezierEasing(float x1, float y1, float x2, float y2)
    {
        if (float.IsNaN(x1) || x1 < 0 || x1 > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x1), "The first x control point must be in [0, 1].");
        }

        if (float.IsNaN(x2) || x2 < 0 || x2 > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(x2), "The second x control point must be in [0, 1].");
        }

        this.x1 = x1;
        this.y1 = y1;
        this.x2 = x2;
        this.y2 = y2;
    }

    public float Transform(float progress)
    {
        if (float.IsNaN(progress))
        {
            return 0;
        }

        float x = Math.Clamp(progress, 0, 1);
        if (x <= 0)
        {
            return 0;
        }

        if (x >= 1)
        {
            return 1;
        }

        float t = SolveTForX(x);
        return Math.Clamp(SampleCurve(t, y1, y2), 0, 1);
    }

    private float SolveTForX(float x)
    {
        float t = x;
        for (int i = 0; i < 8; i++)
        {
            float currentX = SampleCurve(t, x1, x2) - x;
            if (MathF.Abs(currentX) < Epsilon)
            {
                return t;
            }

            float derivative = SampleDerivative(t, x1, x2);
            if (MathF.Abs(derivative) < Epsilon)
            {
                break;
            }

            float next = t - (currentX / derivative);
            if (next < 0 || next > 1)
            {
                break;
            }

            t = next;
        }

        float low = 0;
        float high = 1;
        t = x;
        for (int i = 0; i < 24; i++)
        {
            float currentX = SampleCurve(t, x1, x2);
            if (MathF.Abs(currentX - x) < Epsilon)
            {
                return t;
            }

            if (currentX < x)
            {
                low = t;
            }
            else
            {
                high = t;
            }

            t = (low + high) * 0.5f;
        }

        return t;
    }

    private static float SampleCurve(float t, float control1, float control2)
    {
        float inverse = 1 - t;
        return (3 * inverse * inverse * t * control1)
            + (3 * inverse * t * t * control2)
            + (t * t * t);
    }

    private static float SampleDerivative(float t, float control1, float control2)
    {
        float inverse = 1 - t;
        return (3 * inverse * inverse * control1)
            + (6 * inverse * t * (control2 - control1))
            + (3 * t * t * (1 - control2));
    }
}
