# 7D2D Mods - Agent Instructions

## Project Overview

This repository contains mods for **7 Days to Die V2.5** (PC/Steam). All mods use **Harmony** for runtime patching and target **.NET Framework 4.8**.

**Repository**: https://github.com/fhemmer/7d2d-mods
**Local Path**: `D:\github\HemSoft\7d2d-mods`
**SSH Remote**: `git@github-personal1:fhemmer/7d2d-mods.git`

## Game Installation

- **Game Path**: `C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die`
- **Managed DLLs**: `{GamePath}\7DaysToDie_Data\Managed`
- **Harmony DLL**: `{GamePath}\Mods\0_TFP_Harmony\0Harmony.dll` (V2.5 ships Harmony as a mod)
- **Mods Folder**: `{GamePath}\Mods`
- **Game Logs**: `%APPDATA%\7DaysToDie\logs\`

## Mods

### HemSoft_QoL (v1.2.0)

Quality of Life mod with inventory hotkeys, HUD info panel, and enemy tracking.

**Nexus Mods**: https://www.nexusmods.com/7daystodie/mods/9332

**Inventory Hotkeys** (when container is open):
- **Q**: Quick Stack - deposits items matching existing container stacks
- **Alt+X**: Stash All - deposits all backpack items to container
- **Alt+R**: Restock - pulls items from container to fill partial stacks
- **S**: Sort Container - sorts container items alphabetically
- **Alt+M**: Sort Inventory - sorts backpack items (disabled by default)

> **Note:** Vanilla uses `R` for Loot All. Set `enabled="false"` in config to disable any hotkey.

**HUD Info Panel**:
- Displays Level, Gamestage, Lootstage, Day, Blood Moon countdown, Kills
- **Nearest Enemy**: Shows closest hostile with distance (color-coded by danger)
- **Enemy Count**: Number of hostiles within 100m
- **Alt+P**: Toggle position mode (Arrow Keys to move, Shift for faster)
- Position saved to `Config/InfoPanelPosition.xml`

**Console Commands** (`hs` or `hemsoft`):
- `hs` - Show current settings and hotkey configuration
- `hs level` / `hs enemy` / etc. - Toggle individual panel elements
- `hs all` / `hs none` - Show/hide all panel elements

**Key Files**:
- `HemSoft_QoL/Harmony/HemSoftQoL.cs` - IModApi entry point, config loading
- `HemSoft_QoL/Harmony/InventoryHotkeyPatches.cs` - Harmony patches and inventory logic
- `HemSoft_QoL/Harmony/HemSoftInfoPanel.cs` - XUiController for HUD panel
- `HemSoft_QoL/Harmony/HemSoftConfigWindow.cs` - Console command handler
- `HemSoft_QoL/Config/HemSoftQoL.xml` - User-configurable hotkey bindings
- `HemSoft_QoL/Config/InfoPanelConfig.xml` - Info panel display settings
- `HemSoft_QoL/Config/XUi/windows.xml` - XUi panel definition (appends to HUD)
- `HemSoft_QoL/ModInfo.xml` - Mod metadata for game loader

**Build & Deploy**:
```powershell
cd HemSoft_QoL
dotnet build -c Release
.\deploy.ps1
```

## Build Requirements

- .NET SDK (any version that supports net48 targeting)
- Game must be installed for assembly references

## Key 7D2D V2.5 APIs

### Inventory/Container Classes
| Class | Purpose |
|-------|---------|
| `XUiC_BackpackWindow` | Player backpack UI - patch `XUiController.Update` for hotkey input |
| `XUi.lootContainer` | Currently open container (`ITileEntityLootable`) |
| `XUi.PlayerInventory` | Player inventory model |
| `ITileEntityLootable.items` | Direct array access to container slots |
| `ITileEntityLootable.TryStackItem()` | Returns `(bool anyMoved, bool allMoved)` tuple |
| `ITileEntityLootable.UpdateSlot()` | Update a specific container slot |
| `Bag.GetSlots()` / `Bag.SetSlot()` | Backpack slot access |

### Player Stats Access
| Property | Access |
|----------|--------|
| Level | `player.Progression?.Level` |
| Gamestage | `player.gameStage` |
| Lootstage | `player.GetHighestPartyLootStage(0f, 0f)` |
| Day | `GameUtils.WorldTimeToDays(world.worldTime)` |
| Blood Moon Freq | `GamePrefs.GetInt(EnumGamePrefs.BloodMoonFrequency)` |

### Important Notes
- `TryStackItem()` returns a **tuple**, not a bool - use `.anyMoved` or `.allMoved`
- Harmony is located in `Mods\0_TFP_Harmony`, not in Managed folder
- EAC must be disabled to run DLL mods
- Use `Input.GetKey()` / `Input.GetKeyDown()` for hotkey detection

### XUi Controller Notes
- Override `GetBindingValueInternal` (NOT `GetBindingValue`) for custom bindings
- Controller XML attribute requires **full namespace**: `Namespace.ClassName, AssemblyName`
- Use `ViewComponent.UiTransform.localPosition` for runtime repositioning
- Check game logs for `Type was missing` errors if controller doesn't load

### Vanilla Keybindings (Don't Duplicate)
| Key | Action |
|-----|--------|
| `R` | Loot All (transfers all items from container to backpack) |
| `Shift+Click` | Transfer single stack between container/backpack |
| `Ctrl+Click` | Split stack |

### Sorting Implementation
```csharp
// Get non-empty items, sort alphabetically, rebuild array
var items = container.items.Where(i => !i.IsEmpty()).ToList();
items.Sort((a, b) => a.GetItemName().CompareTo(b.GetItemName()));
for (int i = 0; i < container.items.Length; i++) {
    container.items[i] = i < items.Count ? items[i] : ItemStack.Empty.Clone();
    container.UpdateSlot(i, container.items[i]);
}
```

### Config Pattern for Optional Hotkeys
Empty `key=""` disables a hotkey:
```xml
<SortInventory modifier="LeftAlt" key="" />  <!-- Disabled -->
<SortContainer modifier="LeftAlt" key="C" /> <!-- Enabled -->
```

## Testing

1. Build: `dotnet build -c Release`
2. Deploy: `.\deploy.ps1` (or manually copy to Mods folder)
3. Launch 7D2D with **EAC disabled** (select in launcher)
4. Check console (F1) for `[HemSoft QoL]` log messages
5. Check logs at `%APPDATA%\7DaysToDie\logs\` for errors

> **⚠️ IMPORTANT**: Never stop or kill the game process without asking first! The user may have unsaved progress.

## Reference Resources

- **SphereII.Mods (SCore)**: https://github.com/SphereII/SphereII.Mods - extensive Harmony examples
- **7D2D Modding Wiki**: https://7daystodie.fandom.com/wiki/Modding
- **Nexus Mods**: https://www.nexusmods.com/7daystodie/mods

## Git Workflow

Committer: HemSoft (franz_hemmer@hotmail.com)
SSH Host: `github-personal1`

```powershell
git add .
git commit -m "Description of changes"
git push
```
