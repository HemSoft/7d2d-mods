using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using HarmonyLib;
using HemSoft.QoL;

/// <summary>
/// Configuration window controller for HemSoft QoL settings.
/// Allows toggling info panel display elements and viewing hotkey settings.
/// NOTE: This class is intentionally NOT in a namespace because 7D2D XUi
/// expects controller class names without namespace prefixes.
/// </summary>
public class XUiC_HemSoftConfigWindow : XUiController
{
    public static string ID = "";

    private XUiC_ToggleButton _toggleLevel;
    private XUiC_ToggleButton _toggleGamestage;
    private XUiC_ToggleButton _toggleLootstage;
    private XUiC_ToggleButton _toggleDay;
    private XUiC_ToggleButton _toggleBloodMoon;
    private XUiC_ToggleButton _toggleKills;

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;
        HemSoftQoL.Log($"Config window Init called, ID set to: {ID}");

        // Wire up outclick panel to close window when clicking outside
        var outclick = GetChildById("outclick");
        if (outclick != null)
        {
            outclick.OnPress += (sender, args) => CloseWindow();
            HemSoftQoL.Log("Outclick handler wired up");
        }

        // Get all toggle buttons - they're in a grid, use GetChildrenByType
        var toggles = GetChildrenByType<XUiC_ToggleButton>();
        HemSoftQoL.Log($"Found {toggles.Length} toggle buttons");
        
        foreach (var toggle in toggles)
        {
            var id = toggle.ViewComponent?.ID ?? "unknown";
            HemSoftQoL.Log($"Toggle found: {id}");
            
            switch (id)
            {
                case "toggleLevel": _toggleLevel = toggle; break;
                case "toggleGamestage": _toggleGamestage = toggle; break;
                case "toggleLootstage": _toggleLootstage = toggle; break;
                case "toggleDay": _toggleDay = toggle; break;
                case "toggleBloodMoon": _toggleBloodMoon = toggle; break;
                case "toggleKills": _toggleKills = toggle; break;
            }
        }

        // Wire up buttons - also need to search recursively
        var buttons = GetChildrenByType<XUiC_SimpleButton>();
        HemSoftQoL.Log($"Found {buttons.Length} simple buttons");
        
        foreach (var btn in buttons)
        {
            var id = btn.ViewComponent?.ID ?? "unknown";
            HemSoftQoL.Log($"Button found: {id}");
            
            if (id == "btnSave")
            {
                btn.OnPressed += BtnSave_OnPressed;
                HemSoftQoL.Log("Save button wired up");
            }
            else if (id == "btnCancel")
            {
                btn.OnPressed += BtnCancel_OnPressed;
                HemSoftQoL.Log("Cancel button wired up");
            }
        }

        HemSoftQoL.Log("Config window controller initialized");
    }

    private void CloseWindow()
    {
        xui.playerUI.windowManager.Close(WindowGroup.ID);
    }

    public override void OnOpen()
    {
        base.OnOpen();

        // Load current config values into toggles
        var config = HemSoftQoL.DisplayConfig;

        if (_toggleLevel != null) _toggleLevel.Value = config.ShowLevel;
        if (_toggleGamestage != null) _toggleGamestage.Value = config.ShowGamestage;
        if (_toggleLootstage != null) _toggleLootstage.Value = config.ShowLootstage;
        if (_toggleDay != null) _toggleDay.Value = config.ShowDay;
        if (_toggleBloodMoon != null) _toggleBloodMoon.Value = config.ShowBloodMoon;
        if (_toggleKills != null) _toggleKills.Value = config.ShowKills;

        // Pause the game while config is open
        GameManager.Instance?.Pause(true);
    }

    public override void OnClose()
    {
        base.OnClose();
        GameManager.Instance?.Pause(false);
    }

    private void BtnSave_OnPressed(XUiController sender, int mouseButton)
    {
        // Read values from toggles
        var config = HemSoftQoL.DisplayConfig;

        config.ShowLevel = _toggleLevel?.Value ?? true;
        config.ShowGamestage = _toggleGamestage?.Value ?? true;
        config.ShowLootstage = _toggleLootstage?.Value ?? true;
        config.ShowDay = _toggleDay?.Value ?? true;
        config.ShowBloodMoon = _toggleBloodMoon?.Value ?? true;
        config.ShowKills = _toggleKills?.Value ?? true;

        // Save to file
        config.Save(Path.Combine(HemSoftQoL.ModPath, "Config", "InfoPanelConfig.xml"));

        HemSoftQoL.Log("Config saved");
        GameManager.ShowTooltip(xui.playerUI.entityPlayer, "Settings saved!");

        // Close window
        xui.playerUI.windowManager.Close(WindowGroup.ID);
    }

    private void BtnCancel_OnPressed(XUiController sender, int mouseButton)
    {
        xui.playerUI.windowManager.Close(WindowGroup.ID);
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);

        // Allow ESC to close
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseWindow();
        }
    }
}

/// <summary>
/// Console command to open HemSoft QoL configuration window.
/// Usage: hemsoft (or hs)
/// </summary>
public class ConsoleCmdHemSoft : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new[] { "hemsoft", "hs" };
    }

    public override string getDescription()
    {
        return "Opens HemSoft QoL configuration window";
    }

    public override string getHelp()
    {
        return "Usage: hemsoft\n" +
               "   or: hs\n" +
               "Opens the HemSoft QoL settings window to configure hotkeys and info panel display.";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            // Must be in-game
            if (!GameManager.Instance.gameStateManager.IsGameStarted())
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Must be in-game to open settings.");
                return;
            }

            var player = GameManager.Instance.World?.GetPrimaryPlayer();
            if (player == null)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No player found.");
                return;
            }

            var ui = LocalPlayerUI.GetUIForPlayer(player);
            if (ui == null)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("UI not available.");
                return;
            }

            // Close console first
            ui.windowManager.Close("terminal");

            // Open config window
            ui.windowManager.Open("HemSoftConfigWindow", true);
            HemSoftQoL.Log("Config window opened via console command");
        }
        catch (Exception ex)
        {
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Error: {ex.Message}");
            HemSoftQoL.LogError($"Console command error: {ex.Message}");
        }
    }
}

/// <summary>
/// Configuration for info panel display elements.
/// </summary>
public class DisplayConfig
{
    public bool ShowLevel { get; set; } = true;
    public bool ShowGamestage { get; set; } = true;
    public bool ShowLootstage { get; set; } = true;
    public bool ShowDay { get; set; } = true;
    public bool ShowBloodMoon { get; set; } = true;
    public bool ShowKills { get; set; } = true;

    public static DisplayConfig Load(string path)
    {
        var config = new DisplayConfig();

        try
        {
            if (!File.Exists(path))
            {
                HemSoftQoL.Log($"Display config not found at {path}, using defaults.");
                return config;
            }

            var doc = new XmlDocument();
            doc.Load(path);

            config.ShowLevel = ParseBool(doc, "Level", true);
            config.ShowGamestage = ParseBool(doc, "Gamestage", true);
            config.ShowLootstage = ParseBool(doc, "Lootstage", true);
            config.ShowDay = ParseBool(doc, "Day", true);
            config.ShowBloodMoon = ParseBool(doc, "BloodMoon", true);
            config.ShowKills = ParseBool(doc, "Kills", true);

            HemSoftQoL.Log($"Display config loaded from {path}");
        }
        catch (Exception ex)
        {
            HemSoftQoL.LogError($"Failed to load display config: {ex.Message}");
        }

        return config;
    }

    private static bool ParseBool(XmlDocument doc, string name, bool defaultValue)
    {
        var node = doc.SelectSingleNode($"//Display/{name}");
        if (node?.Attributes?["enabled"] == null) return defaultValue;

        return node.Attributes["enabled"].Value.ToLower() == "true";
    }

    public void Save(string path)
    {
        try
        {
            var doc = new XmlDocument();
            var decl = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(decl);

            var comment = doc.CreateComment(@"
  HemSoft QoL - Info Panel Display Configuration
  
  Set enabled=""true"" to show, enabled=""false"" to hide each element.
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

            doc.Save(path);
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
