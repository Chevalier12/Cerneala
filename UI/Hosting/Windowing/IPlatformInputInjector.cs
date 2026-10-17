using Cerneala.UI.Servo;

namespace Cerneala.UI.Hosting.Windowing;

// Implemented by a platform window that can push synthetic input into the same event
// queue its native events arrive on, so the input travels through the platform's own
// event mapping and IInputSource. Servo's platform input path requires it.
internal interface IPlatformInputInjector
{
    // True while the input source holds input that the frames collected so far have not
    // delivered: input deferred to a later frame, or already placed in the frame under
    // construction.
    bool HasPendingInput { get; }

    // Maps every event first and throws ArgumentException when one cannot be mapped,
    // before anything is pushed. The events reach the app on the next event pump.
    void Inject(ReadOnlySpan<ServoPlatformInput> events);
}
