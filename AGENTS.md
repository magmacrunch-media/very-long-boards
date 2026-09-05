# AGENTS.md — Very Long Boards

Two versions of one game, in one repo, and **they are not ports of each other**:

- **`godot/`** — the desktop version. Godot 4.7 + C#. A 2000 m course with a finish line
  and a clock, on SI physics: real gravity, quadratic air drag, one world unit is one metre.
  Everything is generated in code — meshes, textures, sound — and nothing is loaded from
  disk. Its own [README](godot/README.md) covers the architecture.
- **`web/`** — the browser version. Babylon.js, plain script tags. An endless run for score,
  with obstacles, tricks and a stability meter. Source of truth for the copy the website
  serves; see [`web/README.md`](web/README.md).

## A change to one is not owed to the other

This is the opposite of the rule in `moonlight-drift` and `george-boole`, where a gameplay
change is not done until every version has it. Here the two games have different failure
models, different scoring and different boards, so most changes belong in exactly one of
them. **Do not port a change across on principle.** Where one genuinely does belong in both —
a new Carl, a rename, a colourway — say so in the commit; where it does not, that is the
normal case and needs no note.

The one thing they share is Carl Spatski: three looks, the everyman, the maniac and the
enigma, each riding differently.

## Deploying the browser version

`web/` is the source. The website repo generates `arcade/very-long-boards/` from it:

```bash
make sync-very-long-boards     # run in the website repo
```

That target **deletes** the arcade folder and recopies `web/`, so edits made in the website
copy are destroyed silently. Edit here, sync there, commit there. The website copy carries a
`GENERATED.md` written by the sync itself, so the warning is visible from inside the mirror
as well as from here.

Note the website repo has its own constraints that this repo does not: a pre-commit gate on
whole-index commits, and a Playwright smoke test over every arcade game. Read its
`AGENTS.md` before committing a sync there.

### Cache-buster stamps in `web/index.html`

Every `?v=` in `web/index.html` is the first eight hex of SHA-256 over the file it stamps,
with newlines normalised to LF. Get one wrong and visitors keep serving the cached old
bytes, so the change reaches nobody and the page still loads.

`../shared/chat-widget.css` and `../shared/adenosine-chat.js` are the ones to watch: they
name files that do not exist in this repo at all. They resolve only once `web/` has been
copied into the website's `arcade/`, and they go stale when *that* repo updates a shared
bundle — with nothing here touched and nothing here able to notice. Both went stale exactly
that way when the website took `adenosine-chat` 0.6.0.

The website's hook does rewrite stale stamps, but only in its own generated copy, and that
repair never travels back here. Since `make sync-very-long-boards` recopies `web/` verbatim,
a stamp corrected there is reverted by the next sync. **The fix belongs here.** Recompute one
from a website checkout, and check the whole site after any sync:

```bash
node scripts/check-cache-busters.mjs --digest arcade/shared/adenosine-chat.js
npm run check:cachebust
```

## Single source of truth, in both versions

Each version states where its numbers live, and neither has a second copy of them:

| | tuning lives in | consumed by |
|---|---|---|
| `godot/` | `Main.CarlStats`, and the resources under `Resources/Design/` | the ride physics and the stat pips on the select screens |
| `web/` | `CONFIG`, `CHARACTERS`, `BOARDS` in `js/config.js` | the ride physics and the generated stat bars |

In both, the bars a select screen draws are derived from the same table the physics reads, so
a card cannot promise a rider something the game does not deliver. Keep it that way: a stat
written out by hand in markup or in a mesh is a stat that will drift.

## AI attribution

**No AI attribution.** Do not append `Co-Authored-By: Claude …`, "Generated with
…", or any similar trailer to commit messages, PR bodies, or release notes. If
your tooling adds such a line by default, remove it before committing.
