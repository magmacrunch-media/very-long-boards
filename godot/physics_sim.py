#!/usr/bin/env python3
"""
Ride Physics Simulator for Very Long Boards

The terrain half is no longer mirrored by hand: it is read straight out of
Resources/Design/Frogwood.tres, the same CourseDesign the game loads. Reshape
the hills in the Godot Inspector (or in Scenes/CoursePreview.tscn), save, and
run this to find out whether the ride survives them.

The rider half still mirrors PlayerManager.Update() by hand — tune the
PlayerManager block below, eyeball the report, then port it across.

    python3 physics_sim.py

Everything is SI: 1 world unit = 1 metre, Speed is metres/second.
"""

import io
import math
import os
import re

# ═══════════════════════════════════════════
#  CONSTANTS (mirrored in TerrainManager.cs / PlayerManager.cs)
# ═══════════════════════════════════════════

# -- CourseDesign (read from the .tres, not mirrored) -----------------------
# Godot only writes properties that differ from the C# defaults, so a freshly
# created resource is nearly empty. These are those defaults, copied from
# Scripts/Design/CourseDesign.cs — anything the .tres overrides wins.
#
# Layers are (amplitude m, wavelength m). Steepness is amplitude x frequency,
# so the short-wavelength terms are what make the course feel steep; the long
# ones only make it tall. Keep total relief inside what a rider can climb.
COURSE_TRES = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                           "Resources", "Design", "Frogwood.tres")

DEFAULT_HILL_LAYERS = [
    (5.5, 1256.6371),   # landscape roll
    (4.0, 392.6991),    # long hills
    (1.8, 149.5997),    # rollers
    (0.9, 66.1388),     # sharp pitches
]
DEFAULT_GRADE = 0.08        # net downhill, 8%
DEFAULT_FLAT_START = 20.0   # metres of flat before the hills begin
DEFAULT_LENGTH = 2000.0     # metres from start line to finish banner

# -- PlayerManager ----------------------------------------------------------
GRAVITY = 9.81            # m/s^2. Speed += -slope * GRAVITY * dt  is a = g*sin(theta)
BASE_DRAG = 0.0032        # quadratic air drag; this sets terminal velocity
BASE_MAX_SPEED = 21.0     # m/s (76 km/h) for a 3-pip rider — normaliser + safety cap
SPEED_FLOOR = 2.5         # m/s; bog down on the worst climbs, never dead stop
BASE_WOBBLE_ONSET = 15.8  # m/s (57 km/h) for a 3-pip rider
BASE_WOBBLE_BUILD = 0.35  # wobble units per second once past onset

PUSH_STROKE = 0.45        # s, one kick cycle — also the cooldown
PUSH_TOP_SPEED = 8.0      # m/s (29 km/h); past this your leg can't keep up
PUSH_BOOST = 2.5          # m/s a push adds from a standstill

DT = 1.0 / 60.0           # Godot's fixed physics tick

# Carl's pips: (SPD, HAND, TRK) — mirrors Main.CarlStats
CARLS = [
    ("Office Carl", 4, 4, 4),
    ("Party  Carl", 5, 3, 3),
    ("Dark   Carl", 4, 4, 5),
]


def pip(pips, at_one, at_five):
    """Map a 1-5 stat pip onto a multiplier range. Mirrors PlayerManager.Pip()."""
    return at_one + (at_five - at_one) * (max(1, min(5, pips)) - 1) / 4.0


def read_course(path=COURSE_TRES):
    """
    Pull the hill shape out of a CourseDesign .tres.

    Godot writes scripted resources as plain text: exported SineLayers become
    [sub_resource] blocks and the array on [resource] refers to them by id.
    Anything the file does not override keeps the C# default, which is exactly
    how Godot itself loads it.
    """
    layers = list(DEFAULT_HILL_LAYERS)
    grade, flat_start, length = DEFAULT_GRADE, DEFAULT_FLAT_START, DEFAULT_LENGTH
    source = "defaults (no overrides in %s)" % os.path.basename(path)

    if not os.path.exists(path):
        return layers, grade, flat_start, length, "defaults (%s missing)" % path

    text = io.open(path, encoding="utf-8").read()
    overrides = []

    # A .tres is a sequence of [header] blocks, so split on the headers rather
    # than trying to match across them. Numeric assignments are all we need:
    # SineLayer has two floats, and everything else we read is a scalar.
    number = re.compile(r'^(\w+)\s*=\s*([-\d.eE+]+)\s*$', re.M)
    subs, resource = {}, ""
    for block in re.split(r'^\[', text, flags=re.M)[1:]:
        header, _, body = block.partition("]")
        if header.startswith("sub_resource"):
            found = re.search(r'id="([^"]+)"', header)
            if found:
                subs[found.group(1)] = dict(number.findall(body))
        elif header.strip() == "resource":
            resource = body

    array = re.search(r'^HillLayers\s*=\s*.*?\((.*?)\)\s*$', resource, re.S | re.M)
    if array:
        ids = re.findall(r'SubResource\("([^"]+)"\)', array.group(1))
        parsed = []
        for sub_id in ids:
            fields = subs.get(sub_id, {})
            amp = float(fields.get("Amplitude", 1.0))
            wave = float(fields.get("WavelengthMetres", 500.0))
            parsed.append((amp, wave))
        if parsed:
            layers = parsed
            overrides.append("HillLayers")

    for name, key in (("Grade", "grade"), ("HillFlatStart", "flat"), ("Length", "length")):
        found = re.search(r'^%s\s*=\s*([-\d.eE+]+)\s*$' % name, resource, re.M)
        if not found:
            continue
        value = float(found.group(1))
        overrides.append(name)
        if key == "grade":
            grade = value
        elif key == "flat":
            flat_start = value
        else:
            length = value

    if overrides:
        source = "%s (overrides: %s)" % (os.path.basename(path), ", ".join(overrides))
    return layers, grade, flat_start, length, source


HILL_LAYERS, GRADE, FLAT_START, COURSE_LENGTH, COURSE_SOURCE = read_course()

# (amplitude, angular frequency) — what hill_at actually integrates.
HILL_TERMS = [(amp, 0.0 if wave <= 0 else 2.0 * math.pi / wave)
              for amp, wave in HILL_LAYERS]


def hill_at(z):
    """Mirrors CourseDesign.HillAt()."""
    if z < FLAT_START:
        return 0.0
    a = z - FLAT_START
    return sum(amp * math.sin(a * f) for amp, f in HILL_TERMS) - a * GRADE


def slope_at(d):
    """Mirrors the 3 m forward difference PlayerManager uses."""
    return (hill_at(d + 3.0) - hill_at(d)) / 3.0


def ride(spd, trk):
    """Simulate one full course. Returns per-tick speed history."""
    max_speed = BASE_MAX_SPEED * pip(spd, 0.88, 1.12)
    drag = BASE_DRAG * pip(spd, 1.18, 0.82)        # more SPD = less drag = faster
    onset = BASE_WOBBLE_ONSET * pip(trk, 0.85, 1.15)

    v, d, t = SPEED_FLOOR, 0.0, 0.0
    hist = []
    while d < COURSE_LENGTH and t < 900:
        v += (-slope_at(d) * GRAVITY - drag * v * v) * DT
        v = max(SPEED_FLOOR, min(v, max_speed))
        d += v * DT
        t += DT
        hist.append(v)
    return hist, t, max_speed, onset


def push_report():
    """Kick from the speed floor and see how the cadence builds."""
    print("push-off: boost = %.1f * (1 - Speed/%.1f) m/s, %.2f s stroke, no speed gate"
          % (PUSH_BOOST, PUSH_TOP_SPEED, PUSH_STROKE))
    v, t = SPEED_FLOOR, 0.0
    for i in range(1, 12):
        bite = max(0.0, 1.0 - v / PUSH_TOP_SPEED)
        if bite <= 0.02:
            print("  push %d: bite %.3f — too fast to get a foot down, pushing stops helping"
                  % (i, bite))
            break
        v += PUSH_BOOST * bite
        print("  push %2d: +%.2f -> %5.2f m/s (%4.1f km/h)  at t = %.2f s"
              % (i, PUSH_BOOST * bite, v, v * 3.6, t))
        for _ in range(int(PUSH_STROKE * 60)):      # coast through the cooldown
            v -= BASE_DRAG * v * v * DT
        t += PUSH_STROKE
    print("  => %.0f -> %.0f km/h in %.1f s of pushing, then gravity takes over"
          % (SPEED_FLOOR * 3.6, v * 3.6, t))
    print()


def report():
    print("source : %s" % COURSE_SOURCE)
    print("course : %.0f m | %d hill terms | max local slope %.0f%%"
          % (COURSE_LENGTH, len(HILL_TERMS),
             100 * (sum(a * f for a, f in HILL_TERMS) + GRADE)))

    # The climb the rider actually makes, walked rather than bounded.
    #
    # This used to print 2 * sum(amplitudes), which ignores the grade - and the grade is the
    # largest term in the height. It reported 24 m against a 22 m budget and had done for as
    # long as anyone had looked. The sines do rise 21 m across one long roll, but the road
    # descends at 8% underneath them the whole way and what is left is 5 m.
    worst, trough = 0.0, hill_at(0.0)
    z = 0.0
    while z <= COURSE_LENGTH:
        h = hill_at(z)
        if h < trough:
            trough = h
        if h - trough > worst:
            worst = h - trough
        z += 0.25
    print("         worst climb %.1f m, and the tightest rider can carry %.0f m"
          % (worst, (BASE_MAX_SPEED * pip(min(c[1] for c in CARLS), 0.88, 1.12)) ** 2
             / (2 * GRAVITY)))
    print()
    hdr = ("rider", "run s", "avg", "peak", "min", "floor%", "wobble%", "sf avg")
    print("%-13s %6s %7s %7s %7s %7s %8s %7s" % hdr)
    for name, spd, _hand, trk in CARLS:
        hist, t, max_speed, onset = ride(spd, trk)
        n = len(hist)
        avg = sum(hist) / n
        floor = 100.0 * sum(1 for x in hist if x <= SPEED_FLOOR * 1.02) / n
        wob = 100.0 * sum(1 for x in hist if x >= onset) / n
        print("%-13s %6.0f %7.0f %7.0f %7.0f %7.1f %8.1f %7.3f"
              % (name, t, avg * 3.6, max(hist) * 3.6, min(hist) * 3.6,
                 floor, wob, avg / max_speed))
    print()
    print("km/h columns. targets: run 120-170 s | avg 40-55 | peak 70-85 | floor <15% |")
    print("wobble 25-35% for Office | finish times must differ between riders")


if __name__ == "__main__":
    push_report()
    report()
