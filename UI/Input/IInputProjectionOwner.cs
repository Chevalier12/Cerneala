using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

// An element whose content is drawn in another visual place (for example the root-owned overlay layer) but stays its
// logical descendant. Routed input events from that content continue from the content root to this owner.
internal interface IInputProjectionOwner
{
    // The visual root of the projected content while it is projected; otherwise null.
    UIElement? ProjectedInputRoot { get; }
}
