using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HemSoft.QoL;

/// <summary>
/// Console commands to configure HemSoft QoL mod.
/// 
/// Usage:
///   hs             - Show help and current settings
///   hs level       - Toggle Level display
///   hs gamestage   - Toggle Gamestage display
///   hs lootstage   - Toggle Lootstage display
///   hs day         - Toggle Day display
///   hs bloodmoon   - Toggle Blood Moon display
///   hs kills       - Toggle Kills display
///   hs enemy       - Toggle Nearest Enemy display
///   hs all         - Show all info panel elements
///   hs none        - Hide all info panel elements
/// </summary>
public class ConsoleCmdHemSoft : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return ["hemsoft", "hs"];
    }

    public override string getDescription()
    {
        return "HemSoft QoL - Configure info panel display";
    }

    public override string getHelp()
    {
        var hotkeys = HemSoftQoL.Config;
        var gearsNote = HemSoftQoL.GearsPresent 
            ? "Gears detected - use Options > Mod Settings for full configuration."
            : "Install Gears mod for in-game settings UI.";
        return $@"HemSoft QoL Configuration

Usage:
  hs              - Show current settings
  hs level        - Toggle Level display
  hs gamestage    - Toggle Gamestage display
  hs lootstage    - Toggle Lootstage display
  hs day          - Toggle Day display
  hs bloodmoon    - Toggle Blood Moon display
  hs kills        - Toggle Kills display
  hs enemy        - Toggle Nearest Enemy display
  hs all          - Show all info panel elements
  hs none         - Hide all info panel elements

{gearsNote}

Current Hotkeys (when container is open):
  {FormatHotkey(hotkeys.QuickStack)} = Quick Stack (deposit matching items)
  {FormatHotkey(hotkeys.StashAll)} = Stash All (deposit all items)
  {FormatHotkey(hotkeys.Restock)} = Restock (pull items to fill stacks)
  {FormatHotkey(hotkeys.SortContainer)} = Sort Container
  {FormatHotkey(hotkeys.SortInventory)} = Sort Inventory
";
    }

    private static string FormatHotkey(HotkeyBinding binding)
    {
        if (!binding.Enabled || binding.Key == UnityEngine.KeyCode.None)
            return "Disabled";
        return binding.ToString();
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        var config = HemSoftQoL.DisplayConfig;

        if (_params.Count == 0)
        {
            ShowStatus(config);
            return;
        }

        var arg = _params[0].ToLower();
        string message;
        switch (arg)
        {
            case "level":
                config.ShowLevel = !config.ShowLevel;
                message = $"Level: {(config.ShowLevel ? "ON" : "OFF")}";
                break;
            case "gamestage":
                config.ShowGamestage = !config.ShowGamestage;
                message = $"Gamestage: {(config.ShowGamestage ? "ON" : "OFF")}";
                break;
            case "lootstage":
                config.ShowLootstage = !config.ShowLootstage;
                message = $"Lootstage: {(config.ShowLootstage ? "ON" : "OFF")}";
                break;
            case "day":
                config.ShowDay = !config.ShowDay;
                message = $"Day: {(config.ShowDay ? "ON" : "OFF")}";
                break;
            case "bloodmoon":
                config.ShowBloodMoon = !config.ShowBloodMoon;
                message = $"Blood Moon: {(config.ShowBloodMoon ? "ON" : "OFF")}";
                break;
            case "kills":
                config.ShowKills = !config.ShowKills;
                message = $"Kills: {(config.ShowKills ? "ON" : "OFF")}";
                break;
            case "enemy":
                config.ShowNearestEnemy = !config.ShowNearestEnemy;
                message = $"Nearest Enemy: {(config.ShowNearestEnemy ? "ON" : "OFF")}";
                break;
            case "all":
                message = EnableAll(config);
                break;
            case "none":
                message = DisableAll(config);
                break;
            default:
                message = $"Unknown option: {arg}. Type 'hs' for help.";
                break;
        }

        // Save config
        config.Save(Path.Combine(HemSoftQoL.ModPath, "Config", "InfoPanelConfig.xml"));

        SingletonMonoBehaviour<SdtdConsole>.Instance.Output(message);
    }

    private static string EnableAll(DisplayConfig config)
    {
        config.ShowLevel = true;
        config.ShowGamestage = true;
        config.ShowLootstage = true;
        config.ShowDay = true;
        config.ShowBloodMoon = true;
        config.ShowKills = true;
        config.ShowNearestEnemy = true;
        return "All info panel elements enabled.";
    }

    private static string DisableAll(DisplayConfig config)
    {
        config.ShowLevel = false;
        config.ShowGamestage = false;
        config.ShowLootstage = false;
        config.ShowDay = false;
        config.ShowBloodMoon = false;
        config.ShowKills = false;
        config.ShowNearestEnemy = false;
        return "All info panel elements disabled.";
    }

    private void ShowStatus(DisplayConfig config)
    {
        var console = SingletonMonoBehaviour<SdtdConsole>.Instance;
        var hotkeys = HemSoftQoL.Config;
        
        console.Output("=== HemSoft QoL Info Panel ===");
        console.Output($"  Level:      {OnOff(config.ShowLevel)}");
        console.Output($"  Gamestage:  {OnOff(config.ShowGamestage)}");
        console.Output($"  Lootstage:  {OnOff(config.ShowLootstage)}");
        console.Output($"  Day:        {OnOff(config.ShowDay)}");
        console.Output($"  Blood Moon: {OnOff(config.ShowBloodMoon)}");
        console.Output($"  Kills:      {OnOff(config.ShowKills)}");
        console.Output($"  Enemy:      {OnOff(config.ShowNearestEnemy)}");
        console.Output("");
        console.Output("=== Hotkeys (when container open) ===");
        console.Output($"  Quick Stack:    {FormatHotkey(hotkeys.QuickStack)}");
        console.Output($"  Stash All:      {FormatHotkey(hotkeys.StashAll)}");
        console.Output($"  Restock:        {FormatHotkey(hotkeys.Restock)}");
        console.Output($"  Sort Container: {FormatHotkey(hotkeys.SortContainer)}");
        console.Output($"  Sort Inventory: {FormatHotkey(hotkeys.SortInventory)}");
        console.Output("");
        console.Output("Type 'hs <option>' to toggle. Example: hs level");
        console.Output("Type 'hs all' or 'hs none' to show/hide all.");
        console.Output("Edit Config/HemSoftQoL.xml to change hotkeys.");
    }

    private static string OnOff(bool value) => value ? "ON" : "OFF";
}

/// <summary>
/// Configuration for info panel display elements.
/// Supports both Gears ModSettings.xml format and legacy InfoPanelConfig.xml format.
/// </summary>
public class DisplayConfig
{
    public bool ShowLevel { get; set; } = true;
    public bool ShowGamestage { get; set; } = true;
    public bool ShowLootstage { get; set; } = true;
    public bool ShowDay { get; set; } = true;
    public bool ShowBloodMoon { get; set; } = true;
    public bool ShowKills { get; set; } = true;
    public bool ShowNearestEnemy { get; set; } = true;

    private string _modPath;

    /// <summary>
    /// Load display configuration. Tries Gears ModSettings.xml first, then falls back to legacy config.
    /// </summary>
    public static DisplayConfig Load(string modPath)
    {
        var config = new DisplayConfig { _modPath = modPath };
        var modSettingsPath = Path.Combine(modPath, "ModSettings.xml");
        var legacyPath = Path.Combine(modPath, "Config", "InfoPanelConfig.xml");

        try
        {
            // Try Gears ModSettings.xml first
            if (File.Exists(modSettingsPath))
            {
                if (TryLoadFromModSettings(modSettingsPath, config))
                {
                    HemSoftQoL.Log($"Display configuration loaded from ModSettings.xml (Gears)");
                    return config;
                }
            }

            // Fallback to legacy config
            if (File.Exists(legacyPath))
            {
                LoadFromLegacyConfig(legacyPath, config);
                HemSoftQoL.Log($"Display configuration loaded from InfoPanelConfig.xml (legacy)");
            }
            else
            {
                HemSoftQoL.Log($"No display config found, using defaults.");
            }
        }
        catch (Exception ex)
        {
            HemSoftQoL.LogError($"Failed to load display config: {ex.Message}");
        }

        return config;
    }

    /// <summary>
    /// Load from Gears ModSettings.xml format.
    /// </summary>
    private static bool TryLoadFromModSettings(string path, DisplayConfig config)
    {
        try
        {
            var doc = new XmlDocument();
            doc.Load(path);

            // Find the PanelElements category in the InfoPanel tab
            var category = doc.SelectSingleNode("//Tab[@name='InfoPanel']//Category[@name='PanelElements']");
            if (category == null) return false;

            config.ShowLevel = ParseGearsSwitch(category, "ShowLevel", true);
            config.ShowGamestage = ParseGearsSwitch(category, "ShowGamestage", true);
            config.ShowLootstage = ParseGearsSwitch(category, "ShowLootstage", true);
            config.ShowDay = ParseGearsSwitch(category, "ShowDay", true);
            config.ShowBloodMoon = ParseGearsSwitch(category, "ShowBloodMoon", true);
            config.ShowKills = ParseGearsSwitch(category, "ShowKills", true);
            config.ShowNearestEnemy = ParseGearsSwitch(category, "ShowNearestEnemy", true);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parse a Switch element from Gears ModSettings.xml format.
    /// </summary>
    private static bool ParseGearsSwitch(XmlNode category, string name, bool defaultValue)
    {
        var switchNode = category.SelectSingleNode($"Switch[@name='{name}']");
        if (switchNode?.Attributes?["value"] == null) return defaultValue;

        return switchNode.Attributes["value"].Value.ToLower() == "true";
    }

    /// <summary>
    /// Load from legacy InfoPanelConfig.xml format.
    /// </summary>
    private static void LoadFromLegacyConfig(string path, DisplayConfig config)
    {
        var doc = new XmlDocument();
        doc.Load(path);

        config.ShowLevel = ParseLegacyBool(doc, "Level", true);
        config.ShowGamestage = ParseLegacyBool(doc, "Gamestage", true);
        config.ShowLootstage = ParseLegacyBool(doc, "Lootstage", true);
        config.ShowDay = ParseLegacyBool(doc, "Day", true);
        config.ShowBloodMoon = ParseLegacyBool(doc, "BloodMoon", true);
        config.ShowKills = ParseLegacyBool(doc, "Kills", true);
        config.ShowNearestEnemy = ParseLegacyBool(doc, "NearestEnemy", true);
    }

    private static bool ParseLegacyBool(XmlDocument doc, string name, bool defaultValue)
    {
        var node = doc.SelectSingleNode($"//Display/{name}");
        if (node?.Attributes?["enabled"] == null) return defaultValue;

        return node.Attributes["enabled"].Value.ToLower() == "true";
    }

    /// <summary>
    /// Save display configuration. Updates both ModSettings.xml (if Gears present) and legacy config.
    /// </summary>
    public void Save(string path)
    {
        // Always save to legacy config for backwards compatibility
        SaveToLegacyConfig(path);

        // Also update ModSettings.xml if Gears is present
        if (HemSoftQoL.GearsPresent && _modPath != null)
        {
            var modSettingsPath = Path.Combine(_modPath, "ModSettings.xml");
            SaveToModSettings(modSettingsPath);
        }
    }

    private void SaveToModSettings(string path)
    {
        try
        {
            if (!File.Exists(path)) return;

            var doc = new XmlDocument();
            doc.Load(path);

            // Find the PanelElements category
            var category = doc.SelectSingleNode("//Tab[@name='InfoPanel']//Category[@name='PanelElements']");
            if (category == null) return;

            // Update each switch value
            UpdateGearsSwitch(category, "ShowLevel", ShowLevel);
            UpdateGearsSwitch(category, "ShowGamestage", ShowGamestage);
            UpdateGearsSwitch(category, "ShowLootstage", ShowLootstage);
            UpdateGearsSwitch(category, "ShowDay", ShowDay);
            UpdateGearsSwitch(category, "ShowBloodMoon", ShowBloodMoon);
            UpdateGearsSwitch(category, "ShowKills", ShowKills);
            UpdateGearsSwitch(category, "ShowNearestEnemy", ShowNearestEnemy);

            doc.Save(path);
            HemSoftQoL.Log($"Display config saved to ModSettings.xml (Gears)");
        }
        catch (Exception ex)
        {
            HemSoftQoL.LogError($"Failed to save to ModSettings.xml: {ex.Message}");
        }
    }

    private static void UpdateGearsSwitch(XmlNode category, string name, bool value)
    {
        var switchNode = category.SelectSingleNode($"Switch[@name='{name}']");
        if (switchNode?.Attributes?["value"] != null)
        {
            switchNode.Attributes["value"].Value = value.ToString().ToLower();
        }
    }

    private void SaveToLegacyConfig(string path)
    {
        try
        {
            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(decl);

            var comment = doc.CreateComment(@"
  HemSoft QoL - Info Panel Display Configuration
  
  Set enabled=""true"" to show, enabled=""false"" to hide each element.
  Use console command 'hs' to toggle these settings in-game.
  
  Note: If using Gears mod, settings are also saved to ModSettings.xml.
");
            doc.AppendChild(comment);

            var root = doc.CreateElement("InfoPanelConfig");
            doc.AppendChild(root);

            var display = doc.CreateElement("Display");
            root.AppendChild(display);

            AddDisplayElement(doc, display, "Level", ShowLevel);
            AddDisplayElement(doc, display, "Gamestage", ShowGamestage);
            AddDisplayElement(doc, display, "Lootstage", ShowLootstage);
            AddDisplayElement(doc, display, "Day", ShowDay);
            AddDisplayElement(doc, display, "BloodMoon", ShowBloodMoon);
            AddDisplayElement(doc, display, "Kills", ShowKills);
            AddDisplayElement(doc, display, "NearestEnemy", ShowNearestEnemy);

            doc.Save(path);
            HemSoftQoL.Log($"Display config saved to {path}");
        }
        catch (Exception ex)
        {
            HemSoftQoL.LogError($"Failed to save display config: {ex.Message}");
        }
    }

    private static void AddDisplayElement(XmlDocument doc, XmlElement parent, string name, bool enabled)
    {
        var elem = doc.CreateElement(name);
        elem.SetAttribute("enabled", enabled.ToString().ToLower());
        parent.AppendChild(elem);
    }
}
