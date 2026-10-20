namespace Cerneala.UI.Controls;

// How many items a ListBox lets the user select, as WPF's System.Windows.Controls.SelectionMode.
public enum SelectionMode
{
    // One item at most: a click selects the clicked item only.
    Single,

    // Any number of items: a click selects or deselects the clicked item and keeps the others.
    Multiple,

    // Any number of items: a click selects the clicked item only, Ctrl+click selects or deselects one item, and
    // Shift+click selects the items from the anchor to the clicked item.
    Extended
}
