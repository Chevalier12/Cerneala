namespace Cerneala.UI.Controls;

public class ComboBoxItem : ListBoxItem
{
    // Not focusable, unlike a ListBoxItem: a WPF ComboBoxItem does not focus itself when it handles MouseLeftButtonDown,
    // and the ComboBox keeps the keyboard focus while its drop-down is open, so a press on an item leaves it there (#662).
    public ComboBoxItem()
    {
        SetFrameworkDefault(FocusableProperty, false);
    }
}
