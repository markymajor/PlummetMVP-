# Plummet — Original Design Reference & Rebuild Record

> **Status: design-history reference.** Sections 1–2 document the original 2014
> Cocos2d/Objective-C game exactly (author: Silviu) — the *soul* the rebuild was
> measured against. Section 3 records how the Unity rebuild turned out, including
> where it deliberately diverged. **Planning, backlog, and milestones live in
> [`BUILD_PLAN.md`](BUILD_PLAN.md).**

---

## 1. The soul of the game (what makes Plummet *Plummet*)

You are a person who **falls down an endless vertical shaft**. The character is
pinned near the top-middle of the screen; the **shaft scrolls up past you** to
create the sense of falling. You **steer left/right** to thread a **narrowing,
winding brick corridor**. Touch a wall and you die. The longer you survive, the
**faster and tighter** it gets. Score is the distance fallen.

No enemies. No projectiles. No power-ups. The entire game is *you, gravity, and
a shrinking gap.* That purity is the soul.

---

## 2. Original mechanics (exact, from the 2014 source)

### Player
- Character art: `Initial01` (standing) → `Initial02` → `Initial03` (transition
  poses) → `animation0001…0010` (10-frame falling loop).
- Fixed vertical position at **⅔ of screen height** once the fall begins.
- **Controls: accelerometer tilt only.** `x += smoothedTiltX * 10`, low-pass
  filter `0.05` (`smoothed = smoothed*0.05 + raw*0.95`). Touch-drag was
  simulator-only. Clamped to screen edges.
- Collision: an **8-point polygon** traced around the silhouette
  (`CHARACTER_POINT1…8`, scaled by `CHARACTER_SCALE = 0.7`) tested against wall
  edges via line-segment intersection. Hitting either wall → game over.

### Shaft / corridor (`BackgroundLayer`)
- A connected **zig-zag corridor** of `RECTANGLES_COUNT = 10` segments, recycled.
- Each segment: a **gap width** (`distanceXNow`) and a **left-edge x** (`xLeft`).
  - First gap: `MINIMUM_DISTANCE_WIDTH_FIRST(240) + rand(20)`.
  - Subsequent gap: `minimumDistanceWidth + rand(randomDistanceWidth)`.
  - `xLeft` shifts by a random **step** each segment, with anti-drift logic:
    forced to turn back if it would cross a margin (`MARGIN_X = 10`) or if the
    last `STEPS_COUNT = 4` steps all went one direction.
- Segment height: `MINIMUM_DISTANCE_HEIGHT(100) + rand(30)`.
- Walls drawn as filled polygons in color **RGB(2,53,72)**, decorated with:
  - **Edge bricks** (`Brick-Color`, 36×55) tiled along the corridor edge at the
    edge angle, spaced ~32px.
  - Random **`Briks_02…06`** rubble decals and **`Window`** decals on the wall
    faces (type roll `rand(7)`: 0–1 → window, else a brick variant, random flip).
- Background behind walls: `Bricks-background_01…03` decals scrolling at
  **0.05× speed** (parallax). Base screen color **RGB(59,109,111)**.

### Difficulty & scoring
- `START_SPEED = 7` (points/frame @ 60fps). Walls + decals move up by `speed`
  each frame; background by `speed * 0.05`.
- **Every 5 seconds:** `speed += 0.25`; `minimumDistanceWidth -= 5` (floor 140);
  `randomDistanceWidth -= 5` (floor 20); zig-zag steps grow
  (`min 25→35`, `max 35→55`). **No speed cap** in the original.
- **Score = cumulative distance** (`distance += speed` per frame), shown with a
  bitmap number font.

### Flow / intro (`MainScene`)
1. Character stands (`Initial01`) at the top over the menu.
2. Tap → character **rises to ⅔ screen** (`startingGameStep 0`).
3. As distance passes 200-unit marks, swap `Initial01 → 02 → 03 → falling loop`
   (the **standing-to-falling pose morph**). Then gameplay proper begins.
4. Tap during play = **pause/unpause**. Wall hit → Game Over menu.

### Exact colors & key assets
| Element | RGB | Hex | Unity (0–1) |
|---|---|---|---|
| Sky / base background | 59, 109, 111 | `#3B6D6F` | `(0.231, 0.427, 0.435)` |
| Wall fill ("the dark part") | 2, 53, 72 | `#023548` | `(0.008, 0.208, 0.282)` |

Recovered art (HD/@2x) in the original zip: `Initial01–03`,
`animation0001–0010`, `Brick-Color`, `Briks_02–06`, `Window`,
`Window-Background`, `Bricks-background_01–03`, `Hatch`, `Title`,
`Menu-background`, `GameOver`, `Home`, Dosis fonts, `ScoreFont` bitmap font.

---

## 3. How the rebuild turned out (record, 2026-07)

The staged "faithful rebuild" plan completed and then the game **deliberately
evolved past faithfulness** through play-tested design decisions. The soul
survived intact; several surfaces were consciously modernized.

### Kept faithful to the original
- **The soul**: pure faller, player pinned high, shaft scrolls up, narrowing
  winding corridor, walls kill, distance score, ramping speed.
- **Anti-drift corridor wandering** (the `STEPS_COUNT` turn-back logic lives on
  inside `PathManager.ChooseDirection`).
- **Standing-on-land intro**: character stands on the surface, drops through a
  trapdoor into the shaft — the modern take on the `Initial01` pose-morph flow.
- **10-frame falling flail** (now per-character), brick lining along the
  corridor edge, window decals on the walls, distance scoring.

### Deliberate divergences (decisions, not gaps)
| Original | Rebuild | Why |
|---|---|---|
| Hard zig-zag stepped edges | **Organic Perlin wall edges** + dramatic width swings | Play-tested reference art preferred the canyon look; fairness clamps keep it passable by construction |
| `#023548` teal walls | **Dark navy walls, light teal-grey shaft** | Matched to the chosen visual reference; better player contrast |
| Walls only | **Wall-protrusion obstacles kept** (always-passable lane math) | Modern addition, kept by decision |
| One character (Silviu's Mark) | **Skins: Mark, Evie, Harrison** — per-skin dives (backflip / jump-in), standing poses, Choose Player screen | The point of the rebuild: the kids play as themselves |
| Game Over screen with dead character | **RESCUED!** — a firefighter catches you in a net | Kid-friendly reframe; replaced the planned death-frame entirely |
| Tilt only | Tilt + touch-drag + keyboard | Modern multi-input (tilt tuning still pending a device build) |
| No speed cap | Capped (`maxScrollSpeed`) | Sanity for small players |
| Attract-scrolling menu shaft | **Static home**; the drop itself accelerates the scroll from rest | Makes home→run one continuous fall |
| Dense random decals | **Sparse latticed decals** + rare kids' graffiti tags | Random clumping read as noise at phone scale |

### Where the current numbers live
Tuning values are **owned by the code and the baked scene** (`PathManager`,
`GameManager`, `ObstacleSpawner`, `PlummetSceneRepair`) — not duplicated here.
The engineering invariants that protect the feel are listed in
[`BUILD_PLAN.md` § Design invariants](BUILD_PLAN.md).

### Original tuning (kept for historical comparison)
| Value | Original (2014) |
|---|---|
| Start fall speed | 7 pts/frame (~8 u/s at ~53 pts/unit) |
| Speed ramp | +0.25 per 5 s, uncapped |
| Gap width | 240 → 140 pts |
| Segment height | 100–130 pts |
| Zig-zag step | 25–55 pts |
