namespace Cerneala.UI.Motion.Input;

public readonly record struct MotionRange(float InputStart, float InputEnd, float OutputStart, float OutputEnd)
{
    public float Map(float value)
    {
        if (InputStart == InputEnd)
        {
            return OutputEnd;
        }

        float progress = Math.Clamp((value - InputStart) / (InputEnd - InputStart), 0, 1);
        return OutputStart + ((OutputEnd - OutputStart) * progress);
    }
}
