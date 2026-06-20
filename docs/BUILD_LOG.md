# Plummet Build Log

## Current MVP Direction

The current build is a Unity 2D portrait-mode remake focused on a first playable:
start screen, two opening instruction screens, trapdoor drop, falling player,
scrolling shaft, path/wall collision, score, high score, and game over.

## Workflow Decision

Use one normal Unity command:

`Plummet > Build Latest Playable`

Older helpers live under `Plummet > Legacy` or `Plummet > Tools`.
They are not the day-to-day route.

## Known Local Caution

If Unity shows a scene named `Save`, do not repair that scene.
Open `Assets/Plummet/Scenes/PlummetMVP.unity`, then run the latest build command.

## Recent Cleanup

- Added a single top-level build/repair command.
- Made current-scene repair refuse to run outside `PlummetMVP.unity`.
- Added edit-mode tests for build settings and core scene wiring.
- Added prompt and acceptance docs for repeatable AI-assisted edits.
- Added `tools/plummet-review.sh` for quick static checks before a push.

## Deferred By Choice

Obstacle gameplay remains in place for now.
Do not remove or convert it unless specifically requested.
