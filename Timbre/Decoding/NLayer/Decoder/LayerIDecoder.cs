// Vendored from NLayer 3.0.0 (https://github.com/naudio/NLayer, commit
// 046c7ce422970f8f0f0bc205d6c6341bcb7debf1), MIT License: see
// Cerneala.Timbre.THIRD-PARTY-NOTICES.txt. Local changes: namespace, internal
// visibility, removal of the unused CRC helpers.
#nullable disable

/*
 * NLayer - A C# MPEG1/2/2.5 audio decoder
 *
 */

using System;

namespace Cerneala.Timbre.Decoding.NLayer.Decoder
{
    // Layer I is really just a special case of Layer II...  1 granule, 4 allocation bits per subband, 1 scalefactor per active subband, no grouping
    // That (of course) means we literally have no logic here
    class LayerIDecoder : LayerIIDecoderBase
    {
        // this is simple: all 32 subbands have a 4-bit allocations, and positive allocation values are {bits per sample} - 1
        static readonly int[] _rateTable = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        static readonly int[][] _allocLookupTable = { new int[] { 4, 0, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 } };

        internal LayerIDecoder() : base(_allocLookupTable, 1) { }

        protected override int[] GetRateTable(IMpegFrame frame)
        {
            return _rateTable;
        }

        protected override void ReadScaleFactorSelection(IMpegFrame frame, int[][] scfsi, int channels)
        {
            // this is a no-op since the base logic uses "2" as the "has energy" marker
        }
    }
}
