namespace Cerneala.UI.Input;

// How a mouse press of an InputFrame relates to the click sequence before it. Platform input is ByTime: the press
// continues the sequence only inside the double-click time and size, as in WPF. A Servo action names its sequence
// itself, because Servo input frames carry no real elapsed time.
// Example: ClickAsync(button, clickCount: 2) sends Start for its first press and Continue for its second, so the
// presses count 1 and 2 however long the frames took; the next ClickAsync sends Start and counts 1 again.
internal enum PressClickSequence
{
    ByTime,
    Start,
    Continue
}
