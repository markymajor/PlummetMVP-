# Plummet — Design Spec & Rebuild Plan

> Source of truth for rebuilding Plummet in Unity, derived from the original
> 2014 Cocos2d / Objective-C source (author: Silviu). The original code is the
> **soul** of the game; this document captures its mechanics exactly, then lays
> out the modernized Unity rebuild.

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

## 3. Original → current Unity build: gap analysis

| Aspect | Original (2014) | Current Unity MVP | Action |
|---|---|---|---|
| Falling illusion | Shaft scrolls up, player fixed | Same ✅ | Keep |
| Controls | Tilt only | Tilt + drag + keyboard | Keep modern multi-input |
| Corridor | Hard zig-zag, stepped edges | Smooth width/center random walk | **Rework toward zig-zag** |
| Collision | Silhouette polygon vs edges | Box/rotated colliders | Keep colliders (modern), tune to silhouette |
| Wall color | `#023548` | `#03303B` (close) | **Set exact `#023548`** |
| Wall texture | Edge bricks + decals | Flat tiled sprite | **Add brick edge + decals** |
| Hazards | Walls only | Walls **+ spawned obstacles** | **Remove obstacles (not in original)** |
| Difficulty | Speed 7, +0.25/5s, narrows, no cap | base 3.25→8.5, 0.045/s | **Retune to original character** |
| Intro | Standing→falling pose morph | Trapdoor (new) | Keep trapdoor + add pose morph |
| Score | Distance | Distance | Keep |
| Build scene | — | Build settings point at empty `SampleScene` | **Fix to PlummetMVP** |

---

## 4. Rebuild plan (modernized, staged)

**Principle:** keep the soul (pure corridor-dodge, fixed faller, narrowing
zig-zag, distance score) and modernize the implementation (New Input System,
pooling, real colliders, multi-input, proper aspect handling, tunable values).

- **Phase 0 — Foundation** ✅ done
  - [x] Spec doc (this file).
  - [x] Import recovered decorative wall art (`Briks_02–06`, `Window-Background`,
    `Bricks-background_01–03`).
  - [x] Exact original colors in the shaft (`#023548`).
  - [x] Difficulty values retuned to the original's character.
  - [x] Fix build settings to ship `PlummetMVP.unity`.

- **Phase 1 — Faithful corridor** ✅ done
  - [x] Rework `PathManager` generation to the stepped zig-zag (gap width +
    stepped jumps, anti-drift, narrowing over time).
  - [ ] Decorate walls with random rubble/window decals (`Briks_*`, `Window`)
    — *art-ready, deferred to local tuning.*
  - [ ] Parallax brick background behind the corridor — *deferred to local.*

- **Phase 2 — Faithful feel** 🔧 in progress
  - [x] Solid shaft walls that fill to the screen edge (thick walls; inner
    collision face unchanged). Also fixes the "dark part beside the bricks"
    note in gameplay.
  - [ ] Retune tilt steering to the original (smoothed, edge-clamped); keep drag
    + keyboard for desktop/testing — *needs a device to feel; do locally.*
  - [ ] Standing→falling pose morph — *blocked: Mark art has no Initial02/03
    poses; the trapdoor intro already covers the standing→falling handoff.*

- **Phase 3 — Cleanup**
  - [ ] ~~Remove obstacles~~ — **kept** per design decision (modern addition).
  - [ ] Remove the unused `SampleScene` and dead band-aid tooling once the
    generator is the single source of scene truth.

> Items marked "local" touch runtime feel/visuals and are best landed with the
> Unity play-test loop (play + screenshot) rather than blind from the repo.

---

## 5. Tuning reference (Unity, starting points)

| Value | Original | Unity start | Notes |
|---|---|---|---|
| Start fall speed | 7 pts/frame (~8 u/s) | `baseScrollSpeed ≈ 5.5` | gentler open, modern |
| Speed ramp | +0.25/5s (~+0.057 u/s²) | `speedIncreasePerSecond ≈ 0.06` | steeper than MVP |
| Speed cap | none | `maxScrollSpeed ≈ 12` | sanity cap (original had none) |
| Gap width | 240 → 140 pts | `start 4.6 → min 2.5 u` | ~53 pts/unit |
| Segment height | 100–130 pts | `1.9–2.4 u` | |
| Zig-zag step | 25–55 pts | `0.5–1.0 u` | |
| Wall fill | `#023548` | `(0.008,0.208,0.282)` | exact |
| Sky | `#3B6D6F` | `(0.231,0.427,0.435)` | exact |

*Scale assumes ~53 original points per Unity world unit (≈6 u wide portrait).*
