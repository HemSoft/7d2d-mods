# HemSoft QoL

Quality of Life improvements for **7 Days to Die V2.5+**

## Features

### Inventory Hotkeys
Keyboard shortcuts for inventory management when a container is open:

| Hotkey | Action | Description |
|--------|--------|-------------|
| `Q` | **Quick Stack** | Deposit items that match existing stacks in the container |
| `Alt+X` | **Stash All** | Deposit all items from your backpack into the container |
| `Alt+R` | **Restock** | Pull items from container to fill your existing stacks |
| `S` | **Sort Container** | Sort container items alphabetically |
| `Alt+M` | **Sort Inventory** | Sort your backpack alphabetically (disabled by default) |

> **Note:** Vanilla uses `R` for Loot All. All hotkeys are fully configurable!

### HUD Info Panel
Displays real-time player stats on your HUD:

- **Level** - Your current player level
- **Gamestage** - Affects enemy difficulty and spawns
- **Lootstage** - Affects loot quality
- **Day** - Current in-game day
- **Blood Moon** - Countdown to next blood moon (color-coded!)
- **Kills** - Total zombie kills
- **Nearest Enemy** - Distance and name of closest hostile (color-coded by distance!)
- **Enemy Count** - Number of hostiles within 100m

### Console Commands
Type `hs` in the console (F1) to:
- View current settings and hotkey configuration
- Toggle individual info panel elements: `hs level`, `hs enemy`, etc.
- Show/hide all: `hs all` or `hs none`

### Panel Positioning
- **Alt+P** - Toggle position mode
- **Arrow Keys** - Move panel (hold Shift for faster)
- **Alt+P** again - Save position and exit

Position is saved to `Config/InfoPanelPosition.xml` and persists across sessions.

## Installation

1. **Disable EAC** - DLL mods require Easy Anti-Cheat to be disabled
2. Copy the `HemSoft_QoL` folder to your `7 Days to Die/Mods/` directory
3. Launch the game

## Configuration

### Hotkeys
Edit `Config/HemSoftQoL.xml` to customize hotkeys:

```xml
<Hotkeys>
  <QuickStack enabled="true" modifier="None" key="Q" />
  <StashAll enabled="true" modifier="LeftAlt" key="X" />
  <Restock enabled="true" modifier="LeftAlt" key="R" />
  <SortContainer enabled="true" modifier="None" key="S" />
  <SortInventory enabled="false" modifier="LeftAlt" key="M" />
</Hotkeys>
```

### Info Panel Display
Edit `Config/InfoPanelConfig.xml` or use the `hs` console command to toggle elements.

### Available Modifiers
- `None` - No modifier required
- `LeftAlt`, `RightAlt`
- `LeftControl`, `RightControl`
- `LeftShift`, `RightShift`

### Key Codes
Any valid [Unity KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) (e.g., `Q`, `F1`, `Space`)

## Building from Source

1. Install .NET SDK 8.0+
2. Update `GamePath` in `HemSoft_QoL.csproj` to your 7D2D install location
3. Run `dotnet build -c Release`
4. Run `.\deploy.ps1` to copy to game Mods folder

## Requirements

- 7 Days to Die V2.5+
- EAC Disabled

## License

MIT License - © 2026 HemSoft Developments

## Changelog

### v1.2.0
- Added Nearest Enemy tracking with distance and color-coded warnings
- Added Enemy Count display (hostiles within 100m)
- Added Sort Container hotkey (S by default)
- Added Sort Inventory hotkey (Alt+M, disabled by default)
- Added `hs` console command for settings management
- Dynamic hotkey display - shows actual configured keys
- Improved enemy name display (strips "zombie" and "animalzombie" prefixes)
- Changed Quick Stack default to just Q (no modifier)

### v1.1.0
- Added HUD Info Panel with Level, Gamestage, Lootstage, Day, Blood Moon, Kills
- Added panel repositioning with Alt+P and arrow keys
- Position persists across game sessions

### v1.0.0
- Initial release
- Inventory hotkeys: Quick Stack, Stash All, Restock
