# HemSoft 7D2D Mods

Quality of Life mods for **7 Days to Die V2.5** by HemSoft Developments.

## Mods

### [HemSoft_QoL](./HemSoft_QoL/)

A comprehensive QoL mod featuring inventory hotkeys and a customizable HUD info panel.

#### Inventory Hotkeys
| Hotkey | Action |
|--------|--------|
| **Alt+Q** | Quick Stack - deposits items matching existing container stacks |
| **Alt+X** | Stash All - deposits all backpack items to container |
| **Alt+R** | Restock - pulls items from container to fill partial stacks |
| **Alt+Z** | Loot All - takes everything from container |

#### HUD Info Panel
Displays real-time player stats on the HUD:
- **Level** - Current player level
- **Gamestage** - Current gamestage (affects enemy difficulty)
- **Lootstage** - Effective loot stage (affects loot quality)
- **Day** - Current in-game day
- **Blood Moon** - Countdown to next blood moon
- **Kills** - Total zombie kills

#### Panel Positioning
| Hotkey | Action |
|--------|--------|
| **Alt+P** | Toggle position mode |
| **Arrow Keys** | Move panel (5px per press) |
| **Shift+Arrows** | Move panel faster (20px per press) |
| **Alt+P** | Exit and save position |

Position is saved automatically and persists across game sessions.

## Requirements

- 7 Days to Die **V2.5+**
- **EAC must be disabled** for DLL mods (select in Steam launcher)

## Installation

1. Download the latest release
2. Extract the mod folder to your `7 Days to Die/Mods/` directory
3. Launch the game with **EAC disabled**

## Configuration

Edit `Config/HemSoftQoL.xml` to customize hotkey bindings:

```xml
<Hotkeys>
  <QuickStack enabled="true" modifier="LeftAlt" key="Q" />
  <StashAll enabled="true" modifier="LeftAlt" key="X" />
  <Restock enabled="true" modifier="LeftAlt" key="R" />
  <LootAll enabled="true" modifier="LeftAlt" key="Z" />
</Hotkeys>
```

## Building from Source

```powershell
cd HemSoft_QoL
dotnet build -c Release
.\deploy.ps1
```

Requires .NET SDK and 7 Days to Die installed for assembly references.

## Links

- [Nexus Mods](https://www.nexusmods.com/7daystodie/mods/) *(coming soon)*
- [GitHub Repository](https://github.com/fhemmer/7d2d-mods)
- [Report Issues](https://github.com/fhemmer/7d2d-mods/issues)

## License

MIT License - © 2026 HemSoft Developments
