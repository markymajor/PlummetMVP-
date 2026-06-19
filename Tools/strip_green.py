#!/usr/bin/env python3
"""Remove a green-screen (chroma key) background from character art and
auto-trim the transparent border, producing a clean sprite PNG.

Usage:
    python3 Tools/strip_green.py input.png output.png
    python3 Tools/strip_green.py --dir Assets/Plummet/SkinDrop Assets/Plummet/Sprites/Game/skins

Tuning (optional):
    --tol N     colour distance tolerance for the key (default 110)
    --soft N    soft edge width in colour-distance units (default 40)
    --no-trim   keep the original canvas size instead of cropping to content

The key colour is auto-detected from the most common border pixel, so it
works for any flat green/blue screen, not just pure (0,255,0).
"""
import argparse
import os
import sys

try:
    from PIL import Image
except ImportError:  # pragma: no cover - dependency hint
    sys.exit("Pillow is required: pip install Pillow")


def detect_key(img):
    """Most common colour around the image border = the screen colour."""
    w, h = img.size
    px = img.load()
    counts = {}
    step = max(1, min(w, h) // 100)
    for x in range(0, w, step):
        for y in (0, h - 1):
            counts[px[x, y][:3]] = counts.get(px[x, y][:3], 0) + 1
    for y in range(0, h, step):
        for x in (0, w - 1):
            counts[px[x, y][:3]] = counts.get(px[x, y][:3], 0) + 1
    return max(counts, key=counts.get)


def strip(img, key, tol, soft):
    img = img.convert("RGBA")
    px = img.load()
    w, h = img.size
    kr, kg, kb = key
    tol2 = float(tol)
    span = max(1.0, float(soft))
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            # Distance from the key colour.
            dist = ((r - kr) ** 2 + (g - kg) ** 2 + (b - kb) ** 2) ** 0.5
            if dist <= tol2:
                px[x, y] = (r, g, b, 0)
            elif dist <= tol2 + span:
                # Soft edge: fade alpha across the transition band.
                frac = (dist - tol2) / span
                px[x, y] = (r, g, b, int(round(a * frac)))
            # Despill: pull obvious green fringe back toward grey.
            elif g > r + 25 and g > b + 25:
                g2 = (r + b) // 2 + 10
                px[x, y] = (r, min(g, g2), b, a)
    return img


def trim(img):
    bbox = img.split()[-1].getbbox()
    return img.crop(bbox) if bbox else img


def process(src, dst, tol, soft, do_trim):
    img = Image.open(src)
    key = detect_key(img)
    out = strip(img, key, tol, soft)
    if do_trim:
        out = trim(out)
    os.makedirs(os.path.dirname(dst) or ".", exist_ok=True)
    out.save(dst)
    print(f"  {os.path.basename(src)} -> {dst}  (key={key}, size={out.size})")


def main():
    ap = argparse.ArgumentParser(description="Strip a green-screen background.")
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--dir", action="store_true", help="treat src/dst as folders")
    ap.add_argument("--tol", type=int, default=110)
    ap.add_argument("--soft", type=int, default=40)
    ap.add_argument("--no-trim", action="store_true")
    args = ap.parse_args()

    do_trim = not args.no_trim
    if args.dir:
        for name in sorted(os.listdir(args.src)):
            if name.lower().endswith((".png", ".jpg", ".jpeg")):
                base = os.path.splitext(name)[0] + ".png"
                process(os.path.join(args.src, name),
                        os.path.join(args.dst, base), args.tol, args.soft, do_trim)
    else:
        process(args.src, args.dst, args.tol, args.soft, do_trim)


if __name__ == "__main__":
    main()
