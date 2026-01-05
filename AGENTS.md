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

## Mods

### HemSoft_QoL

Quality of Life inventory hotkeys mod.

**Features**:
- **Alt+Q**: Quick Stack - deposits items matching existing container stacks
- **Alt+X**: Stash All - deposits all backpack items to container
- **Alt+R**: Restock - pulls items from container to fill partial stacks
- **Alt+Z**: Loot All - takes everything from container

**Key Files**:
- `HemSoft_QoL/Harmony/HemSoftQoL.cs` - IModApi entry point, config loading
- `HemSoft_QoL/Harmony/InventoryHotkeyPatches.cs` - Harmony patches and inventory logic
- `HemSoft_QoL/Config/HemSoftQoL.xml` - User-configurable hotkey bindings
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
| `XUiC_BackpackWindow` | Player backpack UI - patch `Update` for hotkey input |
| `XUi.lootContainer` | Currently open container (`ITileEntityLootable`) |
| `XUi.PlayerInventory` | Player inventory model |
| `ITileEntityLootable.items` | Direct array access to container slots |
| `ITileEntityLootable.TryStackItem()` | Returns `(bool anyMoved, bool allMoved)` tuple |
| `ITileEntityLootable.UpdateSlot()` | Update a specific container slot |
| `Bag.GetSlots()` / `Bag.SetSlot()` | Backpack slot access |

### Important Notes
- `TryStackItem()` returns a **tuple**, not a bool - use `.anyMoved` or `.allMoved`
- Harmony is located in `Mods\0_TFP_Harmony`, not in Managed folder
- EAC must be disabled to run DLL mods
- Use `Input.GetKey()` / `Input.GetKeyDown()` for hotkey detection

## Testing

1. Build: `dotnet build -c Release`
2. Deploy: `.\deploy.ps1` (or manually copy to Mods folder)
3. Launch 7D2D with **EAC disabled** (select in launcher)
4. Check console (F1) for `[HemSoft QoL]` log messages
5. Open any container and test hotkeys

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
