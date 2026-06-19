# Character skins ("play as yourself")

Kids pick a character on a dedicated **Choose Player** screen, and the falling
player uses that skin. The choice is saved between runs.

## How it fits together
| Piece | File | Role |
|-------|------|------|
| `Skin` | `Scripts/Skin.cs` | One character: standing sprite + falling frames |
| `SkinLibrary` | `Scripts/SkinLibrary.cs` | Scene registry of all skins (singleton) |
| `SkinSelection` | `Scripts/SkinSelection.cs` | Saves the chosen index in PlayerPrefs |
| `SkinPickerUI` | `Scripts/SkinPickerUI.cs` | Builds the card grid at runtime, applies the pick |
| `PlayerController.ApplySelectedSkin()` | `Scripts/PlayerController.cs` | Swaps the player's sprite/frames |
| Setup + art tool | `Scripts/Editor/PlummetSceneRepair.cs` | Wires the library + screen, strips green screens |

## Flow in game
Start screen → **Players** button → Choose Player screen → tap a character
(highlights + re-skins the player) → **Back** → Play.

## Adding a kid (full workflow)
1. Save their image (any flat green/blue background) as
   `Assets/Plummet/SkinDrop/<Name>.png` — e.g. `Harrison.png`, `Evie.png`.
2. Unity menu: **Plummet ▸ Skins ▸ Process Dropped Art**
   (removes the background, trims, imports as a Sprite).
3. Unity menu: **Plummet ▸ Repair Open Scene**
   (registers the skins and builds/refreshes the Choose Player screen).

To add a brand-new name beyond Harrison/Evie, also add one line in
`EnsureSkinLibrary`: `AddKidSkin(skins, "<name>", "<Name>");`

## Current skins
- **Mark** — the existing character (standing + 8 falling/flail frames).
- **Harrison** — the boy (single pose; drop `Harrison.png`).
- **Evie** — the girl (single pose; drop `Evie.png`).

> Kid skins use a single pose for the fall (no flail frames) because the art is
> one image. If you later draw flail frames for them, drop them as a sheet and
> extend `AddKidSkin` to load multiple frames.

## Notes / not-yet
- The **start-screen standing character** still shows Mark's art; the skin is
  applied to the in-shaft player. Re-skinning the start preview is a small
  follow-up (swap the "Standing Mark" image to the selected skin's standing
  sprite on `ShowChooseSkin`/`ShowStart`).
- All UI is built by the editor tool, so re-running **Repair Open Scene** is the
  way to pick up changes — verify the screen in the editor after running it.
