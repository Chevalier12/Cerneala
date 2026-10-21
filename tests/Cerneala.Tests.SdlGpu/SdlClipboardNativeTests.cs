using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Platform;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlClipboardNativeTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NativePlatformClipboardRoundTripsTextThroughTheSystemClipboard()
    {
        NativeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());
        IClipboard clipboard = Assert.IsType<SdlClipboard>(platform.PlatformServices.Clipboard);
        // The test writes to the real system clipboard, so it restores what the user had.
        string? previous = api.HasClipboardText() ? api.GetClipboardText() : null;
        try
        {
            clipboard.SetText("a\r\nb");

            Assert.True(clipboard.HasText);
            Assert.Equal("a\r\nb", clipboard.GetText());
        }
        finally
        {
            // SDL cannot clear the clipboard through ISdlApi; an empty text is what an empty clipboard reads as.
            api.SetClipboardText(previous ?? string.Empty);
        }
    }
}