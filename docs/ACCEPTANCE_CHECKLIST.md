# Plummet Acceptance Checklist

Run this before calling a build "latest playable".

## Unity Setup

- Run `tools/plummet-review.sh`.
- Open `Assets/Plummet/Scenes/PlummetMVP.unity`.
- Run `Plummet > Build Latest Playable`.
- Confirm there are no red Console errors.
- Confirm Build Settings includes `Assets/Plummet/Scenes/PlummetMVP.unity`.

## Start Flow

- Game opens in a centered 9:16 portrait phone frame.
- The start screen shows the sky, title, standing Mark, red building, wall, skyscraper, and shaft.
- The two instruction screens appear only during the opening flow.
- Returning home from game over does not show the instruction screens again during the same app session.
- Tapping Start plays the trapdoor drop and hands off into gameplay.

## Gameplay

- Falling Mark appears in the shaft using the flailing falling animation.
- Score is visible at the top in the orange/cream Plummet style.
- The shaft scrolls upward while the player stays near the play area.
- The corridor changes shape over time.
- Player can move with keyboard in editor and touch/tilt on device.

## Failure And Reset

- Touching a wall triggers game over.
- Game over shows recovered `gameover.png`, `death.png`, reset, home, and share buttons.
- Reset starts a fresh run.
- Home returns to the start screen.
- High score persists between runs.

## Source Control

- No accidental `Assets/Save.unity` scene is included.
- Only intentional scene, prefab, script, sprite, doc, and meta changes are staged.
