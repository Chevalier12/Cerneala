using System.Globalization;
using Cerneala.UI.Controls.Items;
using Cerneala.UI.Core;

namespace Cerneala.UI.Controls;

// The type-ahead search of ComboBox and ListBox, as WPF's TextSearch: letters typed within one second of each other form
// one prefix, and the first item whose text starts with it matches.
internal sealed class ItemTextSearch
{
    private const long PrefixResetMilliseconds = 1000;

    private string prefix = string.Empty;
    private long lastInputTime;

    // input is one typed text that is not empty. When the prefix with input matches no item, input alone is searched,
    // so typing another first letter starts a new search. Returns the index of the matching item, or -1.
    public int Search(ItemsControl owner, string input, bool caseSensitive)
    {
        long now = Environment.TickCount64;
        if (lastInputTime == 0 || now - lastInputTime > PrefixResetMilliseconds)
        {
            prefix = string.Empty;
        }

        lastInputTime = now;
        string candidatePrefix = prefix + input;
        int matchIndex = FindMatchingItem(owner, candidatePrefix, exactMatch: false, caseSensitive);
        if (matchIndex < 0 && prefix.Length > 0)
        {
            candidatePrefix = input;
            matchIndex = FindMatchingItem(owner, candidatePrefix, exactMatch: false, caseSensitive);
        }

        prefix = candidatePrefix;
        return matchIndex;
    }

    public void Reset()
    {
        prefix = string.Empty;
        lastInputTime = 0;
    }

    public static int FindMatchingItem(ItemsControl owner, string searchText, bool exactMatch, bool caseSensitive)
    {
        CompareInfo compareInfo = CultureInfo.CurrentCulture.CompareInfo;
        CompareOptions options = caseSensitive ? CompareOptions.None : CompareOptions.IgnoreCase;
        for (int index = 0; index < owner.ItemCount; index++)
        {
            string itemText = GetItemText(owner, owner.GetItemAt(index));
            bool matches = exactMatch
                ? compareInfo.Compare(itemText, searchText, options) == 0
                : compareInfo.IsPrefix(itemText, searchText, options);
            if (matches)
            {
                return index;
            }
        }

        return -1;
    }

    // As WPF's TextSearch: an item's own TextSearch.Text, else the value at the owner's TextSearch.TextPath, else the
    // item's display value, as plain text.
    public static string GetItemText(ItemsControl owner, object? item)
    {
        if (item is UiObject uiObject && uiObject.IsSet(TextSearch.TextProperty))
        {
            return TextSearch.GetText(uiObject);
        }

        string textPath = TextSearch.GetTextPath(owner);
        return GetPlainText(textPath.Length > 0
            ? DisplayMemberPathAccessor.Resolve(item, textPath)
            : owner.GetItemDisplayValue(item));
    }

    // As WPF's TextSearch converts a value to plain text: a content element, such as a ComboBoxItem, gives the text
    // of its Content, a TextBlock gives its Text, and any other value gives its ToString().
    private static string GetPlainText(object? value)
    {
        return value switch
        {
            ContentControl contentControl => GetPlainText(contentControl.Content),
            TextBlock textBlock => textBlock.Text,
            _ => value?.ToString() ?? string.Empty
        };
    }
}
