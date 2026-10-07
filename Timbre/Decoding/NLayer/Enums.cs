// Vendored from NLayer 3.0.0 (https://github.com/naudio/NLayer, commit
// 046c7ce422970f8f0f0bc205d6c6341bcb7debf1), MIT License: see
// Cerneala.Timbre.THIRD-PARTY-NOTICES.txt. Local changes: namespace, internal
// visibility.
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cerneala.Timbre.Decoding.NLayer
{
    internal enum MpegVersion
    {
        Unknown = 0,
        Version1 = 10,
        Version2 = 20,
        Version25 = 25,
    }

    internal enum MpegLayer
    {
        Unknown = 0,
        LayerI = 1,
        LayerII = 2,
        LayerIII = 3,
    }

    internal enum MpegChannelMode
    {
        Stereo,
        JointStereo,
        DualChannel,
        Mono,
    }

    internal enum StereoMode
    {
        Both,
        LeftOnly,
        RightOnly,
        DownmixToMono,
    }
}
