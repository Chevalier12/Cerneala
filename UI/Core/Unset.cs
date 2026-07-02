namespace Cerneala.UI.Core;

internal static class Unset
{
    public static object Value { get; } = new UnsetValue();

    private sealed class UnsetValue
    {
        public override string ToString()
        {
            return "<unset>";
        }
    }
}
