using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider healthBar;
    public Text healthText;
    public Image healthBarFill;
    
    [Header("Colors")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public float lowHealthThreshold = 0.3f;
    
    private HealthSystem playerHealth;
    
    void Start()
    {
        // Find player health system
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthUI;
                UpdateHealthUI(playerHealth.currentHealth, playerHealth.maxHealth);
            }
        }
    }

    void Update()
{
    // Flash red when health is low
    if (playerHealth != null && healthBarFill != null)
    {
        float healthPercent = playerHealth.currentHealth / playerHealth.maxHealth;
        if (healthPercent <= lowHealthThreshold)
        {
            // Pulsing effect for low health
            float pulse = Mathf.PingPong(Time.time * 2f, 1f);
            Color currentColor = Color.Lerp(lowHealthColor, Color.white, pulse);
            healthBarFill.color = currentColor;
        }
    }
}
    
    void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)} / {maxHealth}";
        }
        
        if (healthBarFill != null)
        {
            float healthPercent = currentHealth / maxHealth;
            healthBarFill.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                Mathf.InverseLerp(0, lowHealthThreshold, healthPercent));
        }
    }
    
    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }
    }
}