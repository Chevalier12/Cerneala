namespace Cerneala.UI.Controls;

public interface IScrollInfo
{
    float HorizontalOffset { get; }

    float VerticalOffset { get; }

    float ExtentWidth { get; }

    float ExtentHeight { get; }

    float ViewportWidth { get; }

    float ViewportHeight { get; }

    bool CanHorizontallyScroll { get; set; }

    bool CanVerticallyScroll { get; set; }

    void SetHorizontalOffset(float offset);

    void SetVerticalOffset(float offset);
}
