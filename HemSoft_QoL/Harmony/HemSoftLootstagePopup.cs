using HemSoft.QoL;

/// <summary>
/// Popup notification controller for lootstage changes.
/// Displays a temporary non-blocking notification when the player's lootstage increases.
/// NOTE: This class is intentionally NOT in a namespace because 7D2D XUi
/// expects controller class names without namespace prefixes for Type.GetType().
/// </summary>
public class XUiC_HemSoftLootstagePopup : XUiController
{
    private int _newLootstage;
    private int _oldLootstage;
    private float _displayTimer;
    private const float DisplayDuration = 10.0f; // Show notification for 10 seconds
    private const float FadeOutStart = 8.0f; // Start fading at 8 seconds (2 sec fade)
    private bool _isDirty = true;
    private bool _isVisible;
    private float _currentAlpha = 255f;
    
    // Position and size from config (can be overridden via console: hs lootpos X Y)
    public static float PositionX;
    public static float PositionY;
    public static int Width;
    public static int TitleFontSize;
    public static int ValueFontSize;

    public override void Init()
    {
        base.Init();
        
        // Load settings from config on initialization
        var config = HemSoftQoL.DisplayConfig;
        if (config != null)
        {
            PositionX = config.NotificationX;
            PositionY = config.NotificationY;
            Width = config.NotificationWidth;
            TitleFontSize = config.NotificationTitleFontSize;
            ValueFontSize = config.NotificationValueFontSize;
        }
        else
        {
            // Fallback defaults
            PositionX = 960f;
            PositionY = 650f;
            Width = 800;
            TitleFontSize = 36;
            ValueFontSize = 32;
        }
        
        ViewComponent.IsVisible = false; // Start hidden
        ApplyLayout();
        PositionAtScreenCenter();
        Log("Lootstage notification controller initialized");
    }

    /// <summary>
    /// Called by HemSoftInfoPanel to display a lootstage change notification.
    /// </summary>
    public void ShowNotification(int newLootstage, int oldLootstage)
    {
        _newLootstage = newLootstage;
        _oldLootstage = oldLootstage;
        _displayTimer = DisplayDuration;
        _isVisible = true;
        _currentAlpha = 220f; // Full opacity
        _isDirty = true;
        
        // Recalculate position and size in case settings changed
        ApplyLayout();
        PositionAtScreenCenter();
        
        RefreshBindings(false);
        
        Log($"Showing lootstage notification: {oldLootstage} → {newLootstage}");
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);

        // Count down display timer
        if (_isVisible && _displayTimer > 0)
        {
            _displayTimer -= _dt;
            
            // Calculate fade alpha
            if (_displayTimer <= FadeOutStart)
            {
                // Fade from 220 to 0 over 2 seconds
                _currentAlpha = (_displayTimer / FadeOutStart) * 220f;
                _isDirty = true; // Force refresh during fade
            }
            else
            {
                _currentAlpha = 220f; // Full opacity
            }
            
            // Auto-hide when timer expires
            if (_displayTimer <= 0)
            {
                _isVisible = false;
                _currentAlpha = 0f;
                _isDirty = true;
                Log("Lootstage notification hidden (timer expired)");
            }
        }

        // Update bindings if needed
        if (_isDirty)
        {
            RefreshBindings(false);
            _isDirty = false;
        }
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "newlootstage":
                value = _newLootstage.ToString();
                return true;

            case "oldlootstage":
                value = _oldLootstage.ToString();
                return true;

            case "lootstagediff":
                var diff = _newLootstage - _oldLootstage;
                value = diff > 0 ? $"+{diff}" : diff.ToString();
                return true;

            case "lootstagemessage":
                var change = _newLootstage - _oldLootstage;
                if (change > 0)
                    value = "Lootstage Increased!";
                else if (change < 0)
                    value = "Lootstage Decreased";
                else
                    value = "Lootstage Updated";
                return true;

            case "lootstagecolor":
                var delta = _newLootstage - _oldLootstage;
                if (delta > 0)
                    value = "100,255,100,255"; // Green = increase
                else if (delta < 0)
                    value = "255,100,100,255"; // Red = decrease
                else
                    value = "255,255,100,255"; // Yellow = unchanged
                return true;

            case "notificationvisible":
                value = _isVisible ? "true" : "false";
                return true;

            case "notificationbackground":
                value = $"20,20,25,{(int)_currentAlpha}";
                return true;

            case "notificationborder":
                value = $"100,200,100,{(int)(_currentAlpha * 0.9f)}";
                return true;

            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _isDirty = true;
    }

    /// <summary>
    /// Dynamically applies width and font sizes from config to notification elements.
    /// Similar to HemSoftInfoPanel.UpdateLayout().
    /// </summary>
    private void ApplyLayout()
    {
        if (ViewComponent == null) return;
        
        // Fixed height (unchanged from XML)
        const int NotificationHeight = 160;
        
        // Resize main panel
        ViewComponent.Size = new Vector2i(Width, NotificationHeight);
        
        // Update background sprite
        var background = GetChildById("background");
        if (background?.ViewComponent != null)
        {
            background.ViewComponent.Size = new Vector2i(Width, NotificationHeight);
        }
        
        // Update border sprite
        var border = GetChildById("border");
        if (border?.ViewComponent != null)
        {
            border.ViewComponent.Size = new Vector2i(Width, NotificationHeight);
        }
        
        // Update title label (first label with lootstagemessage binding)
        // Position: centered horizontally with padding, font size from config
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child?.ViewComponent is XUiV_Label label)
            {
                // Check if this is the title label (has lootstagemessage binding)
                string bindingValue = "";
                if (child.GetBindingValueInternal(ref bindingValue, "text") && bindingValue.Contains("lootstagemessage"))
                {
                    // Title label - update width and font size
                    int labelWidth = Width - 40; // 20px padding each side
                    label.Size = new Vector2i(labelWidth, 50);
                    label.FontSize = TitleFontSize;
                    continue;
                }
                
                // Check if this is the values label (has oldlootstage binding)
                if (child.GetBindingValueInternal(ref bindingValue, "text") && bindingValue.Contains("oldlootstage"))
                {
                    // Values label - update width and font size
                    int labelWidth = Width / 2; // Centered, 50% of panel width
                    int labelX = Width / 4; // Centered horizontally
                    label.Position = new Vector2i(labelX, label.Position.y);
                    label.Size = new Vector2i(labelWidth, 40);
                    label.FontSize = ValueFontSize;
                }
            }
        }
        
        Log($"Notification layout updated: width={Width}, titleFont={TitleFontSize}, valueFont={ValueFontSize}");
    }
    
    /// <summary>
    /// Positions the notification near the center-top of the viewport.
    /// Uses adjustable static coordinates that can be changed via console.
    /// WORKING BACKUP: ViewComponent.UiTransform.localPosition = new UnityEngine.Vector3(0, 50, 0); (with anchor="CenterTop" in XML)
    /// </summary>
    private void PositionAtScreenCenter()
    {
        ViewComponent.UiTransform.localPosition = new UnityEngine.Vector3(PositionX, PositionY, 0);
        Log($"Positioned notification at ({PositionX}, {PositionY})");
    }

    private static void Log(string message)
    {
        UnityEngine.Debug.Log($"[HemSoft QoL] {message}");
    }
}
