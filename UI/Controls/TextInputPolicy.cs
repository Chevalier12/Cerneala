namespace Cerneala.UI.Controls;

internal readonly record struct TextInputPolicy(
    bool RecordsHistory,
    bool AllowsCopy,
    bool AllowsCut,
    bool AllowsPaste)
{
    public static TextInputPolicy TextBox { get; } = new(
        RecordsHistory: true,
        AllowsCopy: true,
        AllowsCut: true,
        AllowsPaste: true);

    public static TextInputPolicy PasswordBox { get; } = new(
        RecordsHistory: false,
        AllowsCopy: false,
        AllowsCut: false,
        AllowsPaste: true);
}
