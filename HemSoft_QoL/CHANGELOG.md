# Changelog

All notable changes to HemSoft QoL will be documented in this file.

## [1.6.0] - 2026-01-13

### Added
- **Lootstage Change Notification** - Non-blocking HUD notification when your lootstage changes
  - Displays "Lootstage: old → new" with color-coded message (green for increase, red for decrease)
  - 10-second display duration with 2-second fade-out
  - Console commands:
    - `hs lootpopup` - Toggle notifications on/off
    - `hs lootpos X Y` - Adjust notification position in real-time
  - Gears mod settings (if Gears is installed):
    - Toggle notification on/off
    - Adjust X position (0-3440)
    - Adjust Y position (0-1440)
    - Adjust notification width (400-1200)
    - Adjust title font size (18-72)
    - Adjust value font size (16-64)
    - Adjust info panel width (100-300)

## [1.5.0] - 2026-01-11

### Added
- Documented loot and resource harvesting event hooks for future features

## [1.4.0] - 2026-01-10

### Added
- **Vehicle Storage Support** - All inventory hotkeys now work with vehicle storage
  - Quick Stack, Stash All, Restock, and Sort all work when accessing vehicle inventory
  - Works with all vehicle types (motorcycles, 4x4s, gyrocopters, etc.)

### Fixed
- Vehicle storage hotkeys now properly access vehicle inventory instead of the wrong container

## [1.3.0] - 2026-01-08

### Added
- **Gears Integration** - In-game settings UI when Gears mod is installed
  - Configure all hotkeys (enable/disable, modifier keys, key bindings)
  - Toggle info panel elements on/off
  - Access via Main Menu → Mod Settings → HemSoft QoL
- Localization support for settings UI
- Automatic config source detection (Gears ModSettings.xml or legacy XML files)

### Changed
- Config loading now prioritizes ModSettings.xml when Gears is installed
- Console `hs` command shows Gears detection status
- Display settings sync to both ModSettings.xml and legacy config files

### Note
- Gears mod is optional but recommended: https://github.com/s7092910/Gears
- Without Gears, edit Config/HemSoftQoL.xml and Config/InfoPanelConfig.xml manually

## [1.3.0] - 2026-01-08

### Changed
- Renamed deployed mod folder to `S_HemSoft_QoL` to avoid conflicts with another mod
- The `S_` prefix also ensures proper load order

## [1.2.0] - 2026-01-04

### Added
- **Nearest Enemy** tracking with distance and color-coded warnings
- **Enemy Count** display (hostiles within 100m)
- **Sort Container** hotkey (`S` by default)
- **Sort Inventory** hotkey (`Alt+M`, disabled by default)
- `hs` console command for settings management
- Dynamic hotkey display - shows actual configured keys

### Changed
- Improved enemy name display (strips "zombie" and "animalzombie" prefixes)
- Changed Quick Stack default to just `Q` (no modifier)

## [1.1.0] - 2026-01-02

### Added
- **HUD Info Panel** with Level, Gamestage, Lootstage, Day, Blood Moon, Kills
- Panel repositioning with `Alt+P` and arrow keys
- Position persists across game sessions (saved to `Config/InfoPanelPosition.xml`)

## [1.0.0] - 2026-01-01

### Added
- Initial release
- **Quick Stack** (`Q`) - deposits items matching existing container stacks
- **Stash All** (`Alt+X`) - deposits all backpack items to container
- **Restock** (`Alt+R`) - pulls items from container to fill partial stacks
