#!/usr/bin/env python3
"""Build the Block Island course from the real island.

Reads the LiDAR heightfield and the TIGER/Line road centrelines out of the
block-island-simulator repo, walks Spring Street from the Southeast Light end
down toward Old Harbor, and writes Resources/Design/BlockIsland.tres.

Nothing in the game loads the simulator. This script is the only thing that ever
needs it, and the profile it writes is baked into the resource -- the same deal
the website has with crunch-c and the MagmaScript playground. The simulator is
36 MB of terrain tiles and has no business being a dependency of a skateboarding
game.

    python3 build_block_island.py            # writes the resource
    python3 build_block_island.py --report   # prints the numbers and writes nothing

WHY SPRING STREET. Every road on the island was measured before this one was
picked. It has the longest sustained descent there is: 39.9 m over 1595 m. The
island's best grade anywhere is 2.5% and its highest point is 63.7 m, against
the 8% this game's physics is built around -- so the route was never going to be
found by looking for something steep. It was found by looking for the least
gentle thing available and then saying so.

THE ROAD IS IN THREE PARTS and only the middle one is a course. The first 965 m
from the light is clifftop plateau at 40-45 m, real and scenic and flat. Then it
tips over and drops 38 m in 1130 m. Then it climbs back to 22 m before falling
into Old Harbor -- and that climb is 22 m measured, which is already the whole
momentum budget (v^2/2g at top speed) before any exaggeration. So the course
starts a little way back along the plateau, for a run-up and a view, and stops
at the bottom of the descent rather than carrying on into town.
"""
import argparse
import json
import math
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))

# Where the simulator can be. Beside this repo is the documented layout and what
# a fresh clone gets; the wider tree groups repos by kind and puts both under
# games/ -- which lands them in the same relative place, so one candidate covers
# both. BLOCK_ISLAND_SRC covers anywhere else, the way the Wii Makefile's
# MAGNOLIA= override does for the engine.
CANDIDATES = [
    os.environ.get("BLOCK_ISLAND_SRC"),
    os.path.normpath(os.path.join(HERE, "..", "..", "block-island-simulator")),
]

# ── The route ────────────────────────────────────
ROAD = "Spring St"
START_M = 800.0      # a little back along the clifftop, for a run-up
END_M = 2095.0       # the bottom of the descent, before the climb into town
SPACING = 10.0       # metres between samples in the baked profile

# Dead-flat road before the measured profile starts, in metres.
#
# The sine courses had this as CourseDesign.HillFlatStart and it is where the
# whole push-off exists: somewhere level to get a foot down before gravity takes
# over. A MeasuredCourse ignores HillFlatStart -- its HillAt reads the samples
# and nothing else -- so when Frogwood became measured the apron silently went
# with it, the course began on a 2.4% grade at metre zero, and a rider was past
# PushTopSpeed before a second kick could land. Kicking off did nothing you could
# feel, which is exactly what it looked like.
#
# It comes out of the measured span rather than being added to it, so the course
# stays the length it has always been.
FLAT_START = 20.0


VERTICAL_EXAGGERATION = 2.0

# Heading is detrended against this window before being scaled -- see the note
# on MeasuredCourse.CurveScale for why absolute bearing is not observable here.
HEADING_BASELINE_M = 300.0
HEADING_PEAK_RAD = 0.40   # what the projection can draw; Frogwood peaks near 0.55


def find_source():
    for c in CANDIDATES:
        if c and os.path.isfile(os.path.join(c, "Assets", "Terrain", "terrain_manifest.json")):
            return c
    print("Could not find block-island-simulator. Looked in:", file=sys.stderr)
    for c in CANDIDATES:
        if c:
            print("  " + c, file=sys.stderr)
    print("Set BLOCK_ISLAND_SRC=<path to it> to look elsewhere.", file=sys.stderr)
    sys.exit(1)


class Heightfield:
    """The simulator's tiled float32 heightfield, sampled bilinearly."""

    def __init__(self, root):
        self.dir = os.path.join(root, "Assets", "Terrain")
        self.man = json.load(open(os.path.join(self.dir, "terrain_manifest.json")))
        self.n = self.man["samples_per_side"]
        self.tile = self.man["tile_size_m"]
        self.post = self.man["post_spacing_m"]
        self.files = {(t["tx"], t["ty"]): t["file"] for t in self.man["tiles"]}
        self.cache = {}

    def _tile(self, tx, ty):
        key = (tx, ty)
        if key not in self.cache:
            f = self.files.get(key)
            if f is None:
                self.cache[key] = None
            else:
                path = os.path.join(self.dir, *f.split("/"))
                raw = open(path, "rb").read()
                self.cache[key] = struct.unpack("<%df" % (self.n * self.n), raw)
        return self.cache[key]

    def at(self, x, z):
        n, tile, post = self.n, self.tile, self.post
        tx, ty = int(x // tile), int(z // tile)
        fx, fz = (x - tx * tile) / post, (z - ty * tile) / post

        def post_at(px, pz):
            a, b = tx, ty
            if px > n - 1:
                px -= n - 1
                a += 1
            if pz > n - 1:
                pz -= n - 1
                b += 1
            d = self._tile(a, b)
            return None if d is None else d[pz * n + px]

        x0, z0 = int(math.floor(fx)), int(math.floor(fz))
        sx, sz = fx - x0, fz - z0
        q = [post_at(x0, z0), post_at(x0 + 1, z0), post_at(x0, z0 + 1), post_at(x0 + 1, z0 + 1)]
        if None in q:
            return None
        return (q[0] * (1 - sx) + q[1] * sx) * (1 - sz) + (q[2] * (1 - sx) + q[3] * sx) * sz


def southeast_light_xz(root, man):
    """The light's position, in the same local metres the roads use."""
    from pyproj import Transformer
    s = json.load(open(os.path.join(root, "Assets", "Structures", "structures.json")))
    light = [x for x in s["structures"] if x["name"].startswith("Southeast")][0]
    e, n = Transformer.from_crs("EPSG:4326", man["crs"], always_xy=True).transform(*light["lonlat"])
    o = man["origin_utm"]
    return e - o["easting"], o["northing"] - n


def walk(points, hf, step=5.0):
    """Sample the polyline at a fixed step: (along, x, z, height)."""
    out, along = [], 0.0
    for i in range(len(points) - 1):
        (ax, az), (bx, bz) = points[i], points[i + 1]
        seg = math.hypot(bx - ax, bz - az)
        if seg <= 1e-9:
            continue
        steps = max(1, int(seg // step))
        for s in range(steps):
            f = s / steps
            x, z = ax + (bx - ax) * f, az + (bz - az) * f
            h = hf.at(x, z)
            if h is not None:
                out.append((along + seg * f, x, z, h))
        along += seg
    return out


def smooth_angles(angles, half):
    """Circular moving average.

    Averaging bearings as plain numbers is wrong the moment they straddle the
    +/-pi branch cut: a road heading just east of due south averages with one
    just west of it to give due NORTH. That produced a 169-degree phantom bend
    on a road that has nothing of the sort. Averaging the unit vectors and
    taking the angle back off the result has no branch to cross.
    """
    out = []
    for i in range(len(angles)):
        lo, hi = max(0, i - half), min(len(angles), i + half + 1)
        window = angles[lo:hi]
        sx = sum(math.sin(a) for a in window)
        cx = sum(math.cos(a) for a in window)
        out.append(math.atan2(sx, cx))
    return out


def build():
    root = find_source()
    hf = Heightfield(root)
    man = hf.man

    roads = [r for r in man["roads"] if r["name"] == ROAD]
    if not roads:
        sys.exit("No road named %r in the manifest." % ROAD)
    pts = roads[0]["points"]

    light = southeast_light_xz(root, man)
    # Orient the polyline so sample zero is the end nearest the light.
    if (math.hypot(pts[0][0] - light[0], pts[0][1] - light[1])
            > math.hypot(pts[-1][0] - light[0], pts[-1][1] - light[1])):
        pts = pts[::-1]

    track = walk(pts, hf)

    def sample(along):
        for i in range(len(track) - 1):
            if track[i][0] <= along <= track[i + 1][0]:
                a, b = track[i], track[i + 1]
                span = b[0] - a[0]
                f = 0.0 if span <= 0 else (along - a[0]) / span
                return tuple(a[j] + (b[j] - a[j]) * f for j in range(1, 4))
        return track[-1][1:4]

    apron = int(FLAT_START // SPACING)
    count = int((END_M - START_M - FLAT_START) // SPACING) + 1
    raw = [sample(START_M + i * SPACING) for i in range(count)]
    base_h = raw[0][2]

    heights = [0.0] * apron + [(p[2] - base_h) * VERTICAL_EXAGGERATION for p in raw]

    # Heading, as a bearing, then detrended and scaled.
    bearings = []
    for i in range(len(raw) - 1):
        bearings.append(math.atan2(raw[i + 1][0] - raw[i][0], raw[i + 1][1] - raw[i][1]))
    bearings.append(bearings[-1])
    bearings = smooth_angles(bearings, 3)

    half = int(HEADING_BASELINE_M / SPACING)
    baseline = smooth_angles(bearings, half)
    residual = []
    for i in range(len(bearings)):
        d = bearings[i] - baseline[i]
        while d > math.pi:
            d -= 2 * math.pi
        while d < -math.pi:
            d += 2 * math.pi
        residual.append(d)

    peak = max(abs(v) for v in residual) or 1.0
    scale = HEADING_PEAK_RAD / peak
    # The apron is straight as well as level - it is a start line, not a corner.
    headings = [0.0] * apron + [v * scale for v in residual]

    # Which side the Atlantic is on, asked of the terrain rather than assumed. Sample well
    # out to each side along the middle of the course: the seaward side runs off the island
    # into water or off the tiled domain entirely, the landward side does not.
    sea_votes = 0
    for i in range(len(raw) // 4, 3 * len(raw) // 4):
        bx, bz = bearings_for_side(raw, i)
        for probe in (250.0, 400.0):
            # x runs east and z runs SOUTH from the north-west origin, so the rider's
            # right is (-dz, +dx), not (+dz, -dx). Getting this backwards put the
            # Atlantic inland, which the terrain then cheerfully confirmed.
            left = hf.at(raw[i][0] + bz * probe, raw[i][1] - bx * probe)
            right = hf.at(raw[i][0] - bz * probe, raw[i][1] + bx * probe)
            lo = 1e9 if left is None else left
            ro = 1e9 if right is None else right
            if ro < lo:
                sea_votes += 1
            elif lo < ro:
                sea_votes -= 1

    # +X is to the rider's LEFT here: PlayerManager adds to PosX on move_left. So the sea
    # being on the rider's right is SeaSide -1.
    sea_side = -1 if sea_votes > 0 else 1

    # How far the water actually is. Probe seaward from each sample until the ground drops
    # to about sea level or runs off the tiled domain, and take the median -- the road does
    # not run along the cliff edge, and inventing a distance would put it there.
    shore = []
    for i in range(len(raw)):
        bx, bz = bearings_for_side(raw, i)
        px, pz = (-bz, bx) if sea_side < 0 else (bz, -bx)
        for d in range(10, 800, 10):
            h = hf.at(raw[i][0] + px * d, raw[i][1] + pz * d)
            if h is None or h < 3.0:
                shore.append(float(d))
                break
    # Clamped only at the far end: past a few hundred metres the water is over the horizon
    # anyway, and a 770 m strip of ground is geometry nobody sees.
    shore_series = [shore[0] if shore else 320.0] * apron + [min(s, 320.0) for s in shore]
    while len(shore_series) < count + apron:
        shore_series.append(shore_series[-1] if shore_series else 320.0)
    shore = sorted(shore)
    shore_distance = shore[len(shore) // 2] if shore else 60.0

    length = (count - 1) * SPACING + FLAT_START
    drop = heights[0] - heights[-1]
    return {
        "shore_distance": shore_distance,
        "shore_all": shore,
        "shore_series": shore_series,
        "sea_side": sea_side,
        "sea_level": (0.0 - base_h) * VERTICAL_EXAGGERATION,
        "root": root, "length": length, "heights": heights, "headings": headings,
        "drop": drop, "measured_drop": drop / VERTICAL_EXAGGERATION,
        "curve_scale": scale, "raw_peak": peak,
        "light_ground": hf.at(*light),
        "start_ground": base_h, "end_ground": raw[-1][2],
    }


def bearings_for_side(raw, i):
    """Unit vector along the road at sample i, for probing left and right of it."""
    j = min(i + 1, len(raw) - 1)
    dx, dz = raw[j][0] - raw[i][0], raw[j][1] - raw[i][1]
    n = math.hypot(dx, dz) or 1.0
    return dx / n, dz / n


def relief(heights):
    worst, trough = 0.0, heights[0]
    for h in heights:
        trough = min(trough, h)
        worst = max(worst, h - trough)
    return worst


def terminal_kmh(grade):
    """What the ride settles at on a constant grade: gravity against quadratic drag."""
    return math.sqrt(max(grade, 0.0) * 9.81 / 0.0032) * 3.6


def report(c):
    h = c["heights"]
    print("source            %s" % c["root"])
    print("road              %s, %.0f m to %.0f m along from the Southeast Light end"
          % (ROAD, START_M, END_M))
    print("light stands on   %.1f m" % c["light_ground"])
    print("course            %.0f m, from %.1f m down to %.1f m (measured)"
          % (c["length"], c["start_ground"], c["end_ground"]))
    print("drop              %.1f m measured, %.1f m at %.1fx"
          % (c["measured_drop"], c["drop"], VERTICAL_EXAGGERATION))
    print("average grade     %.2f%%  -> %.0f km/h" %
          (c["drop"] / c["length"] * 100, terminal_kmh(c["drop"] / c["length"])))
    print()
    for w in (200, 400, 600, 1000):
        n = int(w / SPACING)
        best = max((h[i] - h[i + n] for i in range(len(h) - n)), default=0)
        print("  steepest %4d m  %5.2f%%  -> %.0f km/h" % (w, best / w * 100, terminal_kmh(best / w)))
    print()
    print("worst climb       %.1f m   (budget is v^2/2g, about 22 m at top speed)" % relief(h))
    print("wobble onset      15.8 m/s = 57 km/h")
    print()
    print("heading           real peak %.2f rad (%.0f deg), scaled by %.3f to %.2f rad"
          % (c["raw_peak"], math.degrees(c["raw_peak"]), c["curve_scale"], HEADING_PEAK_RAD))
    print("sea               on the rider's %s, %.1f m below the start line"
          % ("right" if c["sea_side"] < 0 else "left", -c["sea_level"]))
    s = c["shore_all"]
    print("shore             %.0f m from the road (median, measured)" % c["shore_distance"])
    print("                  closest %.0f m, 25th pct %.0f m, farthest %.0f m over %d samples"
          % (s[0], s[len(s) // 4], s[-1], len(s)))


TEMPLATE = '''[gd_resource type="Resource" script_class="MeasuredCourse" load_steps=2 format=3]

[ext_resource type="Script" path="res://Scripts/Design/MeasuredCourse.cs" id="1_measured"]

[resource]
script = ExtResource("1_measured")
SampleSpacing = {spacing}
Heights = PackedFloat32Array({heights})
Headings = PackedFloat32Array({headings})
VerticalExaggeration = {exag}
CurveScale = {curve_scale}
SourceNote = "{note}"
Length = {length}
HillFlatStart = 0.0
CurveFlatStart = 0.0
Grade = 0.0
RoadWidth = 6.5
ShoulderWidth = 1.4
GroundWidth = 300.0
Density = 0.8
ScenerySeed = 1661
ClosePines = 10
FarPines = 16
Deciduous = 8
Rocks = 70
Stumps = 0
Wildflowers = 150
Ferns = 0
Bushes = 95
Logs = 0
RoadSigns = 6
Houses = 5
Streams = 0
StoneWalls = 48
MarkerSpacing = 250.0
GroundLo = Color({ground_lo})
GroundHi = Color({ground_hi})
FoliageLo = Color({foliage_lo})
FoliageHi = Color({foliage_hi})
FlowerColors = PackedColorArray({flowers})
SunEnergy = 1.95
SunColor = Color(1, 0.97, 0.9, 1)
AmbientEnergy = 0.72
AmbientColor = Color(0.78, 0.82, 0.86, 1)
FogColor = Color(0.76, 0.83, 0.88, 1)
FogDensity = 0.0075
SkyTop = Color(0.22, 0.45, 0.8, 1)
SkyHorizon = Color(0.62, 0.75, 0.88, 1)
SkyGroundHorizon = Color(0.46, 0.49, 0.29, 1)
SkyGroundBottom = Color(0.30, 0.34, 0.18, 1)
HasSea = true
SeaLevel = {sea_level}
SeaSide = {sea_side}
ShoreDistance = {shore_distance}
ShoreSamples = PackedFloat32Array({shore_samples})
SeaColor = Color(0.13, 0.31, 0.44, 1)
'''


def write(c):
    note = (
        "Spring Street, Block Island RI, from %.0f m to %.0f m along from the Southeast Light "
        "end. Heights: USGS 3DEP 1 m LiDAR (RI_Statewide_D22), 2 m posts, EPSG:26919. "
        "Centreline: US Census TIGER/Line 2024. Both public domain. Generated by "
        "build_block_island.py from the block-island-simulator repo -- do not hand-edit. "
        "Heights are exaggerated %.1fx and headings scaled %.3f; see MeasuredCourse."
        % (START_M, END_M, VERTICAL_EXAGGERATION, c["curve_scale"])
    )
    body = TEMPLATE.format(
        spacing=SPACING,
        heights=", ".join("%.3f" % v for v in c["heights"]),
        headings=", ".join("%.5f" % v for v in c["headings"]),
        exag=VERTICAL_EXAGGERATION,
        curve_scale=round(c["curve_scale"], 5),
        note=note,
        length=round(c["length"], 1),
        sea_level=round(c["sea_level"], 2),
        sea_side=c["sea_side"],
        shore_distance=round(c["shore_distance"], 1),
        shore_samples=", ".join("%.1f" % v for v in c["shore_series"]),
        # Summer on the moraine: olive and tawny where Frogwood is forest green, because the
        # island is open grassland and low scrub and almost no tall trees at all.
        ground_lo="0.33, 0.38, 0.19, 1",
        ground_hi="0.52, 0.54, 0.30, 1",
        # Bayberry and shadbush - grey-green, wind-shorn.
        foliage_lo="0.19, 0.30, 0.17, 1",
        foliage_hi="0.36, 0.46, 0.28, 1",
        # Beach rose, goldenrod, Queen Anne's lace.
        # PackedColorArray takes bare floats, four per colour - not nested Color()
        # constructors, which parse as "expected float" and fail the whole resource.
        flowers=("0.86, 0.42, 0.55, 1, 0.95, 0.83, 0.25, 1, "
                 "0.94, 0.95, 0.92, 1, 0.78, 0.55, 0.72, 1"),
    )
    out = os.path.join(HERE, "Resources", "Design", "BlockIsland.tres")
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
