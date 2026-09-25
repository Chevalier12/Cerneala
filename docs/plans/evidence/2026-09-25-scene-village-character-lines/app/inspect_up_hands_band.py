"""Read-only pixel comparison for the user-reported Up hands/shoulder band.

This does not write or transform an image. The nearest-source alpha fit is a
coarse alignment of a filtered screenshot, not an exact renderer emulator.
"""

import argparse
import hashlib
from pathlib import Path

from PIL import Image


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def black(pixel: tuple[int, int, int, int]) -> bool:
    rgb = pixel[:3]
    return pixel[3] > 0 and max(rgb) <= 55 and max(rgb) - min(rgb) <= 8


def runs(bits: list[bool], start: int) -> list[tuple[int, int]]:
    result = []
    run_start = None
    for offset, value in enumerate(bits):
        if value and run_start is None:
            run_start = start + offset
        elif not value and run_start is not None:
            result.append((run_start, start + offset - 1))
            run_start = None
    if run_start is not None:
        result.append((run_start, start + len(bits) - 1))
    return result


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--user-crop", type=Path, required=True)
    parser.add_argument("--linear-capture", type=Path, required=True)
    parser.add_argument("--point-capture", type=Path, required=True)
    args = parser.parse_args()

    with Image.open(args.source) as image:
        source = image.convert("RGBA")
    with Image.open(args.user_crop) as image:
        user = image.convert("RGBA")
    with Image.open(args.linear_capture) as image:
        matrix = image.convert("RGBA")
    with Image.open(args.point_capture) as image:
        production = image.convert("RGBA")

    assert source.size == (128, 128), source.size
    assert user.size == (80, 122), user.size
    assert matrix.size == production.size == (1250, 1063)
    background = user.getpixel((0, 0))[:3]
    print("SOURCE", args.source, sha256(args.source), source.size)
    print("USER_CROP", args.user_crop, sha256(args.user_crop), user.size)
    print("LINEAR_CAPTURE", args.linear_capture, sha256(args.linear_capture), matrix.size)
    print("POINT_CAPTURE", args.point_capture, sha256(args.point_capture), production.size)
    print("background RGB", background)

    # Up is row 1. The neutral poses in columns 1 and 3 have identical
    # visible RGBA and alpha; a screenshot cannot distinguish their indices.
    neutral_visible_differences = sum(
        source.getpixel((32 + x, 32 + y)) != source.getpixel((96 + x, 32 + y))
        for y in range(32)
        for x in range(32)
        if source.getpixel((32 + x, 32 + y))[3] > 0
        or source.getpixel((96 + x, 32 + y))[3] > 0
    )
    print("Up neutral column 1 vs 3 visible RGBA differences", neutral_visible_differences)

    observed_alpha = [
        [user.getpixel((x, y))[:3] != background for x in range(user.width)]
        for y in range(user.height)
    ]
    source_alpha = [
        [source.getpixel((32 + x, 32 + y))[3] > 0 for x in range(32)]
        for y in range(32)
    ]
    # Fit on the ENTIRE screenshot silhouette, not exclusively on the hands ROI. Scale
    # 2.5 matches the native app capture. Quarter-pixel origins bracket the
    # crop's visible character bbox; report every tied optimum.
    scale = 2.5
    best_error = 10**9
    best_fits = []
    for oy_quarters in range(84, 101):       # 21.00..25.00
        oy = oy_quarters / 4
        source_ys = [int((y + 0.5 - oy) // scale) for y in range(user.height)]
        for ox_quarters in range(-40, -23):  # -10.00..-6.00
            ox = ox_quarters / 4
            source_xs = [int((x + 0.5 - ox) // scale) for x in range(user.width)]
            tp = fp = fn = tn = 0
            for y in range(user.height):
                sy = source_ys[y]
                for x in range(user.width):
                    sx = source_xs[x]
                    predicted = (
                        0 <= sx < 32 and 0 <= sy < 32 and source_alpha[sy][sx]
                    )
                    observed = observed_alpha[y][x]
                    if observed and predicted:
                        tp += 1
                    elif observed:
                        fp += 1
                    elif predicted:
                        fn += 1
                    else:
                        tn += 1
            error = fp + fn
            if error < best_error:
                best_error = error
                best_fits = []
            if error == best_error:
                best_fits.append((ox, oy, tp, fp, fn, tn))
    print("whole-image alpha fit scale", scale, "minimum FP+FN", best_error)
    print("equal-best fits (ox,oy,TP,FP,FN,TN)", best_fits)
    ranked_color_fits = []
    for ox, oy, *_ in best_fits:
        total_error = 0
        for y in range(user.height):
            sy = int((y + 0.5 - oy) // scale)
            for x in range(user.width):
                sx = int((x + 0.5 - ox) // scale)
                predicted = background
                if 0 <= sx < 32 and 0 <= sy < 32:
                    texel = source.getpixel((32 + sx, 32 + sy))
                    if texel[3] > 0:
                        predicted = texel[:3]
                actual = user.getpixel((x, y))[:3]
                total_error += sum(abs(actual[channel] - predicted[channel]) for channel in range(3))
        mae = total_error / (user.width * user.height * 3)
        ranked_color_fits.append((mae, ox, oy))
    ranked_color_fits.sort()
    print("same whole-image fits ranked by nearest-visible RGB MAE", ranked_color_fits)

    # The screenshot's actual hands/shoulder band is around y=70..85, NOT
    # y=52..60 (which was mistakenly called the hands in the first ledger).
    # This broad ROI contains both lateral hand extensions and the central
    # torso/neck border. The near-neutral criterion excludes dark red/brown
    # shirt shading without moving the alignment to force a match.
    roi = (8, 58, 70, 86)  # x0,x1,y0,y1, half-open
    x0, x1, y0, y1 = roi
    observed_black = [
        (x, y)
        for y in range(y0, y1)
        for x in range(x0, x1)
        if black(user.getpixel((x, y)))
    ]
    false_positive_counts = []
    for ox, oy, *_ in best_fits:
        false_positives = []
        for x, y in observed_black:
            sx = int((x + 0.5 - ox) // scale)
            sy = int((y + 0.5 - oy) // scale)
            expected = (
                0 <= sx < 32
                and 0 <= sy < 32
                and black(source.getpixel((32 + sx, 32 + sy)))
            )
            if not expected:
                false_positives.append((x, y, sx, sy))
        false_positive_counts.append(len(false_positives))
        print("hands ROI fit", (ox, oy), "observed neutral-black", len(observed_black),
              "not mapped to source black", len(false_positives),
              "coordinates", false_positives)
    print("hands ROI", roi, "source local rows under best fits approx 18..25")
    print("hands ROI black mismatch range", min(false_positive_counts), max(false_positive_counts))

    # Show the source's disjoint hand/torso outline, not just an aggregate.
    for local_y in range(18, 25):
        ink = [black(source.getpixel((32 + x, 32 + local_y))) for x in range(32)]
        print("source Up neutral local y", local_y, "black-x runs", runs(ink, 0))
    for image_y in range(y0, y1):
        ink = [black(user.getpixel((x, image_y))) for x in range(x0, x1)]
        if any(ink):
            print("user hands-band y", image_y, "black-x runs", runs(ink, x0))

    # The same authored band remains in native Point rendering of the actual
    # VillageArt.Player. The matrix Linear and Point controls share the same
    # source frame and 2.5 physical pixels/world; this is not a replacement
    # for comparison to the user's exact UI crop.
    for name, image, left, top in [
        ("Linear atlas matrix Up neutral", matrix, 240, 240),
        ("Point atlas matrix Up neutral", matrix, 240, 330),
        ("Point production Player Up neutral", production, 240, 240),
    ]:
        for relative_rows in [(45, 65), (50, 60)]:
            count = sum(
                black(image.getpixel((left + x, top + y)))
                for y in range(*relative_rows)
                for x in range(80)
            )
            print(name, "relative rows", relative_rows, "neutral-black pixels", count)


if __name__ == "__main__":
    main()
