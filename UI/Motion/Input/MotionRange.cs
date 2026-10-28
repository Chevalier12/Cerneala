namespace Cerneala.UI.Motion.Input;

public readonly record struct MotionRange(float InputStart, float InputEnd, float OutputStart, float OutputEnd)
{
    public float Map(float value)
    {
        if (InputStart == InputEnd || value == InputEnd)
        {
            return OutputEnd;
        }

        if (value == InputStart)
        {
            return OutputStart;
        }

        // In double: for finite float endpoints such as -3e38 and 3e38, the interval widths overflow float to infinity.
        double progress = Math.Clamp(((double)value - InputStart) / ((double)InputEnd - InputStart), 0, 1);

        // The endpoints are returned as they are: with an infinite other endpoint, the formula gives NaN (infinity * 0).
        if (progress == 0)
        {
            return OutputStart;
        }

        if (progress == 1)
        {
            return OutputEnd;
        }

        return (float)(OutputStart + (((double)OutputEnd - OutputStart) * progress));
    }
}
