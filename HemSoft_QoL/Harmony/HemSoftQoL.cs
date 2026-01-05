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

        private static readonly string ModTag = "[HemSoft QoL]";

        // Config window hotkey (Alt+F12)
        private static readonly KeyCode ConfigWindowKey = KeyCode.F12;
        private static readonly KeyCode ConfigWindowModifier = KeyCode.LeftAlt;

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            ModPath = _modInstance.Path;

            Log($"Initializing v1.1.0...");

            // Load configurations
            Config = HotkeyConfig.Load(Path.Combine(ModPath, "Config", "HemSoftQoL.xml"));
            DisplayConfig = DisplayConfig.Load(Path.Combine(ModPath, "Config", "InfoPanelConfig.xml"));

            // Apply Harmony patches
            var harmony = new Harmony("com.hemsoft.qol");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log($"Loaded successfully! Hotkeys active when container is open. Press Alt+F12 for settings.");
        }

        /// <summary>
        /// Called from Update patches to check for config window hotkey.
        /// </summary>
        public static void CheckConfigWindowHotkey()
        {
            if (Input.GetKey(ConfigWindowModifier) && Input.GetKeyDown(ConfigWindowKey))
            {
                OpenConfigWindow();
            }
        }

        public static void OpenConfigWindow()
        {
            var playerUI = LocalPlayerUI.primaryUI;
            if (playerUI?.windowManager != null)
            {
                if (playerUI.windowManager.IsWindowOpen("HemSoftConfigWindow"))
                {
                    playerUI.windowManager.Close("HemSoftConfigWindow");
                }
                else
                {
                    playerUI.windowManager.Open("HemSoftConfigWindow", true);
                }
            }
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
    /// </summary>
    public class HotkeyConfig
    {
        public HotkeyBinding QuickStack { get; set; } = new("LeftAlt", "Q");
        public HotkeyBinding StashAll { get; set; } = new("LeftAlt", "X");
        public HotkeyBinding Restock { get; set; } = new("LeftAlt", "R");
        public HotkeyBinding SortContainer { get; set; } = new("LeftAlt", "C");
        public HotkeyBinding SortInventory { get; set; } = new("LeftAlt", "");

        public static HotkeyConfig Load(string path)
        {
            var config = new HotkeyConfig();

            try
            {
                if (!File.Exists(path))
                {
                    HemSoftQoL.Log($"Config not found at {path}, using defaults.");
                    return config;
                }

                var doc = new XmlDocument();
                doc.Load(path);

                config.QuickStack = ParseHotkey(doc, "QuickStack", config.QuickStack);
                config.StashAll = ParseHotkey(doc, "StashAll", config.StashAll);
                config.Restock = ParseHotkey(doc, "Restock", config.Restock);
                config.SortContainer = ParseHotkey(doc, "SortContainer", config.SortContainer);
                config.SortInventory = ParseHotkey(doc, "SortInventory", config.SortInventory);

                HemSoftQoL.Log($"Configuration loaded from {path}");
            }
            catch (Exception ex)
            {
                HemSoftQoL.LogError($"Failed to load config: {ex.Message}");
            }

            return config;
        }

        private static HotkeyBinding ParseHotkey(XmlDocument doc, string name, HotkeyBinding defaultValue)
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
