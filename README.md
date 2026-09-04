# Very Long Boards

A peaceful downhill skateboarding game set on forested New Hampshire roads. Ride as Carl Spatski and make your way down sunny summer hills.

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
| Frogwood, NH | Playable — 2000m of rolling summer hills |
| Block Island | Locked — coastal cliffs, not built yet |
| ??? ×2 | Placeholder poster slots |

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
2. Open the project folder in Godot (the .NET/Mono version)
3. Press ▶ to play

## Tech

- **Engine:** Godot 4.7
- **Language:** C# (.NET 8.0)
- **Terrain:** Procedural ribbon mesh with curves and hills
- **Scenery:** 190+ trees (pine and deciduous), rocks, stumps, wildflowers, mailboxes
- **Art style:** Low-poly, N64/GameCube era inspired
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

## Architecture

### Scene structure

One shipping scene (`Scenes/Main.tscn`) — everything is built in code, including the garage
hub and menu screens. `Scenes/CarlPreview.tscn` and `Scenes/CoursePreview.tscn` are
editor-only workbenches; nothing at runtime loads them.

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
| `MeshKit.cs` | Static utility: `BoxMesh`, `CylinderMesh`, `SphereMesh`, materials. |
| `BoardBuilder.cs` | Static longboard mesh factory. Same code builds rack display and rideable board. |
| `CarlBuilder.cs` | Static Carl mesh factory with joint rig for procedural animation. |
| `TextureKit.cs` | Procedural 32x32 textures, generated in code and cached. Nothing loads from disk. |
| `Art/ForgeArt.cs` | Decodes art authored in SPRITE//FORGE into a texture. Rows of key characters, not a PNG. |
| `Art/CarlFace.cs` | Carl's face, generated from `Art/carl_face.forge`. A tint map — white field, dark features. |
| `Design/CarlDesign.cs` | Carl's proportions, palette, outfits, mesh detail and standing pose. |
| `Design/BoardDesign.cs` | Deck and truck dimensions plus the four colourways. |
| `Design/CourseDesign.cs` | Hill and curve layers, road widths, draw distance, scenery populations. |
| `Design/SineLayer.cs` | One sine term, stored as amplitude + wavelength in metres. |
| `Tools/CarlPreview.cs` | `[Tool]` script behind `Scenes/CarlPreview.tscn`. Editor only. |
| `Tools/CoursePreview.cs` | `[Tool]` script behind `Scenes/CoursePreview.tscn`. Editor only. |

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
 └── TitleManager     → garage exterior; reads Board for the deck by the door
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

All three are assigned to the root node of `Scenes/Main.tscn`, so you can swap a whole look
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

```bash
python3 physics_sim.py
```

The ride is SI: **1 world unit = 1 metre, `Speed` is m/s**. Gravity is real gravity and top
speed is settled by quadratic air drag rather than by a hard cap, so the grade numbers in
`HillAt()` mean what they say. Two rules worth keeping in mind when reshaping a course:

- **Steepness is amplitude x frequency, not amplitude.** A 0.9 m roll over a 66 m
  wavelength is steeper than a 5.5 m roll over 1257 m.
- **Total relief has to stay under what a rider can climb on momentum** (`v^2/2g`, about
  22 m at top speed), or he bogs down on every crest and the ride dies.

## License

Private project — not for distribution.
