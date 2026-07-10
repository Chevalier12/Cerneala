namespace Cerneala.UI.Markup;

public enum MarkupValueConstraint
{
    None,
    NonNegative,
    Positive
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MarkupValueConstraintAttribute : Attribute
{
    public MarkupValueConstraintAttribute(MarkupValueConstraint constraint)
    {
        Constraint = constraint;
    }

    public MarkupValueConstraint Constraint { get; }
}
