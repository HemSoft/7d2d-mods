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
        public static bool GearsPresent { get; private set; }

        private static readonly string ModTag = "[HemSoft QoL]";

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            ModPath = _modInstance.Path;

            Log($"Initializing v1.4.0...");

            // Check if Gears mod settings framework is present (ModSettings.xml with user changes)
            var modSettingsPath = Path.Combine(ModPath, "ModSettings.xml");
            GearsPresent = File.Exists(modSettingsPath);
            Log(GearsPresent ? "Gears settings detected - using ModSettings.xml" : "Gears not detected - using legacy config files");

            // Load configurations (Gears ModSettings.xml takes priority over legacy configs)
            Config = HotkeyConfig.Load(ModPath);
            DisplayConfig = DisplayConfig.Load(ModPath);

            // Apply Harmony patches
            var harmony = new Harmony("com.hemsoft.qol");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log($"Loaded successfully! Hotkeys active when container is open. Type 'hemsoft' in console for settings.");
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
        /// Load hotkey configuration. Tries Gears ModSettings.xml first, then falls back to legacy config.
        /// </summary>
        public static HotkeyConfig Load(string modPath)
        {
            var config = new HotkeyConfig();
            var modSettingsPath = Path.Combine(modPath, "ModSettings.xml");
            var legacyPath = Path.Combine(modPath, "Config", "HemSoftQoL.xml");

            try
            {
                // Try Gears ModSettings.xml first
                if (File.Exists(modSettingsPath))
                {
                    if (TryLoadFromModSettings(modSettingsPath, config))
                    {
                        HemSoftQoL.Log($"Hotkey configuration loaded from ModSettings.xml (Gears)");
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
        /// Load from Gears ModSettings.xml format.
        /// </summary>
        private static bool TryLoadFromModSettings(string path, HotkeyConfig config)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(path);

                // Parse each hotkey from Gears format
                config.QuickStack = ParseGearsHotkey(doc, "QuickStack", config.QuickStack);
                config.StashAll = ParseGearsHotkey(doc, "StashAll", config.StashAll);
                config.Restock = ParseGearsHotkey(doc, "Restock", config.Restock);
                config.SortContainer = ParseGearsHotkey(doc, "SortContainer", config.SortContainer);
                config.SortInventory = ParseGearsHotkey(doc, "SortInventory", config.SortInventory);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Parse a hotkey from Gears ModSettings.xml format.
        /// Gears uses separate Switch and Selector elements with value="..." attributes.
        /// </summary>
        private static HotkeyBinding ParseGearsHotkey(XmlDocument doc, string name, HotkeyBinding defaultValue)
        {
            // Find the category for this hotkey
            var category = doc.SelectSingleNode($"//Category[@name='{name}']");
            if (category == null) return defaultValue;

            // Get enabled switch (e.g., QuickStackEnabled)
            var enabledNode = category.SelectSingleNode($"Switch[@name='{name}Enabled']");
            var enabled = enabledNode?.Attributes?["value"]?.Value?.ToLower() == "true";

            // Get modifier selector (e.g., QuickStackModifier)
            var modifierNode = category.SelectSingleNode($"Selector[@name='{name}Modifier']");
            var modifier = modifierNode?.Attributes?["value"]?.Value ?? defaultValue.ModifierName;

            // Get key selector (e.g., QuickStackKey)
            var keyNode = category.SelectSingleNode($"Selector[@name='{name}Key']");
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
