using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using HemSoft.QoL;

/// <summary>
/// Custom HUD Info Panel controller that displays player stats.
/// Provides data bindings for level, gamestage, day, blood moon, and kills.
/// Supports repositioning via Alt+P + Arrow Keys.
/// NOTE: This class is intentionally NOT in a namespace because 7D2D XUi
/// expects controller class names without namespace prefixes for Type.GetType().
/// </summary>
public class XUiC_HemSoftInfoPanel : XUiController
{
    private EntityPlayerLocal _player;
    private bool _isDirty = true;

    // Cached values
    private int _cachedLevel;
    private int _cachedGamestage;
    private int _cachedLootstage;
    private int _cachedDay;
    private int _cachedZombieKills;
    private int _cachedBloodMoonDay;
    private float _cachedNearestEnemyDist = -1f;
    private string _cachedNearestEnemyName = "";
    private int _cachedEnemyCount;
    private string _cachedBiomeName = "";
    private string _cachedPOIName = "";
    private int _cachedPOITier = 0;
    private string _cachedCoords = "";
    private string _cachedSession = "";
    private float _sessionStartTime = -1f;  // Track session start time (game doesn't persist total time)
    private float _updateTimer;
    private const float UpdateInterval = 0.25f;
    
    // Config hot-reload
    private float _configCheckTimer;
    private const float ConfigCheckInterval = 2.0f; // Check every 2 seconds
    private static System.DateTime _lastConfigModified = System.DateTime.MinValue;
    private static bool _configInitialized;
    
    // Reusable list to avoid GC allocations
    private readonly List<Entity> _nearbyEntities = new List<Entity>(64);

    // Position mode
    private static bool _positionMode;
    private static Vector2i _customPosition = new Vector2i(190, 170);
    private static int _customWidth = 140; // Custom panel width
    private static float _zoomScale = 1.0f; // Zoom scale (1.0 = 100%, 1.5 = 150%)
    private static bool _positionLoaded;
    private const int MoveStep = 5;
    private const int MoveStepFast = 20;
    private const int WidthStep = 10;
    private const int WidthMin = 100;
    private const int WidthMax = 300;
    private const float ZoomStep = 0.1f;
    private const float ZoomMin = 0.5f;
    private const float ZoomMax = 3.0f;
    
    // Layout constants
    private const int TitleHeight = 28;
    private const int RowHeight = 20;
    private const int PanelPadding = 6;
    
    // Track if layout needs update
    private bool _layoutDirty = true;

    public override void Init()
    {
        base.Init();
        _isDirty = true;
        _layoutDirty = true;

        if (!_positionLoaded)
        {
            LoadPosition();
            _positionLoaded = true;
        }

        ApplyPosition();
        ApplyZoom();
        HemSoftQoL.Log("InfoPanel controller initialized");
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);

        if (!XUi.IsGameRunning()) return;

        // Check for config changes (hot-reload from Gears settings)
        _configCheckTimer += _dt;
        if (_configCheckTimer >= ConfigCheckInterval)
        {
            _configCheckTimer = 0f;
            CheckConfigReload();
        }

        // Handle zoom (Ctrl++ and Ctrl+-)
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                _zoomScale = Mathf.Clamp(_zoomScale + ZoomStep, ZoomMin, ZoomMax);
                ApplyZoom();
                SavePosition();
                HemSoftQoL.Log($"Panel zoom: {(_zoomScale * 100):F0}%");
            }
            else if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                _zoomScale = Mathf.Clamp(_zoomScale - ZoomStep, ZoomMin, ZoomMax);
                ApplyZoom();
                SavePosition();
                HemSoftQoL.Log($"Panel zoom: {(_zoomScale * 100):F0}%");
            }
        }

        // Handle position mode toggle (Alt+P)
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            _positionMode = !_positionMode;
            if (_positionMode)
                HemSoftQoL.Log("Position mode ON - Arrow Keys: move | +/-: width | Shift: faster | Alt+P: save & exit");
            else
            {
                SavePosition();
                HemSoftQoL.Log("Position mode OFF - Position and width saved.");
            }
        }

        // Handle arrow key movement and width adjustment in position mode
        if (_positionMode)
        {
            var step = Input.GetKey(KeyCode.LeftShift) ? MoveStepFast : MoveStep;
            var moved = false;
            var resized = false;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                _customPosition.x -= step;
                moved = true;
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                _customPosition.x += step;
                moved = true;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _customPosition.y += step;
                moved = true;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _customPosition.y -= step;
                moved = true;
            }
            
            // Width adjustment with + and - keys
            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                _customWidth = Mathf.Clamp(_customWidth + WidthStep, WidthMin, WidthMax);
                resized = true;
            }
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                _customWidth = Mathf.Clamp(_customWidth - WidthStep, WidthMin, WidthMax);
                resized = true;
            }

            if (moved)
                ApplyPosition();
            if (resized)
            {
                _layoutDirty = true;
                UpdateLayout(); // Immediately apply width changes
                SavePosition(); // Save new width to XML
                HemSoftQoL.Log($"Panel width: {_customWidth}");
            }
        }

        // Get player reference
        if (_player == null)
        {
            _player = xui?.playerUI?.entityPlayer;
            if (_player == null) return;
            _isDirty = true;
        }

        // Throttle updates
        _updateTimer += _dt;
        if (_updateTimer < UpdateInterval) return;
        _updateTimer = 0f;

        // Update cached values
        var world = GameManager.Instance?.World;
        if (world == null) return;

        var newLevel = _player.Progression?.Level ?? 1;
        var newGamestage = _player.gameStage;
        var newLootstage = _player.GetHighestPartyLootStage(0f, 0f);
        var newDay = GameUtils.WorldTimeToDays(world.worldTime);
        var newKills = _player.KilledZombies;

        if (newLevel != _cachedLevel || newGamestage != _cachedGamestage ||
            newLootstage != _cachedLootstage || newDay != _cachedDay || newKills != _cachedZombieKills)
        {
            _cachedLevel = newLevel;
            _cachedGamestage = newGamestage;
            _cachedLootstage = newLootstage;
            _cachedDay = newDay;
            _cachedZombieKills = newKills;
            _cachedBloodMoonDay = CalculateNextBloodMoon(newDay);
            _isDirty = true;
        }

        // Update nearest enemy (always recalculate as enemies move)
        UpdateNearestEnemy(world);
        
        // Update location info (biome, POI, coords)
        UpdateLocationInfo(world);
        
        // Update session time
        UpdateSession(world);

        if (_isDirty)
        {
            RefreshBindings(false);
            _isDirty = false;
        }
        
        // Update layout if config changed (repositions visible rows, resizes panel)
        if (_layoutDirty)
        {
            UpdateLayout();
        }
    }

    private void CheckConfigReload()
    {
        try
        {
            // Watch the Gears global settings file if present
            var gearsPath = HemSoftQoL.GearsSettingsPath;
            if (!File.Exists(gearsPath))
            {
                // If Gears not present, no hot-reload needed
                if (!_configInitialized)
                {
                    _configInitialized = true;
                    HemSoftQoL.Log("Config hot-reload disabled (Gears settings not found)");
                }
                return;
            }

            var lastWrite = File.GetLastWriteTime(gearsPath);
            
            // On first check, just record the current time without reloading
            // (config was already loaded at mod init)
            if (!_configInitialized)
            {
                _lastConfigModified = lastWrite;
                _configInitialized = true;
                HemSoftQoL.Log($"Config hot-reload initialized. Watching: {gearsPath}");
                return;
            }
            
            // Check if file was modified since last check
            if (lastWrite > _lastConfigModified)
            {
                HemSoftQoL.Log($"Gears settings changed: {_lastConfigModified} -> {lastWrite}");
                _lastConfigModified = lastWrite;
                
                // Reload display config from Gears global settings
                HemSoftQoL.ReloadDisplayConfig();
                _isDirty = true;
                _layoutDirty = true;
                
                HemSoftQoL.Log("Display config reloaded from Gears settings");
            }
        }
        catch (System.Exception ex)
        {
            HemSoftQoL.LogError($"Config reload check failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Dynamically reposition visible rows and resize panel based on config.
    /// This eliminates gaps from hidden rows.
    /// </summary>
    private void UpdateLayout()
    {
        if (!_layoutDirty || ViewComponent == null) return;
        _layoutDirty = false;
        
        var config = HemSoftQoL.DisplayConfig;
        if (config == null) return;
        
        // Row names in order (must match XUi XML)
        var rows = new[] {
            ("levelRow", config.ShowLevel),
            ("gamestageRow", config.ShowGamestage),
            ("lootstageRow", config.ShowLootstage),
            ("dayRow", config.ShowDay),
            ("bloodmoonRow", config.ShowBloodMoon),
            ("killsRow", config.ShowKills),
            ("enemyRow", config.ShowNearestEnemy),
            ("enemyCountRow", config.ShowNearestEnemy), // Same visibility as enemy
            ("biomeRow", config.ShowBiome),
            ("poiRow", config.ShowPOI),
            ("coordsRow", config.ShowCoords),
            ("sessionRow", config.ShowSession)
        };
        
        // Get panel width first (needed for row sizing)
        int panelWidth = _customWidth; // Use custom width from position mode
        int visibleCount = 0;
        int yPos = -TitleHeight; // Start below title bar
        
        // Update title bar width (gray background behind "Player Info")
        var titlebar = GetChildById("titlebar");
        if (titlebar?.ViewComponent != null)
        {
            // Titlebar is inset by 2 pixels on each side
            titlebar.ViewComponent.Size = new Vector2i(panelWidth - 4, 22);
        }
        
        // Update title label width (the "Player Info" text itself)
        // Find it by checking direct children at y=-4 position
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child?.ViewComponent != null && 
                child.ViewComponent.Position.y == -4 &&
                child.ViewComponent is XUiV_Label)
            {
                // This is the title label - resize to keep it centered
                child.ViewComponent.Size = new Vector2i(panelWidth - 10, 20);
                break;
            }
        }
        
        foreach (var (rowName, isVisible) in rows)
        {
            var row = GetChildById(rowName);
            if (row?.ViewComponent == null) continue;
            
            // Update row width to match panel width
            row.ViewComponent.Size = new Vector2i(panelWidth, RowHeight);
            
            // Find and resize the value label (right-aligned label in each row)
            // Value labels are named like "levelValue", "gamestageValue", etc.
            var valueChildName = rowName.Replace("Row", "Value");
            var valueLabel = row.GetChildById(valueChildName);
            if (valueLabel?.ViewComponent != null)
            {
                // Get original position - varies by row (70, 80, or 85)
                int originalX = valueLabel.ViewComponent.Position.x;
                
                // Calculate new width: panelWidth - originalX - rightPadding
                int rightPadding = 8;
                int newWidth = panelWidth - originalX - rightPadding;
                
                if (newWidth > 20) // Minimum reasonable width
                {
                    valueLabel.ViewComponent.Size = new Vector2i(newWidth, RowHeight);
                }
            }
            
            if (isVisible)
            {
                // Position visible rows consecutively
                row.ViewComponent.Position = new Vector2i(0, yPos);
                row.ViewComponent.UiTransform.localPosition = new Vector3(0, yPos, 0);
                yPos -= RowHeight;
                visibleCount++;
            }
        }
        
        // Calculate panel height: title + visible rows + padding
        int panelHeight = TitleHeight + (visibleCount * RowHeight) + PanelPadding;
        
        // Resize main panel and background sprites
        ViewComponent.Size = new Vector2i(panelWidth, panelHeight);
        
        // Update background sprite
        var background = GetChildById("background");
        if (background?.ViewComponent != null)
        {
            background.ViewComponent.Size = new Vector2i(panelWidth, panelHeight);
        }
        
        // Update border sprite
        var border = GetChildById("border");
        if (border?.ViewComponent != null)
        {
            border.ViewComponent.Size = new Vector2i(panelWidth, panelHeight);
        }
        
        // Reapply custom position after resizing to prevent panel from moving
        ApplyPosition();
        
        HemSoftQoL.Log($"Panel layout updated: {visibleCount} rows, height={panelHeight}");
    }

    private void ApplyPosition()
    {
        if (ViewComponent != null)
        {
            ViewComponent.Position = new Vector2i(_customPosition.x, _customPosition.y);
            ViewComponent.IsNavigatable = true;
            ViewComponent.ParseAttribute("pos", $"{_customPosition.x},{_customPosition.y}", null);
            ViewComponent.UiTransform.localPosition = new Vector3(_customPosition.x, _customPosition.y, 0);
        }
    }

    private void ApplyZoom()
    {
        if (ViewComponent?.UiTransform != null)
        {
            ViewComponent.UiTransform.localScale = new Vector3(_zoomScale, _zoomScale, 1f);
        }
    }

    private static string GetConfigPath()
    {
        return Path.Combine(HemSoftQoL.ModPath, "Config", "InfoPanelPosition.xml");
    }

    private static void LoadPosition()
    {
        try
        {
            var path = GetConfigPath();
            if (!File.Exists(path)) return;

            var doc = new XmlDocument();
            doc.Load(path);

            var node = doc.SelectSingleNode("//Position");
            if (node?.Attributes != null)
            {
                if (int.TryParse(node.Attributes["x"]?.Value, out var x))
                    _customPosition.x = x;
                if (int.TryParse(node.Attributes["y"]?.Value, out var y))
                    _customPosition.y = y;
                if (int.TryParse(node.Attributes["width"]?.Value, out var width))
                    _customWidth = Mathf.Clamp(width, WidthMin, WidthMax);
                if (float.TryParse(node.Attributes["zoom"]?.Value, out var zoom))
                    _zoomScale = Mathf.Clamp(zoom, ZoomMin, ZoomMax);

                HemSoftQoL.Log($"InfoPanel position loaded: {_customPosition.x}, {_customPosition.y}, width: {_customWidth}, zoom: {(_zoomScale * 100):F0}%");
            }
        }
        catch
        {
            // Use default position
        }
    }

    private static void SavePosition()
    {
        try
        {
            var path = GetConfigPath();
            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(decl);

            var root = doc.CreateElement("InfoPanel");
            doc.AppendChild(root);

            var pos = doc.CreateElement("Position");
            pos.SetAttribute("x", _customPosition.x.ToString());
            pos.SetAttribute("y", _customPosition.y.ToString());
            pos.SetAttribute("width", _customWidth.ToString());
            pos.SetAttribute("zoom", _zoomScale.ToString("F2"));
            root.AppendChild(pos);

            doc.Save(path);
            HemSoftQoL.Log($"InfoPanel position saved: {_customPosition.x}, {_customPosition.y}, width: {_customWidth}, zoom: {(_zoomScale * 100):F0}%");
        }
        catch (System.Exception ex)
        {
            HemSoftQoL.LogError($"Failed to save position: {ex.Message}");
        }
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        var config = HemSoftQoL.DisplayConfig;

        switch (bindingName)
        {
            // Visibility bindings for row show/hide
            case "showlevel":
                value = config?.ShowLevel == true ? "true" : "false";
                return true;
            case "showgamestage":
                value = config?.ShowGamestage == true ? "true" : "false";
                return true;
            case "showlootstage":
                value = config?.ShowLootstage == true ? "true" : "false";
                return true;
            case "showday":
                value = config?.ShowDay == true ? "true" : "false";
                return true;
            case "showbloodmoon":
                value = config?.ShowBloodMoon == true ? "true" : "false";
                return true;
            case "showkills":
                value = config?.ShowKills == true ? "true" : "false";
                return true;
            case "shownearestenemy":
                value = config?.ShowNearestEnemy == true ? "true" : "false";
                return true;
            case "showbiome":
                value = config?.ShowBiome == true ? "true" : "false";
                return true;
            case "showpoi":
                value = config?.ShowPOI == true ? "true" : "false";
                return true;
            case "showcoords":
                value = config?.ShowCoords == true ? "true" : "false";
                return true;
            case "showsession":
                value = config?.ShowSession == true ? "true" : "false";
                return true;

            // Value bindings
            case "playerlevel":
                value = _cachedLevel > 0 ? _cachedLevel.ToString() : "1";
                return true;

            case "gamestage":
                value = _cachedGamestage > 0 ? _cachedGamestage.ToString() : "1";
                return true;

            case "lootstage":
                value = _cachedLootstage > 0 ? _cachedLootstage.ToString() : "1";
                return true;

            case "currentday":
                value = _cachedDay > 0 ? _cachedDay.ToString() : "1";
                return true;

            case "zombiekills":
                value = _cachedZombieKills.ToString();
                return true;

            case "bloodmoontext":
                var daysUntil = _cachedBloodMoonDay - _cachedDay;
                if (daysUntil <= 0)
                    value = "Tonight!";
                else if (daysUntil == 1)
                    value = "Tomorrow";
                else
                    value = $"{daysUntil}d";
                return true;

            case "bloodmooncolor":
                var days = _cachedBloodMoonDay - _cachedDay;
                if (days <= 0)
                    value = "255,60,60,255";
                else if (days <= 2)
                    value = "255,180,50,255";
                else
                    value = "200,200,200,255";
                return true;

            case "nearestenemy":
                if (_cachedNearestEnemyDist < 0)
                    value = "None";
                else
                    value = $"{_cachedNearestEnemyName} {_cachedNearestEnemyDist:F0}m";
                return true;

            case "nearestenemycolor":
                if (_cachedNearestEnemyDist < 0)
                    value = "100,200,100,255"; // Green = safe
                else if (_cachedNearestEnemyDist <= 10)
                    value = "255,60,60,255"; // Red = danger close
                else if (_cachedNearestEnemyDist <= 25)
                    value = "255,180,50,255"; // Orange = nearby
                else
                    value = "255,255,100,255"; // Yellow = distant
                return true;

            case "enemycount":
                value = _cachedEnemyCount.ToString();
                return true;

            case "enemycountcolor":
                if (_cachedEnemyCount == 0)
                    value = "100,200,100,255"; // Green = safe
                else if (_cachedEnemyCount >= 5)
                    value = "255,60,60,255"; // Red = many
                else if (_cachedEnemyCount >= 3)
                    value = "255,180,50,255"; // Orange = several
                else
                    value = "255,255,100,255"; // Yellow = few
                return true;

            case "biomename":
                value = _cachedBiomeName;
                return true;

            case "poiname":
                value = _cachedPOIName;
                return true;

            case "coords":
                value = _cachedCoords;
                return true;

            case "session":
                value = _cachedSession;
                return true;

            case "positionmode":
                value = _positionMode ? "true" : "false";
                return true;

            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    private void UpdateNearestEnemy(World world)
    {
        var oldDist = _cachedNearestEnemyDist;
        var oldCount = _cachedEnemyCount;
        _cachedNearestEnemyDist = -1f;
        _cachedNearestEnemyName = "";
        _cachedEnemyCount = 0;

        if (_player == null || world == null) return;

        _nearbyEntities.Clear();
        var searchRadius = 100f;
        var bb = new Bounds(_player.position, new Vector3(searchRadius * 2, searchRadius * 2, searchRadius * 2));
        world.GetEntitiesInBounds(typeof(EntityAlive), bb, _nearbyEntities);

        var playerX = _player.position.x;
        var playerZ = _player.position.z;
        float minDistSq = float.MaxValue;
        int enemyCount = 0;
        
        foreach (var entity in _nearbyEntities)
        {
            if (entity is not EntityAlive alive) continue;
            if (alive == _player) continue;
            if (alive.IsDead()) continue;
            if (!IsHostile(alive)) continue;

            enemyCount++;
            
            // Calculate horizontal distance squared (avoid sqrt until final result)
            var dx = playerX - alive.position.x;
            var dz = playerZ - alive.position.z;
            var distSq = dx * dx + dz * dz;
            
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                var name = alive.EntityName ?? "Enemy";
                // Strip prefixes for cleaner display
                if (name.StartsWith("animalzombie", System.StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(12).TrimStart();
                else if (name.StartsWith("animal", System.StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(6).TrimStart();
                else if (name.StartsWith("zombie", System.StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(6).TrimStart();
                _cachedNearestEnemyName = string.IsNullOrEmpty(name) ? "Zombie" : name;
            }
        }
        
        _cachedEnemyCount = enemyCount;
        
        // Only sqrt for the final closest enemy
        if (minDistSq < float.MaxValue)
            _cachedNearestEnemyDist = Mathf.Sqrt(minDistSq);

        // Mark dirty if enemy distance or count changed
        if (System.Math.Abs(oldDist - _cachedNearestEnemyDist) > 1f || oldCount != _cachedEnemyCount)
            _isDirty = true;
    }

    private bool IsHostile(EntityAlive entity)
    {
        // Zombies and hostile animals
        if (entity is EntityZombie) return true;
        if (entity is EntityEnemy) return true;
        if (entity is EntityEnemyAnimal) return true;
        
        // Check if entity is targeting the player (covers edge cases)
        if (entity.GetAttackTarget() == _player) return true;
        if (entity.GetRevengeTarget() == _player) return true;
        
        // Check faction relationship
        if (_player != null)
        {
            var relationship = FactionManager.Instance.GetRelationshipTier(_player, entity);
            if (relationship == FactionManager.Relationship.Hate)
                return true;
        }
        
        return false;
    }

    private void UpdateLocationInfo(World world)
    {
        if (_player == null || world == null) return;

        var oldBiome = _cachedBiomeName;
        var oldPOI = _cachedPOIName;
        var oldCoords = _cachedCoords;

        // Get biome name
        try
        {
            var biome = world.GetBiome((int)_player.position.x, (int)_player.position.z);
            _cachedBiomeName = biome?.m_sBiomeName ?? "Unknown";
        }
        catch
        {
            _cachedBiomeName = "Unknown";
        }

        // Get POI name (if near one)
        _cachedPOIName = GetNearestPOIName(world);

        // Get coordinates
        var x = (int)_player.position.x;
        var z = (int)_player.position.z;
        _cachedCoords = $"{x}, {z}";

        // Mark dirty if location changed
        if (oldBiome != _cachedBiomeName || oldPOI != _cachedPOIName || oldCoords != _cachedCoords)
            _isDirty = true;
    }

    private string GetNearestPOIName(World world)
    {
        if (_player == null || world == null) return "Exploring";

        try
        {
            var playerPos = _player.position;
            var blockX = (int)playerPos.x;
            var blockZ = (int)playerPos.z;
            
            // Method 1: Try GetPrefabFromWorldPosInside
            var decorator = GameManager.Instance?.GetDynamicPrefabDecorator();
            if (decorator != null)
            {
                var prefabInstance = decorator.GetPrefabFromWorldPosInside(blockX, blockZ);
                if (prefabInstance?.prefab != null)
                {
                    var poiName = GetBestPOIName(prefabInstance);
                    var difficultyTier = prefabInstance.prefab.DifficultyTier;
                    _cachedPOITier = difficultyTier;
                    
                    if (!string.IsNullOrEmpty(poiName))
                    {
                        // Format with tier if present: "The McCormick Residence ☠☠"
                        if (difficultyTier > 0)
                            return $"{poiName} {new string('☠', difficultyTier)}";
                        return poiName;
                    }
                }
                
                // Method 2: Try GetPrefabsAtXZ in case we're at the edge
                var prefabs = new System.Collections.Generic.List<PrefabInstance>();
                decorator.GetPrefabsAtXZ(blockX - 1, blockX + 1, blockZ - 1, blockZ + 1, prefabs);
                
                if (prefabs.Count > 0)
                {
                    // Check if player is actually inside any of these prefabs
                    foreach (var prefab in prefabs)
                    {
                        if (IsPlayerInsidePrefab(prefab, blockX, blockZ))
                        {
                            var poiName = GetBestPOIName(prefab);
                            var difficultyTier = prefab.prefab?.DifficultyTier ?? 0;
                            _cachedPOITier = difficultyTier;
                            
                            if (!string.IsNullOrEmpty(poiName))
                            {
                                if (difficultyTier > 0)
                                    return $"{poiName} {new string('☠', difficultyTier)}";
                                return poiName;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            // Log error for debugging
            UnityEngine.Debug.LogWarning($"[HemSoft QoL] POI detection error: {ex.Message}");
        }

        _cachedPOITier = 0;
        return "Exploring";
    }
    
    /// <summary>
    /// Gets the best available display name for a POI from various sources.
    /// Fallback order: LocalizedName → PrefabName (cleaned)
    /// </summary>
    private string GetBestPOIName(PrefabInstance prefabInstance)
    {
        if (prefabInstance?.prefab == null) return null;
        
        // Try LocalizedName first (best for user-facing display)
        var localizedName = prefabInstance.prefab.LocalizedName;
        if (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("rwg_tile"))
            return localizedName;
        
        // Try PrefabName and clean it up (fallback)
        var prefabName = prefabInstance.prefab.PrefabName;
        if (!string.IsNullOrEmpty(prefabName))
        {
            // Clean up prefab names like "rwg_tile_gateway_t" → "Trader"
            if (prefabName.Contains("trader") || prefabName.Contains("gateway"))
                return "Trader";
            
            // Remove common prefixes
            prefabName = prefabName.Replace("rwg_tile_", "").Replace("part_", "");
            
            // Capitalize and replace underscores
            if (!string.IsNullOrEmpty(prefabName))
            {
                prefabName = prefabName.Replace("_", " ");
                // Capitalize first letter of each word
                var words = prefabName.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 0)
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
                return string.Join(" ", words);
            }
        }
        
        return null;
    }
    
    private bool IsPlayerInsidePrefab(PrefabInstance prefab, int playerX, int playerZ)
    {
        if (prefab == null) return false;
        
        var bbPos = prefab.boundingBoxPosition;
        var bbSize = prefab.boundingBoxSize;
        
        return playerX >= bbPos.x && playerX < bbPos.x + bbSize.x &&
               playerZ >= bbPos.z && playerZ < bbPos.z + bbSize.z;
    }

    /// <summary>
    /// Updates session time display.
    /// NOTE: 7D2D V2.5 does not persist total playtime across sessions - only session time is available.
    /// </summary>
    private void UpdateSession(World world)
    {
        if (world == null || _player == null) return;

        var oldSession = _cachedSession;
        
        // Initialize session start time on first call
        if (_sessionStartTime < 0)
        {
            _sessionStartTime = Time.realtimeSinceStartup;
        }
        
        // Calculate current session time
        var sessionSeconds = Time.realtimeSinceStartup - _sessionStartTime;
        var totalMinutes = (int)(sessionSeconds / 60f);
        
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        
        _cachedSession = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";

        if (oldSession != _cachedSession)
            _isDirty = true;
    }

    private int CalculateNextBloodMoon(int currentDay)
    {
        var frequency = GamePrefs.GetInt(EnumGamePrefs.BloodMoonFrequency);
        if (frequency <= 0) frequency = 7;

        if (currentDay > 0 && currentDay % frequency == 0)
            return currentDay;

        return ((currentDay / frequency) + 1) * frequency;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _player = null;
        _isDirty = true;
        ApplyPosition();
    }

    public override void OnClose()
    {
        base.OnClose();
        _player = null;
    }

}
