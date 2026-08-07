using System.Numerics;

namespace Cerneala.Drawing.Prism.Filters;

internal static class PrismMezzotintThreshold
{
    private const int MatrixSize = 16;
    private const int MatrixMask = MatrixSize - 1;
    private const int GrainyPattern = 1;
    private const int LinePattern = 2;


    private static readonly uint[] DispersedRanks =
    [
        0x4d6ce734u, 0xd1551fe1u, 0x79f56927u, 0xc8108cceu,
        0x17c50286u, 0x75fbab8bu, 0x19afdb45u, 0x6341e456u,
        0x3ff69bb3u, 0xc30630d7u, 0xbe600d9au, 0xda74aa33u,
        0x667a512du, 0x7d5c96bbu, 0xfe8938edu, 0x18f00794u,
        0x0c29eac0u, 0x1644f282u, 0x2854e0b0u, 0x834ecd6fu,
        0x4bdfa859u, 0xd02ba1cau, 0xd59f1b6au, 0x9725ac40u,
        0xb48f6d05u, 0x8ee5681cu, 0x0b81c74au, 0xfcc662ebu,
        0xf9143ed6u, 0xad007b3au, 0xb75d35fau, 0x31761a8du,
        0x9d5fbc7eu, 0x26c25acfu, 0xf1a31372u, 0xa64fd439u,
        0x20de47f4u, 0x9346e884u, 0x774cd2e2u, 0x0eba9503u,
        0x36927024u, 0x2ea40fb5u, 0xb22a8761u, 0x88e65eddu,
        0xef04b1ccu, 0xcbf76b53u, 0x42fdbd08u, 0x32521e71u,
        0xa05be367u, 0x3c7c1dc1u, 0x129957aeu, 0x98f3a9c9u,
        0x4380153bu, 0xdc4990d3u, 0xe97f226eu, 0xbf09852fu,
        0x2cff91d9u, 0x119cec0au, 0xd850c4eeu, 0x7348b965u,
        0xa7b62158u, 0x37b86478u, 0x3d01a58au, 0xa2f8239eu
    ];

    private static readonly uint[] GrainyRanks =
    [
        0x9d23bb02u, 0x6a2557c7u, 0x17dd4dc6u, 0xb3d54f73u,
        0x62fb4c70u, 0xdc84e637u, 0x9264b42eu, 0x912baae8u,
        0xc0118ddeu, 0x18b50590u, 0x34f2407au, 0x42f60e5eu,
        0x467db71eu, 0xfe496eebu, 0xa501bf86u, 0xb18778d6u,
        0xcd35f36du, 0x3dc48e22u, 0x6cce4e9bu, 0x3acb5124u,
        0x5aac0485u, 0xda0ab079u, 0x8c28ed20u, 0xe316a1fau,
        0xe243d49fu, 0x5c7ef519u, 0xb85b81a9u, 0x44bd630cu,
        0xa76f215fu, 0x26be3288u, 0x3fe407c8u, 0xea3683cau,
        0x0ffd80c5u, 0xe74771ccu, 0x72a34b89u, 0x09aef02du,
        0x9641ad4au, 0x1da0ee3bu, 0xd915fc67u, 0x99651b8fu,
        0x56cf12efu, 0xd75300bcu, 0x50b93c9au, 0x33df7bafu,
        0xe52a9375u, 0x60c18b68u, 0x2c74d010u, 0xba0654f4u,
        0x9e61c239u, 0x7c27f11cu, 0xc39848ecu, 0x5dd2951au,
        0x52f713dbu, 0xa2c945b2u, 0x5903b629u, 0xa43069e0u,
        0x0d976b8au, 0x580877e1u, 0x82f866d8u, 0x1fffab38u,
        0x76d33ee9u, 0x94f9a62fu, 0xa8319c14u, 0x557f0bd1u
    ];

    public static float Sample(
        int x,
        int y,
        uint seed,
        Vector4 pattern)
    {
        int extent = Math.Max(1, (int)pattern.X);
        int thickness = Math.Max(1, (int)pattern.Y);
        int patternKind = (int)pattern.Z;
        uint phase = Mix(
            seed ^
            unchecked((uint)patternKind * 0x9e3779b9u));
        bool vertical =
            patternKind >= LinePattern &&
            (phase & 0x100u) != 0;
        int primary = vertical ? y : x;
        int secondary = vertical ? x : y;
        int matrixX =
            ((primary / extent) + (int)(phase & MatrixMask)) &
            MatrixMask;
        int matrixY =
            ((secondary / thickness) +
                (int)((phase >> 4) & MatrixMask)) &
            MatrixMask;
        int index = (matrixY * MatrixSize) + matrixX;
        uint[] ranks =
            patternKind == GrainyPattern
                ? GrainyRanks
                : DispersedRanks;
        uint packed = ranks[index >> 2];
        uint rank =
            (packed >> ((index & 3) * 8)) &
            0xffu;
        return (rank + 0.5f) / 256;
    }

    private static uint Mix(uint value)
    {
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        value ^= value >> 16;
        return value;
    }
}
