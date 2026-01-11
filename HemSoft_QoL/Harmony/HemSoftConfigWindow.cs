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
  hs biome        - Toggle Biome display
  hs poi          - Toggle POI/Location display
  hs coords       - Toggle Coordinates display
  hs session      - Toggle Session time display
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
            case "biome":
                config.ShowBiome = !config.ShowBiome;
                message = $"Biome: {(config.ShowBiome ? "ON" : "OFF")}";
                break;
            case "poi":
                config.ShowPOI = !config.ShowPOI;
                message = $"POI: {(config.ShowPOI ? "ON" : "OFF")}";
                break;
            case "coords":
                config.ShowCoords = !config.ShowCoords;
                message = $"Coords: {(config.ShowCoords ? "ON" : "OFF")}";
                break;
            case "session":
                config.ShowSession = !config.ShowSession;
                message = $"Session: {(config.ShowSession ? "ON" : "OFF")}";
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
        config.ShowBiome = true;
        config.ShowPOI = true;
        config.ShowCoords = true;
        config.ShowSession = true;
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
        config.ShowBiome = false;
        config.ShowPOI = false;
        config.ShowCoords = false;
        config.ShowSession = false;
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
        console.Output($"  Biome:      {OnOff(config.ShowBiome)}");
        console.Output($"  POI:        {OnOff(config.ShowPOI)}");
        console.Output($"  Coords:     {OnOff(config.ShowCoords)}");
        console.Output($"  Session:    {OnOff(config.ShowSession)}");
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
/// Supports both Gears global settings and legacy InfoPanelConfig.xml format.
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
    public bool ShowBiome { get; set; } = false;
    public bool ShowPOI { get; set; } = true;
    public bool ShowCoords { get; set; } = false;
    public bool ShowSession { get; set; } = true;
    public int PanelWidth { get; set; } = 140;

    private string _modPath;

    /// <summary>
    /// Load display configuration. Tries Gears global settings first, then falls back to legacy config.
    /// </summary>
    public static DisplayConfig Load(string modPath)
    {
        var config = new DisplayConfig { _modPath = modPath };
        var legacyPath = Path.Combine(modPath, "Config", "InfoPanelConfig.xml");

        try
        {
            // Try Gears global settings first (stored in %APPDATA%/7DaysToDie/Gears/ModSettings.xml)
            if (HemSoftQoL.GearsPresent)
            {
                if (TryLoadFromGearsSettings(config))
                {
                    HemSoftQoL.Log($"Display configuration loaded from Gears settings");
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
    /// Load from Gears global settings file.
    /// Path: %APPDATA%/7DaysToDie/Gears/ModSettings.xml
    /// Structure: <Mod name="S_HemSoft_QoL">/<Tab>/<Category>/<Setting name="..." value="..." />
    /// </summary>
    private static bool TryLoadFromGearsSettings(DisplayConfig config)
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

            // Find the PanelElements category in the Info Panel tab
            var category = modNode.SelectSingleNode(".//Tab[@name='Info Panel']//Category[@name='PanelElements']");
            if (category == null)
            {
                HemSoftQoL.Log("PanelElements category not found in Gears settings");
                return false;
            }

            config.ShowLevel = ParseGearsSetting(category, "ShowLevel", true);
            config.ShowGamestage = ParseGearsSetting(category, "ShowGamestage", true);
            config.ShowLootstage = ParseGearsSetting(category, "ShowLootstage", true);
            config.ShowDay = ParseGearsSetting(category, "ShowDay", true);
            config.ShowBloodMoon = ParseGearsSetting(category, "ShowBloodMoon", true);
            config.ShowKills = ParseGearsSetting(category, "ShowKills", true);
            config.ShowNearestEnemy = ParseGearsSetting(category, "ShowNearestEnemy", true);
            config.ShowBiome = ParseGearsSetting(category, "ShowBiome", false);
            config.ShowPOI = ParseGearsSetting(category, "ShowPOI", true);
            config.ShowCoords = ParseGearsSetting(category, "ShowCoords", false);
            config.ShowSession = ParseGearsSetting(category, "ShowSession", true);
            
            // Load panel width from PanelSettings category
            var panelSettings = modNode.SelectSingleNode(".//Tab[@name='Info Panel']//Category[@name='PanelSettings']");
            if (panelSettings != null)
            {
                var widthNode = panelSettings.SelectSingleNode("Setting[@name='PanelWidth']");
                if (widthNode?.Attributes?["value"] != null && int.TryParse(widthNode.Attributes["value"].Value, out var width))
                {
                    config.PanelWidth = width;
                }
            }

            HemSoftQoL.Log($"Loaded display settings: Level={config.ShowLevel}, GS={config.ShowGamestage}, LS={config.ShowLootstage}, Day={config.ShowDay}, BM={config.ShowBloodMoon}, Kills={config.ShowKills}, Enemy={config.ShowNearestEnemy}, Width={config.PanelWidth}");

            return true;
        }
        catch (Exception ex)
        {
            HemSoftQoL.LogError($"Failed to load from Gears settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Parse a Setting element from Gears global settings.
    /// Values are "Show"/"Hide" for display toggles.
    /// </summary>
    private static bool ParseGearsSetting(XmlNode category, string name, bool defaultValue)
    {
        var settingNode = category.SelectSingleNode($"Setting[@name='{name}']");
        if (settingNode?.Attributes?["value"] == null) return defaultValue;

        var value = settingNode.Attributes["value"].Value;
        return value.Equals("Show", StringComparison.OrdinalIgnoreCase);
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
        config.ShowBiome = ParseLegacyBool(doc, "Biome", false);
        config.ShowPOI = ParseLegacyBool(doc, "POI", true);
        config.ShowCoords = ParseLegacyBool(doc, "Coords", false);
        config.ShowSession = ParseLegacyBool(doc, "Session", true);
        
        // Load panel width
        var widthNode = doc.SelectSingleNode("//PanelWidth");
        if (widthNode?.Attributes?["value"] != null && int.TryParse(widthNode.Attributes["value"].Value, out var width))
        {
            config.PanelWidth = width;
        }
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
            var category = doc.SelectSingleNode("//Tab[@name='Info Panel']//Category[@name='PanelElements']");
            if (category == null) return;

            // Update each switch value using the rightValue/leftValue text
            UpdateGearsSwitch(category, "ShowLevel", ShowLevel);
            UpdateGearsSwitch(category, "ShowGamestage", ShowGamestage);
            UpdateGearsSwitch(category, "ShowLootstage", ShowLootstage);
            UpdateGearsSwitch(category, "ShowDay", ShowDay);
            UpdateGearsSwitch(category, "ShowBloodMoon", ShowBloodMoon);
            UpdateGearsSwitch(category, "ShowKills", ShowKills);
            UpdateGearsSwitch(category, "ShowNearestEnemy", ShowNearestEnemy);
            UpdateGearsSwitch(category, "ShowBiome", ShowBiome);
            UpdateGearsSwitch(category, "ShowPOI", ShowPOI);
            UpdateGearsSwitch(category, "ShowCoords", ShowCoords);
            UpdateGearsSwitch(category, "ShowSession", ShowSession);

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
            // Use rightValue when true, leftValue when false
            var rightValue = switchNode.Attributes["rightValue"]?.Value ?? "Show";
            var leftValue = switchNode.Attributes["leftValue"]?.Value ?? "Hide";
            switchNode.Attributes["value"].Value = value ? rightValue : leftValue;
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
            AddDisplayElement(doc, display, "Biome", ShowBiome);
            AddDisplayElement(doc, display, "POI", ShowPOI);
            AddDisplayElement(doc, display, "Coords", ShowCoords);
            AddDisplayElement(doc, display, "Session", ShowSession);

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
