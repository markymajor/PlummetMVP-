# Plummet Free Fall MVP Setup

This folder is a first playable Unity 2D mobile MVP built from the recovered Plummet assets.

## Folder Layout

- `Assets/Plummet/Sprites/Game/` - player, background, title, obstacles, and shaft details.
- `Assets/Plummet/Sprites/UI/` - end-state art, instruction screens, and buttons.
- `Assets/Plummet/Sprites/Icons/` - app icon destination folder.
- `Assets/Plummet/Scripts/` - runtime C# scripts.
- `Assets/Plummet/Scripts/Editor/` - Unity editor menu tools.
- `Assets/Plummet/Prefabs/` - generated obstacle prefabs.
- `Assets/Plummet/Scenes/` - source scene: `PlummetMVP.unity`.

## Build The Latest Playable

1. Open the Unity project.
2. Open `Assets/Plummet/Scenes/PlummetMVP.unity`.
3. In Unity, choose `Plummet > Build Latest Playable`.
4. Press Play.

`PlummetMVP.unity` is the source of truth. Do not use or commit accidental scenes such as `Assets/Save.unity`.

The builder imports PNGs as sprites, refreshes the camera, player, shaft, scrolling path, obstacle pool, UI canvas, buttons, skins, and scene references.

## Editor Testing

- Press Play.
- On a fresh profile, tap through the two instruction screens, then Start.
- Use left/right arrow keys or A/D to move the player.
- Mouse drag/click also acts as the touch fallback.
- Colliding with a wall or spawned obstacle triggers the RESCUED screen.
- Reset restarts the run. Home returns to the start screen.

## Mobile Testing

1. Open `File > Build Settings`.
2. Confirm `Assets/Plummet/Scenes/PlummetMVP.unity` is in Scenes In Build.
3. Switch platform to iOS or Android.
4. Set orientation to portrait in Player Settings.
5. Build and run on a phone.

On device, tilt left/right to steer. Touch input remains available as a fallback for non-tilt testing.

## Preflight

Before pushing a latest playable:

- Run `bash Tools/plummet-review.sh`.
- Run Unity EditMode tests if the editor bridge is available.
- Walk through `docs/ACCEPTANCE_CHECKLIST.md`.

## MVP Notes

- `mark.png` is the standing start-screen player.
- `mark-falling-flail-sheet.png` is the clean 8-frame horizontal sprite sheet: 2048x256, with 256x256 transparent frames.
- `mark-falling-flail-01.png` through `mark-falling-flail-08.png` are the matching gameplay falling animation frame exports.
- The falling animation is a simple flipbook driven by `PlayerController.cs`.
- The shaft and wall path scroll upward to simulate falling.
- Obstacles and details are pooled and recycled.
- Score increases with distance fallen.
- Difficulty increases by raising scroll speed and reducing spawn intervals.
- High score and selected skin are stored locally with `PlayerPrefs`.
- The share button is a placeholder for native sharing later.
