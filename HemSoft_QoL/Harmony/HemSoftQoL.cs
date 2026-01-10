using System;
using System.IO;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using UnityEngine;

namespace HemSoft.QoL
{
    /// <summary>
    /// Main mod entry point - initializes Harmony patches and loads configuration.
    /// </summary>
    public class HemSoftQoL : IModApi
    {
        public static HemSoftQoL Instance { get; private set; }
        public static HotkeyConfig Config { get; private set; }
        public static DisplayConfig DisplayConfig { get; private set; }
        public static string ModPath { get; private set; }
        public static string ModName { get; private set; }
        public static string GearsSettingsPath { get; private set; }
        public static bool GearsPresent { get; private set; }

        private static readonly string ModTag = "[HemSoft QoL]";

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            ModPath = _modInstance.Path;
            ModName = Path.GetFileName(ModPath); // e.g., "S_HemSoft_QoL"

            Log($"Initializing v1.3.0...");

            // Check if Gears mod settings framework is present
            // Gears stores user settings in %APPDATA%/7DaysToDie/Gears/ModSettings.xml
            GearsSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "7DaysToDie", "Gears", "ModSettings.xml");
            GearsPresent = File.Exists(GearsSettingsPath);
            Log(GearsPresent 
                ? $"Gears settings detected at: {GearsSettingsPath}" 
                : "Gears not detected - using legacy config files");

            // Load configurations (Gears ModSettings.xml takes priority over legacy configs)
            Config = HotkeyConfig.Load(ModPath);
            DisplayConfig = DisplayConfig.Load(ModPath);

            // Apply Harmony patches
            var harmony = new Harmony("com.hemsoft.qol");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log($"Loaded successfully! Hotkeys active when container is open. Type 'hemsoft' in console for settings.");
        }

        /// <summary>
        /// Reload display configuration from ModSettings.xml (called when Gears settings change).
        /// </summary>
        public static void ReloadDisplayConfig()
        {
            DisplayConfig = DisplayConfig.Load(ModPath);
        }

        /// <summary>
        /// Reload hotkey configuration from ModSettings.xml (called when Gears settings change).
        /// </summary>
        public static void ReloadHotkeyConfig()
        {
            Config = HotkeyConfig.Load(ModPath);
        }

        public static void Log(string message)
        {
            Debug.Log($"{ModTag} {message}");
        }

        public static void LogError(string message)
        {
            Debug.LogError($"{ModTag} {message}");
        }
    }

    /// <summary>
    /// Hotkey configuration loaded from XML.
    /// Supports both Gears ModSettings.xml format and legacy HemSoftQoL.xml format.
    /// </summary>
    public class HotkeyConfig
    {
        public HotkeyBinding QuickStack { get; set; } = new("None", "Q");
        public HotkeyBinding StashAll { get; set; } = new("LeftAlt", "X");
        public HotkeyBinding Restock { get; set; } = new("LeftAlt", "R");
        public HotkeyBinding SortContainer { get; set; } = new("None", "S");
        public HotkeyBinding SortInventory { get; set; } = new("LeftAlt", "", false);

        /// <summary>
        /// Load hotkey configuration. Tries Gears global settings first, then falls back to legacy config.
        /// </summary>
        public static HotkeyConfig Load(string modPath)
        {
            var config = new HotkeyConfig();
            var legacyPath = Path.Combine(modPath, "Config", "HemSoftQoL.xml");

            try
            {
                // Try Gears global settings first (stored in %APPDATA%/7DaysToDie/Gears/ModSettings.xml)
                if (HemSoftQoL.GearsPresent)
                {
                    if (TryLoadFromGearsSettings(config))
                    {
                        HemSoftQoL.Log($"Hotkey configuration loaded from Gears settings");
                        return config;
                    }
                }

                // Fallback to legacy config
                if (File.Exists(legacyPath))
                {
                    LoadFromLegacyConfig(legacyPath, config);
                    HemSoftQoL.Log($"Hotkey configuration loaded from HemSoftQoL.xml (legacy)");
                }
                else
                {
                    HemSoftQoL.Log($"No config found, using defaults.");
                }
            }
            catch (Exception ex)
            {
                HemSoftQoL.LogError($"Failed to load hotkey config: {ex.Message}");
            }

            return config;
        }

        /// <summary>
        /// Load from Gears global settings file.
        /// Path: %APPDATA%/7DaysToDie/Gears/ModSettings.xml
        /// Structure: <Mod name="S_HemSoft_QoL">/<Tab>/<Category>/<Setting name="..." value="..." />
        /// </summary>
        private static bool TryLoadFromGearsSettings(HotkeyConfig config)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(HemSoftQoL.GearsSettingsPath);

                // Find our mod's settings node
                var modNode = doc.SelectSingleNode($"//Mod[@name='{HemSoftQoL.ModName}']");
                if (modNode == null)
                {
                    HemSoftQoL.Log($"Mod '{HemSoftQoL.ModName}' not found in Gears settings");
                    return false;
                }

                // Parse each hotkey from Gears format
                config.QuickStack = ParseGearsHotkey(modNode, "QuickStack", config.QuickStack);
                config.StashAll = ParseGearsHotkey(modNode, "StashAll", config.StashAll);
                config.Restock = ParseGearsHotkey(modNode, "Restock", config.Restock);
                config.SortContainer = ParseGearsHotkey(modNode, "SortContainer", config.SortContainer);
                config.SortInventory = ParseGearsHotkey(modNode, "SortInventory", config.SortInventory);

                return true;
            }
            catch (Exception ex)
            {
                HemSoftQoL.LogError($"Failed to load from Gears settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parse a hotkey from Gears global settings format.
        /// Structure: <Category name="QuickStack"><Setting name="QuickStackEnabled" value="On" />...
        /// </summary>
        private static HotkeyBinding ParseGearsHotkey(XmlNode modNode, string name, HotkeyBinding defaultValue)
        {
            // Find the category for this hotkey
            var category = modNode.SelectSingleNode($".//Category[@name='{name}']");
            if (category == null) return defaultValue;

            // Get enabled setting (e.g., QuickStackEnabled)
            var enabledNode = category.SelectSingleNode($"Setting[@name='{name}Enabled']");
            var enabledValue = enabledNode?.Attributes?["value"]?.Value ?? "On";
            var enabled = enabledValue.Equals("On", StringComparison.OrdinalIgnoreCase);

            // Get modifier setting (e.g., QuickStackModifier)
            var modifierNode = category.SelectSingleNode($"Setting[@name='{name}Modifier']");
            var modifier = modifierNode?.Attributes?["value"]?.Value ?? defaultValue.ModifierName;

            // Get key setting (e.g., QuickStackKey)
            var keyNode = category.SelectSingleNode($"Setting[@name='{name}Key']");
            var key = keyNode?.Attributes?["value"]?.Value ?? defaultValue.KeyName;

            return new HotkeyBinding(modifier, key, enabled);
        }

        /// <summary>
        /// Load from legacy HemSoftQoL.xml format.
        /// </summary>
        private static void LoadFromLegacyConfig(string path, HotkeyConfig config)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            config.QuickStack = ParseLegacyHotkey(doc, "QuickStack", config.QuickStack);
            config.StashAll = ParseLegacyHotkey(doc, "StashAll", config.StashAll);
            config.Restock = ParseLegacyHotkey(doc, "Restock", config.Restock);
            config.SortContainer = ParseLegacyHotkey(doc, "SortContainer", config.SortContainer);
            config.SortInventory = ParseLegacyHotkey(doc, "SortInventory", config.SortInventory);
        }

        /// <summary>
        /// Parse a hotkey from legacy HemSoftQoL.xml format.
        /// </summary>
        private static HotkeyBinding ParseLegacyHotkey(XmlDocument doc, string name, HotkeyBinding defaultValue)
        {
            var node = doc.SelectSingleNode($"//Hotkeys/{name}");
            if (node?.Attributes == null) return defaultValue;

            var enabled = node.Attributes["enabled"]?.Value ?? "true";
            var modifier = node.Attributes["modifier"]?.Value ?? defaultValue.ModifierName;
            var key = node.Attributes["key"]?.Value ?? defaultValue.KeyName;

            return new HotkeyBinding(modifier, key, enabled.ToLower() == "true");
        }
    }

    /// <summary>
    /// Represents a single hotkey binding with modifier + key.
    /// </summary>
    public class HotkeyBinding
    {
        public string ModifierName { get; }
        public string KeyName { get; }
        public KeyCode Modifier { get; }
        public KeyCode Key { get; }
        public bool Enabled { get; }
        public bool HasModifier => Modifier != KeyCode.None;

        public HotkeyBinding(string modifier, string key, bool enabled = true)
        {
            ModifierName = modifier;
            KeyName = key;
            Enabled = enabled;

            Modifier = ParseKeyCode(modifier, KeyCode.None);
            Key = ParseKeyCode(key, KeyCode.None);

            if (Key == KeyCode.None && enabled)
            {
                HemSoftQoL.LogError($"Invalid key '{key}' in hotkey config");
            }
        }

        private static KeyCode ParseKeyCode(string value, KeyCode defaultValue)
        {
            if (string.IsNullOrEmpty(value) || value.Equals("None", StringComparison.OrdinalIgnoreCase))
                return defaultValue;

            if (Enum.TryParse<KeyCode>(value, true, out var result))
                return result;

            return defaultValue;
        }

        public bool IsPressed()
        {
            if (!Enabled || Key == KeyCode.None) return false;

            // Check if modifier is held (or no modifier required)
            var modifierHeld = !HasModifier || Input.GetKey(Modifier);

            // Check if key was just pressed this frame
            return modifierHeld && Input.GetKeyDown(Key);
        }

        public override string ToString() => HasModifier ? $"{ModifierName}+{KeyName}" : KeyName;
    }
}
