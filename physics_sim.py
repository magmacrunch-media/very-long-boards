#!/usr/bin/env python3
"""
Ride Physics Simulator for Very Long Boards

Sibling of garage_layout.py: check the numbers here before they reach C#.
Mirrors TerrainManager.HillAt() and the speed integration in
PlayerManager.Update(). Tune in this file, eyeball the report, then port the
constants across — the CONSTANTS block below is mirrored in both files.

    python3 physics_sim.py

Everything is SI: 1 world unit = 1 metre, Speed is metres/second.
"""

import math

# ═══════════════════════════════════════════
#  CONSTANTS (mirrored in TerrainManager.cs / PlayerManager.cs)
# ═══════════════════════════════════════════

# -- TerrainManager.HillAt --------------------------------------------------
# (amplitude m, angular frequency rad/m). Steepness is amplitude x frequency,
# so the short-wavelength terms are what make the course feel steep; the long
# ones only make it tall. Keep total relief inside what a rider can climb.
HILL_TERMS = [
    (5.5, 0.005),   # landscape roll,  wavelength 1257 m, slope 2.8%
    (4.0, 0.016),   # long hills,      wavelength  393 m, slope 6.4%
    (1.8, 0.042),   # rollers,         wavelength  150 m, slope 7.6%
    (0.9, 0.095),   # sharp pitches,   wavelength   66 m, slope 8.6%
]
GRADE = 0.08        # net downhill, 8%
FLAT_START = 20.0   # metres of flat before the hills begin

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

COURSE_LENGTH = 2000.0
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


def hill_at(z):
    """Mirrors TerrainManager.HillAt()."""
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
    print("course : %.0f m | %d hill terms | max local slope %.0f%% | relief %.0f m"
          % (COURSE_LENGTH, len(HILL_TERMS),
             100 * (sum(a * f for a, f in HILL_TERMS) + GRADE),
             2 * sum(a for a, _ in HILL_TERMS)))
    climbable = BASE_MAX_SPEED ** 2 / (2 * GRAVITY)
    print("         a rider at top speed can climb %.0f m — relief must stay under that"
          % climbable)
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
