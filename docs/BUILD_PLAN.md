# Plummet — Build Plan

*Living planning doc. Update the board as items move. Supersedes the phase plan in
`PLUMMET_SPEC.md` (keep that one as the faithful-rebuild design reference).*

**Vision:** a modern rebuild of the 2014 Plummet — a pure faller down a procedurally
narrowing shaft — where the kids play as themselves: pick Evie/Harrison/Mark, dive
through the trapdoor in their own style, dodge the walls, get **RESCUED!** by the
firefighter when a run ends. Target: playable on the family's phones.

---

## Status snapshot — 2026-07 (Bali polish sprint)

### ✅ Shipped
| Area | What's in |
|------|-----------|
| Core loop | Static home → tap → gravity drop through trapdoor → scrolling run → RESCUED! → retry. One continuous fall, no hand-offs. |
| Corridor | Perlin organic walls, width swings tight↔wide with retargeting, brick lining with mortar gaps + jut, fairness-by-construction (`SafeGap` / `EdgeAmplitudeForWidth` / `MaxObstacleReach`). |
| Obstacles | Wall protrusions, always-passable lane math, live retract (`ObstacleRider`), rooted inside the wall, difficulty-ramped frequency/depth/chance. |
| Skins | Mark (10-frame flail), Evie (backflip dive), Harrison (jump dive); Choose Player screen with pending-pick → Select; per-skin one scale (no pulsing), standing/fall size matched by body-length normalization. |
| World dressing | Lit windows (sparse + rarity roll), shaft/wall decals on shared lattices (no overlap by construction), kids' graffiti tags (rare treats), surface city + Great Wall + floor. |
| Screens | Home (surface + trapdoor), instructions (legacy art), HUD, Choose Player, **RESCUED!** (firefighter + net + bounced skin + big orange score). |
| Pipeline | Single-generator scene (`Plummet ▸ Repair Open Scene`), green-screen/checkerboard art intake (cloud-side), `docs/ART_PIPELINE.md` rig recipe. |

### 🔄 In flight
| Item | Owner | Notes |
|------|-------|-------|
| Kid falling flutter loops (hair/clothes waving) | Mark (art) → cloud intake → local wiring | 6 frames per kid, prompts in chat history; loaders already support multi-frame |
| Rescue puff (optional) | Mark (art) | regenerate on green #00FF00; screen ships fine without it |

---

## Backlog

### P1 — the road to phones (next sessions)
| # | Task | Owner | Size |
|---|------|-------|------|
| 1 | **Audio pass**: 1 music loop + core SFX (drop whoosh, wall thud, rescue boing, button tap). Sourcing: kid-friendly free/CC packs or generated. Mute toggle. | local Claude (+ Mark picks sounds) | M |
| 2 | **Android build on a real device**: build support module, player settings (portrait lock, icon, name), first APK sideload. | Mark + local Claude | M |
| 3 | **Tilt tuning on device**: sensitivity/deadzone/lean feel; keep touch-drag fallback. First time accelerometer code gets felt. | Mark + local Claude | M |
| 4 | **Safe-area / notch check** on the phone (HUD + buttons inside safe area). | local Claude | S |

### P2 — polish
| # | Task | Owner | Size |
|---|------|-------|------|
| 5 | New pipe art with wall flange (+ optionally AC unit / brick ledge for variety) to replace legacy obstacle sprites. | Mark (art) → local | M |
| 6 | Instruction screens: redraw in current style or cut entirely (game is self-explanatory). | decision Mark, then local | S |
| 7 | Share button: hide until wired (dead button invites kid-taps). Native share later. | local | S |
| 8 | App icon + name ("Plummet") for the home screen. | Mark (art) → local | S |
| 9 | Rig Mark in Unity 2D Animation (`ART_PIPELINE.md`) for richer animation; reuse skeleton for kids. | Mark, someday | L |

### P3 — hygiene
| # | Task | Owner | Size |
|---|------|-------|------|
| 10 | Finish dead-code sweep: delete `IntroTransition.cs`, `PlummetTrapdoorIntro.cs`, `PlummetSceneBuilder.cs`, `PlummetStartShaftPolish.cs` if still unreferenced (UIManager fields already cleaned). | local | S |
| 11 | Unused art cull: `death.png`, `mark-falling-flail-sheet/preview/custom`, legacy start-shaft pieces no longer loaded. | local | S |
| 12 | Refresh `PLUMMET_SPEC.md` header: mark it as the design-history reference; this doc owns planning. | cloud | S |

---

## Milestones
- **M1 — Feature-complete on desktop** *(now – next session or two)*: P1 #1 audio in;
  in-flight art landed. Everything playable and delightful in the editor.
- **M2 — On a phone**: P1 #2–4. The kids play it on a real device. This is the moment
  that matters; prioritize over all polish.
- **M3 — Kid-proofed**: P2 #6–8 (no dead buttons, clear or no instructions, icon).
  Survives a 5-year-old mashing the screen.
- **M4 — Stretch**: more family skins, rigged animation, per-skin best scores,
  new obstacle art, seasonal themes.

## Working model (how this project ships)
Three seats, one branch (`claude/trusting-wright-6otx5h`):
- **Mark** — direction, play-testing (annotated screenshots work great), art generation (ChatGPT).
- **Local Claude** (Unity MCP) — everything needing editor eyes: scene rebakes, tuning, verification. *Owns the scene file.*
- **Cloud Claude (Fable 5)** — reviews every push, writes precise specs for local, asset intake/processing (green-screen, checkerboard, defringe), docs. *Never touches the scene.*

Rules that keep it smooth: pull-before-work / push-after; one driver per file domain;
scene commits separate from code commits; cloud reviews land within a message of "pushed".

## Design invariants (do not break casually)
1. **Fairness by construction** — wall mesh, collider, lining and obstacle math all
   derive from `SafeGap`/`EdgeAmplitudeForWidth`/`MaxObstacleReach`; obstacle rooting
   shares `ObstacleRider.RootOffset`. Change these constants only with a play-test.
2. **Decal lattice rule** — every scrolling decal population joins the shared lattice of
   its territory (wall side or shaft centre). Never stratify a new set independently.
3. **One scale per skin** — anchored to the falling reference by body length; standing
   normalized separately; never re-normalize per animation frame.
4. **Lethal = visible** — the wall collider tracks the brick face; cosmetic jut is
   forgiveness, never punishment.
5. **Scene = generator output** — visual changes go through `Repair Open Scene` +
   rebake, not hand-edits.

## Doc index
- `docs/BUILD_PLAN.md` — this file (planning, backlog, milestones)
- `docs/PLUMMET_SPEC.md` — original-game analysis + faithful-rebuild design (historical)
- `docs/SKINS.md` — skin system + add-a-character workflow
- `docs/ART_PIPELINE.md` — rig-once animation recipe + AI art intake conventions
- `Assets/Plummet/SkinDrop/README.md` — green-screen drop-folder workflow
