#!/usr/bin/env python3
"""Build the Frogwood course from the roads it was always based on.

Frogwood is Windham, New Hampshire: the lanes around Foster's Pond, and
specifically Millstone Road and Crestwood Road, which meet end to end at a
crest. This walks that route over real elevation and writes
Resources/Design/Frogwood.tres.

    python3 build_frogwood.py            # writes the resource
    python3 build_frogwood.py --report   # prints the numbers and writes nothing

Unlike Block Island, there is no sibling repo holding the survey -- so this
fetches its own, into a gitignored cache beside the script, and reuses it after.
Two public-domain sources, the same two the block-island-simulator uses:

    elevation   USGS 3DEP, via the National Map's ImageServer, 7.4 m posts
    centreline  US Census TIGER/Line 2024, Rockingham County NH, FIPS 33015

THE ROUTE is Old Mill Rd -> Millstone Rd -> Crestwood Rd -> Kendall Pond Rd,
which join at gaps of a metre or less, cut at 2000 m to keep Frogwood the length
it has always been. Millstone and Crestwood are the middle 1550 m of it and the
reason it is this route rather than a better one -- the network nearby has a
steeper descent (Carr Hill to Kendall Pond, 1128 m at 6.0%), and it is not these
roads.

WHAT IS EMBELLISHED, and it is the whole of the fiction here.

The real road is not a descent. It rolls gently for 600 m, climbs to a crest of
93 m at the 1017 m mark -- the Millstone high point -- and falls away to 69 m,
finishing 0.5 m BELOW where it started. Ridden as measured it is a hill, and a
downhill game cannot use it.

So the profile is detrended against its own endpoints and a net grade is put
underneath. Every roll stays exactly where the road puts it, at its real size
times ROLL_GAIN; the descent is invented. That is the opposite of the usual
compromise: the shape is measured and the slope is fiction, rather than a
plausible shape on a real slope.

ROLL_GAIN exists because tilting flattens what it is tilting, and 1.2 is where
physics_sim.py put it rather than where this file wanted it. 1.5 was the guess:
it gives a worst climb of 11.7 m, comfortably inside the 25.3 m a rider can
carry, and every other number it produces is wrong -- 189 s against a 120-170 s
target, 38 km/h against 40-55, and a quarter of the run spent bogged down at
the speed floor against a limit of 15%. The climbs are inside the budget one at
a time and ruinous in a row.

    rolls   run    avg   floor  wobble
    x1.5    189 s  38     24.9%   28.0%   too slow, bogs down
    x1.2    168 s  43      9.9%   31.5%   every target met
    x1.0    150 s  48      0.0%   34.9%   also fine, less of the road left

1.2 keeps more of the real road than 1.0 does and still lands inside every
target, so it is the one that survives. Change it and re-run physics_sim.py;
that is what it is for.
"""
import argparse
import io
import json
import math
import os
import sys
import urllib.parse
import urllib.request
import zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
CACHE = os.path.join(HERE, ".survey-cache")

# ── Sources ──────────────────────────────────────
COUNTY_FIPS = "33015"          # Rockingham County, New Hampshire
TIGER_YEAR = "2024"
TIGER = (f"https://www2.census.gov/geo/tiger/TIGER{TIGER_YEAR}/ROADS/"
         f"tl_{TIGER_YEAR}_{COUNTY_FIPS}_roads.zip")
DEM_BBOX = "-71.38,42.75,-71.22,42.88"      # the Windham lanes, with room around them
DEM_SIZE = "2000,2000"                      # 7.4 m posts over that box
DEM = ("https://elevation.nationalmap.gov/arcgis/rest/services/3DEPElevation/"
       "ImageServer/exportImage?" + urllib.parse.urlencode({
           "bbox": DEM_BBOX, "bboxSR": 4326, "imageSR": 26919, "format": "tiff",
           "pixelType": "F32", "size": DEM_SIZE, "f": "image",
           "interpolation": "RSP_BilinearInterpolation"}))

# ── The route ────────────────────────────────────
ROADS = ("Old Mill Rd", "Millstone Rd", "Crestwood Rd", "Kendall Pond Rd")
# There are roads of these names elsewhere in the county; this box is Windham.
AREA = (42.79, 42.86, -71.37, -71.30)
LENGTH = 2000.0
SPACING = 10.0

GRADE = 0.08          # the invented descent, and Frogwood's own from the start
ROLL_GAIN = 1.2       # how much of the real rolls survives the tilt

HEADING_BASELINE_M = 300.0
HEADING_PEAK_RAD = 0.34    # what the ribbon can draw; the old sine Frogwood peaked here


def fetch(url, name):
    os.makedirs(CACHE, exist_ok=True)
    path = os.path.join(CACHE, name)
    if os.path.exists(path) and os.path.getsize(path) > 0:
        return path
    print("fetching %s ..." % name, file=sys.stderr)
    urllib.request.urlretrieve(url, path)
    print("  %.1f MB" % (os.path.getsize(path) / 1e6), file=sys.stderr)
    return path


def load():
    import rasterio
    import shapefile
    from pyproj import Transformer

    dem = rasterio.open(fetch(DEM, "windham_3dep.tif"))
    band = dem.read(1)
    to_utm = Transformer.from_crs("EPSG:4326", dem.crs, always_xy=True)

    def height(x, y):
        col, row = ~dem.transform * (x, y)
        r0, c0 = int(math.floor(row)), int(math.floor(col))
        if r0 < 0 or c0 < 0 or r0 >= band.shape[0] - 1 or c0 >= band.shape[1] - 1:
            return None
        fr, fc = row - r0, col - c0
        q = band[r0:r0 + 2, c0:c0 + 2]
        if q.min() < -1000:
            return None
        return float((q[0, 0] * (1 - fc) + q[0, 1] * fc) * (1 - fr) +
                     (q[1, 0] * (1 - fc) + q[1, 1] * fc) * fr)

    zf = zipfile.ZipFile(fetch(TIGER, "rockingham_roads.zip"))
    base = f"tl_{TIGER_YEAR}_{COUNTY_FIPS}_roads"
    sf = shapefile.Reader(shp=io.BytesIO(zf.read(base + ".shp")),
                          dbf=io.BytesIO(zf.read(base + ".dbf")),
                          shx=io.BytesIO(zf.read(base + ".shx")))

    picked = {}
    for rec, shp in zip(sf.records(), sf.shapes()):
        name = rec["FULLNAME"] or ""
        if name not in ROADS or not shp.points:
            continue
        lat = sum(p[1] for p in shp.points) / len(shp.points)
        lon = sum(p[0] for p in shp.points) / len(shp.points)
        if not (AREA[0] < lat < AREA[1] and AREA[2] < lon < AREA[3]):
            continue
        picked.setdefault(name, []).append([to_utm.transform(*p) for p in shp.points])

    missing = [r for r in ROADS if not picked.get(r)]
    if missing:
        sys.exit("Not found in TIGER %s for FIPS %s: %s"
                 % (TIGER_YEAR, COUNTY_FIPS, ", ".join(missing)))
    return height, picked


def chain(picked):
    """Join the named roads in order.

    A road is several TIGER segments and the one that connects is not always the
    longest, nor does it always meet end to end: Kendall Pond Rd runs 4.2 km past
    the end of Crestwood and joins it side-on. So this looks for the closest
    VERTEX across every segment of the next road, then leaves along whichever
    half of that segment is longer. Picking the longest segment and comparing
    only its endpoints put two of the three joins 175 m and 1.9 km out.
    """
    # The first road is split at its closest approach to the second, exactly like
    # every join below, and the longer half of it leads into that point.
    #
    # It cannot just be oriented: Old Mill Rd passes within a metre of Millstone
    # Rd's end through its INTERIOR, not an endpoint, so choosing between its two
    # ends leaves the course starting 175 m from the junction whichever one wins.
    best = None
    for seg in picked[ROADS[0]]:
        for i, v in enumerate(seg):
            for s2 in picked[ROADS[1]]:
                for w in s2:
                    d = math.dist(v, w)
                    if best is None or d < best[0]:
                        best = (d, seg, i)
    _, seg, i = best
    head, tail = seg[:i + 1], seg[i:][::-1]
    first = head if _span(head) >= _span(tail) else tail

    route, joins = list(first), []
    for name in ROADS[1:]:
        best = None
        for seg in picked[name]:
            for i, v in enumerate(seg):
                d = math.dist(route[-1], v)
                if best is None or d < best[0]:
                    best = (d, seg, i)
        gap, seg, i = best
        forward, backward = seg[i:], seg[:i + 1][::-1]
        run = forward if _span(forward) >= _span(backward) else backward
        joins.append((name, gap))
        route += run[1:] if gap < 5.0 else run
    return route, joins


def _span(pts):
    return sum(math.dist(pts[i], pts[i + 1]) for i in range(len(pts) - 1))


def walk(route, height):
    prof, dist = [], 0.0
    for i in range(len(route) - 1):
        a, b = route[i], route[i + 1]
        seg = math.dist(a, b)
        if seg < 1e-9:
            continue
        steps = max(1, int(seg // 5))
        for k in range(steps):
            f = k / steps
            h = height(a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f)
            if h is None:
                sys.exit("The route leaves the elevation box at %.0f m." % dist)
            prof.append((dist + seg * f, a[0] + (b[0] - a[0]) * f,
                         a[1] + (b[1] - a[1]) * f, h))
        dist += seg
    return prof


def smooth_angles(angles, half):
    """Circular moving average — see build_block_island.py for why not a plain one."""
    out = []
    for i in range(len(angles)):
        lo, hi = max(0, i - half), min(len(angles), i + half + 1)
        w = angles[lo:hi]
        out.append(math.atan2(sum(math.sin(a) for a in w), sum(math.cos(a) for a in w)))
    return out


def build():
    height, picked = load()
    route, joins = chain(picked)
    track = walk(route, height)

    def at(along):
        for i in range(len(track) - 1):
            if track[i][0] <= along <= track[i + 1][0]:
                a, b = track[i], track[i + 1]
                span = b[0] - a[0]
                f = 0.0 if span <= 0 else (along - a[0]) / span
                return tuple(a[j] + (b[j] - a[j]) * f for j in range(1, 4))
        return track[-1][1:4]

    count = int(LENGTH // SPACING) + 1
    raw = [at(i * SPACING) for i in range(count)]
    span = (count - 1) * SPACING

    # Detrend against the endpoints, keep the rolls, put a grade underneath.
    h0, h1 = raw[0][2], raw[-1][2]
    rolls = [p[2] - (h0 + (h1 - h0) * (i * SPACING) / span) for i, p in enumerate(raw)]
    heights = [rolls[i] * ROLL_GAIN - i * SPACING * GRADE for i in range(count)]

    bearings = [math.atan2(raw[i + 1][0] - raw[i][0], raw[i + 1][1] - raw[i][1])
                for i in range(count - 1)]
    bearings.append(bearings[-1])
    bearings = smooth_angles(bearings, 3)
    baseline = smooth_angles(bearings, int(HEADING_BASELINE_M / SPACING))
    residual = []
    for i in range(count):
        d = bearings[i] - baseline[i]
        while d > math.pi:
            d -= 2 * math.pi
        while d < -math.pi:
            d += 2 * math.pi
        residual.append(d)
    peak = max(abs(v) for v in residual) or 1.0
    scale = HEADING_PEAK_RAD / peak
    headings = [v * scale for v in residual]

    return {"heights": heights, "headings": headings, "rolls": rolls, "raw": raw,
            "length": span, "joins": joins, "curve_scale": scale, "raw_peak": peak,
            "real_lo": min(p[2] for p in raw), "real_hi": max(p[2] for p in raw)}


def worst_climb(vals):
    worst, trough = 0.0, vals[0]
    for v in vals:
        trough = min(trough, v)
        worst = max(worst, v - trough)
    return worst


def report(c):
    h = c["heights"]
    print("route             " + " -> ".join(ROADS))
    print("joins             " + ", ".join("%s at %.1f m" % (n, g) for n, g in c["joins"]))
    print("length            %.0f m" % c["length"])
    print()
    print("as measured       %.1f m to %.1f m, finishing %+.1f m against the start"
          % (c["real_lo"], c["real_hi"], c["raw"][-1][2] - c["raw"][0][2]))
    print("                  it is a hill, not a descent - crest %.1f m at %.0f m along"
          % (max(p[2] for p in c["raw"]),
             SPACING * max(range(len(c["raw"])), key=lambda i: c["raw"][i][2])))
    print("real rolls        %.1f m peak to trough, once its own slope is removed"
          % (max(c["rolls"]) - min(c["rolls"])))
    print()
    print("tilted            grade %.0f%%, rolls x%.1f" % (GRADE * 100, ROLL_GAIN))
    print("                  drop %.1f m, worst climb %.1f m (budget 25.3 m)"
          % (h[0] - h[-1], worst_climb(h)))
    for w in (200, 400, 600):
        n = int(w / SPACING)
        best = max((h[i] - h[i + n] for i in range(len(h) - n)), default=0)
        print("  steepest %3d m   %5.2f%%  -> %.0f km/h"
              % (w, best / w * 100, math.sqrt(max(best / w, 0) * 9.81 / 0.0032) * 3.6))
    print("wobble onset      57 km/h")
    print()
    print("heading           real peak %.2f rad (%.0f deg), scaled by %.3f to %.2f rad"
          % (c["raw_peak"], math.degrees(c["raw_peak"]), c["curve_scale"], HEADING_PEAK_RAD))


TEMPLATE = '''[gd_resource type="Resource" script_class="MeasuredCourse" load_steps=2 format=3]

[ext_resource type="Script" path="res://Scripts/Design/MeasuredCourse.cs" id="1_measured"]

[resource]
script = ExtResource("1_measured")
SampleSpacing = {spacing}
Heights = PackedFloat32Array({heights})
Headings = PackedFloat32Array({headings})
VerticalExaggeration = {roll_gain}
CurveScale = {curve_scale}
SourceNote = "{note}"
Length = {length}
HillFlatStart = 0.0
CurveFlatStart = 0.0
Grade = 0.0
'''


def write(c):
    note = (
        "Windham NH: %s, cut at %.0f m. Elevation USGS 3DEP (7.4 m posts, EPSG:26919); "
        "centreline US Census TIGER/Line %s, FIPS %s. Both public domain. The real road "
        "is a hill, not a descent - it is detrended and a %.0f%% grade put underneath, with "
        "the rolls kept at %.1fx. Generated by build_frogwood.py - do not hand-edit."
        % (" to ".join(ROADS), c["length"], TIGER_YEAR, COUNTY_FIPS, GRADE * 100, ROLL_GAIN)
    )
    body = TEMPLATE.format(
        spacing=SPACING,
        heights=", ".join("%.3f" % v for v in c["heights"]),
        headings=", ".join("%.5f" % v for v in c["headings"]),
        roll_gain=ROLL_GAIN,
        curve_scale=round(c["curve_scale"], 5),
        note=note,
        length=round(c["length"], 1),
    )
    out = os.path.join(HERE, "Resources", "Design", "Frogwood.tres")
    with open(out, "wb") as f:
        f.write(body.encode("utf-8"))
    print("wrote %s (%d samples)" % (os.path.relpath(out, HERE), len(c["heights"])))


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--report", action="store_true", help="print the numbers, write nothing")
    args = ap.parse_args()
    course = build()
    report(course)
    if not args.report:
        print()
        write(course)
