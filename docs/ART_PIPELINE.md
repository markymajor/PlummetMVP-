# Art & animation pipeline

How to make and animate Plummet characters **without** generating every frame by
hand. The project's flat-vector characters (Mark, Evie, Harrison) are perfect for
**cutout / skeletal rigging**: draw the character once in parts, animate by posing
bones. Consistent, smooth, editable, and reusable across skins.

> TL;DR: stop generating animation frames with AI. Generate/draw the character
> **once, in separable parts**, rig it in Unity, and pose bones into clips.

## Already installed (no setup needed)
- `com.unity.2d.animation` — bones, skinning, the Skinning Editor
- `com.unity.2d.ik` — IK for arms/legs
- `com.unity.2d.psdimporter` — import layered `.psb`/`.psd` as separate sprites
- `com.unity.2d.aseprite` — only if you go pixel-art (not our style)

## The pipeline

### 1. Draw the character in layers (parts)
Each movable part on its own layer, roughly:
`head`, `face` (optional), `torso`, `upper-arm-L/R`, `lower-arm-L/R`,
`hand-L/R`, `thigh-L/R`, `shin-L/R`, `foot-L/R`.
- Vector tools: **Figma / Illustrator / Inkscape**. Raster: **Photoshop / Procreate / Krita**.
- Keep parts on a transparent background, in a single document, **don't merge layers**.
- Overlap joints slightly (e.g. upper arm tucks under the torso) so there are no gaps when posed.
- Export as **`.psb`** (or `.psd`) preserving layers.

### 2. Import as a riggable sprite
- Drop the `.psb` into `Assets/Plummet/Sprites/...`.
- In the importer: **Sprite Mode = Multiple**, **Use Layer Grouping** on, **Pixels Per Unit = 100** (match the other sprites), **Mosaic** on.

### 3. Rig it (Sprite Editor → Skinning Editor)
- **Create Bones** down the body (spine → head; shoulder → elbow → wrist; hip → knee → ankle).
- **Auto Geometry** to mesh the parts, then **Auto Weights** to bind mesh to bones.
- Clean up weights where parts bend. Add **2D IK** to arms/legs for easy posing.

### 4. Animate (Animation window)
Create one clip per state and keyframe **bone poses** (Unity tweens between them):
- `Idle` — standing on the trapdoor (subtle breathing optional)
- `Dive` — the tuck/transition as he leaves the trapdoor
- `Flail` — looping mid-air fall (4–8 keyframes is plenty; the tween fills the rest)
Keep `Flail` a clean loop (first and last keyframe match).

## Integrating with the player
The player currently swaps a `Sprite[] fallingFrames` array manually
(`PlayerController` / `Skin`). Two ways to use a rig:

**Option A — move the player to an Animator (best quality)**
- Replace the `SpriteRenderer` + frame array with the rigged sprite + an
  `Animator` holding `Idle`/`Dive`/`Flail`.
- `PlayerController` triggers clips instead of swapping sprites; the
  `IntroTransition` plays `Dive` then `Flail`.
- Skins become different rigged prefabs (or the same rig with swapped part art).

**Option B — bake the rig to frames (least disruption)**
- Pose the rig, **export the clip to PNG frames**, and feed them into the
  existing `mark-falling-flail-01..N.png` system.
- You keep today's code; the frames are just now perfectly consistent because
  they came from one rig. Good stopgap if you don't want to rewire the Animator.

## Reusing the rig across skins
Build the rig **once**; for Evie/Harrison, swap the **part art** onto the same
skeleton (same bone names + proportions). One set of animations drives every
character — no re-animating per kid.

## ChatGPT "parts sheet" prompt (if generating the base art)
Use AI for the **character design once**, not for motion. Attach a reference of
the existing character, then:

> "Draw this character as a **flat vector cartoon**, matching the attached
> reference's face, hair, outfit, colours, and proportions. Lay the body out as
> **separate, non-overlapping parts on one transparent canvas**: head, torso,
> upper arm L, lower arm L, hand L, upper arm R, lower arm R, hand R, thigh L,
> shin L, foot L, thigh R, shin R, foot R. Each part fully drawn (including the
> bits that tuck under others), clearly separated with space around it, front
> view, no shadows, no background. Square canvas, 2048×2048."

Then cut the parts onto layers in Figma/Photoshop, export `.psb`, and rig.

## Conventions
- Frame art (if baking): `<skin>-falling-flail-01.png` … zero-padded, cycle order.
- Keep one canonical character height; the player normalizes by **visible**
  height, so consistent framing keeps the animation from jittering.
- Transparent PNGs always; if a fill is unavoidable use green `#00FF00`
  (protects white shoes/cuffs), then run `Tools/strip_green.py`.

## Recommendation for Plummet
The packages are already here and the style is flat-vector — so **rig Mark once
in Unity 2D Animation, build `Idle`/`Dive`/`Flail`, and reuse the rig for Evie
and Harrison.** Highest quality, far fewer prompts, and new animations become a
quick pose job instead of a generation marathon.
