namespace Cerneala.UI.Controls.Primitives;

// WPF's DoubleUtil comparisons, which its Slider and TickBar use, with the float machine epsilon in place of the double
// one: two values are close when they differ by less than about 10 epsilons of their size.
internal static class FloatComparison
{
    private const float Epsilon = 1.1920929e-7f;

    public static bool AreClose(float value1, float value2)
    {
        if (value1 == value2)
        {
            return true;
        }

        float epsilon = (MathF.Abs(value1) + MathF.Abs(value2) + 10) * Epsilon;
        float delta = value1 - value2;
        return -epsilon < delta && epsilon > delta;
    }

    public static bool LessThan(float value1, float value2)
    {
        return value1 < value2 && !AreClose(value1, value2);
    }

    public static bool GreaterThan(float value1, float value2)
    {
        return value1 > value2 && !AreClose(value1, value2);
    }

    public static bool LessThanOrClose(float value1, float value2)
    {
        return value1 < value2 || AreClose(value1, value2);
    }

    public static bool GreaterThanOrClose(float value1, float value2)
    {
        return value1 > value2 || AreClose(value1, value2);
    }

    public static bool GreaterThanZero(float value)
    {
        return value >= 10 * Epsilon;
    }
}
