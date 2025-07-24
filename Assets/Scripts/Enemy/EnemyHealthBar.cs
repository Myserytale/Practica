using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Image fillImage;
    
    [Header("Settings")]
    public float heightOffset = 2.5f;
    public bool alwaysFaceCamera = true;
    public bool hideWhenFull = true;
    
    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color halfHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;
    
    private Transform target;
    private Camera playerCamera;
    private HealthSystem enemyHealth;
    private Canvas canvas;
    
    void Start()
    {
        // Get references
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
            
        canvas = GetComponent<Canvas>();
        
        // Auto-find slider if not assigned
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();
            
        // Auto-find fill image if not assigned
        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();
    }
    
    void Update()
    {
        // Position above target
        if (target != null)
        {
            transform.position = target.position + Vector3.up * heightOffset;
        }
        
        // Always face camera
        if (alwaysFaceCamera && playerCamera != null)
        {
            transform.LookAt(transform.position + playerCamera.transform.rotation * Vector3.forward,
                           playerCamera.transform.rotation * Vector3.up);
        }
        
        // Update health display
        UpdateHealthDisplay();
    }
    
    public void Initialize(Transform enemyTransform, HealthSystem health)
    {
        target = enemyTransform;
        enemyHealth = health;
        
        // Subscribe to health events
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged += OnHealthChanged;
            enemyHealth.OnDeath += OnEnemyDeath;
        }
        
        UpdateHealthDisplay();
    }
    
    void UpdateHealthDisplay()
    {
        if (enemyHealth == null || healthSlider == null) return;
        
        float healthPercent = enemyHealth.currentHealth / enemyHealth.maxHealth;
        healthSlider.value = healthPercent;
        
        // Update color based on health
        if (fillImage != null)
        {
            if (healthPercent > 0.6f)
                fillImage.color = fullHealthColor;
            else if (healthPercent > lowHealthThreshold)
                fillImage.color = Color.Lerp(halfHealthColor, fullHealthColor, 
                    (healthPercent - lowHealthThreshold) / (0.6f - lowHealthThreshold));
            else
                fillImage.color = Color.Lerp(lowHealthColor, halfHealthColor, 
                    healthPercent / lowHealthThreshold);
        }
        
        // Hide when at full health
        if (hideWhenFull && canvas != null)
        {
            canvas.gameObject.SetActive(healthPercent < 1f);
        }
    }
    
    void OnHealthChanged(float currentHealth, float maxHealth)
    {
        UpdateHealthDisplay();
    }
    
    void OnEnemyDeath()
    {
        // Hide health bar when enemy dies
        if (canvas != null)
            canvas.gameObject.SetActive(false);
            
        // Optionally destroy after delay
        Destroy(gameObject, 2f);
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= OnHealthChanged;
            enemyHealth.OnDeath -= OnEnemyDeath;
        }
    }
}