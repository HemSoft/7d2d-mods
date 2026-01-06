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
    private float _updateTimer;
    private const float UpdateInterval = 0.25f;
    
    // Reusable list to avoid GC allocations
    private readonly List<Entity> _nearbyEntities = new List<Entity>(64);

    // Position mode
    private static bool _positionMode;
    private static Vector2i _customPosition = new Vector2i(190, 170);
    private static bool _positionLoaded;
    private const int MoveStep = 5;
    private const int MoveStepFast = 20;

    public override void Init()
    {
        base.Init();
        _isDirty = true;

        if (!_positionLoaded)
        {
            LoadPosition();
            _positionLoaded = true;
        }

        ApplyPosition();
        HemSoftQoL.Log("InfoPanel controller initialized");
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);

        if (!XUi.IsGameRunning()) return;

        // Handle position mode toggle (Alt+P)
        if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.P))
        {
            _positionMode = !_positionMode;
            if (_positionMode)
                HemSoftQoL.Log("Position mode ON - Use Arrow Keys to move (hold Shift for faster). Press Alt+P to save and exit.");
            else
            {
                SavePosition();
                HemSoftQoL.Log("Position mode OFF - Position saved.");
            }
        }

        // Handle arrow key movement in position mode
        if (_positionMode)
        {
            var step = Input.GetKey(KeyCode.LeftShift) ? MoveStepFast : MoveStep;
            var moved = false;

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

            if (moved)
                ApplyPosition();
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

        if (_isDirty)
        {
            RefreshBindings(false);
            _isDirty = false;
        }
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

                HemSoftQoL.Log($"InfoPanel position loaded: {_customPosition.x}, {_customPosition.y}");
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
            root.AppendChild(pos);

            doc.Save(path);
            HemSoftQoL.Log($"InfoPanel position saved: {_customPosition.x}, {_customPosition.y}");
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
