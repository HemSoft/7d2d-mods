using System.IO;
using System.Xml;
using UnityEngine;

namespace HemSoft.QoL
{
    /// <summary>
    /// Custom HUD Info Panel controller that displays player stats.
    /// Provides data bindings for level, gamestage, day, blood moon, and kills.
    /// Supports repositioning via Alt+P + Arrow Keys.
    /// Respects DisplayConfig for showing/hiding elements.
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
        private float _updateTimer;
        private const float UpdateInterval = 0.25f;

        // Position mode
        private static bool _positionMode;
        private static Vector2i _customPosition = new Vector2i(190, 140);
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
            var displayConfig = HemSoftQoL.DisplayConfig;

            switch (bindingName)
            {
                // Visibility bindings
                case "showlevel":
                    value = displayConfig?.ShowLevel.ToString().ToLower() ?? "true";
                    return true;
                case "showgamestage":
                    value = displayConfig?.ShowGamestage.ToString().ToLower() ?? "true";
                    return true;
                case "showlootstage":
                    value = displayConfig?.ShowLootstage.ToString().ToLower() ?? "true";
                    return true;
                case "showday":
                    value = displayConfig?.ShowDay.ToString().ToLower() ?? "true";
                    return true;
                case "showbloodmoon":
                    value = displayConfig?.ShowBloodMoon.ToString().ToLower() ?? "true";
                    return true;
                case "showkills":
                    value = displayConfig?.ShowKills.ToString().ToLower() ?? "true";
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

                case "positionmode":
                    value = _positionMode ? "true" : "false";
                    return true;

                default:
                    return base.GetBindingValueInternal(ref value, bindingName);
            }
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
}
