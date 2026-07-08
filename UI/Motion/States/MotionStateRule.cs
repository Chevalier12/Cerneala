namespace Cerneala.UI.Motion.States;

public sealed class MotionStateRule
{
    public MotionStateRule(string stateName)
    {
        StateName = string.IsNullOrWhiteSpace(stateName)
            ? throw new ArgumentException("Motion state name cannot be empty.", nameof(stateName))
            : stateName;
    }

    public string StateName { get; }
}
