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
whole-index commits, `?v=` cache-buster stamps rewritten by a hook, and a Playwright smoke
test over every arcade game. Read its `AGENTS.md` before committing a sync there.

## AI attribution

**No AI attribution.** Do not append `Co-Authored-By: Claude …`, "Generated with
…", or any similar trailer to commit messages, PR bodies, or release notes. If
your tooling adds such a line by default, remove it before committing.
