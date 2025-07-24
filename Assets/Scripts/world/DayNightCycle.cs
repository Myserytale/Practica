using UnityEngine;
using System;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;
    
    [Header("Time Settings")]
    [Tooltip("Length of a full day in real-world seconds")]
    public float dayDuration = 300f; // 5 minutes = full day
    
    [Tooltip("Starting time (0 = midnight, 0.5 = noon)")]
    [Range(0f, 1f)]
    public float startTime = 0.25f; // Start at dawn
    
    [Header("Sun Settings")]
    public Light sunLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensityCurve;
    
    [Header("Sky Settings")]
    public Material skyboxMaterial;
    public Gradient skyTint;
    public AnimationCurve skyExposure;
    
    [Header("Fog Settings")]
    public bool enableFog = true;
    public Gradient fogColor;
    public AnimationCurve fogDensity;
    
    [Header("Ambient Lighting")]
    public Gradient ambientColor;
    public AnimationCurve ambientIntensity;
    
    [Header("Day/Night Events")]
    public bool pauseTimeInDialogue = true;
    
    // Current time state
    [Range(0f, 1f)]
    public float currentTime = 0f;
    public bool isDay = true;
    public bool isPaused = false;
    
    // Time periods (0-1 normalized)
    private const float DAWN_START = 0.2f;
    private const float DAY_START = 0.3f;
    private const float DUSK_START = 0.7f;
    private const float NIGHT_START = 0.8f;
    
    // Events
    public event Action OnSunrise;
    public event Action OnNoon;
    public event Action OnSunset;
    public event Action OnMidnight;
    public event Action<bool> OnDayNightChange;
    public event Action<float> OnTimeChanged;
    
    private bool wasDay = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        currentTime = startTime;
        
        // Auto-find sun if not assigned
        if (sunLight == null)
        {
            sunLight = FindObjectOfType<Light>();
            if (sunLight != null && sunLight.type != LightType.Directional)
            {
                sunLight = null; // Only use directional lights
            }
        }
        
        // Set initial state
        UpdateLighting();
        CheckTimeEvents();
    }
    
    void Update()
    {
        // Check if time should be paused
        bool shouldPause = pauseTimeInDialogue && 
                          DialogueManager.Instance != null && 
                          DialogueManager.Instance.IsDialogueActive;
        
        if (!isPaused && !shouldPause)
        {
            // Advance time
            currentTime += Time.deltaTime / dayDuration;
            
            // Wrap around after 24 hours
            if (currentTime >= 1f)
            {
                currentTime = 0f;
            }
            
            UpdateLighting();
            CheckTimeEvents();
            
            // Notify systems of time change
            OnTimeChanged?.Invoke(currentTime);
        }
    }
    
    void UpdateLighting()
    {
        UpdateSun();
        UpdateSkybox();
        UpdateFog();
        UpdateAmbientLighting();
    }
    
    void UpdateSun()
    {
        if (sunLight == null) return;
        
        // Calculate sun rotation (sun rises in east, sets in west)
        float sunAngle = currentTime * 360f - 90f; // -90 so sun starts at horizon
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 30f, 0f);
        
        // Update sun color and intensity
        if (sunColor.colorKeys.Length > 0)
        {
            sunLight.color = sunColor.Evaluate(currentTime);
        }
        
        if (sunIntensityCurve.keys.Length > 0)
        {
            sunLight.intensity = sunIntensityCurve.Evaluate(currentTime);
        }
        
        // Enable/disable sun based on time
        sunLight.enabled = currentTime > 0.2f && currentTime < 0.8f;
    }
    
    void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;
        
        // Update skybox tint
        if (skyTint.colorKeys.Length > 0)
        {
            skyboxMaterial.SetColor("_Tint", skyTint.Evaluate(currentTime));
        }
        
        // Update skybox exposure
        if (skyExposure.keys.Length > 0)
        {
            skyboxMaterial.SetFloat("_Exposure", skyExposure.Evaluate(currentTime));
        }
        
        // Apply changes
        DynamicGI.UpdateEnvironment();
    }
    
    void UpdateFog()
    {
        if (!enableFog) return;
        
        RenderSettings.fog = true;
        
        if (fogColor.colorKeys.Length > 0)
        {
            RenderSettings.fogColor = fogColor.Evaluate(currentTime);
        }
        
        if (fogDensity.keys.Length > 0)
        {
            RenderSettings.fogDensity = fogDensity.Evaluate(currentTime);
        }
    }
    
    void UpdateAmbientLighting()
    {
        if (ambientColor.colorKeys.Length > 0)
        {
            RenderSettings.ambientLight = ambientColor.Evaluate(currentTime);
        }
        
        if (ambientIntensity.keys.Length > 0)
        {
            RenderSettings.ambientIntensity = ambientIntensity.Evaluate(currentTime);
        }
    }
    
    void CheckTimeEvents()
    {
        // Check day/night transition
        bool currentlyDay = IsDay();
        if (currentlyDay != wasDay)
        {
            isDay = currentlyDay;
            wasDay = currentlyDay;
            OnDayNightChange?.Invoke(currentlyDay);
        }
        
        // Check specific time events (with small tolerance to prevent multiple triggers)
        float tolerance = 0.01f;
        
        if (Mathf.Abs(currentTime - DAY_START) < tolerance)
        {
            OnSunrise?.Invoke();
        }
        else if (Mathf.Abs(currentTime - 0.5f) < tolerance)
        {
            OnNoon?.Invoke();
        }
        else if (Mathf.Abs(currentTime - DUSK_START) < tolerance)
        {
            OnSunset?.Invoke();
        }
        else if (Mathf.Abs(currentTime) < tolerance)
        {
            OnMidnight?.Invoke();
        }
    }
    
    // Public utility methods
    public bool IsDay()
    {
        return currentTime >= DAY_START && currentTime < DUSK_START;
    }
    
    public bool IsNight()
    {
        return currentTime >= NIGHT_START || currentTime < DAWN_START;
    }
    
    public bool IsDawn()
    {
        return currentTime >= DAWN_START && currentTime < DAY_START;
    }
    
    public bool IsDusk()
    {
        return currentTime >= DUSK_START && currentTime < NIGHT_START;
    }
    
    public string GetTimeOfDay()
    {
        if (IsDawn()) return "Dawn";
        if (IsDay()) return "Day";
        if (IsDusk()) return "Dusk";
        return "Night";
    }
    
    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(currentTime * 24f);
        int minutes = Mathf.FloorToInt((currentTime * 24f - hours) * 60f);
        return $"{hours:D2}:{minutes:D2}";
    }
    
    public void SetTime(float newTime)
    {
        currentTime = Mathf.Clamp01(newTime);
        UpdateLighting();
        CheckTimeEvents();
    }
    
    public void PauseTime(bool pause)
    {
        isPaused = pause;
    }
    
    // Debug methods
    [ContextMenu("Set to Dawn")]
    void SetToDawn() { SetTime(DAWN_START); }
    
    [ContextMenu("Set to Day")]
    void SetToDay() { SetTime(DAY_START); }
    
    [ContextMenu("Set to Dusk")]
    void SetToDusk() { SetTime(DUSK_START); }
    
    [ContextMenu("Set to Night")]
    void SetToNight() { SetTime(NIGHT_START); }
}