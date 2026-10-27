namespace Cerneala.UI.Motion.Input;

public readonly record struct MotionRange(float InputStart, float InputEnd, float OutputStart, float OutputEnd)
{
    public float Map(float value)
    {
        if (InputStart == InputEnd)
        {
            return OutputEnd;
        }

        // In double: for finite float endpoints such as -3e38 and 3e38, the interval widths overflow float to infinity.
        double progress = Math.Clamp(((double)value - InputStart) / ((double)InputEnd - InputStart), 0, 1);
        return (float)(OutputStart + (((double)OutputEnd - OutputStart) * progress));
    }
}
