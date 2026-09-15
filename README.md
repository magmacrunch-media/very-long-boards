# Very Long Boards

Downhill skateboarding with Carl Spatski. One repo, both versions of the game.

| Version | Folder | Engine | Where it runs |
|---------|--------|--------|---------------|
| Desktop | [`godot/`](godot/) | Godot 4.7 + C# | anywhere Godot runs |
| Browser | [`web/`](web/) | [Babylon.js](https://www.babylonjs.com/) | [magmacrunch.com/arcade/very-long-boards](https://magmacrunch.com/arcade/very-long-boards/) |

## They are not ports of each other

Worth saying up front, because every other multi-version game in this org works the other
way. In `moonlight-drift` and `george-boole` a rules change is carried into all three
versions and the commit says so if one is skipped. **Here it is not.** These are two
different games wearing the same character:

| | `godot/` | `web/` |
|---|---|---|
| Shape of a run | a **course** — 2000 m of Frogwood, with a finish line and a clock | **endless** — ride until you wipe out, for score |
| What ends it | crossing the line, or speed wobble | hitting an obstacle, or the stability meter bottoming out |
| Failure model | wobble builds when you hold a line too long at speed; carving resets it | a stability meter, plus obstacles to dodge |
| Scoring | best time | points, tricks and near-misses |
| Physics | SI throughout — real gravity, quadratic air drag, 1 unit = 1 m | tuned arcade constants in `web/js/config.js` |
| The garage | a 3D hub world you move a camera through | a 2D canvas backdrop behind HTML overlays |
| Boards | Classic, Neon, Dark, Natural | Standard, Cruiser, Carver, Old School |

So a change to one is **not** owed to the other. Where a change genuinely belongs in both —
a new Carl, a rename, a colourway — say so in the commit. Where it does not, that is the
normal case and needs no explanation.

What they do share is Carl Spatski himself: three looks, the everyman, the maniac and the
enigma, each riding differently.

## Layout

- **`godot/`** — the desktop version. Everything is built in code (meshes, textures, sound);
  nothing is loaded from disk. Open this folder in Godot, not the repo root. Has its own
  [README](godot/README.md) covering the architecture, the design resources and three
  analysis tools.
- **`web/`** — the browser version. **This folder is the source of truth**; the copy served
  from the website repo at `arcade/very-long-boards/` is generated from it by
  `make sync-very-long-boards` there, which deletes that folder and recopies it. Edit here,
  sync there, commit the result in the website repo. See [`web/README.md`](web/README.md).

## Controls

Both versions steer with ← →, and both use Esc to pause.

| | `godot/` | `web/` |
|---|---|---|
| Steer | ← → | ← → or A D |
| Slow down | Space or ↓ | ↓, S or Space |
| ↑ does | kick off, to build speed | a **trick**, for points |
| Confirm | ↑ | Enter |
| Back out | Space or ↓ | Esc, or Tab from the game-over screen |
| Pause | Esc | Esc |

`web/` also takes touch: the left and right thirds of the screen steer, and a tap in the
middle is a trick or a confirm.


## Support This Project

If you find this useful, consider supporting its development:

[![Sponsor](https://img.shields.io/badge/%E2%9D%A4_Sponsor-pink)](https://github.com/sponsors/magmacrunch-media)
## License

Private project — not for distribution.
