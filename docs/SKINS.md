# Character skins (play as yourself)

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

Start screen -> **Players** button -> Choose Player screen -> tap a character
(highlights + previews the player) -> **Select** -> Play.

## Adding a kid (full workflow)

1. Save their image (any flat green/blue background) as
   `Assets/Plummet/SkinDrop/<Name>.png`, for example `Harrison.png` or `Evie.png`.
2. Unity menu: **Plummet -> Skins -> Process Dropped Art**
   (removes the background, trims, imports as a Sprite).
3. Unity menu: **Plummet -> Build Latest Playable**
   (registers the skins and rebuilds/refreshes the Choose Player screen).

To add a brand-new name beyond Harrison/Evie, also add one line in
`EnsureSkinLibrary`: `AddKidSkin(skins, "<name>", "<Name>");`

## Current skins

- **Mark** - the existing character (standing + 8 falling/flail frames).
- **Harrison** - the boy (single pose; drop `Harrison.png`).
- **Evie** - the girl (single pose; drop `Evie.png`).

Kid skins use a single pose for the fall because the art is one image. If you
later draw flail frames for them, drop them as a sheet and extend `AddKidSkin`
to load multiple frames.

## Notes

- The player preview and gameplay player both use the selected skin.
- Re-running **Build Latest Playable** is the normal way to pick up new skin art.
- Keep original asset file names obvious so we can discuss edits by asset name.
