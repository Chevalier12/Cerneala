namespace Cerneala.UI.Motion.Specs;

public static class Easings
{
    public static IEasing Linear { get; } = new LinearEasing();

    public static IEasing Standard { get; } = new CubicBezierEasing(0.2f, 0, 0, 1);

    public static IEasing Emphasized { get; } = new CubicBezierEasing(0.2f, 0, 0, 1);

    public static IEasing EaseIn { get; } = new CubicBezierEasing(0.4f, 0, 1, 1);

    public static IEasing EaseOut { get; } = new CubicBezierEasing(0, 0, 0.2f, 1);

    public static IEasing EaseInOut { get; } = new CubicBezierEasing(0.4f, 0, 0.2f, 1);

    public static IEasing Sharp { get; } = new CubicBezierEasing(0.4f, 0, 0.6f, 1);

    private sealed class LinearEasing : IEasing
    {
        public float Transform(float progress)
        {
            if (float.IsNaN(progress))
            {
                return 0;
            }

            return Math.Clamp(progress, 0, 1);
        }
    }
}
