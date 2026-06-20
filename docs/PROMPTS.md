# Plummet Prompt Library

Use these prompts to keep edits focused and comparable between sessions.

## Gameplay Feel Pass

Review the current Unity Plummet MVP against `docs/PLUMMET_SPEC.md`.
Prioritize the falling feel, corridor readability, score clarity, and mobile portrait play.
Do not change obstacle behavior unless explicitly asked.
Return findings first, then implement the smallest safe fix.

## Start Screen Pass

Compare the start screen against the recovered 2014 references.
Keep the mobile portrait frame centered.
Preserve the assets and names in `Assets/Plummet/Sprites/Game`.
Fix only layout, ordering, scale, color, or tap flow issues.

## Build Repair Pass

Use `Plummet > Build Latest Playable`.
The command must target `Assets/Plummet/Scenes/PlummetMVP.unity`.
After running it, check the Console for compile errors, missing references, and scene drift.

## Sprite/Asset Pass

Preserve original file names.
For sprites used with tiled renderers or tiled UI images, use Full Rect mesh type.
For pixel-style recovered assets, keep mipmaps off and avoid unnecessary compression.

## Review Before Push

Before pushing:
- Run `tools/plummet-review.sh`.
- Run edit-mode tests if Unity is available.
- Confirm `Assets/Plummet/Scenes/PlummetMVP.unity` is the scene in Build Settings.
- Check for accidental scenes such as `Assets/Save.unity`.
- Commit only intentional changes.
