# HemSoft QoL

Quality of Life improvements for **7 Days to Die V2.5+**

![HemSoft QoL Banner](https://raw.githubusercontent.com/fhemmer/7d2d-mods/main/HemSoft_QoL/images/banner.png)

## Features

### Inventory Hotkeys
Adds keyboard shortcuts for inventory management when a container is open:

| Hotkey | Action | Description |
|--------|--------|-------------|
| `Alt + Q` | **Quick Stack** | Deposit items that match existing stacks in the container |
| `Alt + X` | **Stash All** | Deposit all items from your backpack into the container |
| `Alt + R` | **Restock** | Pull items from container to fill your existing stacks |
| `Alt + Z` | **Loot All** | Take all items from the container |

### HUD Info Panel
Displays real-time player stats on the HUD:

| Stat | Description |
|------|-------------|
| **Level** | Current player level |
| **Gamestage** | Affects enemy difficulty and spawns |
| **Lootstage** | Affects loot quality in containers |
| **Day** | Current in-game day |
| **Blood Moon** | Countdown to next blood moon horde |
| **Kills** | Total zombie kills |

### Panel Positioning
Customize where the info panel appears on your screen:

| Hotkey | Action |
|--------|--------|
| `Alt + P` | Toggle position mode |
| `Arrow Keys` | Move panel (5px per press) |
| `Shift + Arrow Keys` | Move panel faster (20px per press) |
| `Alt + P` | Exit position mode and save |

Position is saved automatically to `Config/InfoPanelPosition.xml` and persists across game sessions.

## Installation

1. **Disable EAC** - DLL mods require Easy Anti-Cheat to be disabled (select in Steam launcher)
2. Copy the `HemSoft_QoL` folder to your `7 Days to Die/Mods/` directory
3. Launch the game

## Configuration

Edit `Config/HemSoftQoL.xml` to customize hotkeys:

```xml
<Hotkeys>
  <QuickStack enabled="true" modifier="LeftAlt" key="Q" />
  <StashAll enabled="true" modifier="LeftAlt" key="X" />
  <Restock enabled="true" modifier="LeftAlt" key="R" />
  <LootAll enabled="true" modifier="LeftAlt" key="Z" />
</Hotkeys>
```

### Available Modifiers
- `None` - No modifier required
- `LeftAlt`, `RightAlt`
- `LeftControl`, `RightControl`
- `LeftShift`, `RightShift`

### Key Codes
Any valid [Unity KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) (e.g., `Q`, `F1`, `Space`)

## Building from Source

```powershell
cd HemSoft_QoL
dotnet build -c Release
.\deploy.ps1
```

Requires .NET SDK and 7 Days to Die installed for assembly references.

## Requirements

- 7 Days to Die V2.5+
- EAC Disabled

## Links

- [GitHub Repository](https://github.com/fhemmer/7d2d-mods)
- [Report Issues](https://github.com/fhemmer/7d2d-mods/issues)

## License

MIT License - © 2026 HemSoft Developments

## Changelog

### v1.1.0
- Added HUD Info Panel with Level, Gamestage, Lootstage, Day, Blood Moon, Kills
- Added panel repositioning with Alt+P and arrow keys
- Position persists across game sessions

### v1.0.0
- Initial release
- Inventory hotkeys: Quick Stack, Stash All, Restock, Loot All
