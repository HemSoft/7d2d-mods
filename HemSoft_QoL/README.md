# HemSoft QoL

Quality of Life improvements for **7 Days to Die V2.5+**

## Features

### Inventory Hotkeys
Adds keyboard shortcuts for inventory management when a container is open:

| Hotkey | Action | Description |
|--------|--------|-------------|
| `Alt + Q` | **Quick Stack** | Deposit items that match existing stacks in the container |
| `Alt + X` | **Stash All** | Deposit all items from your backpack into the container |
| `Alt + R` | **Restock** | Pull items from container to fill your existing stacks |
| `Alt + Z` | **Loot All** | Take all items from the container |

## Installation

1. **Disable EAC** - DLL mods require Easy Anti-Cheat to be disabled
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

1. Install .NET SDK 8.0+
2. Update `GamePath` in `HemSoft_QoL.csproj` to your 7D2D install location
3. Run `dotnet build`
4. The DLL will be copied to the mod folder automatically

## Requirements

- 7 Days to Die V2.5+
- EAC Disabled

## License

MIT License - © 2026 HemSoft Developments

## Changelog

### v1.0.0
- Initial release
- Inventory hotkeys: Quick Stack, Stash All, Restock, Loot All
