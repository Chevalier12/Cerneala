namespace Cerneala.UI.Animation;

public static class Easing
{
    public static float Linear(float progress)
    {
        return Clamp(progress);
    }

    public static float EaseInQuad(float progress)
    {
        float clamped = Clamp(progress);
        return clamped * clamped;
    }

    public static float EaseOutQuad(float progress)
    {
        float clamped = Clamp(progress);
        return 1 - ((1 - clamped) * (1 - clamped));
    }

    public static float Clamp(float progress)
    {
        if (float.IsNaN(progress))
        {
            return 0;
        }

        return Math.Clamp(progress, 0, 1);
    }
}
