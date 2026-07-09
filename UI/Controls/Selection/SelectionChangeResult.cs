namespace Cerneala.UI.Controls.Selection;

public readonly record struct SelectionChangeResult(int OldIndex, int NewIndex, bool Changed);
