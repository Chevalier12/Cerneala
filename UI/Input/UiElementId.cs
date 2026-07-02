namespace Cerneala.UI.Input;

public readonly record struct UiElementId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}
