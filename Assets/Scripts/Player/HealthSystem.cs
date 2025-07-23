using UnityEngine;
using UnityEngine.UI;
using System;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool canRegenerate = false;
    public float regenRate = 1f; // Health per second
    public float regenDelay = 3f; // Delay after taking damage before regen starts
    
    [Header("UI References (Player Only)")]
    public Slider healthBar;
    public Text healthText;
    public GameObject damageEffect; // Optional red screen effect
    
    [Header("Death Settings")]
    public bool respawnOnDeath = true;
    public Transform respawnPoint;
    public float respawnDelay = 3f;
    
    // Events
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action OnDeath;
    public event Action OnTakeDamage;
    
    private float lastDamageTime;
    private bool isDead = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
        
        // Find respawn point if not set
        if (respawnPoint == null && gameObject.CompareTag("Player"))
        {
            GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
            if (spawnPoint != null)
                respawnPoint = spawnPoint.transform;
        }
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Handle regeneration
        if (canRegenerate && currentHealth < maxHealth)
        {
            if (Time.time - lastDamageTime >= regenDelay)
            {
                Heal(regenRate * Time.deltaTime);
            }
        }
    }
    
    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        lastDamageTime = Time.time;
        
        // Trigger events
        OnTakeDamage?.Invoke();
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Visual feedback
        if (damageEffect != null)
        {
            StartCoroutine(ShowDamageEffect());
        }
        
        UpdateHealthUI();
        
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        UpdateHealthUI();
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        
        Debug.Log($"{gameObject.name} died!");
        
        if (gameObject.CompareTag("Player"))
        {
            HandlePlayerDeath();
        }
        else
        {
            HandleEnemyDeath();
        }
    }
    
    void HandlePlayerDeath()
    {
        // Disable player controls
        var playerMovement = GetComponent<NewPlayerMovement>();
        if (playerMovement) playerMovement.enabled = false;
        
        if (respawnOnDeath)
        {
            Invoke(nameof(RespawnPlayer), respawnDelay);
        }
    }
    
    void HandleEnemyDeath()
    {
        // Drop loot, play death animation, etc.
        var enemy = GetComponent<EnemyAI>();
        if (enemy) enemy.OnDeath();
        
        // Destroy after a delay
        Destroy(gameObject, 2f);
    }
    
    void RespawnPlayer()
    {
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
        
        // Reset health
        currentHealth = maxHealth;
        isDead = false;
        UpdateHealthUI();
        
        // Re-enable player controls
        var playerMovement = GetComponent<NewPlayerMovement>();
        if (playerMovement) playerMovement.enabled = true;
        
        Debug.Log("Player respawned!");
    }
    
    void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(currentHealth)} / {maxHealth}";
        }
    }
    
    System.Collections.IEnumerator ShowDamageEffect()
    {
        if (damageEffect != null)
        {
            damageEffect.SetActive(true);
            yield return new WaitForSeconds(0.2f);
            damageEffect.SetActive(false);
        }
    }
    
    // Public getters
    public bool IsDead => isDead;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsFullHealth => currentHealth >= maxHealth;
}