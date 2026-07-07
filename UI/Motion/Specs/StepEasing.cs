namespace Cerneala.UI.Motion.Specs;

public enum StepPosition
{
    JumpStart,
    JumpEnd,
    JumpBoth,
    JumpNone
}

public sealed class StepEasing : IEasing
{
    public StepEasing(int steps, StepPosition position = StepPosition.JumpEnd)
    {
        if (steps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), "Steps must be positive.");
        }

        if (position == StepPosition.JumpNone && steps < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(steps), "JumpNone requires at least two steps.");
        }

        Steps = steps;
        Position = position;
    }

    public int Steps { get; }

    public StepPosition Position { get; }

    public float Transform(float progress)
    {
        if (float.IsNaN(progress))
        {
            return 0;
        }

        float p = Math.Clamp(progress, 0, 1);
        return Position switch
        {
            StepPosition.JumpStart => Math.Clamp((MathF.Floor(p * Steps) + 1) / Steps, 0, 1),
            StepPosition.JumpEnd => p >= 1 ? 1 : Math.Clamp(MathF.Floor(p * Steps) / Steps, 0, 1),
            StepPosition.JumpBoth => p >= 1 ? 1 : Math.Clamp((MathF.Floor(p * Steps) + 1) / (Steps + 1), 0, 1),
            StepPosition.JumpNone => TransformJumpNone(p),
            _ => p
        };
    }

    private float TransformJumpNone(float progress)
    {
        if (progress <= 0)
        {
            return 0;
        }

        if (progress >= 1)
        {
            return 1;
        }

        return Math.Clamp(MathF.Floor(progress * Steps) / (Steps - 1), 0, 1);
    }
}
