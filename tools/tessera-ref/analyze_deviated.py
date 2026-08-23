#!/usr/bin/env python3
"""Statistical analysis + full-UI annotation for pre-cropped Tessera deviated refs."""

from __future__ import annotations

import argparse
import json
import math
import statistics
from collections import Counter
from dataclasses import asdict, dataclass, field
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_IN = ROOT / ".local" / "Tessera" / "deviated"
DEFAULT_OUT = ROOT / ".local" / "Tessera" / "deviated" / "analysis"

# Semantic hints per style (Rainmeter structure); used to label detected bands/columns.
STYLE_LAYOUT: dict[str, dict] = {
    "pixel": {
        "orientation": "columns",
        "labels": ["media_transport", "media_toggles", "volume_slider", "device_menu"],
    },
    "coreui": {
        "orientation": "rows",
        "labels": ["volume_bar", "media_block"],
    },
    "win11": {
        "orientation": "rows",
        "labels": ["volume_bar", "media_block"],
    },
    "smouti": {
        "orientation": "single",
        "labels": ["flyout_panel"],
    },
}


@dataclass
class Box:
    x: int
    y: int
    w: int
    h: int
    label: str = "region"
    color: tuple[int, int, int] = (255, 80, 80)

    @property
    def x2(self) -> int:
        return self.x + self.w

    @property
    def y2(self) -> int:
        return self.y + self.h

    @property
    def area(self) -> int:
        return self.w * self.h

    def to_dict(self) -> dict:
        return {
            "label": self.label,
            "x": self.x,
            "y": self.y,
            "w": self.w,
            "h": self.h,
            "area_share": round(self.area, 2),
        }


@dataclass
class RefStats:
    file: str
    style_id: str
    width: int
    height: int
    aspect: float
    canvas: Box
    content_bounds: Box
    margins: dict[str, int]
    dominant_colors: list[dict]
    regions: list[dict] = field(default_factory=list)
    bands: list[dict] = field(default_factory=list)
    columns: list[dict] = field(default_factory=list)
    luminance: dict = field(default_factory=dict)
    edge_density: float = 0.0
    estimated_corner_radius: int | None = None

    def to_dict(self) -> dict:
        d = asdict(self)
        d["canvas"] = self.canvas.to_dict()
        d["content_bounds"] = self.content_bounds.to_dict()
        return d


def style_from_name(path: Path) -> str:
    name = path.stem.lower()
    if name.startswith("ref-"):
        return name[4:]
    return name


def rgba(img: Image.Image) -> Image.Image:
    return img.convert("RGBA")


def lum(r: int, g: int, b: int) -> float:
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def sample_corners(img: Image.Image, n: int = 6) -> tuple[int, int, int]:
    w, h = img.size
    pts = [
        (1, 1),
        (w - 2, 1),
        (1, h - 2),
        (w - 2, h - 2),
        (w // 2, 1),
        (w // 2, h - 2),
    ]
    rs, gs, bs = [], [], []
    px = img.load()
    for x, y in pts[:n]:
        r, g, b, a = px[x, y]
        if a < 16:
            continue
        rs.append(r)
        gs.append(g)
        bs.append(b)
    if not rs:
        return (0, 0, 0)
    return (
        int(statistics.mean(rs)),
        int(statistics.mean(gs)),
        int(statistics.mean(bs)),
    )


def color_dist(c1: tuple[int, int, int], c2: tuple[int, int, int]) -> float:
    return math.sqrt(sum((a - b) ** 2 for a, b in zip(c1, c2)))


def content_mask(img: Image.Image, backdrop: tuple[int, int, int], tol: float = 42.0) -> list[list[bool]]:
    w, h = img.size
    px = img.load()
    mask = [[False] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 24:
                continue
            if color_dist((r, g, b), backdrop) > tol:
                mask[y][x] = True
            elif lum(r, g, b) < 48 and a > 200:
                mask[y][x] = True
    return mask


def mask_bounds(mask: list[list[bool]]) -> Box | None:
    h = len(mask)
    if h == 0:
        return None
    w = len(mask[0])
    min_x, min_y = w, h
    max_x, max_y = -1, -1
    for y in range(h):
        for x in range(w):
            if mask[y][x]:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if max_x < 0:
        return None
    return Box(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1, "content_bounds", (0, 220, 255))


def row_fill(mask: list[list[bool]], x0: int, x1: int) -> list[float]:
    h = len(mask)
    out: list[float] = []
    span = max(1, x1 - x0 + 1)
    for y in range(h):
        c = sum(1 for x in range(x0, x1 + 1) if mask[y][x])
        out.append(c / span)
    return out


def col_fill(mask: list[list[bool]], y0: int, y1: int) -> list[float]:
    if not mask:
        return []
    w = len(mask[0])
    out: list[float] = []
    span = max(1, y1 - y0 + 1)
    for x in range(w):
        c = sum(1 for y in range(y0, y1 + 1) if mask[y][x])
        out.append(c / span)
    return out


def find_gaps(profile: list[float], low: float = 0.08, min_gap: int = 4) -> list[tuple[int, int]]:
    gaps: list[tuple[int, int]] = []
    i = 0
    n = len(profile)
    while i < n:
        if profile[i] <= low:
            start = i
            while i < n and profile[i] <= low:
                i += 1
            if i - start >= min_gap:
                gaps.append((start, i - 1))
        else:
            i += 1
    return gaps


def split_rows(mask: list[list[bool]], content: Box, labels: list[str]) -> list[Box]:
    fills = row_fill(mask, content.x, content.x2 - 1)
    gaps = find_gaps(fills, low=0.06, min_gap=max(3, content.h // 40))
    cuts = [content.y] + [(a + b) // 2 + 1 for a, b in gaps] + [content.y2]
    cuts = sorted(set(cuts))
    boxes: list[Box] = []
    palette = [(255, 120, 80), (255, 200, 80), (120, 255, 160), (180, 140, 255)]
    for i in range(len(cuts) - 1):
        y0, y1 = cuts[i], cuts[i + 1]
        if y1 - y0 < 8:
            continue
        label = labels[i] if i < len(labels) else f"row_{i + 1}"
        boxes.append(Box(content.x, y0, content.w, y1 - y0, label, palette[i % len(palette)]))
    if len(boxes) <= 1 and content.h > 24:
        # Fallback: equal split for known two-row styles
        if len(labels) >= 2:
            mid = content.y + content.h // 2
            boxes = [
                Box(content.x, content.y, content.w, mid - content.y, labels[0], palette[0]),
                Box(content.x, mid, content.w, content.y2 - mid, labels[1], palette[1]),
            ]
    return boxes


def split_columns(mask: list[list[bool]], content: Box, labels: list[str]) -> list[Box]:
    fills = col_fill(mask, content.y, content.y2 - 1)
    gaps = find_gaps(fills, low=0.06, min_gap=max(3, content.w // 40))
    cuts = [content.x] + [(a + b) // 2 + 1 for a, b in gaps] + [content.x2]
    cuts = sorted(set(cuts))
    boxes: list[Box] = []
    palette = [(255, 120, 80), (255, 200, 80), (120, 255, 160), (180, 140, 255)]
    idx = 0
    for i in range(len(cuts) - 1):
        x0, x1 = cuts[i], cuts[i + 1]
        if x1 - x0 < 8:
            continue
        label = labels[idx] if idx < len(labels) else f"col_{idx + 1}"
        boxes.append(Box(x0, content.y, x1 - x0, content.h, label, palette[idx % len(palette)]))
        idx += 1
    return boxes


def find_subregions(mask: list[list[bool]], parent: Box, prefix: str, max_regions: int = 8) -> list[Box]:
    """Find bright/dense blobs inside a panel (icons, sliders, art tiles)."""
    w, h = parent.w, parent.h
    local = [[False] * w for _ in range(h)]
    for y in range(h):
        gy = parent.y + y
        if gy >= len(mask):
            break
        for x in range(w):
            gx = parent.x + x
            if gx < len(mask[0]) and mask[gy][gx]:
                local[y][x] = True

    # Downsample for connected-component labeling
    ds = 2
    gw = max(1, w // ds)
    gh = max(1, h // ds)
    grid = [[0] * gw for _ in range(gh)]
    for gy in range(gh):
        for gx in range(gw):
            c = 0
            for yy in range(gy * ds, min((gy + 1) * ds, h)):
                for xx in range(gx * ds, min((gx + 1) * ds, w)):
                    if local[yy][xx]:
                        c += 1
            if c >= max(1, (ds * ds) // 3):
                grid[gy][gx] = 1

    seen = [[False] * gw for _ in range(gh)]
    comps: list[tuple[int, int, int, int, int]] = []
    for y in range(gh):
        for x in range(gw):
            if grid[y][x] != 1 or seen[y][x]:
                continue
            stack = [(x, y)]
            seen[y][x] = True
            minx = maxx = x
            miny = maxy = y
            area = 0
            while stack:
                cx, cy = stack.pop()
                area += 1
                minx, maxx = min(minx, cx), max(maxx, cx)
                miny, maxy = min(miny, cy), max(maxy, cy)
                for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1)):
                    if 0 <= nx < gw and 0 <= ny < gh and grid[ny][nx] == 1 and not seen[ny][nx]:
                        seen[ny][nx] = True
                        stack.append((nx, ny))
            if area >= 6:
                comps.append((minx * ds, miny * ds, (maxx - minx + 1) * ds, (maxy - miny + 1) * ds, area))

    comps.sort(key=lambda t: t[4] * t[2] * t[3], reverse=True)
    palette = [(255, 255, 120), (120, 255, 255), (255, 160, 255), (160, 255, 160)]
    out: list[Box] = []
    panel_area = max(1, parent.area)
    for i, (x, y, bw, bh, _) in enumerate(comps[:max_regions]):
        if bw * bh < panel_area * 0.004:
            continue
        out.append(
            Box(
                parent.x + x,
                parent.y + y,
                min(bw, w - x),
                min(bh, h - y),
                f"{prefix}_blob_{i + 1}",
                palette[i % len(palette)],
            )
        )
    return out


def dominant_colors(img: Image.Image, k: int = 6) -> list[dict]:
    small = img.convert("RGB").resize((max(1, img.width // 4), max(1, img.height // 4)))
    px = list(small.get_flattened_data())
    # Quantize to 5-bit channels
    buckets: Counter[tuple[int, int, int]] = Counter()
    for r, g, b in px:
        q = (r >> 3, g >> 3, b >> 3)
        buckets[q] += 1
    top = buckets.most_common(k)
    total = sum(c for _, c in top) or 1
    out: list[dict] = []
    for (qr, qg, qb), count in top:
        rr, gg, bb = qr << 3, qg << 3, qb << 3
        out.append(
            {
                "rgb": [rr, gg, bb],
                "hex": f"#{rr:02x}{gg:02x}{bb:02x}",
                "share": round(count / total, 4),
            }
        )
    return out


def estimate_corner_radius(img: Image.Image, box: Box) -> int | None:
    px = img.load()
    x0, y0 = box.x, box.y
    best = 0
    for r in range(2, min(box.w, box.h) // 2):
        ok = True
        for t in range(r):
            sx, sy = x0 + t, y0 + (r - 1 - t)
            if sx >= img.width or sy >= img.height:
                ok = False
                break
            _, _, _, a = px[sx, sy]
            if a < 32:
                ok = False
                break
        if ok:
            best = r
        else:
            break
    return best or None


def edge_density(img: Image.Image) -> float:
    edges = img.convert("L").filter(ImageFilter.FIND_EDGES)
    stat = ImageStat.Stat(edges)
    return round(stat.mean[0] / 255.0, 4)


def luminance_stats(img: Image.Image) -> dict:
    gray = img.convert("L")
    stat = ImageStat.Stat(gray)
    return {
        "mean": round(stat.mean[0], 2),
        "stdev": round(stat.stddev[0], 2),
        "min": stat.extrema[0][0],
        "max": stat.extrema[0][1],
    }


def region_color_stats(img: Image.Image, box: Box) -> dict:
    crop = img.crop((box.x, box.y, box.x2, box.y2)).convert("RGB")
    stat = ImageStat.Stat(crop)
    return {
        "mean_rgb": [round(v, 1) for v in stat.mean[:3]],
        "stdev_rgb": [round(v, 1) for v in stat.stddev[:3]],
    }


def analyze(path: Path) -> tuple[RefStats, list[Box]]:
    img = rgba(Image.open(path))
    w, h = img.size
    style = style_from_name(path)
    layout = STYLE_LAYOUT.get(style, {"orientation": "single", "labels": ["flyout_panel"]})

    canvas = Box(0, 0, w, h, "flyout_canvas", (255, 255, 255))
    backdrop = sample_corners(img)
    mask = content_mask(img, backdrop)
    content = mask_bounds(mask) or Box(0, 0, w, h, "content_bounds", (0, 220, 255))

    margins = {
        "top": content.y,
        "left": content.x,
        "right": w - content.x2,
        "bottom": h - content.y2,
    }

    regions: list[Box] = [canvas, content]
    bands: list[Box] = []
    columns: list[Box] = []

    if layout["orientation"] == "rows":
        bands = split_rows(mask, content, layout["labels"])
        regions.extend(bands)
    elif layout["orientation"] == "columns":
        columns = split_columns(mask, content, layout["labels"])
        regions.extend(columns)
    else:
        bands = [Box(content.x, content.y, content.w, content.h, layout["labels"][0], (255, 160, 80))]
        regions.extend(bands)

    # Sub-blobs inside each major panel
    for panel in bands + columns:
        regions.extend(find_subregions(mask, panel, panel.label, max_regions=6))

    stats = RefStats(
        file=path.name,
        style_id=style,
        width=w,
        height=h,
        aspect=round(w / h, 4),
        canvas=canvas,
        content_bounds=content,
        margins=margins,
        dominant_colors=dominant_colors(img),
        regions=[{**b.to_dict(), **region_color_stats(img, b)} for b in regions],
        bands=[b.to_dict() for b in bands],
        columns=[b.to_dict() for b in columns],
        luminance=luminance_stats(img),
        edge_density=edge_density(img),
        estimated_corner_radius=estimate_corner_radius(img, content),
    )

    # Fill area shares
    total = w * h
    for r in stats.regions:
        r["area_share"] = round((r["w"] * r["h"]) / total, 4)

    return stats, regions


def load_font(size: int = 12) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for name in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def annotate(path: Path, regions: list[Box], out_path: Path, stats: RefStats) -> None:
    base = rgba(Image.open(path))
    overlay = base.copy()
    draw = ImageDraw.Draw(overlay, "RGBA")
    font = load_font(11)
    title_font = load_font(13)

    # Dim outside canvas slightly (should be no-op on precrops)
    draw.rectangle((0, 0, base.width, base.height), outline=(255, 255, 255, 180), width=2)

    # Draw regions: canvas/content first (thin), panels thick, blobs medium
    for box in regions:
        if box.label == "flyout_canvas":
            continue
        width = 3 if box.label in ("content_bounds",) or box.label.endswith("_bar") or box.label.endswith("_block") else 2
        if "_blob_" in box.label:
            width = 1
        rgba_color = (*box.color, 220 if width >= 2 else 160)
        draw.rectangle((box.x, box.y, box.x2 - 1, box.y2 - 1), outline=rgba_color, width=width)

        tag = f"{box.label} {box.w}x{box.h}"
        tx, ty = box.x + 2, max(0, box.y - 14)
        if ty <= 0:
            ty = box.y + 2
        tw, th = draw.textbbox((0, 0), tag, font=font)[2:]
        draw.rectangle((tx - 1, ty - 1, tx + tw + 2, ty + th + 1), fill=(0, 0, 0, 170))
        draw.text((tx, ty), tag, fill=(255, 255, 255, 255), font=font)

    # Stats strip below image
    header = f"{stats.style_id}  {stats.width}x{stats.height}  aspect={stats.aspect}  r~{stats.estimated_corner_radius}px"
    margin_line = f"margins T{stats.margins['top']} L{stats.margins['left']} R{stats.margins['right']} B{stats.margins['bottom']}"
    colors = "  ".join(f"{c['hex']} {c['share']*100:.0f}%" for c in stats.dominant_colors[:4])
    lum = stats.luminance
    footer = f"L* mean={lum['mean']} stdev={lum['stdev']}  edge={stats.edge_density}"

    pad = 8
    strip_h = 72
    out = Image.new("RGBA", (base.width, base.height + strip_h), (18, 18, 24, 255))
    out.paste(overlay, (0, 0))
    d2 = ImageDraw.Draw(out)
    y = base.height + pad
    d2.text((pad, y), header, fill=(240, 240, 255), font=title_font)
    d2.text((pad, y + 18), margin_line, fill=(200, 200, 210), font=font)
    d2.text((pad, y + 34), colors, fill=(180, 220, 180), font=font)
    d2.text((pad, y + 50), footer, fill=(180, 180, 200), font=font)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    out.save(out_path)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--in-dir", type=Path, default=DEFAULT_IN)
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT)
    args = parser.parse_args()

    refs = sorted(args.in_dir.glob("ref-*.png"))
    if not refs:
        print(f"No ref-*.png in {args.in_dir}")
        return 1

    manifest: list[dict] = []
    for path in refs:
        stats, regions = analyze(path)
        out_png = args.out_dir / f"{path.stem}_annotated.png"
        annotate(path, regions, out_png, stats)
        manifest.append(stats.to_dict())
        print(f"{path.name}: {stats.width}x{stats.height}  regions={len(regions)}  -> {out_png.name}")

    summary = {
        "source_dir": str(args.in_dir),
        "styles": [style_from_name(p) for p in refs],
        "refs": manifest,
    }
    out_json = args.out_dir / "deviated_stats.json"
    out_json.write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(f"Wrote {out_json}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
