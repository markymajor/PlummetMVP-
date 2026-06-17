# Plummet Free Fall MVP Setup

This folder is a first playable Unity 2D mobile MVP built from the recovered Plummet assets.

## Folder Layout

- `Assets/Plummet/Sprites/Game/` - player, background, title, obstacles, and shaft details.
- `Assets/Plummet/Sprites/UI/` - game-over art and buttons.
- `Assets/Plummet/Sprites/Icons/` - app icon destination folder.
- `Assets/Plummet/Scripts/` - runtime C# scripts.
- `Assets/Plummet/Scripts/Editor/` - scene builder menu item.
- `Assets/Plummet/Prefabs/` - generated obstacle prefabs.
- `Assets/Plummet/Scenes/` - generated `PlummetMVP.unity` scene.

## Build The Scene

1. Create or open a Unity 2D project.
2. Copy the `Assets/Plummet` folder into the project's `Assets` folder.
3. Wait for Unity to import the sprites and scripts.
4. In Unity, choose `Plummet > Build MVP Scene`.
5. Open `Assets/Plummet/Scenes/PlummetMVP.unity`.
6. Press Play.

The builder imports PNGs as sprites, creates the camera, player, walls, scrolling background, obstacle pool, UI canvas, buttons, and scene references.

## Editor Testing

- Press Play.
- Click/tap the start screen or press Space to open the instruction flow, then tap through to begin.
- Use left/right arrow keys or A/D to move the player.
- Mouse drag/click also acts as the touch fallback.
- Colliding with a wall or spawned obstacle triggers game over.
- Reset restarts the run. Home returns to the start screen.

## Mobile Testing

1. Open `File > Build Settings`.
2. Add `PlummetMVP.unity` to Scenes In Build.
3. Switch platform to iOS or Android.
4. Set orientation to portrait in Player Settings.
5. Build and run on a phone.

On device, tilt left/right to steer. Touch input remains available as a fallback for non-tilt testing.

## MVP Notes

- `mark.png` is the standing start-screen player.
- `mark-falling-flail-01.png` through `mark-falling-flail-08.png` are the gameplay falling animation frames.
- The falling animation is a simple flipbook driven by `PlayerController.cs`.
- `Background.png` scrolls upward to simulate falling.
- Obstacles and details are pooled and recycled.
- Score increases with distance fallen.
- Difficulty increases by raising scroll speed and reducing spawn intervals.
- High score is stored locally with `PlayerPrefs`.
- The share button is a placeholder for native sharing later.
