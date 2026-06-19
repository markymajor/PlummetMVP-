# Skin drop folder

Drop your kids' character images here, then run the menu
**Plummet ▸ Skins ▸ Process Dropped Art** in Unity.

## Naming matters
The file name (without extension) becomes the skin id, and the id must match
what the setup wires up. Currently expected:

| File name        | Becomes skin |
|------------------|--------------|
| `Harrison.png`   | Harrison (the boy)  |
| `Evie.png`       | Evie (the girl)     |

(Names are lower-cased automatically, so `Harrison.png` → `harrison`.)

## What the tool does
1. Auto-detects the screen colour (green/blue) from the image border.
2. Removes that background to transparency, with a soft edge + green despill.
3. Auto-crops to the character.
4. Writes the result to `Assets/Plummet/Sprites/Game/skins/<id>.png` and
   imports it as a Sprite.

Then run **Plummet ▸ Repair Open Scene** to register the skins and build the
"Choose Player" screen.

## Adding more kids later
1. Drop `<Name>.png` here.
2. In `PlummetSceneRepair.EnsureSkinLibrary`, add one line:
   `AddKidSkin(skins, "<name>", "<Name>");`
3. Run Process Dropped Art, then Repair Open Scene.

> Prefer the command line? `python3 Tools/strip_green.py --dir Assets/Plummet/SkinDrop Assets/Plummet/Sprites/Game/skins`
