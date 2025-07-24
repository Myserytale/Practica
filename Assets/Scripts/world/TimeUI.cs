using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    [Header("UI References")]
    public Text timeText;
    public Text dayCountText;
    public Image timeIcon;
    public Slider timeSlider;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private int dayCount = 1;
    
    void Start()
    {
        Debug.Log("=== TimeUI START ===");
        
        // Auto-find UI elements
        AutoFindUIElements();
        
        // Connect to DayNightCycle
        ConnectToDayNightCycle();
        
        // Force initial display
        ForceInitialDisplay();
        
        Debug.Log("=== TimeUI SETUP COMPLETE ===");
    }
    
        void AutoFindUIElements()
    {
        Debug.Log("=== AUTO-FINDING UI ELEMENTS ===");
        
        // Find TimeText (your "TimeDisplay")
        if (timeText == null)
        {
            timeText = transform.Find("TimeDisplay")?.GetComponent<Text>();  // ✅ Changed
            if (timeText == null)
            {
                // Try finding any Text component
                Text[] allTexts = GetComponentsInChildren<Text>();
                if (allTexts.Length > 0)
                {
                    timeText = allTexts[0];
                    Debug.Log($"Found TimeText via GetComponentsInChildren: {timeText.name}");
                }
            }
            else
            {
                Debug.Log($"Found TimeText by name: {timeText.name}");
            }
        }
        
        // Find DayCountText (your "DayCounter")
        if (dayCountText == null)
        {
            dayCountText = transform.Find("DayCounter")?.GetComponent<Text>();  // ✅ Changed
            if (dayCountText == null)
            {
                // Try finding second Text component
                Text[] allTexts = GetComponentsInChildren<Text>();
                if (allTexts.Length > 1)
                {
                    dayCountText = allTexts[1];
                    Debug.Log($"Found DayCountText via GetComponentsInChildren: {dayCountText.name}");
                }
            }
            else
            {
                Debug.Log($"Found DayCountText by name: {dayCountText.name}");
            }
        }
        
        // Find TimeSlider (your "TimeProgress")
        if (timeSlider == null)
        {
            timeSlider = transform.Find("TimeProgress")?.GetComponent<Slider>();  // ✅ Changed
            if (timeSlider == null)
            {
                timeSlider = GetComponentInChildren<Slider>();
                if (timeSlider != null)
                    Debug.Log($"Found TimeSlider via GetComponentsInChildren: {timeSlider.name}");
            }
            else
            {
                Debug.Log($"Found TimeSlider by name: {timeSlider.name}");
            }
        }
        
        // Find TimeIcon (your "TimeIcon" - this one is correct)
        if (timeIcon == null)
        {
            timeIcon = transform.Find("TimeIcon")?.GetComponent<Image>();  // ✅ Already correct
            if (timeIcon == null)
            {
                timeIcon = GetComponentInChildren<Image>();
                if (timeIcon != null)
                    Debug.Log($"Found TimeIcon via GetComponentsInChildren: {timeIcon.name}");
            }
            else
            {
                Debug.Log($"Found TimeIcon by name: {timeIcon.name}");
            }
        }
        
        // Summary
        Debug.Log($"UI ELEMENTS FOUND:");
        Debug.Log($"- TimeText: {timeText != null} ({timeText?.name})");
        Debug.Log($"- DayCountText: {dayCountText != null} ({dayCountText?.name})");
        Debug.Log($"- TimeSlider: {timeSlider != null} ({timeSlider?.name})");
        Debug.Log($"- TimeIcon: {timeIcon != null} ({timeIcon?.name})");
    }
    
    void ConnectToDayNightCycle()
    {
        Debug.Log("=== CONNECTING TO DAY/NIGHT CYCLE ===");
        
        if (DayNightCycle.Instance != null)
        {
            Debug.Log($"DayNightCycle.Instance found: {DayNightCycle.Instance.name}");
            Debug.Log($"Current time: {DayNightCycle.Instance.currentTime}");
            
            // Subscribe to events
            DayNightCycle.Instance.OnTimeChanged += UpdateTimeDisplay;
            DayNightCycle.Instance.OnMidnight += OnNewDay;
            DayNightCycle.Instance.OnDayNightChange += OnDayNightChange;
            
            Debug.Log("Successfully subscribed to DayNightCycle events");
        }
        else
        {
            Debug.LogError("DayNightCycle.Instance is NULL! Make sure DayNightCycle script is in the scene and starts before TimeUI.");
        }
    }
    
    void ForceInitialDisplay()
    {
        Debug.Log("=== FORCING INITIAL DISPLAY ===");
        
        // Set default values to make sure UI is visible
        if (timeText != null)
        {
            timeText.text = "06:00";
            timeText.color = Color.white;
            timeText.gameObject.SetActive(true);
            Debug.Log($"TimeText set to: {timeText.text}");
        }
        
        if (dayCountText != null)
        {
            dayCountText.text = "Day 1";
            dayCountText.color = Color.white;
            dayCountText.gameObject.SetActive(true);
            Debug.Log($"DayCountText set to: {dayCountText.text}");
        }
        
        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 1f;
            timeSlider.value = 0.25f; // Dawn
            timeSlider.gameObject.SetActive(true);
            Debug.Log($"TimeSlider set to: {timeSlider.value}");
        }
        
        if (timeIcon != null)
        {
            timeIcon.color = Color.white;
            timeIcon.gameObject.SetActive(true);
            Debug.Log("TimeIcon made visible");
        }
        
        // Try to update with current DayNightCycle time
        if (DayNightCycle.Instance != null)
        {
            UpdateTimeDisplay(DayNightCycle.Instance.currentTime);
        }
    }
    
    void UpdateTimeDisplay(float currentTime)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"=== UpdateTimeDisplay called with time: {currentTime:F3} ===");
        }
        
        if (DayNightCycle.Instance == null)
        {
            Debug.LogError("DayNightCycle.Instance is null in UpdateTimeDisplay!");
            return;
        }
        
        // Update time text
        if (timeText != null)
        {
            string timeString = DayNightCycle.Instance.GetTimeString();
            timeText.text = timeString;
            if (enableDebugLogs)
                Debug.Log($"Updated timeText to: {timeString}");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("timeText is null - cannot update");
        }
        
        // Update time slider - THIS IS THE KEY PART
        if (timeSlider != null)
        {
            float oldValue = timeSlider.value;
            timeSlider.value = currentTime;
            
            if (enableDebugLogs)
                Debug.Log($"Updated timeSlider: {oldValue:F3} → {currentTime:F3}");
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("timeSlider is null - cannot update");
        }
        
        // Update day counter
        if (dayCountText != null)
        {
            dayCountText.text = $"Day {dayCount}";
            if (enableDebugLogs)
                Debug.Log($"Updated dayCountText to: Day {dayCount}");
        }
        
        // Update icon color based on time of day
        if (timeIcon != null)
        {
            if (DayNightCycle.Instance.IsDay())
                timeIcon.color = Color.yellow;
            else if (DayNightCycle.Instance.IsNight())
                timeIcon.color = Color.blue;
            else
                timeIcon.color = Color.orange;
        }
    }
    
    void OnNewDay()
    {
        dayCount++;
        Debug.Log($"=== NEW DAY! Count: {dayCount} ===");
    }
    
    void OnDayNightChange(bool isDay)
    {
        Debug.Log($"=== DAY/NIGHT CHANGE: {(isDay ? "DAY" : "NIGHT")} ===");
    }
    
    void OnDestroy()
    {
        Debug.Log("=== TimeUI DESTROYED - UNSUBSCRIBING ===");
        
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.OnTimeChanged -= UpdateTimeDisplay;
            DayNightCycle.Instance.OnMidnight -= OnNewDay;
            DayNightCycle.Instance.OnDayNightChange -= OnDayNightChange;
        }
    }
    
    // Manual test methods
    [ContextMenu("Force Update UI")]
    void ForceUpdateUI()
    {
        if (DayNightCycle.Instance != null)
        {
            UpdateTimeDisplay(DayNightCycle.Instance.currentTime);
        }
        else
        {
            UpdateTimeDisplay(0.5f); // Test with noon
        }
    }
    
    [ContextMenu("Test Slider")]
    void TestSlider()
    {
        if (timeSlider != null)
        {
            Debug.Log($"Testing slider - current value: {timeSlider.value}");
            timeSlider.value = 0.75f; // Test value
            Debug.Log($"Set slider to: {timeSlider.value}");
        }
        else
        {
            Debug.LogError("TimeSlider is null!");
        }
    }
}