using HemSoft.QoL;

/// <summary>
/// Popup notification controller for lootstage changes.
/// Displays a temporary notification when the player's lootstage changes.
/// The notification is embedded in the HUD as a sibling rect to the info panel.
/// It uses visibility binding + alpha fade for smooth appearance/disappearance.
/// NOTE: This class is intentionally NOT in a namespace because 7D2D XUi
/// expects controller class names without namespace prefixes for Type.GetType().
/// </summary>
public class XUiC_HemSoftLootstagePopup : XUiController
{
    // Instance fields - this controller persists in the HUD
    private int _newLootstage;
    private int _oldLootstage;
    private float _displayTimer;
    private bool _isVisible;
    private int _currentAlpha = 220;
    private bool _initLogged;
    private int _updateCount;
    private int _bindingCallCount;
    private bool _assumptionsValidated;
    private float _heartbeatTimer;
    private int _showCount;
    private int _heartbeatCount;
    private int _visibleUpdateLogCount;
    private int _visibilityBindingLogCount;
    
    private const float DisplayDuration = 10.0f; // Show popup for 10 seconds
    private const float FadeStartTime = 2.0f;    // Start fading with 2 seconds left
    private const float HeartbeatInterval = 30.0f; // Log heartbeat every 30 seconds
    
    // LOG CAPS - prevent spam over long sessions
    private const int MaxHeartbeatLogs = 20;        // ~10 minutes of heartbeats
    private const int MaxVisibleUpdateLogs = 50;    // ~5 notifications worth
    private const int MaxVisibilityBindingLogs = 30; // enough to confirm it works

    public override void Init()
    {
        base.Init();
        _isVisible = false;
        _displayTimer = 0f;
        _currentAlpha = 0;
        _initLogged = false;
        _updateCount = 0;
        _bindingCallCount = 0;
        _assumptionsValidated = false;
        _heartbeatTimer = 0f;
        _showCount = 0;
        _heartbeatCount = 0;
        _visibleUpdateLogCount = 0;
        _visibilityBindingLogCount = 0;
        
        // Log detailed init info
        var viewExists = ViewComponent != null;
        var transformExists = ViewComponent?.UiTransform != null;
        var id = ViewComponent?.ID ?? "null";
        Log($"[INIT] Controller initialized - ViewComponent={viewExists}, UiTransform={transformExists}, ID={id}");
        
        // ASSUMPTION VALIDATION: Controller should be attached to rect with ID "hemsoft_lootstage_notification"
        Log($"[ASSUME] EXPECT: ID='hemsoft_lootstage_notification', ACTUAL: ID='{id}'");
        if (id != "hemsoft_lootstage_notification")
        {
            Log($"[ASSUME] WARNING: ID mismatch! Controller may be attached to wrong element");
        }
        
        // Log Unity GameObject state
        LogGameObjectState("[INIT]");
    }

    /// <summary>
    /// Log detailed Unity GameObject state for debugging visibility issues.
    /// </summary>
    private void LogGameObjectState(string prefix)
    {
        try
        {
            if (ViewComponent?.UiTransform == null)
            {
                Log($"{prefix} [UNITY] UiTransform is null, cannot inspect GameObject");
                return;
            }
            
            var go = ViewComponent.UiTransform.gameObject;
            var activeSelf = go.activeSelf;
            var activeInHierarchy = go.activeInHierarchy;
            var layer = go.layer;
            var layerName = UnityEngine.LayerMask.LayerToName(layer);
            
            Log($"{prefix} [UNITY] GameObject: activeSelf={activeSelf}, activeInHierarchy={activeInHierarchy}, layer={layer} ({layerName})");
            
            // Check parent chain for inactive objects
            var parent = go.transform.parent;
            var depth = 0;
            while (parent != null && depth < 10)
            {
                var parentGo = parent.gameObject;
                if (!parentGo.activeSelf)
                {
                    Log($"{prefix} [UNITY] WARNING: Parent '{parentGo.name}' at depth {depth} is INACTIVE!");
                }
                parent = parent.parent;
                depth++;
            }
            
            // Log screen position
            var screenPos = UnityEngine.Camera.main?.WorldToScreenPoint(ViewComponent.UiTransform.position);
            Log($"{prefix} [UNITY] ScreenPos={screenPos}, LocalPos={ViewComponent.UiTransform.localPosition}");
            
            // Log scale (zero scale = invisible)
            var scale = ViewComponent.UiTransform.localScale;
            if (scale.x == 0 || scale.y == 0 || scale.z == 0)
            {
                Log($"{prefix} [UNITY] WARNING: Scale has zero component: {scale}");
            }
        }
        catch (System.Exception ex)
        {
            Log($"{prefix} [UNITY] Exception inspecting GameObject: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate assumptions about the runtime environment.
    /// Called once when notification is first shown.
    /// </summary>
    private void ValidateAssumptions()
    {
        if (_assumptionsValidated) return;
        _assumptionsValidated = true;
        
        Log($"[ASSUME] === ASSUMPTION VALIDATION START ===");
        
        // 1. ViewComponent should exist
        Log($"[ASSUME] #1 ViewComponent exists: EXPECT=true, ACTUAL={ViewComponent != null}");
        
        // 2. UiTransform should exist
        Log($"[ASSUME] #2 UiTransform exists: EXPECT=true, ACTUAL={ViewComponent?.UiTransform != null}");
        
        // 3. ID should be hemsoft_lootstage_notification
        var id = ViewComponent?.ID ?? "null";
        Log($"[ASSUME] #3 ViewComponent.ID: EXPECT='hemsoft_lootstage_notification', ACTUAL='{id}'");
        
        // 4. Config should be loaded
        var config = HemSoftQoL.DisplayConfig;
        Log($"[ASSUME] #4 DisplayConfig loaded: EXPECT=true, ACTUAL={config != null}");
        
        // 5. Config values should be reasonable
        if (config != null)
        {
            Log($"[ASSUME] #5a NotificationX: EXPECT=800-1200, ACTUAL={config.NotificationX}");
            Log($"[ASSUME] #5b NotificationY: EXPECT=500-800, ACTUAL={config.NotificationY}");
            Log($"[ASSUME] #5c NotificationWidth: EXPECT=400-1000, ACTUAL={config.NotificationWidth}");
            Log($"[ASSUME] #5d TitleFontSize: EXPECT=18-48, ACTUAL={config.NotificationTitleFontSize}");
            Log($"[ASSUME] #5e ValueFontSize: EXPECT=14-40, ACTUAL={config.NotificationValueFontSize}");
            Log($"[ASSUME] #5f ShowLootstagePopup: EXPECT=true, ACTUAL={config.ShowLootstagePopup}");
        }
        
        // 6. Parent hierarchy check
        var parent = Parent;
        var parentId = parent?.ViewComponent?.ID ?? "null";
        var parentType = parent?.GetType().Name ?? "null";
        Log($"[ASSUME] #6 Parent: ID='{parentId}', Type={parentType}");
        
        // 7. WindowGroup check
        var wg = windowGroup;
        var wgName = wg?.ID ?? "null";
        Log($"[ASSUME] #7 WindowGroup: EXPECT='HUDLeftStatBars' or 'RWSUI_LeftHUD', ACTUAL='{wgName}'");
        
        // 8. Initial visibility state
        Log($"[ASSUME] #8 Initial _isVisible: EXPECT=false (before show), ACTUAL={_isVisible}");
        Log($"[ASSUME] #8 Initial _currentAlpha: EXPECT=0, ACTUAL={_currentAlpha}");
        
        // 9. Log full parent hierarchy
        LogParentHierarchy();
        
        // 10. Log Unity state
        LogGameObjectState("[ASSUME]");
        
        // 11. Screen info
        Log($"[ASSUME] #11 Screen: {UnityEngine.Screen.width}x{UnityEngine.Screen.height}");
        
        Log($"[ASSUME] === ASSUMPTION VALIDATION END ===");
    }
    
    /// <summary>
    /// Log the full parent hierarchy for debugging.
    /// </summary>
    private void LogParentHierarchy()
    {
        Log($"[HIERARCHY] === PARENT HIERARCHY ===");
        var current = this as XUiController;
        var depth = 0;
        while (current != null && depth < 15)
        {
            var id = current.ViewComponent?.ID ?? "null";
            var type = current.GetType().Name;
            var isWindow = current.GetType().Name.Contains("Window");
            Log($"[HIERARCHY] Depth {depth}: ID='{id}', Type={type}, IsWindow={isWindow}");
            current = current.Parent;
            depth++;
        }
        Log($"[HIERARCHY] === END HIERARCHY ===");
    }

    /// <summary>
    /// Called by HemSoftInfoPanel to show the notification.
    /// This is an INSTANCE method - InfoPanel finds this controller via GetChildById().
    /// </summary>
    public void ShowNotification(int newLootstage, int oldLootstage)
    {
        _showCount++;
        Log($"[SHOW] === SHOW NOTIFICATION #{_showCount} ===");
        Log($"[SHOW] ShowNotification called: old={oldLootstage}, new={newLootstage}");
        
        // Validate assumptions on first show
        ValidateAssumptions();
        
        var prevVisible = _isVisible;
        var prevAlpha = _currentAlpha;
        var prevTimer = _displayTimer;
        
        _newLootstage = newLootstage;
        _oldLootstage = oldLootstage;
        _displayTimer = DisplayDuration;
        _isVisible = true;
        _currentAlpha = 220;
        
        // Log state transition
        Log($"[SHOW] State transition: visible {prevVisible}→{_isVisible}, alpha {prevAlpha}→{_currentAlpha}, timer {prevTimer:F1}→{_displayTimer:F1}");
        
        // Log config values
        var config = HemSoftQoL.DisplayConfig;
        Log($"[SHOW] Config: X={config?.NotificationX}, Y={config?.NotificationY}, Width={config?.NotificationWidth}");
        
        ApplyLayout();
        
        // Log ViewComponent state before refresh
        LogGameObjectState("[SHOW]");
        
        Log($"[SHOW] Calling RefreshBindings(false)...");
        RefreshBindings(false);
        Log($"[SHOW] RefreshBindings completed");
        
        // Verify bindings are working
        string testValue = "";
        var gotBinding = GetBindingValueInternal(ref testValue, "notificationvisible");
        Log($"[SHOW] Verify: GetBindingValueInternal('notificationvisible') returned {gotBinding}, value='{testValue}'");
        
        Log($"[SHOW] === SHOW COMPLETE ===");
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        
        _updateCount++;
        _heartbeatTimer += _dt;
        
        // Log first update to confirm Update is being called
        if (!_initLogged && _updateCount == 1)
        {
            _initLogged = true;
            Log($"[UPDATE] First Update called - controller is active, _isVisible={_isVisible}");
        }
        
        // Periodic heartbeat log (every 30 seconds, capped)
        if (_heartbeatTimer >= HeartbeatInterval)
        {
            _heartbeatTimer = 0f;
            _heartbeatCount++;
            if (_heartbeatCount <= MaxHeartbeatLogs)
            {
                Log($"[HEARTBEAT] Controller alive: updates={_updateCount}, shows={_showCount}, bindings={_bindingCallCount}, visible={_isVisible}");
                if (_heartbeatCount == MaxHeartbeatLogs)
                {
                    Log($"[HEARTBEAT] Max heartbeat logs reached ({MaxHeartbeatLogs}), suppressing future heartbeats");
                }
            }
        }

        if (!_isVisible) return;

        // Log periodic status while visible (every ~1 second assuming 60fps, capped)
        if (_updateCount % 60 == 0 && _visibleUpdateLogCount < MaxVisibleUpdateLogs)
        {
            _visibleUpdateLogCount++;
            Log($"[UPDATE] Visible: timer={_displayTimer:F1}s, alpha={_currentAlpha}");
        }

        // Count down display timer
        _displayTimer -= _dt;
        
        // Start fading when timer reaches FadeStartTime
        if (_displayTimer <= FadeStartTime && _displayTimer > 0)
        {
            // Linear fade from 220 to 0 over FadeStartTime seconds
            float fadeProgress = 1.0f - (_displayTimer / FadeStartTime);
            int newAlpha = (int)(220 * (1.0f - fadeProgress));
            if (newAlpha < 0) newAlpha = 0;
            
            // Log when fade starts
            if (_currentAlpha == 220 && newAlpha < 220)
            {
                Log($"[FADE] Starting fade: timer={_displayTimer:F2}s, alpha {_currentAlpha}→{newAlpha}");
            }
            
            _currentAlpha = newAlpha;
            RefreshBindings(false);
        }
        
        // Hide when timer expires
        if (_displayTimer <= 0)
        {
            Log($"[HIDE] Timer expired - hiding notification (was visible for {DisplayDuration}s)");
            _isVisible = false;
            _currentAlpha = 0;
            RefreshBindings(false);
            LogGameObjectState("[HIDE]");
        }
    }

    public override bool GetBindingValueInternal(ref string value, string bindingName)
    {
        _bindingCallCount++;
        
        // Log first few binding calls to confirm bindings are being queried
        if (_bindingCallCount <= 20)
        {
            Log($"[BIND] GetBinding #{_bindingCallCount}: {bindingName}");
        }
        
        var config = HemSoftQoL.DisplayConfig;
        
        switch (bindingName)
        {
            case "notificationvisible":
                value = _isVisible ? "true" : "false";
                // Log visibility binding when visible (throttled and capped)
                if (_isVisible && _bindingCallCount % 30 == 0 && _visibilityBindingLogCount < MaxVisibilityBindingLogs)
                {
                    _visibilityBindingLogCount++;
                    Log($"[BIND] notificationvisible = {value} (alpha={_currentAlpha}, timer={_displayTimer:F1}s)");
                }
                return true;

            case "notificationbackground":
                value = $"20,20,25,{_currentAlpha}";
                return true;

            case "notificationborder":
                var borderColor = (_newLootstage > _oldLootstage) ? "100,200,100" : "200,100,100";
                value = $"{borderColor},{_currentAlpha}";
                return true;

            case "notificationtitlebar":
                var titlebarColor = (_newLootstage > _oldLootstage) ? "50,120,50" : "120,50,50";
                value = $"{titlebarColor},{_currentAlpha}";
                return true;

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
                    value = "Lootstage Decreased!";
                else
                    value = "Lootstage Updated";
                return true;

            case "lootstagesubtitle":
                var delta = _newLootstage - _oldLootstage;
                if (delta > 0)
                    value = "Better loot will now drop!";
                else if (delta < 0)
                    value = "Loot quality has decreased.";
                else
                    value = "";
                return true;

            case "lootstagecolor":
                var d = _newLootstage - _oldLootstage;
                if (d > 0)
                    value = $"100,255,100,{_currentAlpha}"; // Green = increase
                else if (d < 0)
                    value = $"255,100,100,{_currentAlpha}"; // Red = decrease
                else
                    value = $"255,255,100,{_currentAlpha}"; // Yellow = unchanged
                return true;

            case "subtitlecolor":
                value = $"200,200,200,{_currentAlpha}";
                return true;

            case "valuecolor":
                value = $"180,180,180,{_currentAlpha}";
                return true;

            // Font size bindings from config
            case "notificationtitlefontsize":
                value = (config?.NotificationTitleFontSize ?? 36).ToString();
                return true;

            case "notificationvaluefontsize":
                value = (config?.NotificationValueFontSize ?? 32).ToString();
                return true;

            // Dimension bindings from config
            case "notificationwidth":
                value = (config?.NotificationWidth ?? 700).ToString();
                return true;

            default:
                // Log unknown bindings (might indicate XML has bindings we don't handle)
                if (_bindingCallCount <= 100)
                {
                    Log($"[BIND] UNKNOWN binding: '{bindingName}' - passing to base");
                }
                return base.GetBindingValueInternal(ref value, bindingName);
        }
    }

    /// <summary>
    /// Apply layout and position the notification from config settings.
    /// </summary>
    private void ApplyLayout()
    {
        Log($"[LAYOUT] ApplyLayout called");
        
        try
        {
            if (ViewComponent == null)
            {
                Log($"[LAYOUT] ERROR: ViewComponent is null!");
                return;
            }
            
            if (ViewComponent.UiTransform == null)
            {
                Log($"[LAYOUT] ERROR: UiTransform is null!");
                return;
            }
            
            var config = HemSoftQoL.DisplayConfig;
            int x = config?.NotificationX ?? 960;
            int y = config?.NotificationY ?? 650;
            int width = config?.NotificationWidth ?? 700;
            
            // Log screen bounds check
            var screenW = UnityEngine.Screen.width;
            var screenH = UnityEngine.Screen.height;
            Log($"[LAYOUT] Screen: {screenW}x{screenH}, Target pos: ({x},{y}), width={width}");
            
            if (x < 0 || x > screenW || y < 0 || y > screenH)
            {
                Log($"[LAYOUT] WARNING: Position ({x},{y}) may be outside screen bounds!");
            }
            
            var oldPos = ViewComponent.UiTransform.localPosition;
            Log($"[LAYOUT] Old localPosition: {oldPos}");
            
            // Set position from config
            ViewComponent.UiTransform.localPosition = new UnityEngine.Vector3(x, y, 0);
            
            var newPos = ViewComponent.UiTransform.localPosition;
            Log($"[LAYOUT] New localPosition: {newPos}");
            
            // Also log world position
            var worldPos = ViewComponent.UiTransform.position;
            Log($"[LAYOUT] World position: {worldPos}");
            
            // Try to resize the rect
            var rectTransform = ViewComponent.UiTransform as UnityEngine.RectTransform;
            if (rectTransform != null)
            {
                var oldSize = rectTransform.sizeDelta;
                rectTransform.sizeDelta = new UnityEngine.Vector2(width, 110);
                var newSize = rectTransform.sizeDelta;
                Log($"[LAYOUT] Size: {oldSize} → {newSize}");
                Log($"[LAYOUT] Anchors: min={rectTransform.anchorMin}, max={rectTransform.anchorMax}");
                Log($"[LAYOUT] Pivot: {rectTransform.pivot}");
            }
            else
            {
                Log($"[LAYOUT] WARNING: UiTransform is not a RectTransform (type={ViewComponent.UiTransform.GetType().Name})");
            }
            
            Log($"[LAYOUT] Complete: pos=({x},{y}), width={width}");
        }
        catch (System.Exception ex)
        {
            Log($"[LAYOUT] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Log($"[LAYOUT] Stack: {ex.StackTrace}");
        }
    }

    private static void Log(string message)
    {
        UnityEngine.Debug.Log($"[HemSoft QoL] LootstagePopup {message}");
    }
}
