using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.SourceGen;

// Image.Stretch and Image.StretchDirection (issue #687) are assigned from markup attributes.
public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void ImageStretchAndStretchDirectionAreAssignedFromAttributes()
    {
        Image image = Assert.IsType<Image>(CreateRichFixture(
            "ImageStretch",
            """<Image Stretch="UniformToFill" StretchDirection="DownOnly" />"""));

        Assert.Equal(DrawBrushStretch.UniformToFill, image.Stretch);
        Assert.Equal(StretchDirection.DownOnly, image.StretchDirection);
    }
}
