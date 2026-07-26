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

## The garage

The garage is the hub and doubles as the title screen — only the camera moves between
screens. Carl stands on a lit podium, four boards hang face-out on a rack against the back
wall, and the courses are posters along the left wall.

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

## Garage layout tool

`garage_layout.py` plots the garage from above with each camera's frustum and checks that
nothing clips a wall. Change positions there first, eyeball the PNGs, then port the numbers
into `GarageManager.cs` — the anchors are mirrored in both files.

```bash
python3 garage_layout.py
```

## License

Private project — not for distribution.
