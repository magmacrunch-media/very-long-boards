# Very Long Boards — Godot version

A peaceful downhill skateboarding game set on forested New Hampshire roads. Ride as Carl
Spatski and make your way down sunny summer hills.

This is the desktop version. The browser version is a different game built on the same
characters and lives in [`../web/`](../web/) — see the [repository README](../README.md) for
what separates them.

## Carl Spatski

One rider, three looks — and the look you pick changes how he rides:

| | SPD | HAND | TRK | |
|---|---|---|---|---|
| **Office Carl** | 4 | 4 | 4 | The everyman. Red dress shirt, grey slacks. Balanced. |
| **Party Carl** | 5 | 3 | 3 | The maniac. Purple shirt, orange pants. Fast but wobbly. |
| **Dark Carl** | 4 | 4 | 5 | The enigma. All black. Smooth and controlled. |

**SPD** sets top speed, **HAND** steering authority, **TRK** how long he holds a line before
speed wobble sets in. The pips live in `Main.CarlStats` and are the single source of truth —
the bars on the select screen and the numbers `PlayerManager` rides with both derive from that
one table, so they can't drift apart. Tune a rider by changing a pip.

## The title screen

The garage seen from the driveway on a summer afternoon — door closed under a lit VLB sign,
the board you last picked leaning by the jamb, treeline round the clearing. Pressing ↑ takes
you inside.

## The garage

The garage is the hub. Carl stands on a lit podium, four boards hang face-out on a rack
against the back wall, and the courses are posters along the left wall. Only the camera moves
between the three select screens.

## Courses

| Course | Status |
|--------|--------|
| Frogwood, NH | Playable — 2000 m of rolling summer hills, composed from sine layers |
| Block Island | Playable — 1290 m down Spring Street, measured off the real island |
| ??? ×2 | Placeholder poster slots |

A poster is unlocked exactly when a course resource is assigned to it in the Inspector.
`Main.LevelUnlocked` is derived from that in `_Ready` rather than written down a second time.

## Block Island is measured, not composed

Frogwood is four sine layers on a net grade. Block Island is a **sampled profile** taken off
the real island: heights from USGS 3DEP 1 m LiDAR, centreline from US Census TIGER/Line, both
public domain, both read out of the [`block-island-simulator`](../../block-island-simulator)
repo by `build_block_island.py`. Nothing loads that repo at runtime — the profile is baked
into `Resources/Design/BlockIsland.tres` and the generator is the only thing that needs it.

`MeasuredCourse` subclasses `CourseDesign` and overrides `HillAt`, `CurveAt` and
`TotalRelief`. Everything downstream asks the same three questions and never learns which
kind of course it got.

**The route is Spring Street, from the Southeast Light end down toward Old Harbor.** It was
picked by measuring every road on the island: Spring Street has the longest sustained descent
there is, 39.9 m over 1595 m. The road divides into three parts and only the middle one is a
course — 965 m of clifftop plateau at 40-45 m, then the drop, then a climb back to 22 m
before Old Harbor. That last climb is the entire momentum budget before any exaggeration, so
the course starts a little way back along the plateau for a run-up and stops at the bottom of
the descent rather than carrying on into town.

**Two things were done to the measurements, and both are recorded on the resource.**

*Heights are doubled.* The island tops out at 63.7 m and its best grade anywhere is 2.5%,
against the 8% this game is built around. At the real grade a rider settles at 31 km/h, never
reaches the 15.8 m/s wobble onset, and the course has no way to end badly. Doubling puts the
steepest 600 m at 9.6% and the average at 5.9%, which brackets Frogwood.

*Headings are detrended and scaled.* The ribbon draws a point at `CurveAt(z) * lookAhead`, so
heading only means anything as a small angle — the real road swings 59° off its own baseline,
which would throw the far end of the road hundreds of metres sideways. And the camera always
sits behind the rider, so a road's absolute bearing is not observable anyway. Where the bends
fall and which way they go is measured; how hard they bite is not.

### What it looks like

Both courses are summer. They are still nothing alike, because Block Island is not a forest:
it is glacial moraine, open grassland and low windshorn scrub, with almost no tall trees and
several hundred miles of dry stone field wall. The palette and the prop populations carry all
of that, and both live on the course resource rather than in the code.

| | Frogwood | Block Island |
|---|---|---|
| Ground | New Hampshire green | olive and tawny moor |
| Trees | 210 pines and broadleaves | 34, sparse and wind-shorn |
| Scrub | 40 bushes | 95 |
| Walls | none | 48 runs of dry stone |
| Sea | none | the Atlantic, on the rider's right |

**The sea is measured too.** Its level is where real sea level falls once the same 2x
exaggeration is applied to the drop, and the distance to it is sampled per-point from the same
LiDAR: Spring Street is 40 m from the water where it leaves the Southeast Light and 770 m from
it further down, so the Atlantic arrives at the bluffs and recedes as the road runs inland.
Which side it is on is derived by probing the terrain either side of the road rather than
asserted.

Three things the palette work turned up, all of which had been invisible because Frogwood is
the colour of the things it sits in front of:

- **The sky has a ground hemisphere**, and it fills most of the middle distance past where the
  ground ribbons stop. It is New Hampshire green by default, which is why nobody noticed until
  a tawnier course put a seam across the horizon. It is part of the palette now.
- **A coastal course needs two ground ribbons**, not one centred on the road, because the land
  runs out at the shore. A single 300 m ribbon put the water's edge 150 m away and 26 m down,
  which from a camera five metres up is not visible at all — the sea was there the whole time
  and could not be seen from the road it runs beside.
- **`StartRide` hid the garage but not the title screen.** Every route to a ride goes through
  the garage, which hides the title on the way, so the title world's own grass plane had never
  been left standing in the middle of a course. It hides both now.

```bash
python3 build_block_island.py            # regenerate the resource
python3 build_block_island.py --report   # print the numbers, write nothing
```

It finds the simulator beside this repo or grouped under `games/`; `BLOCK_ISLAND_SRC=<path>`
overrides. `pyproj` is needed for the one coordinate conversion that places the lighthouse.

## Controls

| Key | Action |
|-----|--------|
| ↑ | Kick off / Confirm selection |
| ← → | Steer / Browse selections |
| Space / ↓ | Brake / Back out of a select screen |
| Esc | Pause / Back |

## Running

Requires [Godot 4.7+ with .NET](https://godotengine.org/download) and [.NET 8.0 SDK](https://dotnet.microsoft.com/download).

1. Clone the repo
2. Open **this `godot/` folder** in Godot (the .NET/Mono version) — the repo root holds two
   versions of the game, and `project.godot` is in here
3. Press ▶ to play

## Tech

- **Engine:** Godot 4.7
- **Language:** C# (.NET 8.0)
- **Terrain:** Procedural ribbon mesh with curves and hills
- **Scenery:** 190+ trees (pine and deciduous), rocks, stumps, wildflowers, mailboxes
- **Art style:** Low-poly, N64/GameCube era inspired
- **Sound:** Synthesized in code at 22 kHz (`AudioKit`). No audio files — see [Sound](#sound)
- **Models:** Everything is built in code from boxes, cylinders and spheres
  (`MeshKit`). `BoardBuilder` and `CarlBuilder` are the single source of truth for the
  longboard and for Carl, shared by the player rig and the garage displays — the board you
  pick off the rack is the board you ride.
- **Tuning:** the numbers those builders use live in editable resources under
  `Resources/Design/`, not in the code. Two editor-only scenes preview them live — see
  [Design resources](#design-resources).

### Rendering

The N64 antialiased every edge in hardware and then composite video smeared the result.
Reproducing its 320x240 pixel count without either just looks blocky, so the render is
640x480 — the hi-res mode 1080 Snowboarding shipped — with 4x MSAA, scaled up to the window
by `viewport` stretch.

| Setting in `project.godot` | Value |
|----------------------------|-------|
| `display/window/size/viewport_width` / `_height` | 640 x 480 |
| `display/window/stretch/mode` | `viewport` |
| `rendering/anti_aliasing/quality/msaa_3d` | 2 (4x) |

That reasoning lives here rather than as a comment next to those settings because Godot
rewrites `project.godot` whenever any project setting changes, and discards every comment it
did not write itself. A note in that file is guaranteed to disappear eventually.

The matching decision on the texture side is in `MeshKit.Mat()`: `TextureFilter` is left at
Godot's default linear-with-mipmaps, because that blur is the artefact we want.

### Sound

There are no audio files. `AudioKit` synthesizes every sound in the game the way `TextureKit`
synthesizes every surface — filtered noise and summed sine partials, built on first use and
cached — so the whole art pipeline stays in the repo as code you can diff.

Two kinds of stream come out of it. **Beds** loop forever and are ridden by volume and pitch;
**one-shots** fire and finish.

| Bed | Driven by |
|-----|-----------|
| Wheels on tarmac | speed — volume as its square root, pitch linearly |
| Wheels on gravel | the same, crossfaded in when a wheel leaves the tarmac |
| Wind | speed *squared*, the same `v^2` the drag term takes it out of |
| Truck rattle | wobble level, squared, so the warning arrives late and then fast |
| Scrub | foot brake bite, or a carve hard enough to break the wheels loose |
| Summer afternoon | which world is live, with birds over it at random intervals |

`AudioManager` reads all of that off the game state once a frame rather than being told about
it, so no other system has to remember the audio exists. The exceptions are events rather than
conditions — a kick, a crash, the countdown, a menu blip — and those are pushed in by whoever
knows they happened.

A looping noise bed has to join back to its own start without a click. The white noise
underneath has period *n* by construction, and a stable one-pole filter fed a periodic signal
settles onto a periodic output, so each filter is run twice around the buffer and only the
second lap is kept. It is the same argument `TextureKit` makes for wrapping its noise lattice
at the octave period, in one dimension instead of two. Any amplitude modulation is counted in
whole cycles across the buffer for the same reason.

The mix levels live in `Resources/Design/Audio.tres`, not in the manager — see
[Design resources](#design-resources).

## Architecture

### Scene structure

One shipping scene (`Scenes/Main.tscn`) — everything is built in code, including the garage
hub and menu screens. `Scenes/CarlPreview.tscn`, `Scenes/CoursePreview.tscn`,
`Scenes/AudioProbe.tscn`, `Scenes/CourseProbe.tscn` and `Scenes/CourseShot.tscn` are
workbenches; nothing at runtime loads them.

### Scripts

| Script | Role |
|--------|------|
| `Main.cs` | Root orchestrator. Game state, enums, `CarlStats` data table, state machine. |
| `PlayerManager.cs` | Player controller. Physics, carving, wobble/crash, procedural animation, particles. |
| `TerrainManager.cs` | Infinite procedural road. Rebuilt every frame. Provides `HillAt(z)` / `CurveAt(z)` queries. |
| `SceneryManager.cs` | World props (trees, rocks, flowers, mailboxes, clouds, animals). Scrolls with terrain offset. |
| `GarageManager.cs` | Hub world / menu. Builds garage interior in code. Camera lerps between the 3 select screens. |
| `TitleManager.cs` | Title world. The garage exterior, built in code, with its own light and camera drift. |
| `GameCamera.cs` | Chase camera with speed-proportional distance, shake, FOV, curve look-ahead. |
| `GameUI.cs` | All HUD and menu screens (pixel font, stat pips, speed display, wobble warning). |
| `AudioManager.cs` | The mix. Six looping beds ridden by game state, plus a one-shot voice pool. |
| `MeshKit.cs` | Static utility: `BoxMesh`, `CylinderMesh`, `SphereMesh`, materials. |
| `BoardBuilder.cs` | Static longboard mesh factory. Same code builds rack display and rideable board. |
| `CarlBuilder.cs` | Static Carl mesh factory with joint rig for procedural animation. |
| `TextureKit.cs` | Procedural 32x32 textures, generated in code and cached. Nothing loads from disk. |
| `AudioKit.cs` | Every sound, synthesized at 22 kHz and cached. Nothing loads from disk. |
| `Art/ForgeArt.cs` | Decodes art authored in SPRITE//FORGE into a texture. Rows of key characters, not a PNG. |
| `Art/CarlFace.cs` | Carl's face, generated from `Art/carl_face.forge`. A tint map — white field, dark features. |
| `Art/DeckChevron.cs` | The deck graphic, generated from `Art/deck_chevron.forge`. A cutout, so the colourway shows around it. |
| `Design/CarlDesign.cs` | Carl's proportions, palette, outfits, mesh detail and standing pose. |
| `Design/BoardDesign.cs` | Deck and truck dimensions plus the four colourways. |
| `Design/CourseDesign.cs` | Hill and curve layers, road widths, draw distance, scenery populations. |
| `Design/MeasuredCourse.cs` | A course sampled off real survey data instead of composed from sines. |
| `Design/AudioDesign.cs` | Mix levels in dB and the pitch range each bed rides. |
| `Design/SineLayer.cs` | One sine term, stored as amplitude + wavelength in metres. |
| `Tools/CarlPreview.cs` | `[Tool]` script behind `Scenes/CarlPreview.tscn`. Editor only. |
| `Tools/CoursePreview.cs` | `[Tool]` script behind `Scenes/CoursePreview.tscn`. Editor only. |
| `Tools/AudioProbe.cs` | Measures what AudioKit generated. Headless only — see [Audio probe](#audio-probe). |
| `Tools/CourseProbe.cs` | Reads a course back and reports what the ride does on it. Headless only. |
| `Tools/CourseShot.cs` | Stands the camera on a course and saves a frame. The palette's only real check. |

### Key patterns

- **Hub-and-spoke** — every subsystem gets a `Main` back-reference; no signals or event bus
- **No inheritance** — flat classes only; Godot `Node3D` hierarchy is the only composition
- **Single source of truth** — `Main.CarlStats` feeds both UI stat bars and ride physics;
  `CourseDesign` feeds the road, the scenery band, the finish trigger and `physics_sim.py`
- **Data out of code** — builders and managers take a design resource and hold no literals of
  their own, so the same class can dress the game or an editor preview
- **Infinite scrolling** — terrain meshes rebuilt every frame; scenery repositioned via `ScrollOffset`
- **Three worlds, one scene** — title, garage and road each own only their own root and
  lighting; `Main` decides which is live via `SetRideWorldVisible()` / `ApplyRideLighting()`

### Dependency diagram

```
Main (root)
 ├── TerrainManager   → provides height/curve queries
 ├── PlayerManager    → reads TerrainManager + CarlStats, writes speed/distance
 ├── SceneryManager   → reads TerrainManager, positions world objects
 ├── GameCamera       → reads TerrainManager + PlayerManager
 ├── GameUI           → reads CarlStats + PlayerManager, handles input
 ├── GarageManager    → reads Carl/Board/Level selections, uses BoardBuilder + CarlBuilder
 ├── TitleManager     → garage exterior; reads Board for the deck by the door
 └── AudioManager     → reads game state + PlayerManager, rides the mix
```

## Design resources

Carl, his board and the course are described by resources under `Resources/Design/`, not by
constants buried in method bodies. Godot only writes properties that differ from the C#
defaults, so a fresh `.tres` is nearly empty and every value you see in the Inspector comes
from the corresponding `Scripts/Design/*.cs` — which is also where each field is documented.

| Resource | Drives |
|----------|--------|
| `Carl.tres` | `CarlBuilder` — proportions, palette, the three outfits, mesh detail, standing pose |
| `Board.tres` | `BoardBuilder` — deck and truck dimensions, the four colourways |
| `Frogwood.tres` | `TerrainManager` + `SceneryManager` — hills, curves, road widths, prop populations |
| `BlockIsland.tres` | The same, but a `MeasuredCourse`. **Generated — do not hand-edit.** |
| `Audio.tres` | `AudioManager` — level and pitch range for every bed, and the sound-effect trim |

All four are assigned to the root node of `Scenes/Main.tscn`, so you can swap a whole look
by dropping a different resource into the slot. An empty slot falls back to the stock build
rather than crashing.

### Live preview

Two editor-only scenes rebuild as you drag:

- **`Scenes/CarlPreview.tscn`** — Carl on his board under game lighting. Pick an outfit and a
  deck from the dropdowns, spin the `Turntable` slider to check his silhouette, and every
  edit to `Carl.tres` reshapes him in the viewport immediately.
- **`Scenes/CoursePreview.tscn`** — the road with the real terrain and scenery managers
  driving it. Drag `Distance` to fly down the course; `Show Scenery` is off by default
  because rebuilding a few thousand meshes on every slider tick is slow.

Both listen to the resource's `Changed` signal, and both carry a **Rebuild** button for when
you would rather force it. Their children are added without an `Owner`, so nothing they build
is ever serialised into the scene file.

**These are C# `[Tool]` scripts.** After editing anything under `Scripts/`, press **Build**
(the hammer, top right) before the preview picks the change up; if a scene still looks stale,
close and reopen it.

## Garage layout tool

`garage_layout.py` plots the garage from above with each camera's frustum and checks that
nothing clips a wall. Change positions there first, eyeball the PNGs, then port the numbers
into `GarageManager.cs` — the anchors are mirrored in both files.

Run it from this folder:

```bash
python3 garage_layout.py
```

## Ride physics tool

`physics_sim.py` simulates a full run for each Carl — run length, average and peak km/h, time
spent bogged down, and how much of the ride sits in the wobble band.

The terrain half is **not** mirrored: it parses `Resources/Design/Frogwood.tres` directly, so
reshaping the hills in the Inspector and re-running the sim needs no porting step. The report
opens with a `source:` line naming what it actually read. The rider half still mirrors
`PlayerManager.Update()` by hand — tune those constants in the script, then port them across,
same workflow as the garage tool.

Run it from this folder:

```bash
python3 physics_sim.py
```

The ride is SI: **1 world unit = 1 metre, `Speed` is m/s**. Gravity is real gravity and top
speed is settled by quadratic air drag rather than by a hard cap, so the grade numbers in
`HillAt()` mean what they say. Two rules worth keeping in mind when reshaping a course:

- **Steepness is amplitude x frequency, not amplitude.** A 0.9 m roll over a 66 m
  wavelength is steeper than a 5.5 m roll over 1257 m.
- **The climb has to stay under what a rider can carry momentum through** (`v^2/2g` at his
  own top speed — 25.3 m for the slowest Carl), or he bogs down on every crest and the ride
  dies. What counts is the climb he actually makes, which is the rise left in the height after
  the grade, not the rise in the hill layers.

## Course probe

`Scenes/CourseProbe.tscn` loads every course resource and reports what the ride will actually
do on it — net drop, average grade, the steepest sustained stretches and the settling speed on
each, the worst climb, and the peak heading.

```bash
godot --headless --path godot --scene res://Scenes/CourseProbe.tscn
```

The two numbers that decide whether a course works are **the steepest sustained grade**, which
has to put the rider past the 57 km/h wobble onset or nothing can ever go wrong, and **the
worst climb**, which has to stay under `v^2/2g` at the slowest rider's top speed or he bogs
down and the ride dies.

| | Frogwood | Block Island |
|---|---|---|
| Worst climb | 4.7 m | 8.4 m |
| Budget (slowest Carl) | 25.3 m | 25.3 m |

Both are comfortably inside it, and the real road has the bigger climbs of the two.

**That worst climb used to be reported as 24.4 m, and it was wrong twice over.** It summed the
hill layers' amplitudes and doubled them, which bounds the rise in the *sines* rather than
measuring the climb the rider makes — and it ignored the grade, which is the largest term in
the height. The sines really do rise 20.9 m across one long roll, but the road is descending at
8% underneath them the whole way, and what is left for the rider to climb is 4.7 m, over 46 m,
once in 2 km.

The budget was wrong too. 22 m is `v^2/2g` for a 3-pip baseline rider who does not exist: every
Carl in the game has SPD 4 or 5, so the tightest real budget is 25.3 m. Frogwood was never near
it, and `physics_sim.py` had been agreeing the whole time — 152 s, 47 km/h average, 11.4% of
the run bogged down, all inside their targets. The alarm was the only thing broken.

Both figures are measured now, by walking `HillAt` over the course, which is why
`MeasuredCourse` no longer needs its own copy of the calculation.

## The headless leak warning

A headless run of the game reports, on most runs:

```
WARNING: 12 ObjectDB instances were leaked at exit
```

Twelve is the six audio beds and their six playbacks. **It is a property of the headless audio
driver, not of the game, and it has been chased once already — please read this before
chasing it again.**

What it does and does not depend on, measured on one build:

| | |
|---|---|
| Windowed, real audio driver | clean, every time |
| Headless, dummy audio driver | leaks about three runs in four |
| Frame count | changes how often, not whether |
| `--quit-after` vs `GetTree().Quit()` | no difference; both do it |

The beds are started once and never stopped, which is what keeps a loop from clicking, so six
of them are still playing at exit. With a real driver the audio server reclaims them. With the
dummy driver it does not, reliably.

Ruled out, each tried and each making no difference:

- Stopping the players unconditionally rather than only when `Playing` — they have already left
  the tree when `Main._ExitTree` runs, so `Playing` is false and a guarded stop skips them.
  Removing the guard does not help either.
- Clearing each player's `Stream`.
- Disposing AudioKit's cached streams, which is what actually releases a Godot C# wrapper.
  This made it *more* frequent.
- Nulling AudioKit's static cache and forcing `GC.Collect` plus `WaitForPendingFinalizers`.
  This made it flakier in both directions, which is what a timing dependency looks like.
- Giving each player its own `_ExitTree` to stop itself at the one moment it is both in one
  piece and still owned — the earliest point the game controls. No change.

Nothing accumulates while the game runs. Every path the game actually ships through is clean.
The next thing worth trying, if it ever matters, is not starting a bed until it first becomes
audible — five of the six are silent on the title screen where this is measured, so there
would be almost nothing left to reclaim. That is a change to how the mix behaves, though, and
it should be made because it is better, not to quiet a warning that only appears without a
sound card.

## Course shot

A palette is the one thing no headless number can check, so `Scenes/CourseShot.tscn` starts a
ride at a given distance and saves a frame to `user://`.

```bash
godot --path godot --scene res://Scenes/CourseShot.tscn -- --course=BlockIsland --at=950
```

It needs a real window — the point is what the thing looks like — so no `--headless`. It also
runs the course-switch path from a standing start, which is how it caught `StartRide` leaving
the title world visible.

## Audio probe

`Scenes/AudioProbe.tscn` measures what `AudioKit` actually generated and prints a table. It is
the audio equivalent of `physics_sim.py`, and it exists because the two ways a synthesized
stream goes wrong are both silent to the compiler and invisible on screen.

```bash
godot --headless --path godot --scene res://Scenes/AudioProbe.tscn
```

For a bed, the column that matters is **`dSeam/dRms`**: the jump from the last sample back to
the first, divided by an ordinary sample-to-sample step. At 1 the loop join is indistinguishable
from the signal either side of it. Anything much above about 5 is a click you will hear once per
loop, forever.

For a one-shot it is **`first`** and **`last`**, which must both be 0 or the sound clicks on and
off, and **`peak`**, which must stay under 1.0 or the stream is clipped before the mix gets a
say. `centroidHz` is a zero-crossing estimate of where the energy sits — coarse, but enough to
catch a filter wired the wrong way round, or a blip landing on the wrong note.

## License

Private project — not for distribution.
