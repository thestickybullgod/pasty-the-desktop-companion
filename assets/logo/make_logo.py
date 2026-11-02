"""Builds Pasty's wordmark logo as SVG: Fredoka letters converted to outlines, the teal glue-stick cap on the P,
and the orange swirl underneath. Usage: python -I make_logo.py <font.ttf> <out-dir>"""
import math
import sys
from pathlib import Path

from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

FONT, OUT = Path(sys.argv[1]), Path(sys.argv[2])
OUT.mkdir(parents=True, exist_ok=True)

INK, INK_DARK = "#123B37", "#F2FBF9"
TAGLINE, TAGLINE_DARK = "#1F5E57", "#A9DCD3"
SWIRL = "#F57A2A"   # glue-stick orange


def instance(weight):
    font = TTFont(FONT)
    return instantiateVariableFont(font, {"wght": weight, "wdth": 100}, inplace=False)


def text_paths(font, text, size, x0=0.0, baseline=0.0, tracking=0.0):
    """Returns (svg path data, bounds) for text at `size` px, using advance widths + kerning pairs."""
    glyphs = font.getGlyphSet()
    cmap = font.getBestCmap()
    scale = size / font["head"].unitsPerEm
    kern = kerning_pairs(font)
    pen = SVGPathPen(glyphs)
    bounds = BoundsPen(glyphs)
    x = x0
    names = [cmap[ord(ch)] for ch in text]
    for i, name in enumerate(names):
        # Font units are y-up; SVG is y-down.
        t = (scale, 0, 0, -scale, x, baseline)
        glyphs[name].draw(TransformPen(pen, t))
        glyphs[name].draw(TransformPen(bounds, t))
        x += glyphs[name].width * scale + tracking
        if i + 1 < len(names):
            x += kern.get((name, names[i + 1]), 0) * scale
    return pen.getCommands(), bounds.bounds, x


def kerning_pairs(font):
    """Simple pair kerning from GPOS PairPos format 1 (enough for one short word)."""
    pairs = {}
    if "GPOS" not in font:
        return pairs
    for lookup in font["GPOS"].table.LookupList.Lookup:
        for sub in lookup.SubTable:
            sub = getattr(sub, "ExtSubTable", sub)
            if getattr(sub, "LookupType", None) != 2 or sub.Format != 1:
                continue
            firsts = sub.Coverage.glyphs
            for first, pset in zip(firsts, sub.PairSet):
                for rec in pset.PairValueRecord:
                    if rec.Value1 and getattr(rec.Value1, "XAdvance", 0):
                        pairs.setdefault((first, rec.SecondGlyph), rec.Value1.XAdvance)
    return pairs


def _x_of(font, text, char, size, x0):
    """Pen x position where `char` starts in `text` (same spacing as text_paths)."""
    glyphs = font.getGlyphSet()
    cmap = font.getBestCmap()
    scale = size / font["head"].unitsPerEm
    kern = kerning_pairs(font)
    names = [cmap[ord(c)] for c in text]
    x = x0
    for i, name in enumerate(names):
        if text[i] == char:
            return x
        x += glyphs[name].width * scale - 2
        if i + 1 < len(names):
            x += kern.get((name, names[i + 1]), 0) * scale
    raise ValueError(char)


def glyph_bounds(font, char, size, x, baseline):
    glyphs = font.getGlyphSet()
    name = font.getBestCmap()[ord(char)]
    scale = size / font["head"].unitsPerEm
    b = BoundsPen(glyphs)
    glyphs[name].draw(TransformPen(b, (scale, 0, 0, -scale, x, baseline)))
    return b.bounds


def cap(cx, bottom, width, angle):
    """Pasty's glue-stick cap: softly domed, teal gradient, a rim and a highlight; rotated around its bottom centre.
    Same proportions as the cap in GlueStickBody.axaml (84 x 42 units)."""
    k = width / 84
    x, y = cx - width / 2, bottom - 42 * k

    def pt(a, b):
        return f"{x + a * k:.1f},{y + b * k:.1f}"

    dome = (f"M {pt(0, 38)} L {pt(0, 18)} Q {pt(0, 0)} {pt(22, 0)} L {pt(62, 0)} Q {pt(84, 0)} {pt(84, 18)} "
            f"L {pt(84, 38)} Q {pt(84, 42)} {pt(80, 42)} L {pt(4, 42)} Q {pt(0, 42)} {pt(0, 38)} Z")
    return (f'<g transform="rotate({angle} {cx:.1f} {bottom:.1f})">'
            f'<path d="{dome}" fill="url(#cap)" stroke="#0B6B62" stroke-width="{width * 0.035:.1f}" />'
            f'<rect x="{x + 1 * k:.1f}" y="{y + 33 * k:.1f}" width="{82 * k:.1f}" height="{4 * k:.1f}" fill="#0B6B62" opacity="0.45" />'
            f'<path d="M {pt(12, 10)} Q {pt(18, 5)} {pt(32, 5)}" fill="none" stroke="#9AF0E5" '
            f'stroke-width="{3 * k:.1f}" stroke-linecap="round" opacity="0.7" />'
            f'</g>')


def squiggle(x1, x2, y, amplitude, stroke):
    """The swirl from Pasty's label, as an even wave from x1 to x2."""
    waves = 4
    w = (x2 - x1) / waves
    d = f"M {x1:.1f},{y:.1f} " + " ".join(
        f"q {w / 4:.1f},{-amplitude:.1f} {w / 2:.1f},0 t {w / 2:.1f},0" for _ in range(waves))
    return (f'<path d="{d}" fill="none" stroke="{SWIRL}" stroke-width="{stroke:.1f}" '
            f'stroke-linecap="round" stroke-linejoin="round" />')


def build(dark=False, tagline=False):
    bold = instance(650)
    size = 200
    baseline = 260
    x0 = 30
    word, wb, _ = text_paths(bold, "Pasty", size, x0, baseline, tracking=-2)
    left, top, right, bottom = wb

    # Cap sits on the P's top, slightly right of the stem, tipped back like a hat.
    p = glyph_bounds(bold, "P", size, x0, baseline)
    cap_w = (p[2] - p[0]) * 0.72
    # Centred on the P, sunk a touch into its top so it clearly sits there, tipped back like a hat.
    cap_svg = cap(p[0] + (p[2] - p[0]) * 0.5, p[1] + size * 0.05, cap_w, -9)

    # Squiggle under the x-height letters, clear of the y's descender.
    sq_y = baseline + size * 0.17
    # Ends just before the y so its tail doesn't cross the wave.
    y_letter = glyph_bounds(bold, "y", size, _x_of(bold, "Pasty", "y", size, x0), baseline)
    sq = squiggle(left + size * 0.06, y_letter[0] - size * 0.06, sq_y, size * 0.09, size * 0.075)

    height = sq_y + size * 0.16
    extra = ""
    if tagline:
        light = instance(500)
        tsize = size * 0.235
        _, tb, tend = text_paths(light, "the Desktop Companion", tsize, 0, 0, tracking=tsize * 0.02)
        tw = tend - 0
        tx = left + (right - left - tw) / 2
        ty = sq_y + size * 0.34
        tag, _, _ = text_paths(light, "the Desktop Companion", tsize, tx, ty, tracking=tsize * 0.02)
        extra = f'<path d="{tag}" fill="{TAGLINE_DARK if dark else TAGLINE}" />'
        height = ty + tsize * 0.35

    cap_top = p[1] - cap_w * 0.95
    min_y = min(top, cap_top) - 12
    width = right + x0
    svg = f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 {min_y:.0f} {width:.0f} {height - min_y:.0f}" role="img" aria-label="Pasty the Desktop Companion">
  <title>Pasty the Desktop Companion</title>
  <defs>
    <linearGradient id="cap" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="#2CC5B5" /><stop offset="0.5" stop-color="#14A99A" /><stop offset="1" stop-color="#0E857A" />
    </linearGradient>
  </defs>
  {sq}
  <path d="{word}" fill="{INK_DARK if dark else INK}" />
  {cap_svg}
  {extra}
</svg>
'''
    return svg


variants = {
    "pasty-logo.svg": build(),
    "pasty-logo-dark.svg": build(dark=True),
    "pasty-logo-tagline.svg": build(tagline=True),
    "pasty-logo-tagline-dark.svg": build(dark=True, tagline=True),
}
for name, svg in variants.items():
    (OUT / name).write_text(svg, encoding="utf-8")
    print(name, len(svg), "bytes")
