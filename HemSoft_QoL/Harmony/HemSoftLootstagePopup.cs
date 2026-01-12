/// <summary>
/// Popup notification controller for lootstage changes.
/// Displays a temporary notification when the player's lootstage increases.
/// NOTE: This class is intentionally NOT in a namespace because 7D2D XUi
/// expects controller class names without namespace prefixes for Type.GetType().
/// </summary>
public class XUiC_HemSoftLootstagePopup : XUiController
{
    private int _newLootstage;
    private int _oldLootstage;
    private float _displayTimer;
    private const float DisplayDuration = 5.0f; // Show popup for 5 seconds
    private bool _isDirty = true;

    public override void Init()
    {
        base.Init();
        Log("Lootstage popup controller initialized");
    }

    /// <summary>
    /// Called by HemSoftInfoPanel to display a lootstage change notification.
    /// </summary>
    public void ShowNotification(int newLootstage, int oldLootstage)
    {
        _newLootstage = newLootstage;
        _oldLootstage = oldLootstage;
        _displayTimer = DisplayDuration;
        _isDirty = true;
        RefreshBindings(false);
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);

        // Count down display timer
        if (_displayTimer > 0)
        {
            _displayTimer -= _dt;
            
            // Auto-close when timer expires
            if (_displayTimer <= 0)
            {
                var windowManager = xui?.playerUI?.windowManager;
                if (windowManager != null)
                {
                    windowManager.Close("hemsoft_lootstage_popup");
                }
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

            default:
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        _isDirty = true;
    }

    public override void OnClose()
    {
        base.OnClose();
        _displayTimer = 0;
    }

    private static void Log(string message)
    {
        UnityEngine.Debug.Log($"[HemSoft QoL] {message}");
    }
}
