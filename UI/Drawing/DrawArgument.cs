namespace Cerneala.Drawing;

internal static class DrawArgument
{
    private const float MaxPixelSize = 2_000_000_000f;
    private const float MaxTextSize = 16_384f;

    public static void ThrowIfNotFinite(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ThrowIfNegativeOrNotFinite(float value, string parameterName)
    {
        if (value < 0 || !float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ThrowIfNotValidPixelCoordinate(float value, string parameterName)
    {
        ThrowIfNotFinite(value, parameterName);

        if (value < -MaxPixelSize || value > MaxPixelSize)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ThrowIfNegativeOrNotValidPixelSize(float value, string parameterName)
    {
        ThrowIfNegativeOrNotFinite(value, parameterName);

        if (value > MaxPixelSize)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ThrowIfNotPositiveFinite(float value, string parameterName)
    {
        if (value <= 0 || !float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ThrowIfNotValidPixelSize(float value, string parameterName)
    {
        ThrowIfNotPositiveFinite(value, parameterName);

        if (value > MaxPixelSize)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    public static void ThrowIfNotValidTextSize(float value, string parameterName)
    {
        ThrowIfNotPositiveFinite(value, parameterName);

        if (value > MaxTextSize)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
