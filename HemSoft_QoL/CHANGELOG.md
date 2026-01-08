# Changelog

All notable changes to HemSoft QoL will be documented in this file.

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
