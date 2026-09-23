using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.SdlGpuSmoke;

public partial class RenderSurface3DPreview : UserControl
{
    private void OnDraw(RenderSurface3D _, RenderSurface3DFrame frame)
    {
        frame.DrawLine(new Vector3(-1, 0, 0), new Vector3(1, 0, 0), new Color(220, 200, 80), 2);
        frame.DrawMarker(Vector3.Zero, new Color(60, 200, 230), 10);
    }
}
